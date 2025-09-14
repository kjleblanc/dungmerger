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
    public class HeroController : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, ISelectable
    {
        public HeroKind kind = HeroKind.Warrior;
        public int level = 1;
        public int exp = 0;
        public int expToLevel = 2;

        public int stamina = 3;
        public int maxStamina = 5;
        [Header("Health")]
        public int maxHp = 3;
        public int hp = 3;
        public bool isDowned = false;

        [HideInInspector] public BoardCell currentCell;

        [Header("Visuals")]
        public Image bg;
        public TMP_Text label;
        [Tooltip("Optional portrait image. If not set, bg will be used for sprites.")]
        public Image portrait;
        public HeroVisual heroVisual;
        public Image staminaFill; // fill image 0..1
        public Image healthFill; // optional health fill 0..1
        [Header("Spawning")]
        public AbilitySpawnTable spawnTable;
        public float spawnCooldown = 0.2f;
        private CanvasGroup _cg;
        private Transform _originalParent;
        private bool _dragging;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (heroVisual == null) heroVisual = GetComponentInChildren<HeroVisual>();
        }

        private void Start()
        {
            RefreshVisual();
            RefreshUI();
            if (heroVisual == null) heroVisual = GetComponentInChildren<HeroVisual>();
            if (heroVisual != null)
            {
                // If no override assigned yet, try to pull from GridManager's library
                if (heroVisual.overrideController == null && GridManager.Instance != null && GridManager.Instance.heroVisualLibrary != null)
                {
                    var def = GridManager.Instance.heroVisualLibrary.Get(kind);
                    if (def != null && def.overrideController != null)
                    {
                        heroVisual.overrideController = def.overrideController;
                        heroVisual.ApplyOverride();
                    }
                }
                heroVisual.PlayIdle();
            }
        }

        public void RefreshVisual()
        {
            if (label != null)
            {
                label.text = $"{kind} L{level}";
            }
            if (bg != null)
            {
                bg.color = kind == HeroKind.Warrior ? new Color(0.8f, 0.6f, 0.6f) : new Color(0.6f, 0.8f, 0.9f);
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
                label.text = $"{kind} L{level} ({stamina}/{maxStamina}) HP:{hp}/{maxHp}{status}";
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
            if (defToSpawn != null && GridManager.Instance != null && GridManager.Instance.tileFactory != null)
            {
                var targetCell = GridManager.Instance.FindNearestEmptyCell(currentCell);
                if (targetCell != null)
                {
                    var tile = GridManager.Instance.tileFactory.Create(defToSpawn);
                    if (tile != null)
                    {
                        // Temporarily parent under drag layer and position at hero center
                        var tileRT = tile.GetComponent<RectTransform>();
                        var heroRT = GetComponent<RectTransform>();
                        var layer = GridManager.Instance.dragLayer != null ? GridManager.Instance.dragLayer : transform.parent;
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
            if (GridManager.Instance != null)
            {
                GridManager.Instance.RegisterHeroUse();
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
            if (spawnTable != null)
            {
                return spawnTable.RollForLevel(GridManager.Instance != null ? GridManager.Instance.tileDatabase : null, level);
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
            if (GridManager.Instance != null && GridManager.Instance.dragLayer != null)
            {
                transform.SetParent(GridManager.Instance.dragLayer, true);
                transform.SetAsLastSibling();
            }
            if (_cg != null) _cg.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Convert screen to local anchored position within the drag layer to support Screen Space - Camera canvases
            var layer = GridManager.Instance != null ? GridManager.Instance.dragLayer : null;
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
            if (cell != null && GridManager.Instance.TryPlaceHeroInCell(this, cell))
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
