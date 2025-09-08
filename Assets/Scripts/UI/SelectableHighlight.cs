using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    // Reusable selection highlight for any UI object (tile, hero, enemy).
    public class SelectableHighlight : MonoBehaviour
    {
        [Header("Visuals")]
        public Image overlay; // optional ring overlay
        public Color selectedColor = new Color(1f, 0.95f, 0.3f, 0.9f);
        public bool scaleOnSelect = true;
        public float selectionScale = 1.06f;

        [Header("Fade")]
        public bool fadeEnabled = true;
        [Min(0f)] public float fadeInDuration = 0.12f;
        [Min(0f)] public float fadeOutDuration = 0.12f;
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Outline _outline;
        private Image _targetImage;
        private Coroutine _fadeCo;

        private void Awake()
        {
            if (overlay != null)
            {
                overlay.enabled = false;
                overlay.color = selectedColor;
            }
            // Choose a good target for Outline if overlay is not provided
            _targetImage = GetComponent<Image>();
            if (_targetImage == null)
            {
                _targetImage = GetComponentInChildren<Image>();
            }
            if (overlay == null && _targetImage != null)
            {
                _outline = _targetImage.GetComponent<Outline>();
                if (_outline == null) _outline = _targetImage.gameObject.AddComponent<Outline>();
                _outline.enabled = false;
                _outline.effectColor = selectedColor;
                _outline.effectDistance = new Vector2(2f, -2f);
            }
        }

        public void SetSelected(bool selected)
        {
            if (fadeEnabled)
            {
                if (_fadeCo != null) StopCoroutine(_fadeCo);
                _fadeCo = StartCoroutine(FadeTo(selected));
            }
            else
            {
                if (overlay != null)
                {
                    var c = selectedColor; c.a = selected ? selectedColor.a : 0f;
                    overlay.color = c;
                    overlay.enabled = selected;
                }
                if (_outline != null)
                {
                    var c = selectedColor; c.a = selected ? selectedColor.a : 0f;
                    _outline.effectColor = c;
                    _outline.enabled = selected;
                }
            }
            if (scaleOnSelect)
            {
                float s = selected ? selectionScale : 1f;
                transform.localScale = new Vector3(s, s, 1f);
            }
        }

        public void ApplyStyle(Color color, bool scale, float scaleValue, Vector2 outlineDistance)
        {
            selectedColor = color;
            scaleOnSelect = scale;
            selectionScale = scaleValue;
            if (overlay != null)
            {
                overlay.color = color;
            }
            if (_outline != null)
            {
                _outline.effectColor = color;
                _outline.effectDistance = outlineDistance;
            }
        }

        public void ApplyFadeStyle(bool enabled, float inDuration, float outDuration, AnimationCurve curve)
        {
            fadeEnabled = enabled;
            fadeInDuration = Mathf.Max(0f, inDuration);
            fadeOutDuration = Mathf.Max(0f, outDuration);
            if (curve != null) fadeCurve = curve;
        }

        private System.Collections.IEnumerator FadeTo(bool selected)
        {
            float target = selected ? (selectedColor.a) : 0f;
            float current = 0f;
            // Determine current alpha from overlay first, then outline
            if (overlay != null)
            {
                current = overlay.color.a;
                if (selected && !overlay.enabled) overlay.enabled = true;
            }
            else if (_outline != null)
            {
                current = _outline.effectColor.a;
                if (selected && !_outline.enabled) _outline.enabled = true;
            }

            float duration = selected ? fadeInDuration : fadeOutDuration;
            if (duration <= 0f)
            {
                SetVisualAlphaImmediate(target, selected);
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime; // UI feels better unscaled
                float u = Mathf.Clamp01(t / duration);
                float k = fadeCurve != null ? fadeCurve.Evaluate(u) : u;
                float a = Mathf.Lerp(current, target, k);
                SetVisualAlpha(a, keepEnabled:true);
                yield return null;
            }

            SetVisualAlphaImmediate(target, selected);
        }

        private void SetVisualAlphaImmediate(float alpha, bool selected)
        {
            SetVisualAlpha(alpha, keepEnabled:true);
            // After fade out completes, disable components to stop raycasts/costs
            if (!selected)
            {
                if (overlay != null) overlay.enabled = false;
                if (_outline != null) _outline.enabled = false;
            }
        }

        private void SetVisualAlpha(float alpha, bool keepEnabled)
        {
            if (overlay != null)
            {
                var c = selectedColor; c.a = alpha;
                overlay.color = c;
                if (!overlay.enabled && keepEnabled) overlay.enabled = true;
            }
            if (_outline != null)
            {
                var c = selectedColor; c.a = alpha;
                _outline.effectColor = c;
                if (!_outline.enabled && keepEnabled) _outline.enabled = true;
            }
        }
    }
}
