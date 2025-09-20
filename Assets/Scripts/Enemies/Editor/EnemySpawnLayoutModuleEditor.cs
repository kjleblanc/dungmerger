using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MergeDungeon.Core.Editor
{
    [CustomEditor(typeof(EnemySpawnLayoutModule))]
    public class EnemySpawnLayoutModuleEditor : UnityEditor.Editor
    {
        private const int MaxPreviewColumns = 12;
        private const int MaxPreviewRows = 8;

        private int _previewOffsetX;
        private int _previewOffsetY;

        private SerializedProperty _relativeCellsProperty;

        private void OnEnable()
        {
            _relativeCellsProperty = serializedObject.FindProperty("relativeCells");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawLayoutControls();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_relativeCellsProperty, includeChildren: true);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayoutControls()
        {
            if (_relativeCellsProperty == null)
            {
                EditorGUILayout.HelpBox("relativeCells property not found.", MessageType.Error);
                return;
            }

            EditorGUILayout.LabelField("Spawn Cells", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click cells to toggle relative spawn positions. Coordinates are relative to the layout anchor (0,0 at the top-left).", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All Cells"))
            {
                ClearCells();
            }
            if (GUILayout.Button("Reset To Anchor Only (0,0)"))
            {
                ResetToAnchor();
            }
            EditorGUILayout.EndHorizontal();

            GetBoardDimensionsHint(out int boardWidth, out int boardHeight);
            boardWidth = Mathf.Max(1, boardWidth);
            boardHeight = Mathf.Max(1, boardHeight);

            int maxOffsetX = Mathf.Max(0, boardWidth - MaxPreviewColumns);
            int maxOffsetY = Mathf.Max(0, boardHeight - MaxPreviewRows);
            _previewOffsetX = Mathf.Clamp(_previewOffsetX, 0, maxOffsetX);
            _previewOffsetY = Mathf.Clamp(_previewOffsetY, 0, maxOffsetY);

            if (maxOffsetX > 0)
            {
                _previewOffsetX = EditorGUILayout.IntSlider("Column Offset", _previewOffsetX, 0, maxOffsetX);
            }
            else
            {
                _previewOffsetX = 0;
            }

            if (maxOffsetY > 0)
            {
                _previewOffsetY = EditorGUILayout.IntSlider("Row Offset", _previewOffsetY, 0, maxOffsetY);
            }
            else
            {
                _previewOffsetY = 0;
            }

            int previewColumns = Mathf.Clamp(boardWidth - _previewOffsetX, 1, MaxPreviewColumns);
            int previewRows = Mathf.Clamp(boardHeight - _previewOffsetY, 1, MaxPreviewRows);

            var selected = BuildSelectionSet();
            float cellSize = 24f;

            for (int y = 0; y < previewRows; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < previewColumns; x++)
                {
                    var cell = new Vector2Int(x + _previewOffsetX, y + _previewOffsetY);
                    bool isSelected = selected.Contains(cell);
                    Color previous = GUI.backgroundColor;

                    if (cell == Vector2Int.zero)
                    {
                        GUI.backgroundColor = isSelected ? new Color(0.1f, 0.6f, 0.9f) : new Color(0.25f, 0.45f, 0.65f);
                    }
                    else if (isSelected)
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                    }

                    string label = isSelected ? "X" : string.Empty;
                    if (GUILayout.Button(label, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        ToggleCell(cell, selected);
                    }

                    GUI.backgroundColor = previous;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (boardWidth > previewColumns || boardHeight > previewRows)
            {
                int maxColumnIndex = boardWidth - 1;
                int maxRowIndex = boardHeight - 1;
                int colStart = _previewOffsetX;
                int colEnd = Mathf.Min(_previewOffsetX + previewColumns - 1, maxColumnIndex);
                int rowStart = _previewOffsetY;
                int rowEnd = Mathf.Min(_previewOffsetY + previewRows - 1, maxRowIndex);
                EditorGUILayout.HelpBox($"Previewing columns {colStart}-{colEnd} of {maxColumnIndex}, rows {rowStart}-{rowEnd} of {maxRowIndex}. Adjust offsets or edit using the list below for other cells.", MessageType.None);
            }
        }

        private HashSet<Vector2Int> BuildSelectionSet()
        {
            var set = new HashSet<Vector2Int>();
            for (int i = 0; i < _relativeCellsProperty.arraySize; i++)
            {
                var element = _relativeCellsProperty.GetArrayElementAtIndex(i);
                set.Add(element.vector2IntValue);
            }
            return set;
        }

        private void ToggleCell(Vector2Int cell, HashSet<Vector2Int> selected)
        {
            if (selected.Contains(cell))
            {
                RemoveCell(cell);
                selected.Remove(cell);
            }
            else
            {
                AddCell(cell);
                selected.Add(cell);
            }

            SortCells();
        }

        private void AddCell(Vector2Int cell)
        {
            if (ContainsCell(cell)) return;
            int index = _relativeCellsProperty.arraySize;
            _relativeCellsProperty.InsertArrayElementAtIndex(index);
            var element = _relativeCellsProperty.GetArrayElementAtIndex(index);
            element.vector2IntValue = cell;
        }

        private void RemoveCell(Vector2Int cell)
        {
            for (int i = 0; i < _relativeCellsProperty.arraySize; i++)
            {
                var element = _relativeCellsProperty.GetArrayElementAtIndex(i);
                if (element.vector2IntValue == cell)
                {
                    _relativeCellsProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }

        private bool ContainsCell(Vector2Int cell)
        {
            for (int i = 0; i < _relativeCellsProperty.arraySize; i++)
            {
                if (_relativeCellsProperty.GetArrayElementAtIndex(i).vector2IntValue == cell)
                {
                    return true;
                }
            }
            return false;
        }

        private void ClearCells()
        {
            _relativeCellsProperty.arraySize = 0;
        }

        private void ResetToAnchor()
        {
            ClearCells();
            AddCell(Vector2Int.zero);
            SortCells();
        }

        private void SortCells()
        {
            var values = new List<Vector2Int>();
            for (int i = 0; i < _relativeCellsProperty.arraySize; i++)
            {
                values.Add(_relativeCellsProperty.GetArrayElementAtIndex(i).vector2IntValue);
            }

            values.Sort((a, b) =>
            {
                int compareY = a.y.CompareTo(b.y);
                return compareY != 0 ? compareY : a.x.CompareTo(b.x);
            });

            _relativeCellsProperty.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                _relativeCellsProperty.GetArrayElementAtIndex(i).vector2IntValue = values[i];
            }
        }

#if UNITY_2023_1_OR_NEWER
        private static T FindAnyObject<T>() where T : Object => Object.FindFirstObjectByType<T>();
#else
        private static T FindAnyObject<T>() where T : Object => Object.FindObjectOfType<T>();
#endif

        private void GetBoardDimensionsHint(out int width, out int height)
        {
            width = 10;
            height = 6;

            var board = FindAnyObject<BoardController>();
            if (board != null)
            {
                width = Mathf.Max(1, board.width);
                height = Mathf.Max(1, board.height);
                return;
            }

            var gridManager = FindAnyObject<GridManager>();
            if (gridManager != null)
            {
                var boardController = gridManager.boardController != null ? gridManager.boardController : gridManager.GetComponent<BoardController>();
                if (boardController != null)
                {
                    width = Mathf.Max(1, boardController.width);
                    height = Mathf.Max(1, boardController.height);
                }
            }
        }
    }
}
