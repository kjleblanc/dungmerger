using System.Collections.Generic;
using MergeDungeon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class TileBase : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, ISelectable
    {
        public TileKind kind;
        [HideInInspector] public BoardCell currentCell;
        [Header("Loot Bag State")]
        public int lootRemaining = 0; // used only for LootBag kinds

        [Header("Visuals")]
        public Image iconBg;
        public TMP_Text label;

        [Header("Selection")]
        public Image selectionImage; // Optional overlay image to show selection
        public Color selectionColor = new Color(1f, 0.95f, 0.3f, 0.9f);
        public bool scaleOnSelect = true;
        public float selectionScale = 1.06f;

        private CanvasGroup _cg;
        private Transform _originalParent;
        private bool _dragging;
        private Outline _outline;
        private Image _outlineTarget;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (selectionImage != null)
            {
                selectionImage.enabled = false;
                selectionImage.color = selectionColor;
            }
            // Fallback: add outline to the best available Image if no explicit selection image
            _outlineTarget = iconBg != null ? iconBg : GetComponent<Image>();
            if (_outlineTarget == null)
            {
                // Try find any child Image (last resort)
                _outlineTarget = GetComponentInChildren<Image>();
            }
            if (selectionImage == null && _outlineTarget != null)
            {
                _outline = _outlineTarget.GetComponent<Outline>();
                if (_outline == null) _outline = _outlineTarget.gameObject.AddComponent<Outline>();
                _outline.enabled = false;
                _outline.effectColor = selectionColor;
                _outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        private void Start()
        {
            RefreshVisual();
        }

        private void OnDestroy()
        {
            if (UISelectionManager.Instance != null && UISelectionManager.Instance.CurrentSelectedGO == gameObject)
            {
                UISelectionManager.Instance.ClearSelection();
            }
        }

        public void RefreshVisual()
        {
            var gm = GridManager.Instance;
            var visuals = gm != null ? gm.tileVisuals : null;

            if (label != null)
            {
                string baseName = visuals != null ? (visuals.Get(kind)?.displayName ?? string.Empty) : string.Empty;
                if (string.IsNullOrEmpty(baseName)) baseName = kind.ToString();
                // Loot bag label overrides to show remaining count
                switch (kind)
                {
                    case TileKind.LootBagSlime:
                        label.text = $"Slime Bag x{Mathf.Max(0, lootRemaining)}";
                        break;
                    case TileKind.LootBagBat:
                        label.text = $"Bat Pouch x{Mathf.Max(0, lootRemaining)}";
                        break;
                    default:
                        label.text = baseName;
                        break;
                }
            }

            if (iconBg != null)
            {
                var entry = visuals != null ? visuals.Get(kind) : null;
                if (entry != null && entry.sprite != null)
                {
                    iconBg.sprite = entry.sprite;
                    iconBg.color = Color.white;
                    iconBg.preserveAspect = true;
                }
                else
                {
                    iconBg.sprite = null;
                    iconBg.color = entry != null ? entry.fallbackColor : ColorForKind(kind);
                }
            }
        }

        private string GetDisplayLabel()
        {
            switch (kind)
            {
                case TileKind.LootBagSlime: return $"Slime Bag x{Mathf.Max(0, lootRemaining)}";
                case TileKind.LootBagBat: return $"Bat Pouch x{Mathf.Max(0, lootRemaining)}";
                default: return kind.ToString();
            }
        }

        private Color ColorForKind(TileKind k)
        {
            switch (k)
            {
                case TileKind.SwordStrike: return new Color(0.85f, 0.85f, 1f);
                case TileKind.Cleave: return new Color(0.7f, 0.7f, 1f);
                case TileKind.Spark: return new Color(1f, 0.9f, 0.7f);
                case TileKind.Fireball: return new Color(1f, 0.7f, 0.4f);
                case TileKind.Goo: return new Color(0.7f, 1f, 0.7f);
                case TileKind.GooJelly: return new Color(0.5f, 1f, 0.5f);
                case TileKind.Mushroom: return new Color(0.9f, 0.7f, 1f);
                case TileKind.MushroomStew: return new Color(0.75f, 0.5f, 1f);
                case TileKind.BatWing: return new Color(0.75f, 0.75f, 0.3f);
                case TileKind.Bone: return new Color(0.85f, 0.85f, 0.85f);
                case TileKind.LootBagSlime: return new Color(0.8f, 0.8f, 0.6f);
                case TileKind.LootBagBat: return new Color(0.7f, 0.7f, 0.55f);
                default: return Color.white;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            // Deselect on drag start
            var mgr = UISelectionManager.Instance;
            if (mgr != null) mgr.ClearSelection();
            _originalParent = transform.parent;
            transform.SetParent(GridManager.Instance.dragLayer, true);
            transform.SetAsLastSibling();
            _cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            _cg.blocksRaycasts = true;

            var go = eventData.pointerCurrentRaycast.gameObject;
            if (go == null)
            {
                // Revert to original cell
                ReturnToCell();
                return;
            }

            // Drop over hero to feed
            var hero = go.GetComponentInParent<HeroController>();
            if (hero != null)
            {
                if (GridManager.Instance.TryFeedHero(this, hero))
                {
                    // Consumed by hero
                    if (currentCell != null) currentCell.ClearTileIf(this);
                    Destroy(gameObject);
                    return;
                }
                ReturnToCell();
                return;
            }

            // Drop over enemy to attack
            var enemy = go.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                if (GridManager.Instance.TryUseAbilityOnEnemy(this, enemy))
                {
                    if (currentCell != null) currentCell.ClearTileIf(this);
                    Destroy(gameObject);
                    return;
                }
                ReturnToCell();
                return;
            }

            // Drop onto a crafting station to deposit
            var station = go.GetComponentInParent<CraftingStationTile>();
            if (station != null && station != this)
            {
                bool accepted = station.Deposit(this);
                if (accepted)
                {
                    if (currentCell != null) currentCell.ClearTileIf(this);
                    Destroy(gameObject);
                    return;
                }
                ReturnToCell();
                return;
            }

            // Drop onto another tile to attempt a merge (3/5 rule)
            var otherTile = go.GetComponentInParent<TileBase>();
            if (otherTile != null && otherTile != this)
            {
                if (GridManager.Instance.TryMergeOnDrop(this, otherTile))
                {
                    // Merge consumed this tile; nothing more to do
                    return;
                }
                // Failed to merge, revert
                ReturnToCell();
                return;
            }

            // Drop into a free cell
            var cell = go.GetComponentInParent<BoardCell>();
            if (cell != null && GridManager.Instance.TryPlaceTileInCell(this, cell))
            {
                var selMgr = UISelectionManager.Instance;
                if (selMgr != null) selMgr.HandleClick(gameObject);
                return;
            }

            // Otherwise, revert
            ReturnToCell();
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (_dragging) return;
            var mgr = UISelectionManager.Instance;
            if (mgr != null) mgr.HandleClick(gameObject);
        }

        public virtual void OnSelectTap() { }
        public virtual void OnActivateTap() { }

        public void SetSelected(bool selected)
        {
            if (selectionImage != null)
            {
                selectionImage.enabled = selected;
                selectionImage.color = selectionColor;
            }
            if (_outline != null)
            {
                _outline.enabled = selected;
                _outline.effectColor = selectionColor;
            }
            if (scaleOnSelect)
            {
                float s = selected ? selectionScale : 1f;
                transform.localScale = new Vector3(s, s, 1f);
            }
        }

        

        private void ReturnToCell()
        {
            if (currentCell != null)
            {
                var rt = GetComponent<RectTransform>();
                rt.SetParent(currentCell.rectTransform, worldPositionStays: false);
                rt.anchoredPosition = Vector2.zero;
            }
            else
            {
                transform.SetParent(_originalParent, true);
            }
        }
    }
}
