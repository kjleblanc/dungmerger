using UnityEngine;

namespace MergeDungeon.Core
{
    [CreateAssetMenu(menuName = "MergeDungeon/Enemies/Enemy Definition", fileName = "Enemy_")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;

        [Header("Visuals")]
        public Color backgroundColor = Color.white;
        public Sprite portrait;
        public AnimatorOverrideController overrideController;

        [Header("Stats")]
        [Min(1)] public int baseHp = 1;
        [Min(0)] public int baseDamage = 1;
        [Tooltip("Optional per-floor HP scaling. Curve time = floor index, value = multiplier.")]
        public AnimationCurve hpScaling = AnimationCurve.Linear(0f, 1f, 10f, 1f);

        [Header("Behaviours")]
        public EnemyMovementBehaviour movement;
        public EnemyAttackBehaviour attack;

        [Header("Board Link")]
        public TileDefinition enemyTile;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        public int GetScaledHp(int floor)
        {
            if (hpScaling == null) return Mathf.Max(1, baseHp);
            var multiplier = Mathf.Max(0f, hpScaling.Evaluate(Mathf.Max(0f, floor)));
            return Mathf.Max(1, Mathf.RoundToInt(baseHp * multiplier));
        }
    }
}
