using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class DungeonManager : ServicesConsumerBehaviour
    {
        [Header("Config")]
        public int roomsPerFloor = 5;
        public float nextRoomDelay = 1.0f;
        public bool autoStart = true;

        [Header("Refs")]
        public GridManager grid;
        public RoomMapUI mapUI;
        public RoomChangedEventChannelSO roomChanged;
        [Tooltip("Optional: Per-floor pools defining waves and rooms-per-floor.")]
        public List<FloorRoomPool> floorPools = new List<FloorRoomPool>();

        [Header("State")]
        public int currentFloor = 1;
        public int currentRoom = 1;

        [Header("Fallback Enemies")]
        [Tooltip("Used when no wave data is available.")]
        public EnemyDefinition fallbackPrimaryEnemy;
        [Min(1)] public int fallbackPrimaryBaseHp = 1;
        public EnemyDefinition fallbackSecondaryEnemy;
        [Min(1)] public int fallbackSecondaryBaseHp = 2;
        public EnemyDefinition fallbackBossEnemy;
        [Min(1)] public int fallbackBossBaseHp = 8;

        private EnemyWave _currentWave;
        private class PendingSpawn
        {
            public EnemyDefinition definition;
            public int remaining;
            public int hp;
            public bool isBoss;
        }
        private readonly List<PendingSpawn> _pending = new List<PendingSpawn>();

        private void Start()
        {
            if (grid == null) grid = services != null ? services.Grid : FindFirstObjectByType<GridManager>();
            if (grid == null)
            {
                Debug.LogError("DungeonManager: GridManager not found in scene");
                return;
            }

            grid.spawnEnemiesContinuously = false;
            grid.testEnemiesOnStart = 0;

            grid.EnemyDied += OnEnemyDied;
            grid.EnemyAdvanced += OnEnemyAdvanced;

            if (autoStart)
            {
                StartCoroutine(DeferredStartRun());
            }
            UpdateMap();
            TryLoadState();
        }

        private IEnumerator DeferredStartRun()
        {
            yield return new WaitForEndOfFrame();
            while (grid != null && !grid.IsBoardReady)
            {
                yield return null;
            }
            StartRun();
        }

        private void OnDestroy()
        {
            if (grid != null)
            {
                grid.EnemyDied -= OnEnemyDied;
                grid.EnemyAdvanced -= OnEnemyAdvanced;
            }
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) TrySaveState();
        }

        private void OnApplicationQuit()
        {
            TrySaveState();
        }

        public void StartRun()
        {
            currentFloor = 1;
            currentRoom = 1;
            UpdateMap();
            SpawnCurrentRoom();
        }

        private void UpdateMap()
        {
            int rpf = RoomsPerFloorForFloor(currentFloor);
            mapUI?.Set(currentFloor, currentRoom, rpf);
            roomChanged?.Raise(currentFloor, currentRoom, rpf);
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            if (grid == null) return;
            if (grid.ActiveEnemyCount <= 0 && PendingTotal() <= 0)
            {
                StartCoroutine(AdvanceRoomAfterDelay());
            }
        }

        private IEnumerator AdvanceRoomAfterDelay()
        {
            yield return new WaitForSeconds(nextRoomDelay);
            currentRoom++;
            var roomsOnFloor = RoomsPerFloorForFloor(currentFloor);
            if (currentRoom > roomsOnFloor)
            {
                currentFloor++;
                currentRoom = 1;
            }
            UpdateMap();
            SpawnCurrentRoom();
            TrySaveState();
        }

        private void SpawnCurrentRoom()
        {
            if (grid == null) return;
            int roomsThisFloor = RoomsPerFloorForFloor(currentFloor);
            bool isBossRoom = currentRoom >= roomsThisFloor;

            _pending.Clear();
            _currentWave = null;
            var pool = GetPoolForFloor(currentFloor);
            if (pool != null)
            {
                var wave = isBossRoom ? pool.RollBossWave() : pool.RollNormalWave();
                if (wave != null && wave.spawns != null && wave.spawns.Count > 0)
                {
                    _currentWave = wave;
                    foreach (var s in wave.spawns)
                    {
                        if (s?.enemyDefinition == null) continue;
                        int count = Mathf.Clamp(Random.Range(s.countMin, s.countMax + 1), 0, 999);
                        if (count <= 0) continue;
                        int hp = ResolveHp(s.enemyDefinition, currentFloor, s.hpOverride, s.hpBonusPerFloor);
                        _pending.Add(new PendingSpawn { definition = s.enemyDefinition, remaining = count, hp = hp, isBoss = s.bossFlagForAll });
                    }
                }
            }

            if (_pending.Count == 0)
            {
                BuildFallbackPending(isBossRoom);
            }

            int initial = Mathf.Max(1, _currentWave != null ? Mathf.Max(1, _currentWave.initialSpawn) : 1);
            SpawnPending(initial);
            UpdateMap();
        }

        private void SpawnHeuristic(bool isBossRoom)
        {
            if (grid == null) return;
            if (isBossRoom)
            {
                var bossDef = fallbackBossEnemy ?? fallbackPrimaryEnemy ?? fallbackSecondaryEnemy;
                if (bossDef == null) return;
                int bossHp = ResolveHp(bossDef, currentFloor, 0, 0);
                bossHp = Mathf.Max(1, bossHp + currentFloor * 4);
                SpawnEnemyUsingDefinition(bossDef, bossHp, true);
                return;
            }

            int total = Mathf.Clamp(2 + currentFloor + Mathf.FloorToInt((currentRoom - 1) / 2f), 2, grid.Width * 2);
            for (int i = 0; i < total; i++)
            {
                var def = RollFallbackEnemyDefinition();
                if (def == null) break;
                int baseHp = ResolveHp(def, currentFloor, 0, 0);
                baseHp = Mathf.Max(1, baseHp + Mathf.Max(0, currentFloor / 2));
                var spawned = SpawnEnemyUsingDefinition(def, baseHp, false);
                if (spawned == null) break;
            }
        }

        private EnemyDefinition RollFallbackEnemyDefinition()
        {
            if (fallbackPrimaryEnemy != null && fallbackSecondaryEnemy != null)
            {
                return Random.value < 0.7f ? fallbackPrimaryEnemy : fallbackSecondaryEnemy;
            }
            return fallbackPrimaryEnemy ?? fallbackSecondaryEnemy ?? fallbackBossEnemy;
        }

        private void BuildFallbackPending(bool isBossRoom)
        {
            _pending.Clear();
            if (isBossRoom)
            {
                var bossDef = fallbackBossEnemy ?? fallbackPrimaryEnemy ?? fallbackSecondaryEnemy;
                if (bossDef != null)
                {
                    int bossHp = ResolveHp(bossDef, currentFloor, 0, 0);
                    bossHp = Mathf.Max(1, bossHp + currentFloor * 4);
                    _pending.Add(new PendingSpawn { definition = bossDef, remaining = 1, hp = bossHp, isBoss = true });
                }
                return;
            }

            int total = Mathf.Clamp(3 + currentFloor, 1, grid.Width * 2);
            for (int i = 0; i < total; i++)
            {
                var def = RollFallbackEnemyDefinition();
                if (def == null) break;
                int baseHp = ResolveHp(def, currentFloor, 0, 0);
                baseHp = Mathf.Max(1, baseHp + Mathf.Max(0, currentFloor / 2));
                _pending.Add(new PendingSpawn { definition = def, remaining = 1, hp = baseHp, isBoss = false });
            }
        }

        private int PendingTotal()
        {
            int sum = 0;
            foreach (var p in _pending) sum += Mathf.Max(0, p.remaining);
            return sum;
        }

        private void SpawnPending(int count)
        {
            if (count <= 0) return;
            int attempts = 0;
            int spawned = 0;
            int index = 0;
            while (spawned < count && PendingTotal() > 0 && attempts < 1000)
            {
                attempts++;
                if (_pending.Count == 0) break;
                if (index >= _pending.Count) index = 0;
                var p = _pending[index];
                index++;
                if (p.remaining <= 0) continue;
                var e = SpawnEnemyUsingDefinition(p.definition, p.hp, p.isBoss);
                if (e != null)
                {
                    p.remaining -= 1;
                    spawned++;
                }
                else break;
            }
        }

        private void OnEnemyAdvanced()
        {
            int perAdvance = Mathf.Max(1, _currentWave != null ? Mathf.Max(1, _currentWave.perAdvanceSpawn) : 1);
            SpawnPending(perAdvance);
            TrySaveState();
        }

        private int ResolveHp(EnemyDefinition definition, int floor, int hpOverride, int hpBonusPerFloor)
        {
            if (hpOverride > 0) return hpOverride;

            int baseHp = definition != null ? definition.GetScaledHp(floor) : 1;

            if (definition == fallbackPrimaryEnemy)
                baseHp = Mathf.Max(baseHp, fallbackPrimaryBaseHp);
            else if (definition == fallbackSecondaryEnemy)
                baseHp = Mathf.Max(baseHp, fallbackSecondaryBaseHp);
            else if (definition == fallbackBossEnemy)
                baseHp = Mathf.Max(baseHp, fallbackBossBaseHp);

            return Mathf.Max(1, baseHp + Mathf.Max(0, hpBonusPerFloor) * Mathf.Max(0, floor));
        }

        private EnemyController SpawnEnemyUsingDefinition(EnemyDefinition definition, int hp, bool isBoss)
        {
            if (definition == null || grid == null) return null;
            return grid.TrySpawnEnemyTopRowsOfDefinition(definition, hp, isBoss);
        }

        private FloorRoomPool GetPoolForFloor(int floor)
        {
            if (floorPools == null || floorPools.Count == 0) return null;
            int idx = Mathf.Clamp(floor - 1, 0, floorPools.Count - 1);
            return floorPools[idx];
        }

        private int RoomsPerFloorForFloor(int floor)
        {
            var pool = GetPoolForFloor(floor);
            return pool != null ? Mathf.Max(1, pool.roomsPerFloor) : Mathf.Max(1, roomsPerFloor);
        }

        private async void TryLoadState()
        {
            var host = SaveServiceHost.Instance;
            if (host == null) return;
            await System.Threading.Tasks.Task.Yield();
            while (grid != null && !grid.IsBoardReady)
            {
                await System.Threading.Tasks.Task.Yield();
            }
            var (ok, state) = await host.LoadAsync();
            if (!ok || state == null) return;
            currentFloor = Mathf.Max(1, state.floor);
            currentRoom = Mathf.Max(1, state.room);
            UpdateMap();
            SpawnCurrentRoom();
            if (grid != null)
            {
                var meter = grid.GetComponent<AdvanceMeterController>();
                if (meter != null)
                {
                    meter.enemyAdvanceMeter = Mathf.Max(0, state.meter);
                    meter.RefreshUI();
                }
            }
        }

        private async void TrySaveState()
        {
            var host = SaveServiceHost.Instance;
            if (host == null) return;
            int meterVal = 0;
            var meter = grid != null ? grid.GetComponent<AdvanceMeterController>() : null;
            if (meter != null) meterVal = meter.enemyAdvanceMeter;
            var state = new GameState { floor = currentFloor, room = currentRoom, meter = meterVal };
            await host.SaveAsync(state);
        }
    }
}
