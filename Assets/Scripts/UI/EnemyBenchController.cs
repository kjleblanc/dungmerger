using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    /// <summary>
    /// Manages the enemy bench UI along the top of the board. Responsible for
    /// assigning spawn slots, keeping track of occupants, and providing slot
    /// metadata that other systems (coordinator, behaviours) can query.
    /// </summary>
    [AddComponentMenu("MergeDungeon/UI/Enemy Bench Controller")]
    public class EnemyBenchController : MonoBehaviour
    {
        [System.Serializable]
        public class Slot
        {
            [Tooltip("Optional identifier to make slots easier to recognize in the inspector.")]
            public string id;
            [Tooltip("Root transform that will parent the spawned enemy.")]
            public RectTransform root;
            [Tooltip("Optional override transform for parenting. Falls back to the root when null.")]
            public RectTransform spawnAnchor;
            [Tooltip("Anchor used by behaviours/VFX when executing a turn. Falls back to spawn anchor when null.")]
            public RectTransform turnAnchor;
            [Tooltip("Preferred board column used for targeting (loot drops, hero focus, etc.).")]
            public int preferredBoardColumn = -1;
            [Tooltip("Row offset from the top of the board that this slot targets for board interactions (0 = top row).")]
            public int targetRowOffsetFromTop = 0;
            [Tooltip("If true, this slot can be skipped when registering enemies (e.g., reserved for bosses).")]
            public bool lockUntilExplicit;

            [HideInInspector] public EnemyController occupant;

            public RectTransform GetSpawnAnchor()
            {
                return spawnAnchor != null ? spawnAnchor : root;
            }

            public RectTransform GetTurnAnchor()
            {
                var anchor = GetSpawnAnchor();
                return turnAnchor != null ? turnAnchor : anchor;
            }
        }

        public struct SlotMetadata
        {
            public readonly int index;
            public readonly string id;
            public readonly RectTransform root;
            public readonly RectTransform spawnAnchor;
            public readonly RectTransform turnAnchor;
            public readonly int preferredBoardColumn;
            public readonly EnemyController occupant;
            public readonly int targetRowOffsetFromTop;

            public SlotMetadata(int index, string id, RectTransform root, RectTransform spawnAnchor, RectTransform turnAnchor, int preferredBoardColumn, int targetRowOffsetFromTop, EnemyController occupant)
            {
                this.index = index;
                this.id = id;
                this.root = root;
                this.spawnAnchor = spawnAnchor;
                this.turnAnchor = turnAnchor;
                this.preferredBoardColumn = preferredBoardColumn;
                this.targetRowOffsetFromTop = targetRowOffsetFromTop;
                this.occupant = occupant;
            }
        }

        [Header("Layout")]
        [SerializeField] private RectTransform benchRoot;
        [SerializeField] private RectTransform enemySpawnOrigin;
        [SerializeField] private List<Slot> slots = new List<Slot>();

        private readonly Dictionary<EnemyController, int> _slotLookup = new Dictionary<EnemyController, int>();

        public RectTransform BenchRoot => benchRoot != null ? benchRoot : (RectTransform)transform;
        public RectTransform EnemySpawnOrigin => enemySpawnOrigin != null ? enemySpawnOrigin : BenchRoot;
        public IReadOnlyList<Slot> Slots => slots;

        public bool TryReserveSlot(EnemyController enemy, out int reservedIndex)
        {
            reservedIndex = -1;
            if (enemy == null || slots == null || slots.Count == 0)
            {
                return false;
            }

            if (_slotLookup.TryGetValue(enemy, out reservedIndex))
            {
                return true;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null) continue;
                if (slot.lockUntilExplicit) continue;
                if (slot.occupant == null)
                {
                    slot.occupant = enemy;
                    slots[i] = slot;
                    reservedIndex = i;
                    _slotLookup[enemy] = i;
                    AttachEnemyToSlot(enemy, slot);
                    return true;
                }
            }

            return false;
        }

        public bool TryAssignEnemyToSlot(EnemyController enemy, int slotIndex)
        {
            if (enemy == null || slots == null) return false;
            if (slotIndex < 0 || slotIndex >= slots.Count) return false;
            var slot = slots[slotIndex];
            if (slot == null || slot.root == null) return false;
            if (slot.occupant != null && slot.occupant != enemy) return false;

            slot.occupant = enemy;
            slots[slotIndex] = slot;
            _slotLookup[enemy] = slotIndex;
            AttachEnemyToSlot(enemy, slot);
            return true;
        }

        public void ReleaseEnemy(EnemyController enemy)
        {
            if (enemy == null || slots == null) return;
            if (_slotLookup.TryGetValue(enemy, out var slotIndex))
            {
                if (slotIndex >= 0 && slotIndex < slots.Count)
                {
                    var slot = slots[slotIndex];
                    if (slot != null && slot.occupant == enemy)
                    {
                        slot.occupant = null;
                        slots[slotIndex] = slot;
                    }
                }
                _slotLookup.Remove(enemy);
            }
        }

        public bool TryGetSlotIndex(EnemyController enemy, out int slotIndex)
        {
            if (enemy == null)
            {
                slotIndex = -1;
                return false;
            }
            return _slotLookup.TryGetValue(enemy, out slotIndex);
        }

        public bool TryGetSlotMetadata(EnemyController enemy, out SlotMetadata metadata)
        {
            metadata = default;
            if (!TryGetSlotIndex(enemy, out var index))
            {
                return false;
            }
            return TryGetSlotMetadata(index, out metadata);
        }

        public bool TryGetSlotMetadata(int slotIndex, out SlotMetadata metadata)
        {
            metadata = default;
            if (slots == null || slotIndex < 0 || slotIndex >= slots.Count)
            {
                return false;
            }

            var slot = slots[slotIndex];
            if (slot == null)
            {
                return false;
            }

            metadata = new SlotMetadata(
                slotIndex,
                slot.id,
                slot.root != null ? slot.root : EnemySpawnOrigin,
                slot.GetSpawnAnchor() != null ? slot.GetSpawnAnchor() : EnemySpawnOrigin,
                slot.GetTurnAnchor() != null ? slot.GetTurnAnchor() : EnemySpawnOrigin,
                slot.preferredBoardColumn,
                slot.targetRowOffsetFromTop,
                slot.occupant);
            return true;
        }

        public IReadOnlyList<SlotMetadata> GetSlotMetadataSnapshot(List<SlotMetadata> buffer = null)
        {
            if (slots == null || slots.Count == 0)
            {
                return buffer != null ? (IReadOnlyList<SlotMetadata>)buffer : System.Array.Empty<SlotMetadata>();
            }

            if (buffer == null)
            {
                buffer = new List<SlotMetadata>(slots.Count);
            }
            else
            {
                buffer.Clear();
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (TryGetSlotMetadata(i, out var meta))
                {
                    buffer.Add(meta);
                }
            }

            return buffer;
        }

        private void AttachEnemyToSlot(EnemyController enemy, Slot slot)
        {
            if (enemy == null || slot == null) return;
            var rt = enemy.GetComponent<RectTransform>();
            if (rt == null) return;

            var parent = slot.GetSpawnAnchor();
            if (parent == null)
            {
                parent = EnemySpawnOrigin;
            }

            rt.SetParent(parent, worldPositionStays: false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private void OnValidate()
        {
            if (benchRoot == null)
            {
                benchRoot = GetComponent<RectTransform>();
            }

            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null) continue;
                if (slot.root == null && slot.spawnAnchor != null)
                {
                    slot.root = slot.spawnAnchor;
                }
                if (slot.spawnAnchor == null && slot.root != null)
                {
                    slot.spawnAnchor = slot.root;
                }
                if (slot.turnAnchor == null && slot.spawnAnchor != null)
                {
                    slot.turnAnchor = slot.spawnAnchor;
                }
            }
        }
    }
}
