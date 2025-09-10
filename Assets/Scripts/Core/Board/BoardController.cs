using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MergeDungeon.Core
{
    public class BoardController : MonoBehaviour
    {
        [Header("Board Size")]
        public int width = 5;
        public int height = 5;

        [Header("UI Refs")]
        public RectTransform boardContainer; // Must have GridLayoutGroup with width columns
        public GridLayoutGroup boardLayout;   // optional, to set constraints
        public bool autoFitCellSize = true;

        [Header("Prefabs")]
        public BoardCell cellPrefab;

        private Vector2 _lastBoardSize;
        private BoardCell[,] _cells;
        public bool IsBoardReady => _cells != null;
        

        // Configure via Inspector; no implicit seeding from GridManager

        public void BuildBoard(BoardCell prefab = null)
        {
            var cp = prefab != null ? prefab : cellPrefab;
            if (boardContainer == null || cp == null)
            {
                Debug.LogError("BoardController: Missing boardContainer or cellPrefab");
                return;
            }

            if (boardLayout == null)
                boardLayout = boardContainer.GetComponent<GridLayoutGroup>();

            if (boardLayout != null)
            {
                boardLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                boardLayout.constraintCount = width;
            }

            _cells = new BoardCell[width, height];

            for (int i = boardContainer.childCount - 1; i >= 0; i--)
            {
                var go = boardContainer.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
            }

            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = Instantiate(cp, boardContainer);
                    cell.x = x;
                    cell.y = y;
                    _cells[x, y] = cell;
                }
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                // Only update layout sizing in edit mode; no rebuild
                RecomputeGridCellSize(force: true);
            }
        }

        private void OnEnable()
        {
            // No automatic rebuilds in edit mode; only via inspector button
        }

        // Rebuilds occur only when explicitly invoked (e.g., via custom editor button)

        public void RecomputeGridCellSize(bool force = false)
        {
            if (!autoFitCellSize || boardLayout == null || boardContainer == null) return;
            var size = boardContainer.rect.size;
            if (!force && (Vector2)size == _lastBoardSize) return;
            _lastBoardSize = size;

            var pad = boardLayout.padding;
            var spacing = boardLayout.spacing;
            float availW = Mathf.Max(0, size.x - pad.left - pad.right - spacing.x * (width - 1));
            float availH = Mathf.Max(0, size.y - pad.top - pad.bottom - spacing.y * (height - 1));
            float cellW = width > 0 ? availW / width : 0f;
            float cellH = height > 0 ? availH / height : 0f;
            float cell = Mathf.Max(8f, Mathf.Min(cellW, cellH));
            boardLayout.cellSize = new Vector2(cell, cell);
        }

        public BoardCell GetCell(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return null;
            return _cells[x, y];
        }

        public List<BoardCell> CollectEmptyCells()
        {
            var list = new List<BoardCell>(width * height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var c = _cells[x, y];
                    if (c != null && c.IsEmpty()) list.Add(c);
                }
            }
            return list;
        }
    }
}
