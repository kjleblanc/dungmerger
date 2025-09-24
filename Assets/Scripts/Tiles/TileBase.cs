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
    public class TileBase : ServicesConsumerBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, ISelectable
    {
        // Legacy field removed; tiles are identified by TileDefinition.
        public TileDefinition def;
        [HideInInspector] public BoardCell currentCell;
        [Header("Loot Bag State")]
        public int lootRemaining = 0; // used only for LootBag kinds

        [Header("Visuals")]
        public RectTransform iconArtRoot;
        public Image backgroundImage;
        public Image iconBg;
        public TMP_Text label;

        private CanvasGroup _cg;
        private Transform _originalParent;
        private bool _dragging;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            EnsureIconArtRoot();
            ApplyIconScale(def);
        }

        private void OnValidate()
        {
            EnsureIconArtRoot();
            ApplyIconScale(def);
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
            EnsureIconArtRoot();
            ApplyIconScale(def);

            if (def != null)
            {
                if (label != null)
                {
                    if (def.category == TileCategory.LootBag)
                        label.text = $"{def.DisplayName} x{Mathf.Max(0, lootRemaining)}";
                    else
                        label.text = def.DisplayName;
                }
                if (backgroundImage != null)
                {
                    if (def.background != null)
                    {
                        backgroundImage.sprite = def.background;
                        backgroundImage.color = def.backgroundTint;
                        backgroundImage.enabled = true;
                    }
                    else
                    {
                        backgroundImage.sprite = null;
                        backgroundImage.color = Color.white;
                        backgroundImage.enabled = false;
                    }
                }

                if (iconBg != null)
                {
                    if (def.icon != null)
                    {
                        iconBg.sprite = def.icon;
                        iconBg.color = def.iconTint;
                        iconBg.preserveAspect = true;
                        iconBg.enabled = true;
                    }
                    else
                    {
                        iconBg.sprite = null;
                        iconBg.color = Color.white;
                        iconBg.enabled = false;
                    }
                }
                return;
            }
            if (label != null) label.text = string.Empty;
            if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
                backgroundImage.color = Color.white;
                backgroundImage.enabled = false;
            }
            if (iconBg != null)
            {
                iconBg.sprite = null;
                iconBg.color = Color.white;
                iconBg.enabled = false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var grid = services != null ? services.Grid : null;
            if (grid != null && grid.ArePlayerActionsLocked) return;

            _dragging = true;
            // Deselect on drag start
            var mgr = UISelectionManager.Instance;
            if (mgr != null) mgr.ClearSelection();
            _originalParent = transform.parent;
            var dragLayer = services != null && services.DragLayer != null ? services.DragLayer.dragLayer : null;
            if (dragLayer != null) transform.SetParent(dragLayer, true);
            transform.SetAsLastSibling();
            _cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Convert screen position to local anchored position in the drag layer's RectTransform
            var layer = services != null && services.DragLayer != null ? services.DragLayer.dragLayer : null;
            var layerRT = layer as RectTransform;
            if (layerRT != null)
            {
                Canvas canvas = layerRT.GetComponentInParent<Canvas>();
                if (canvas == null) canvas = layerRT.GetComponentInParent<Canvas>();
                var cam = canvas != null ? canvas.worldCamera : null;
                Vector2 local;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRT, eventData.position, cam, out local))
                {
                    var rt = transform as RectTransform;
                    if (rt != null) rt.anchoredPosition = local; else transform.position = eventData.position;
                }
                else
                {
                    transform.position = eventData.position;
                }
            }
            else
            {
                transform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            _cg.blocksRaycasts = true;

            var grid = services != null ? services.Grid : null;
            if (grid != null && grid.ArePlayerActionsLocked)
            {
                ReturnToCell();
                return;
            }

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
                if (services != null && services.Grid != null && services.Grid.TryFeedHero(this, hero))
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
                if (services != null && services.Grid != null && services.Grid.TryUseAbilityOnEnemy(this, enemy))
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
                if (services != null && services.Tiles != null && services.Tiles.TryMergeOnDrop(this, otherTile))
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
            if (cell != null && services != null && services.Tiles != null && services.Tiles.TryPlaceTileInCell(this, cell))
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

        public void SetDefinition(TileDefinition d)
        {
            EnsureIconArtRoot();
            def = d;
            if (def != null && def.modules != null)
            {
                for (int i = 0; i < def.modules.Count; i++)
                {
                    var module = def.modules[i];
                    module?.Configure(this);
                }
            }
            RefreshVisual();
        }

        private void EnsureIconArtRoot()
        {
            if (iconArtRoot == null)
            {
                if (iconBg != null)
                {
                    iconArtRoot = iconBg.rectTransform;
                }
                else if (backgroundImage != null)
                {
                    iconArtRoot = backgroundImage.rectTransform;
                }
            }
        }

        private void ApplyIconScale(TileDefinition definition)
        {
            if (iconArtRoot == null) return;

            if (definition != null)
            {
                var scale = definition.iconScale;
                iconArtRoot.localScale = new Vector3(scale.x, scale.y, 1f);
            }
            else
            {
                iconArtRoot.localScale = Vector3.one;
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
