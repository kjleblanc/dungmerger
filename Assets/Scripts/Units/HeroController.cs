using MergeDungeon.Core;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class HeroController : ServicesConsumerBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, ISelectable
    {
        [Header("Definition")]
        [SerializeField] private HeroDefinition definition;
        private HeroDefinition _appliedDefinition;

        [Header("Progression")]
        public int level = 1;
        public int exp = 0;
        public int expToLevel = 2;

        [Header("Stamina")]
        public int stamina = 3;
        public int maxStamina = 5;
        [Header("Health")]
        public int maxHp = 3;
        public int hp = 3;
        public bool isDowned = false;

        public HeroDefinition Definition => definition;

        [HideInInspector] public BoardCell currentCell;

        [Header("Visuals")]
        public Image bg;
        public TMP_Text label;
        [Tooltip("Optional portrait image. If not set, bg will be used for sprites.")]
        public Image portrait;
        public HeroVisual heroVisual;
        public Image staminaFill; // fill image 0..1
        public Image healthFill; // optional health fill 0..1
        public float spawnCooldown = 0.2f;
        private AbilitySpawnTable ActiveSpawnTable => definition != null ? definition.spawnTable : null;
        private CanvasGroup _cg;
        private Transform _originalParent;
        private bool _dragging;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (heroVisual == null) heroVisual = GetComponentInChildren<HeroVisual>();
            ApplyDefinition(true);
            RefreshVisual();
            RefreshUI();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                ApplyDefinition(true);
                RefreshVisual();
                RefreshUI();
            }
        }

        protected override void OnServicesReady()
        {
            base.OnServicesReady();
            TryApplyVisualLibrary();
        }

        private void Start()
        {
            RefreshVisual();
            RefreshUI();
            if (heroVisual == null) heroVisual = GetComponentInChildren<HeroVisual>();
            TryApplyVisualLibrary();
        }

        public void SetDefinition(HeroDefinition newDefinition, bool resetStats = true)
        {
            definition = newDefinition;
            ApplyDefinition(resetStats);
            RefreshVisual();
            RefreshUI();
            TryApplyVisualLibrary();
        }

        private void ApplyDefinition(bool resetStats)
        {
            if (definition == null)
            {
                _appliedDefinition = null;
                return;
            }

            bool shouldReset = resetStats || _appliedDefinition != definition;

            if (shouldReset)
            {
                level = Mathf.Max(1, definition.startingLevel);
                exp = Mathf.Max(0, definition.startingExp);
                expToLevel = Mathf.Max(1, definition.expToLevel);
                maxStamina = Mathf.Max(1, definition.maxStamina);
                stamina = Mathf.Clamp(definition.startingStamina, 0, maxStamina);
                maxHp = Mathf.Max(1, definition.maxHp);
                hp = Mathf.Clamp(definition.startingHp, 0, maxHp);
                isDowned = false;
                _appliedDefinition = definition;
            }

            if (portrait != null && definition.portrait != null)
            {
                portrait.sprite = definition.portrait;
                portrait.enabled = true;
            }

            if (bg != null && definition != null)
            {
                bg.color = definition.backgroundColor;
            }
        }

        private string GetHeroName()
        {
            if (definition != null && !string.IsNullOrEmpty(definition.DisplayName))
            {
                return definition.DisplayName;
            }
            return gameObject != null ? gameObject.name : "Hero";
        }

        private void TryApplyVisualLibrary()
        {
            if (heroVisual == null) heroVisual = GetComponentInChildren<HeroVisual>();
            if (heroVisual == null) return;

            if (services != null && services.HeroVisualLibrary != null)
            {
                var entry = services.HeroVisualLibrary.Get(definition);
                if (entry != null)
                {
                    if (entry.overrideController != null)
                    {
                        heroVisual.overrideController = entry.overrideController;
                    }
                    else if (entry.defaultSprite != null)
                    {
                        heroVisual.SetStaticSprite(entry.defaultSprite);
                    }
                }
            }

            if (heroVisual.overrideController != null)
            {
                heroVisual.ApplyOverride();
            }
            heroVisual.PlayIdle();
        }

        public void RefreshVisual()
        {
            var heroName = GetHeroName();
            if (label != null)
            {
                label.text = $"{heroName} L{level}";
            }
            if (bg != null && definition != null)
            {
                bg.color = definition.backgroundColor;
            }
            if (portrait != null && definition != null && definition.portrait != null)
            {
                portrait.sprite = definition.portrait;
                portrait.enabled = true;
            }
        }

        private void RefreshUI()
        {
            if (staminaFill != null)
            {
                staminaFill.fillAmount = Mathf.Clamp01((float)stamina / Mathf.Max(1, maxStamina));
            }
            if (healthFill != null)
            {
                healthFill.fillAmount = Mathf.Clamp01((float)hp / Mathf.Max(1, maxHp));
            }
            if (label != null)
            {
                var status = isDowned ? " DOWNED" : string.Empty;
                var heroName = GetHeroName();
                label.text = $"{heroName} L{level} ({stamina}/{maxStamina}) HP:{hp}/{maxHp}{status}";
            }
        }
        public void GainStamina(int amount)
        {
            stamina = Mathf.Min(maxStamina, stamina + amount);
            RefreshUI();
        }

        public void GainExp(int amount)
        {
            exp += amount;
            if (exp >= expToLevel)
            {
                exp -= expToLevel;
                level++;
                // Slightly increase special spawn chance by level indirectly handled in GetSpawnKind
                RefreshVisual();
            }
            RefreshUI();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_dragging) return;
            var mgr = UISelectionManager.Instance;
            if (mgr != null) mgr.HandleClick(gameObject);
        }

        private float _lastSpawnTime;

        private void TrySpawnAbility()
        {
            if (isDowned) return;
            if (stamina <= 0) return;
            if (Time.time - _lastSpawnTime < spawnCooldown) return;

            var defToSpawn = GetSpawnDefinition();
            if (defToSpawn != null && services != null && services.TileFactory != null)
            {
                var board = services.Board;
                var targetCell = board != null ? board.FindNearestEmptyCell(currentCell) : null;
                if (targetCell != null)
                {
                    var tile = services.TileFactory.Create(defToSpawn);
                    if (tile != null)
                    {
                        // Temporarily parent under drag layer and position at hero center
                        var tileRT = tile.GetComponent<RectTransform>();
                        var heroRT = GetComponent<RectTransform>();
                        var layer = services.DragLayer != null ? services.DragLayer.dragLayer : transform.parent;
                        if (tileRT != null && heroRT != null)
                        {
                            tileRT.SetParent(layer, worldPositionStays: true);
                            tileRT.position = heroRT.position;
                            tileRT.localScale = Vector3.zero;
                            StartCoroutine(AnimateTileSpawnToCell(tile, targetCell, 0.2f));
                            _lastSpawnTime = Time.time;
                        }
                        else
                        {
                            // Fallback: place immediately if RectTransforms missing
                            targetCell.SetTile(tile);
                            _lastSpawnTime = Time.time;
                        }
                    }
                }
            }
            // Play visual use animation if available
            if (heroVisual != null)
            {
                heroVisual.PlayUse();
            }
            // Increment enemy advance meter
            if (services != null && services.Grid != null)
            {
                services.Grid.RegisterHeroUse();
            }
            stamina -= 1;
            RefreshUI();
        }

        private IEnumerator AnimateTileSpawnToCell(TileBase tile, BoardCell targetCell, float duration)
        {
            if (tile == null || targetCell == null) yield break;

            var tileRT = tile.GetComponent<RectTransform>();
            var targetRT = targetCell.rectTransform;
            if (tileRT == null || targetRT == null)
            {
                targetCell.SetTile(tile);
                yield break;
            }

            Vector3 startPos = tileRT.position;
            Vector3 endPos = targetRT.position;
            Vector3 startScale = Vector3.zero;
            Vector3 endScale = Vector3.one;
            float t = 0f;
            duration = Mathf.Max(0.0001f, duration);

            while (t < 1f && tile != null)
            {
                t += Time.deltaTime / duration;
                float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                tileRT.position = Vector3.LerpUnclamped(startPos, endPos, e);
                tileRT.localScale = Vector3.LerpUnclamped(startScale, endScale, e);
                yield return null;
            }

            if (tile != null)
            {
                // Finalize placement
                targetCell.SetTile(tile);
            }
        }

        public void OnSelectTap() {}
        public void OnActivateTap()
        {
            if (isDowned) return;
            TrySpawnAbility();
        }

        private TileDefinition GetSpawnDefinition()
        {
            var table = ActiveSpawnTable;
            if (table != null)
            {
                if (services != null && services.TileDatabase != null)
                {
                    return table.RollForLevel(services.TileDatabase, level);
                }
                return table.RollForLevel(level);
            }
            // Fallback if no table assigned
            return null;
        }
        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragging = true;
            // Clear current selection before reparenting so the old cell highlight can be found and cleared
            var selMgr = UISelectionManager.Instance;
            if (selMgr != null) selMgr.ClearSelection();
            _originalParent = transform.parent;
            var dragLayer = services != null && services.DragLayer != null ? services.DragLayer.dragLayer : null;
            if (dragLayer != null)
            {
                transform.SetParent(dragLayer, true);
                transform.SetAsLastSibling();
            }
            if (_cg != null) _cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Convert screen to local anchored position within the drag layer to support Screen Space - Camera canvases
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
            if (_cg != null) _cg.blocksRaycasts = true;

            var go = eventData.pointerCurrentRaycast.gameObject;
            var cell = go != null ? go.GetComponentInParent<BoardCell>() : null;
            if (cell != null && services != null && services.Grid != null && services.Grid.TryPlaceHeroInCell(this, cell))
            {
                var selMgr = UISelectionManager.Instance;
                if (selMgr != null) selMgr.HandleClick(gameObject);
                _dragging = false;
                return;
            }

            // revert
            _dragging = false;
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

        private void OnDestroy()
        {
            if (UISelectionManager.Instance != null && UISelectionManager.Instance.CurrentSelectedGO == gameObject)
            {
                UISelectionManager.Instance.ClearSelection();
            }
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            hp = Mathf.Max(0, hp - amount);
            if (hp <= 0)
            {
                isDowned = true;
            }
            RefreshUI();
        }
    }
}
