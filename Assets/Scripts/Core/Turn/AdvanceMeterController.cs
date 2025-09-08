using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    public class AdvanceMeterController : MonoBehaviour
    {
        [Header("Meter")]
        [Tooltip("How many hero uses before enemies advance by one tile.")]
        public int enemyAdvanceThreshold = 5;
        [Tooltip("Current meter value towards the next enemy advance.")]
        public int enemyAdvanceMeter = 0;

        [Header("UI (Optional)")]
        public Image enemyAdvanceFill;
        public TMP_Text enemyAdvanceLabel;

        [Header("Pulse (Optional)")]
        public bool enemyAdvancePulse = true;
        [Range(0f, 1f)] public float enemyAdvancePulseThreshold = 0.8f;
        public float enemyAdvancePulseSpeed = 6f;
        public float enemyAdvancePulseAmplitude = 0.05f;
        public RectTransform enemyAdvancePulseTarget; // defaults to fill rect

        public void InitializeFrom(GridManager grid)
        {
            // GridManager no longer owns meter fields; keep existing controller values.
            if (enemyAdvancePulseTarget == null && enemyAdvanceFill != null)
                enemyAdvancePulseTarget = enemyAdvanceFill.rectTransform;
            RefreshUI();
        }

        public void Increment()
        {
            enemyAdvanceMeter = Mathf.Clamp(enemyAdvanceMeter + 1, 0, Mathf.Max(1, enemyAdvanceThreshold));
        }

        public bool IsFull() => enemyAdvanceMeter >= Mathf.Max(1, enemyAdvanceThreshold);
        public void ResetMeter() => enemyAdvanceMeter = 0;

        public void RefreshUI()
        {
            if (enemyAdvanceFill != null && enemyAdvanceThreshold > 0)
            {
                enemyAdvanceFill.type = Image.Type.Filled;
                // Respect inspector orientation (set Vertical/Bottom in scene)
                enemyAdvanceFill.fillAmount = Mathf.Clamp01((float)enemyAdvanceMeter / Mathf.Max(1, enemyAdvanceThreshold));
            }
            if (enemyAdvanceLabel != null)
            {
                enemyAdvanceLabel.text = $"ENEMY MOVE: {enemyAdvanceMeter}/{Mathf.Max(1, enemyAdvanceThreshold)}";
            }
        }

        private void LateUpdate()
        {
            if (!enemyAdvancePulse || enemyAdvancePulseTarget == null || enemyAdvanceThreshold <= 0) return;
            float fill = Mathf.Clamp01((float)enemyAdvanceMeter / Mathf.Max(1, enemyAdvanceThreshold));
            if (fill >= enemyAdvancePulseThreshold)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * enemyAdvancePulseSpeed) * enemyAdvancePulseAmplitude;
                enemyAdvancePulseTarget.localScale = new Vector3(1f, s, 1f);
            }
            else
            {
                enemyAdvancePulseTarget.localScale = Vector3.one;
            }
        }
    }
}
