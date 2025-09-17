using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    [RequireComponent(typeof(RectTransform))]
    public class EnemyController : ServicesConsumerBehaviour, IPointerClickHandler, ISelectable
    {
        [Header("Definition")]
        [SerializeField] private EnemyDefinition definition;

        public EnemyDefinition Definition => definition;
        public TileDefinition EnemyTile => definition != null ? definition.enemyTile : null;

        public int maxHp = 1;
        public int hp = 1;
        public bool isBoss = false;
        [Tooltip("If true and sharing a cell with a hero, this unit will attack instead of moving on enemy advance.")]
        public bool engagedWithHero = false;

        [HideInInspector] public BoardCell currentCell;

        [Header("Visuals")]
        public Image bg;
        public TMP_Text label;
        public Image healthFill;
        public EnemyVisual enemyVisual;

        private EnemyMovementBehaviour _movementBehaviour;
        private EnemyAttackBehaviour _attackBehaviour;
        private EnemyDefinition _cachedDefinition;
        private bool _behavioursInitialized;

        private void Awake()
        {
            if (enemyVisual == null) enemyVisual = GetComponentInChildren<EnemyVisual>();
        }

        protected override void OnServicesReady()
        {
            base.OnServicesReady();
            ApplyDefinitionData(resetStats: false);
            ApplyVisualOverrides();
            RefreshVisual();
        }

        private void Start()
        {
            ApplyDefinitionData(resetStats: false);
            ApplyVisualOverrides();
            RefreshVisual();
        }

        public void SetupSpawn(EnemyDefinition newDefinition, int baseHp, bool bossFlag)
        {
            definition = newDefinition;
            isBoss = bossFlag;
            ApplyDefinitionData(resetStats: false);
            ApplyVisualOverrides();
            InitializeStats(baseHp);
        }

        private void ApplyDefinitionData(bool resetStats)
        {
            if (definition == null)
            {
                _movementBehaviour = null;
                _attackBehaviour = null;
                _cachedDefinition = null;
                _behavioursInitialized = false;
                if (resetStats)
                {
                    InitializeStats(maxHp);
                }
                return;
            }

            if (_cachedDefinition != definition)
            {
                _cachedDefinition = definition;
                _behavioursInitialized = false;
            }

            _movementBehaviour = definition.movement;
            _attackBehaviour = definition.attack;
            if (!_behavioursInitialized)
            {
                _movementBehaviour?.Initialize(this);
                _attackBehaviour?.Initialize(this);
                _behavioursInitialized = true;
            }

            if (resetStats)
            {
                InitializeStats(definition.baseHp);
            }
        }

        private void ApplyVisualOverrides()
        {
            if (enemyVisual == null) enemyVisual = GetComponentInChildren<EnemyVisual>();
            if (enemyVisual == null) return;

            bool appliedOverride = false;
            if (definition != null && definition.overrideController != null)
            {
                enemyVisual.overrideController = definition.overrideController;
                enemyVisual.ApplyOverride();
                enemyVisual.PlayIdle();
                appliedOverride = true;
            }

            if (!appliedOverride && services != null && services.EnemyVisualLibrary != null && EnemyTile != null)
            {
                var entry = services.EnemyVisualLibrary.Get(EnemyTile);
                if (entry != null)
                {
                    if (entry.overrideController != null)
                    {
                        enemyVisual.overrideController = entry.overrideController;
                        enemyVisual.ApplyOverride();
                        enemyVisual.PlayIdle();
                    }
                    else if (entry.defaultSprite != null)
                    {
                        enemyVisual.SetStaticSprite(entry.defaultSprite);
                    }
                }
            }
        }

        public void RefreshVisual()
        {
            var displayName = definition != null ? definition.DisplayName : "Enemy";
            if (label != null)
            {
                label.text = isBoss ? $"BOSS {displayName} {hp}/{maxHp}" : $"{displayName} {hp}/{maxHp}";
            }

            if (bg != null)
            {
                var baseColor = definition != null ? definition.backgroundColor : Color.white;
                bg.color = isBoss ? new Color(0.9f, 0.6f, 0.3f) : baseColor;
            }

            if (healthFill != null)
            {
                healthFill.type = Image.Type.Filled;
                healthFill.fillMethod = Image.FillMethod.Horizontal;
                healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                healthFill.fillAmount = Mathf.Clamp01(maxHp > 0 ? (float)hp / maxHp : 0f);
            }
        }

        public void InitializeStats(int baseHp)
        {
            int resolvedHp = baseHp > 0 ? baseHp : (definition != null ? definition.baseHp : maxHp);
            maxHp = Mathf.Max(1, resolvedHp);
            hp = Mathf.Clamp(hp, 0, maxHp);
            if (hp <= 0) hp = maxHp;
            RefreshVisual();
        }

        public void ApplyHit(int damage)
        {
            var grid = services != null ? services.Grid : null;
            if (grid != null)
            {
                grid.SpawnDamagePopup(GetComponent<RectTransform>(), damage, grid.damageNumberColor);
            }
            if (enemyVisual != null) enemyVisual.PlayHit();
            hp -= Mathf.Max(0, damage);
            if (hp <= 0)
            {
                DieWithLoot();
            }
            else
            {
                RefreshVisual();
            }
        }

        public void TryAttackHero(HeroController hero)
        {
            if (hero == null) return;

            if (_attackBehaviour != null)
            {
                if (_attackBehaviour.CanAttack(this, hero))
                {
                    _attackBehaviour.PerformAttack(this, hero);
                }
                return;
            }

            var damage = Mathf.Max(1, definition != null ? definition.baseDamage : 1);
            AttackHero(hero, damage);
        }

        public void AttackHero(HeroController hero, int damage)
        {
            if (hero == null) return;
            engagedWithHero = true;
            hero.TakeDamage(Mathf.Max(0, damage));
        }

        public void ApplyCleave()
        {
            DieWithLoot();
        }

        public void DieWithLoot()
        {
            if (enemyVisual != null) enemyVisual.PlayDeath();
            if (currentCell != null)
            {
                currentCell.ClearEnemyIf(this);
            }
            services?.Enemies?.OnEnemyDied(this);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (UISelectionManager.Instance != null && UISelectionManager.Instance.CurrentSelectedGO == gameObject)
            {
                UISelectionManager.Instance.ClearSelection();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            UISelectionManager.Instance?.HandleClick(gameObject);
        }

        public void OnSelectTap() { }
        public void OnActivateTap() { }

        public bool TryExecuteMovementBehaviour()
        {
            if (_movementBehaviour == null) return false;
            _movementBehaviour.Tick(this);
            return true;
        }
    }
}
