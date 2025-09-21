using UnityEngine;

namespace MergeDungeon.Core
{
    [System.Serializable]
    public struct EnemySpawnProfile
    {
        public bool useExactCell;
        public Vector2Int exactCell; // x,y from top-left
        public Vector2Int columnRange;
        public int rowOffsetFromTop;

        public static EnemySpawnProfile Default => new EnemySpawnProfile
        {
            useExactCell = false,
            exactCell = Vector2Int.zero,
            columnRange = new Vector2Int(-1, -1),
            rowOffsetFromTop = -1
        };

        public bool HasColumnRange => columnRange.x >= 0 && columnRange.y >= columnRange.x;
        public bool HasRowPreference => rowOffsetFromTop >= 0;

        public Vector2Int ClampColumnRange(int boardWidth)
        {
            if (boardWidth <= 0)
            {
                return Vector2Int.zero;
            }

            if (!HasColumnRange)
            {
                int maxCol = Mathf.Max(0, boardWidth - 1);
                return new Vector2Int(0, maxCol);
            }

            int maxColumn = Mathf.Max(0, boardWidth - 1);
            int min = Mathf.Clamp(columnRange.x, 0, maxColumn);
            int max = Mathf.Clamp(columnRange.y, min, maxColumn);
            return new Vector2Int(min, max);
        }

        public bool TryGetRowIndex(int boardHeight, out int resolvedRow)
        {
            resolvedRow = -1;
            if (!HasRowPreference || boardHeight <= 0)
            {
                return false;
            }

            if (rowOffsetFromTop >= boardHeight)
            {
                return false;
            }

            int topIndex = boardHeight - 1;
            resolvedRow = Mathf.Clamp(topIndex - Mathf.Max(0, rowOffsetFromTop), 0, topIndex);
            return true;
        }

        public bool TryGetExactBoardCoordinates(int boardWidth, int boardHeight, out Vector2Int coordinates)
        {
            coordinates = Vector2Int.zero;
            if (!useExactCell || boardWidth <= 0 || boardHeight <= 0)
            {
                return false;
            }

            if (exactCell.x < 0 || exactCell.y < 0)
            {
                return false;
            }

            if (exactCell.x >= boardWidth || exactCell.y >= boardHeight)
            {
                return false;
            }

            int resolvedY = Mathf.Clamp(boardHeight - 1 - exactCell.y, 0, boardHeight - 1);
            coordinates = new Vector2Int(exactCell.x, resolvedY);
            return true;
        }

        public bool IsExactCellValidForBoard(int boardWidth, int boardHeight)
        {
            if (!useExactCell)
            {
                return true;
            }

            return boardWidth > 0 && boardHeight > 0 && exactCell.x >= 0 && exactCell.y >= 0 && exactCell.x < boardWidth && exactCell.y < boardHeight;
        }
    }

    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Visuals")]
        public Color backgroundColor = Color.white;
        public Sprite portrait;
        public AnimatorOverrideController overrideController;

        [Header("Stats")]
        [Min(1)] public int baseHp = 1;
        [Min(0)] public int baseDamage = 1;
        [Tooltip("Optional per-floor HP scaling. Curve time = floor index, value = multiplier.")]
        public AnimationCurve hpScaling = AnimationCurve.Linear(0f, 1f, 10f, 1f);

        [Header("Behaviours")]
        public EnemyTurnBehaviour turnBehaviour;

        [Header("Board Link")]
        public TileDefinition enemyTile;

        [Header("Spawning")]
        public EnemySpawnProfile spawnProfile = EnemySpawnProfile.Default;

        [Header("Loot")]
        [SerializeField] private EnemyLootEmitterModule lootModule;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public EnemyLootEmitterModule LootModule => lootModule;
        public LootContainerDefinition LootContainer => lootModule != null ? lootModule.lootContainer : null;
        public LootTable DirectLootTable => lootModule != null ? lootModule.directLootTable : null;
        public EnemySpawnProfile SpawnProfile => spawnProfile;
        public EnemyTurnBehaviour TurnBehaviour => turnBehaviour;

        public int GetScaledHp(int floor)
        {
            if (hpScaling == null) return Mathf.Max(1, baseHp);
            var multiplier = Mathf.Max(0f, hpScaling.Evaluate(Mathf.Max(0f, floor)));
            return Mathf.Max(1, Mathf.RoundToInt(baseHp * multiplier));
        }
    }
}


