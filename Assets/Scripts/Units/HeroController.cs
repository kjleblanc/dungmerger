using MergeDungeon.Core;
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

        private void TrySpawnAbility()
        {
            if (isDowned) return;
            if (stamina <= 0) return;

            var defToSpawn = GetSpawnDefinition();
            if (defToSpawn != null && GridManager.Instance != null && GridManager.Instance.tileFactory != null)
            {
                var empty = GridManager.Instance.CollectEmptyCells();
                if (empty.Count > 0)
                {
                    var cell = empty[Random.value < 1f ? Random.Range(0, empty.Count) : 0];
                    var t = GridManager.Instance.tileFactory.Create(defToSpawn);
                    if (t != null)
                    {
                        cell.SetTile(t);
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
            transform.position = eventData.position;
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
