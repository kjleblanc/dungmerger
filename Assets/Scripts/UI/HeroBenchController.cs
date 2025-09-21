using System.Collections.Generic;
using UnityEngine;

namespace MergeDungeon.Core
{
    /// <summary>
    /// Manages the hero bench UI slots at the bottom of the board.
    /// Provides spawn anchors and keeps track of which hero occupies each slot.
    /// </summary>
    [AddComponentMenu("MergeDungeon/UI/Hero Bench Controller")]
    public class HeroBenchController : MonoBehaviour
    {
        [System.Serializable]
        public class Slot
        {
            [Tooltip("Optional identifier to make slots easier to recognize in the inspector.")]
            public string id;
            [Tooltip("Container where the hero prefab will be parented.")]
            public RectTransform root;
            [Tooltip("Optional anchor used when spawning hero abilities. Defaults to the root if null.")]
            public RectTransform spawnAnchor;
            [Tooltip("Highlight component used when the hero slot is selected.")]
            public SelectableHighlight highlight;
            [Tooltip("Which board column this slot prefers when finding a spawn cell.")]
            public int preferredBoardColumn;

            [HideInInspector] public HeroController occupant;

            public RectTransform GetSpawnAnchor()
            {
                return spawnAnchor != null ? spawnAnchor : root;
            }
        }

        [Header("Layout")]
        [SerializeField] private RectTransform benchRoot;
        [SerializeField] private RectTransform heroSpawnOrigin;
        [SerializeField] private List<Slot> slots = new List<Slot>();

        public RectTransform BenchRoot => benchRoot != null ? benchRoot : (RectTransform)transform;
        public RectTransform HeroSpawnOrigin => heroSpawnOrigin != null ? heroSpawnOrigin : BenchRoot;
        public IReadOnlyList<Slot> Slots => slots;

        public bool TryPlaceHero(HeroController hero, out int slotIndex)
        {
            slotIndex = -1;
            if (hero == null || slots == null) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.root == null) continue;
                if (slot.occupant != null && slot.occupant != hero) continue;

                slot.occupant = hero;
                slots[i] = slot;
                AttachHeroToSlot(hero, slot);
                slotIndex = i;
                return true;
            }

            return false;
        }

        public bool TryGetSlotIndex(HeroController hero, out int slotIndex)
        {
            slotIndex = -1;
            if (hero == null || slots == null) return false;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i]?.occupant == hero)
                {
                    slotIndex = i;
                    return true;
                }
            }
            return false;
        }

        public RectTransform GetSpawnAnchorForHero(HeroController hero)
        {
            if (hero != null && TryGetSlotIndex(hero, out var index))
            {
                var slot = slots[index];
                return slot?.GetSpawnAnchor() ?? HeroSpawnOrigin;
            }
            return HeroSpawnOrigin;
        }

        public int GetPreferredColumnForHero(HeroController hero)
        {
            if (hero != null && TryGetSlotIndex(hero, out var index))
            {
                var slot = slots[index];
                if (slot != null)
                {
                    return Mathf.Max(0, slot.preferredBoardColumn);
                }
            }
            return 0;
        }

        public void ReleaseHero(HeroController hero)
        {
            if (hero == null || slots == null) return;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.occupant == hero)
                {
                    slot.occupant = null;
                    slots[i] = slot;
                    break;
                }
            }
        }

        public void SnapHeroToSlot(HeroController hero)
        {
            if (hero == null || slots == null) return;
            if (!TryGetSlotIndex(hero, out var index)) return;

            var slot = slots[index];
            if (slot == null || slot.root == null) return;

            AttachHeroToSlot(hero, slot);
        }

        private void AttachHeroToSlot(HeroController hero, Slot slot)
        {
            if (hero == null || slot == null || slot.root == null) return;
            var heroRT = hero.GetComponent<RectTransform>();
            if (heroRT == null) return;

            heroRT.SetParent(slot.root, worldPositionStays: false);
            heroRT.anchorMin = new Vector2(0.5f, 0.5f);
            heroRT.anchorMax = new Vector2(0.5f, 0.5f);
            heroRT.anchoredPosition = Vector2.zero;
            heroRT.localScale = Vector3.one;
        }

        private void OnValidate()
        {
            if (benchRoot == null)
            {
                benchRoot = GetComponent<RectTransform>();
            }

            if (slots == null) return;
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                if (slot.root == null && slot.highlight != null)
                {
                    slot.root = slot.highlight.GetComponent<RectTransform>();
                }
                if (slot.highlight == null && slot.root != null)
                {
                    slot.highlight = slot.root.GetComponent<SelectableHighlight>();
                }
            }
        }
    }
}
