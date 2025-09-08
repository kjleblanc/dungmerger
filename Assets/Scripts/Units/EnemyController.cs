using MergeDungeon.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    [RequireComponent(typeof(RectTransform))]
    public class EnemyController : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler, ISelectable
    {
        public EnemyKind kind = EnemyKind.Slime;
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

        private void Start()
        {
            RefreshVisual();
            if (enemyVisual == null) enemyVisual = GetComponentInChildren<EnemyVisual>();
            
            if (enemyVisual != null && GridManager.Instance != null && GridManager.Instance.enemyVisualLibrary != null)
            {
                var def = GridManager.Instance.enemyVisualLibrary.Get(kind);
                if (def != null)
                {
                    if (def.overrideController != null)
                    {
                        // Use animations
                        enemyVisual.overrideController = def.overrideController;
                        enemyVisual.ApplyOverride();
                        enemyVisual.PlayIdle();
                    }
                    else if (def.defaultSprite != null)
                    {
                        // Use static sprite
                        enemyVisual.SetStaticSprite(def.defaultSprite);
                    }
                }
            }
        }




        public void RefreshVisual()
        {
            if (label != null)
            {
                label.text = isBoss ? $"BOSS {kind} {hp}/{maxHp}" : $"{kind} {hp}/{maxHp}";
            }
            if (bg != null)
            {
                if (isBoss)
                    bg.color = new Color(0.9f, 0.6f, 0.3f);
                else
                    bg.color = kind == EnemyKind.Slime ? new Color(0.5f, 0.9f, 0.5f) : new Color(0.6f, 0.6f, 0.9f);
            }
            if (healthFill != null)
            {
                healthFill.type = Image.Type.Filled;
                healthFill.fillMethod = Image.FillMethod.Horizontal;
                healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                healthFill.fillAmount = Mathf.Clamp01(maxHp > 0 ? (float)hp / maxHp : 0f);
            }
        }

        public void ApplyHit(int damage, TileKind source)
        {
            // Show damage number
            var gm = GridManager.Instance;
            if (gm != null)
            {
                gm.SpawnDamagePopup(this.GetComponent<RectTransform>(), damage, gm.damageNumberColor);
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

        public void AttackHero(HeroController hero, int damage)
        {
            if (hero == null) return;
            engagedWithHero = true;
            hero.TakeDamage(damage);
        }

        public void ApplyCleave()
        {
            // Cleave kills Bat instantly; Slime also dies here for simplicity
            DieWithLoot();
        }

        public void InitializeStats(int baseHp)
        {
            maxHp = Mathf.Max(1, baseHp);
            hp = maxHp;
            RefreshVisual();
        }

        public void DieWithLoot()
        {
            if (enemyVisual != null) enemyVisual.PlayDeath();
            if (currentCell != null)
            {
                currentCell.ClearEnemyIf(this);
            }
            GridManager.Instance.OnEnemyDied(this);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (UISelectionManager.Instance != null && UISelectionManager.Instance.CurrentSelectedGO == gameObject)
            {
                UISelectionManager.Instance.ClearSelection();
            }
        }

        public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
        {
            var mgr = UISelectionManager.Instance;
            if (mgr != null) mgr.HandleClick(gameObject);
        }

        public void OnSelectTap() {}
        public void OnActivateTap() { }
    }
}
