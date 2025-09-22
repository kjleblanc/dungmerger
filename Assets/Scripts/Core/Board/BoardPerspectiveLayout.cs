using UnityEngine;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    [ExecuteAlways]
    [RequireComponent(typeof(BoardController))]
    [DisallowMultipleComponent]
    public class BoardPerspectiveLayout : MonoBehaviour
    {
        private const float MinTilt = -80f;
        private const float MaxTilt = 80f;

        [Header("Perspective")]
        [SerializeField, Range(MinTilt, MaxTilt)]
        private float tiltAngle = 35f;

        [SerializeField]
        private float rowDepthOffset = -25f;

        [SerializeField]
        private AnimationCurve perRowScale = AnimationCurve.Linear(0f, 1f, 1f, 0.8f);

        [SerializeField]
        private float horizontalSkew = 0f;

        [SerializeField]
        private float rowZOffset = 0f;

        private BoardController _controller;

        private void Awake()
        {
            CacheController();
        }

        private void OnEnable()
        {
            CacheController();
            _controller?.RecomputeGridCellSize(force: true);
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.RecomputeGridCellSize(force: true);
            }
        }

        private void OnValidate()
        {
            CacheController();
            if (!Application.isPlaying)
            {
                ApplyLayout();
            }
        }

        private void CacheController()
        {
            if (_controller == null)
            {
                _controller = GetComponent<BoardController>();
            }
        }

        public void ApplyLayout()
        {
            CacheController();
            if (!isActiveAndEnabled || _controller == null) return;
            if (!_controller.IsBoardReady) return;

            var container = _controller.boardContainer;
            if (container == null) return;

            var layout = _controller.boardLayout;
            if (layout != null && layout.enabled)
            {
                layout.enabled = false;
            }
            Vector2 cellSize;
            Vector2 spacing = Vector2.zero;
            RectOffset padding = new RectOffset();
            if (layout != null)
            {
                cellSize = layout.cellSize;
                spacing = layout.spacing;
                padding = layout.padding;
            }
            else
            {
                // fall back to prefab/instance size
                var fallbackCell = _controller.GetCell(0, 0);
                if (fallbackCell != null && fallbackCell.rectTransform != null)
                {
                    cellSize = fallbackCell.rectTransform.rect.size;
                }
                else
                {
                    cellSize = new Vector2(100f, 100f);
                }
            }

            float totalWidth = _controller.width * cellSize.x + Mathf.Max(0, _controller.width - 1) * spacing.x + padding.left + padding.right;
            float totalHeight = _controller.height * cellSize.y + Mathf.Max(0, _controller.height - 1) * spacing.y + padding.top + padding.bottom;

            float startX = -totalWidth * 0.5f + padding.left + cellSize.x * 0.5f;
            float startY = totalHeight * 0.5f - padding.top - cellSize.y * 0.5f;

            float stepX = cellSize.x + spacing.x;
            float stepY = cellSize.y + spacing.y;
            float columnCenter = _controller.width > 1 ? (_controller.width - 1) * 0.5f : 0f;

            var rotation = Quaternion.Euler(tiltAngle, 0f, 0f);

            for (int y = 0; y < _controller.height; y++)
            {
                float rowNormalized = _controller.height <= 1 ? 0f : (float)y / (_controller.height - 1);
                float scale = perRowScale != null ? perRowScale.Evaluate(rowNormalized) : 1f;
                float offsetY = rowDepthOffset * rowNormalized;
                float skewPerColumn = horizontalSkew * rowNormalized;
                float offsetZ = rowZOffset * rowNormalized;

                for (int x = 0; x < _controller.width; x++)
                {
                    var cell = _controller.GetCell(x, y);
                    if (cell == null) continue;

                    var rt = cell.rectTransform != null ? cell.rectTransform : cell.GetComponent<RectTransform>();
                    if (rt == null) continue;

                    rt.anchorMin = Vector2.one * 0.5f;
                    rt.anchorMax = Vector2.one * 0.5f;
                    rt.pivot = Vector2.one * 0.5f;

                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cellSize.x);
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cellSize.y);

                    int rowFromTop = _controller.height - 1 - y;
                    float baseX = startX + x * stepX;
                    float baseY = startY - rowFromTop * stepY;

                    float centerDelta = x - columnCenter;
                    float skewOffset = skewPerColumn * centerDelta;

                    rt.anchoredPosition3D = new Vector3(baseX + skewOffset, baseY + offsetY, offsetZ);
                    rt.localScale = new Vector3(scale, scale, 1f);
                    rt.localRotation = rotation;
                }
            }

            if (container.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.MarkLayoutForRebuild(container);
            }
        }
    }
}
