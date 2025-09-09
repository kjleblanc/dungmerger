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
        // Legacy field removed; tiles are identified by TileDefinition.
        public TileDefinition def;
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
            if (def != null)
            {
                if (label != null)
                {
                    if (def.category == TileCategory.LootBag)
                        label.text = $"{def.DisplayName} x{Mathf.Max(0, lootRemaining)}";
                    else
                        label.text = def.DisplayName;
                }
                if (iconBg != null)
                {
                    if (def.icon != null)
                    {
                        iconBg.sprite = def.icon;
                        iconBg.color = def.iconTint;
                        iconBg.preserveAspect = true;
                    }
                    else if (def.background != null)
                    {
                        iconBg.sprite = def.background;
                        iconBg.color = def.backgroundTint;
                        iconBg.preserveAspect = true;
                    }
                }
                return;
            }
            if (label != null) label.text = string.Empty;
            if (iconBg != null) { iconBg.sprite = null; iconBg.color = Color.white; }
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

        public void SetDefinition(TileDefinition d)
        {
            def = d;
            RefreshVisual();
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
