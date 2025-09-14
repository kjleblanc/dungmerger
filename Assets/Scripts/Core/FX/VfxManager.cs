using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MergeDungeon.Core
{
    public class VfxManager : MonoBehaviour
    {
        [Header("Library")]
        public VfxLibrary library;

        [Header("UI FX Layer")] 
        public Transform fxLayer; // where to spawn UI effects
        public bool forceFxAbove = true;
        public int fxSortingOrder = 5000;

        [Header("Defaults")] 
        public string damagePopupEffectId = "damage_popup";
        public Color damageNumberColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Header("Refs (Optional)")]
        public DragLayerController dragLayerController;

        [Header("World FX (Camera Stack)")]
        [Tooltip("Parent for world-space VFX (e.g., particles) rendered by an overlay camera.")]
        public Transform worldVfxRoot;
        [Tooltip("Camera used to place world VFX when converting from UI anchors (usually your overlay VFX camera). If null, uses Camera.main.")]
        public Camera worldVfxCamera;
        [Tooltip("Z position to place world VFX in worldVfxRoot space.")]
        public float worldVfxZ = 0f;

        private readonly Dictionary<string, Queue<GameObject>> _pools = new();
        private readonly Dictionary<string, Transform> _poolParents = new();

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

        private Transform GetUiLayer()
        {
            if (fxLayer != null) return fxLayer;
            if (dragLayerController != null) return dragLayerController.dragLayer;
            return null;
        }

        private Transform GetOrCreatePoolParent(string id)
        {
            if (_poolParents.TryGetValue(id, out var t) && t != null) return t;
            var go = new GameObject($"Pool_{id}");
            go.SetActive(true);
            go.transform.SetParent(transform, false);
            _poolParents[id] = go.transform;
            return go.transform;
        }

        private GameObject Acquire(string id, GameObject prefab, Transform parent)
        {
            if (prefab == null) return null;
            if (!_pools.TryGetValue(id, out var q))
            {
                q = new Queue<GameObject>();
                _pools[id] = q;
            }
            GameObject inst = q.Count > 0 ? q.Dequeue() : Instantiate(prefab);
            if (inst != null)
            {
                var key = inst.GetComponent<VfxPoolKey>();
                if (key == null) key = inst.AddComponent<VfxPoolKey>();
                key.effectId = id;
                key.manager = this;
                var tr = inst.transform;
                tr.SetParent(parent != null ? parent : transform, false);
                inst.SetActive(true);
                // Match the instance layer to its parent container for correct camera culling
                if (parent != null)
                {
                    SetLayerRecursively(inst, parent.gameObject.layer);
                }
            }
            return inst;
        }

        public void Release(GameObject instance)
        {
            if (instance == null) return;
            var key = instance.GetComponent<VfxPoolKey>();
            if (key != null && !string.IsNullOrEmpty(key.effectId))
            {
                Release(key.effectId, instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        public void Release(string id, GameObject instance)
        {
            if (instance == null) return;
            if (!_pools.TryGetValue(id, out var q))
            {
                q = new Queue<GameObject>();
                _pools[id] = q;
            }
            instance.SetActive(false);
            var parent = GetOrCreatePoolParent(id);
            instance.transform.SetParent(parent, false);
            q.Enqueue(instance);
        }

        public GameObject Play(string effectId, Transform anchor)
        {
            if (library == null || string.IsNullOrEmpty(effectId)) return null;
            var prefab = library.GetPrefab(effectId);
            if (prefab == null) return null;

            var inst = Acquire(effectId, prefab, anchor != null ? anchor : transform);
            if (inst == null) return null;
            if (anchor != null)
            {
                inst.transform.position = anchor.position;
            }
            inst.transform.SetAsLastSibling();
            return inst;
        }

        public GameObject PlayUI(string effectId, RectTransform anchor)
        {
            var layer = GetUiLayer();
            if (layer == null || library == null || string.IsNullOrEmpty(effectId)) return null;
            var prefab = library.GetPrefab(effectId);
            if (prefab == null) return null;

            var inst = Acquire(effectId, prefab, layer);
            if (inst == null) return null;
            var layerRT = layer as RectTransform;
            var rt = inst.GetComponent<RectTransform>();
            if (layerRT != null && rt != null)
            {
                Vector2 anchored = Vector2.zero;
                Canvas parentCanvas = layerRT.GetComponent<Canvas>();
                if (parentCanvas == null) parentCanvas = layerRT.GetComponentInParent<Canvas>();
                var cam = parentCanvas != null ? parentCanvas.worldCamera : null;
                if (anchor != null)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, anchor.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRT, screen, cam, out anchored);
                }
                rt.anchoredPosition = anchor != null ? anchored : Vector2.zero;
            }
            else if (anchor != null)
            {
                inst.transform.position = anchor.position;
            }
            inst.transform.SetAsLastSibling();
            return inst;
        }

        // Back-compat helper for direct prefab spawning (non-pooled)
        public GameObject SpawnAbilityFx(RectTransform target, GameObject prefab)
        {
            if (prefab == null) return null;

            // If prefab uses ParticleSystem, treat it as a world-space VFX and place via camera stacking
            bool isParticle = prefab.GetComponentInChildren<ParticleSystem>(true) != null;
            if (isParticle)
            {
                var worldRoot = worldVfxRoot != null ? worldVfxRoot : transform;
                var fx = Acquire("__world__" + prefab.name, prefab, worldRoot);
                // Convert UI anchor to screen, then to world position using worldVfxCamera (or Camera.main)
                var camUI = (dragLayerController != null && dragLayerController.dragLayer != null)
                    ? dragLayerController.dragLayer.GetComponentInParent<Canvas>()?.worldCamera
                    : null;
                var camWorld = worldVfxCamera != null ? worldVfxCamera : Camera.main;
                Vector3 worldPos = new Vector3(0, 0, worldVfxZ);
                if (target != null && camWorld != null)
                {
                    Vector2 screen = RectTransformUtility.WorldToScreenPoint(camUI, target.position);
                    float depth = Mathf.Abs(camWorld.transform.position.z - worldVfxZ);
                    worldPos = camWorld.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
                    worldPos.z = worldVfxZ;
                }
                fx.transform.position = worldPos;
                return fx;
            }
            else
            {
                // UI prefab path (legacy behaviour)
                var layer = GetUiLayer();
                if (layer == null) return null;
                var fx = Instantiate(prefab, layer);
                var rt = fx.GetComponent<RectTransform>();
                var layerRT = layer as RectTransform;
                if (rt != null)
                {
                    if (layerRT != null && target != null)
                    {
                        Canvas parentCanvas = layerRT.GetComponent<Canvas>();
                        if (parentCanvas == null) parentCanvas = layerRT.GetComponentInParent<Canvas>();
                        var cam = parentCanvas != null ? parentCanvas.worldCamera : null;
                        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, target.position);
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(layerRT, screen, cam, out var anchored);
                        rt.anchoredPosition = anchored;
                    }
                    else if (target != null) rt.position = target.position;
                    else rt.anchoredPosition = Vector2.zero;
                }
                fx.transform.SetAsLastSibling();
                return fx;
            }
        }

        // World-space play using library id at a UI anchor (for camera stack overlay)
        public GameObject PlayWorldAtUIAnchor(string effectId, RectTransform uiAnchor)
        {
            if (string.IsNullOrEmpty(effectId) || library == null) return null;
            var prefab = library.GetPrefab(effectId);
            if (prefab == null) return null;
            var worldRoot = worldVfxRoot != null ? worldVfxRoot : transform;
            var camUI = (dragLayerController != null && dragLayerController.dragLayer != null)
                ? dragLayerController.dragLayer.GetComponentInParent<Canvas>()?.worldCamera
                : null;
            var camWorld = worldVfxCamera != null ? worldVfxCamera : Camera.main;
            var inst = Acquire(effectId, prefab, worldRoot);
            if (inst == null) return null;
            if (uiAnchor != null && camWorld != null)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(camUI, uiAnchor.position);
                float depth = Mathf.Abs(camWorld.transform.position.z - worldVfxZ);
                Vector3 worldPos = camWorld.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
                worldPos.z = worldVfxZ;
                inst.transform.position = worldPos;
            }
            else
            {
                inst.transform.position = new Vector3(0, 0, worldVfxZ);
            }
            return inst;
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                if (child == null) continue;
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
