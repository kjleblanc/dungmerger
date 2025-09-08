using TMPro;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class DamagePopup : MonoBehaviour
    {
        public TMP_Text label;
        public float lifetime = 0.8f;
        public Vector2 drift = new Vector2(0f, 40f);
        public AnimationCurve alphaOverLife = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public AnimationCurve scaleOverLife = AnimationCurve.EaseInOut(0, 0.8f, 1, 1f);

        private RectTransform _rt;
        private float _t;
        private Vector2 _startAnchored;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (label == null) label = GetComponentInChildren<TMP_Text>();
            _startAnchored = _rt.anchoredPosition;
        }

        public void Set(int amount, Color color)
        {
            if (label != null)
            {
                label.text = amount.ToString();
                label.color = color;
            }
            if (_rt == null) _rt = GetComponent<RectTransform>();
            _startAnchored = _rt.anchoredPosition;
            _t = 0f;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / Mathf.Max(0.001f, lifetime));
            if (_rt != null)
            {
                _rt.anchoredPosition = _startAnchored + drift * u;
                float s = scaleOverLife.Evaluate(u);
                _rt.localScale = new Vector3(s, s, 1f);
            }
            if (label != null)
            {
                var c = label.color;
                c.a = alphaOverLife.Evaluate(u);
                label.color = c;
            }
            if (u >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
