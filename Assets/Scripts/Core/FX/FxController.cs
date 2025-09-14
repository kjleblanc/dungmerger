using TMPro;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class FxController : MonoBehaviour
    {
        [Header("FX Layer")]
        public Transform fxLayer; // where to spawn damage popups
        public bool forceFxAbove = true;
        public int fxSortingOrder = 5000;

        [Header("Damage Popup")]
        public DamagePopup damagePopupPrefab;
        public Color damageNumberColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private DamagePopupPool popupPool;

        [Header("Refs (Optional)")]
        public DragLayerController dragLayerController;

        public void Setup()
        {
            if (fxLayer == null) return;
            fxLayer.SetAsLastSibling();
            if (forceFxAbove)
            {
                var canvas = fxLayer.GetComponent<Canvas>();
                if (canvas == null) canvas = fxLayer.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = fxSortingOrder;
                var cg = fxLayer.GetComponent<CanvasGroup>();
                if (cg == null) cg = fxLayer.gameObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
            if (dragLayerController == null)
            {
                dragLayerController = GetComponent<DragLayerController>();
            }
        }

        public void SpawnDamagePopup(RectTransform target, int amount, Color color)
        {
            Transform layer = fxLayer != null ? fxLayer : (dragLayerController != null ? dragLayerController.dragLayer : null);
            if (layer == null) return;
            RectTransform layerRT = layer as RectTransform;
            Canvas parentCanvas = layerRT != null ? layerRT.GetComponent<Canvas>() : null;
            if (parentCanvas == null && layerRT != null) parentCanvas = layerRT.GetComponentInParent<Canvas>();
            Vector2 anchored = Vector2.zero;
            if (layerRT != null && target != null)
            {
                var cam = parentCanvas != null ? parentCanvas.worldCamera : null;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRT, screen, cam, out anchored);
            }

            if (damagePopupPrefab != null)
            {
                var popup = popupPool != null ? popupPool.Get(layer) : Object.Instantiate(damagePopupPrefab, layer);
                var rt = popup.GetComponent<RectTransform>();
                if (layerRT != null) rt.anchoredPosition = anchored; else if (target != null) rt.position = target.position; else rt.anchoredPosition = Vector2.zero;
                if (popup.label == null)
                {
                    popup.label = popup.GetComponentInChildren<TextMeshProUGUI>();
                }
                popup.Set(amount, color);
                rt.SetAsLastSibling();
            }
            else
            {
                var go = new GameObject("DamagePopup", typeof(RectTransform));
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(layer, worldPositionStays: false);
                if (layerRT != null) rt.anchoredPosition = anchored; else if (target != null) rt.position = target.position; else rt.anchoredPosition = Vector2.zero;

                var tmpGO = new GameObject("Text", typeof(RectTransform));
                var tmpRT = tmpGO.GetComponent<RectTransform>();
                tmpRT.SetParent(rt, worldPositionStays: false);
                var tmp = tmpGO.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 28f;
                tmp.raycastTarget = false;

                var popup = go.AddComponent<DamagePopup>();
                popup.label = tmp;
                popup.Set(amount, color);
                rt.SetAsLastSibling();
            }
        }

        public void SpawnAbilityFx(RectTransform target, GameObject prefab)
        {
            Transform layer = fxLayer != null ? fxLayer : (dragLayerController != null ? dragLayerController.dragLayer : null);
            if (layer == null || prefab == null) return;
            RectTransform layerRT = layer as RectTransform;
            Canvas parentCanvas = layerRT != null ? layerRT.GetComponent<Canvas>() : null;
            if (parentCanvas == null && layerRT != null) parentCanvas = layerRT.GetComponentInParent<Canvas>();
            Vector2 anchored = Vector2.zero;
            if (layerRT != null && target != null)
            {
                var cam = parentCanvas != null ? parentCanvas.worldCamera : null;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRT, screen, cam, out anchored);
            }

            var fx = Object.Instantiate(prefab, layer);
            var rt = fx.GetComponent<RectTransform>();
            if (rt != null)
            {
                if (layerRT != null) rt.anchoredPosition = anchored;
                else if (target != null) rt.position = target.position;
                else rt.anchoredPosition = Vector2.zero;
            }
            fx.transform.SetAsLastSibling();
        }
    }
}

