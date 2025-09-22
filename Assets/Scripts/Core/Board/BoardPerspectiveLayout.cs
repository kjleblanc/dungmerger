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

            float totalHeight = _controller.height * cellSize.y + Mathf.Max(0, _controller.height - 1) * spacing.y + padding.top + padding.bottom;

            float startY = totalHeight * 0.5f - padding.top - cellSize.y * 0.5f;

            float stepY = cellSize.y + spacing.y;
            float tiltRad = tiltAngle * Mathf.Deg2Rad;
            float columnCenter = _controller.width > 1 ? (_controller.width - 1) * 0.5f : 0f;

            int rowCount = _controller.height;
            if (rowCount <= 0) return;

            int columnCount = _controller.width;
            int columnsMinusOne = Mathf.Max(0, columnCount - 1);
            var rowScales = new float[rowCount];
            var rowDesignerYOffset = new float[rowCount];
            var rowSkewPerColumn = new float[rowCount];
            var rowDesignerZOffset = new float[rowCount];
            var rowStartX = new float[rowCount];
            var rowStepX = new float[rowCount];

            float baseRowTotalWidth = cellSize.x * columnCount + columnsMinusOne * spacing.x;
            float basePaddedRowWidth = baseRowTotalWidth + padding.left + padding.right;
            float baseRowStep = cellSize.x + spacing.x;
            float baseRowStart = -basePaddedRowWidth * 0.5f + padding.left + cellSize.x * 0.5f;
            float baseRowCenterX = baseRowStart + columnsMinusOne * baseRowStep * 0.5f;

            for (int y = 0; y < rowCount; y++)
            {
                float rowNormalized = rowCount <= 1 ? 0f : (float)y / (rowCount - 1);
                float scale = perRowScale != null ? perRowScale.Evaluate(rowNormalized) : 1f;
                rowScales[y] = scale;
                rowDesignerYOffset[y] = rowDepthOffset * rowNormalized;
                rowSkewPerColumn[y] = horizontalSkew * rowNormalized;
                rowDesignerZOffset[y] = rowZOffset * rowNormalized;

                rowStepX[y] = baseRowStep;
                rowStartX[y] = baseRowStart;
            }

            var cumulativeCompression = new float[rowCount];
            var cumulativeDepth = new float[rowCount];
            for (int y = 1; y < rowCount; y++)
            {
                float lowerScale = rowScales[y - 1];
                float currentScale = rowScales[y];
                float halfHeightLower = cellSize.y * lowerScale * 0.5f;
                float halfHeightCurrent = cellSize.y * currentScale * 0.5f;
                float spacingBeforeTilt = halfHeightLower + spacing.y + halfHeightCurrent;
                float desiredYSpacing = spacingBeforeTilt * Mathf.Cos(tiltRad);
                float desiredDepthSpacing = spacingBeforeTilt * Mathf.Sin(tiltRad);

                cumulativeCompression[y] = cumulativeCompression[y - 1] + (stepY - desiredYSpacing);
                cumulativeDepth[y] = cumulativeDepth[y - 1] + desiredDepthSpacing;
            }

            var rotation = Quaternion.Euler(tiltAngle, 0f, 0f);

            for (int y = 0; y < rowCount; y++)
            {
                float scale = rowScales[y];
                float offsetY = rowDesignerYOffset[y];
                float skewPerColumn = rowSkewPerColumn[y];
                float offsetZ = rowDesignerZOffset[y];
                float compressionOffset = cumulativeCompression[y];
                float depthOffset = cumulativeDepth[y];
                float baseRowStartX = rowStartX[y];
                float rowStep = rowStepX[y];

                for (int x = 0; x < columnCount; x++)
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

                    int rowFromTop = rowCount - 1 - y;
                    float baseX = baseRowStartX + x * rowStep;
                    float baseY = startY - rowFromTop * stepY;
                    float adjustedY = baseY - compressionOffset + offsetY;
                    float adjustedZ = depthOffset + offsetZ;

                    float centerDelta = x - columnCenter;
                    float skewOffset = skewPerColumn * centerDelta;

                    float offsetFromCenter = baseX - baseRowCenterX;
                    float scaledX = baseRowCenterX + offsetFromCenter * scale;
                    float adjustedX = scaledX + skewOffset;

                    rt.anchoredPosition3D = new Vector3(adjustedX, adjustedY, adjustedZ);
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








