using UnityEngine;

namespace MergeDungeon.Core
{
    // Per-instance controller to set outline properties on a SpriteRenderer material via MaterialPropertyBlock.
    // Use with the URP Shader Graph outline material (properties: _OutlineColor, _OutlineThickness).
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteOutlineController : MonoBehaviour
    {
        [ColorUsage(false, true)] public Color outlineColor = Color.black;
        [Min(0f)] public float outlineThickness = 1.5f; // pixels

        [Tooltip("Optional: assign the outline material created from the Shader Graph. If left null, the current material is used.")]
        public Material outlineMaterial;

        private SpriteRenderer _sr;
        private MaterialPropertyBlock _mpb;

        private static readonly int ID_OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int ID_OutlineThickness = Shader.PropertyToID("_OutlineThickness");

        private void Awake()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Apply();
        }

        private void OnEnable()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Apply();
        }

        private void OnValidate()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            Apply();
        }

        public void Apply()
        {
            if (_sr == null) return;
            if (outlineMaterial != null)
            {
                // In play mode use instance material; in edit mode prefer sharedMaterial to avoid generating instances.
                if (Application.isPlaying)
                    _sr.material = outlineMaterial;
                else
                    _sr.sharedMaterial = outlineMaterial;
            }
            _sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(ID_OutlineColor, outlineColor);
            _mpb.SetFloat(ID_OutlineThickness, Mathf.Max(0f, outlineThickness));
            _sr.SetPropertyBlock(_mpb);
        }
    }
}

