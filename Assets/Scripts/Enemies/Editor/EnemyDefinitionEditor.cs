using UnityEditor;
using UnityEngine;

namespace MergeDungeon.Core.Editor
{
    [CustomEditor(typeof(EnemyDefinition))]
    public class EnemyDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _lootModuleProperty;
        private SerializedProperty _spawnProfileProperty;
        private SerializedProperty _useExactCellProperty;
        private SerializedProperty _exactCellProperty;
        private SerializedProperty _columnRangeProperty;
        private SerializedProperty _rowOffsetProperty;

        private UnityEditor.Editor _lootModuleEditor;

        private void OnEnable()
        {
            _lootModuleProperty = serializedObject.FindProperty("lootModule");
            _spawnProfileProperty = serializedObject.FindProperty("spawnProfile");
            if (_spawnProfileProperty != null)
            {
                _useExactCellProperty = _spawnProfileProperty.FindPropertyRelative("useExactCell");
                _exactCellProperty = _spawnProfileProperty.FindPropertyRelative("exactCell");
                _columnRangeProperty = _spawnProfileProperty.FindPropertyRelative("columnRange");
                _rowOffsetProperty = _spawnProfileProperty.FindPropertyRelative("rowOffsetFromTop");
            }
        }

        private void OnDisable()
        {
            if (_lootModuleEditor != null)
            {
                DestroyImmediate(_lootModuleEditor);
                _lootModuleEditor = null;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script", "lootModule", "spawnProfile");

            DrawSpawnProfileSection();

            EditorGUILayout.Space();
            DrawLootModuleSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSpawnProfileSection()
        {
            if (_spawnProfileProperty == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn Profile", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            GetBoardDimensionsHint(out int boardWidth, out int boardHeight);
            EditorGUILayout.LabelField($"Board hint: {boardWidth} x {boardHeight}", EditorStyles.miniLabel);

            DrawExactCellControls(boardWidth, boardHeight);
            DrawColumnRangeControls(boardWidth);
            DrawRowOffsetControls(boardHeight);
            DrawSpawnProfilePreview(boardWidth, boardHeight);

            EditorGUI.indentLevel--;
        }

        private void DrawExactCellControls(int boardWidth, int boardHeight)
        {
            if (_useExactCellProperty == null || _exactCellProperty == null) return;

            bool useExact = _useExactCellProperty.boolValue;
            bool newUseExact = EditorGUILayout.Toggle("Use Exact Cell", useExact);
            if (newUseExact != useExact)
            {
                _useExactCellProperty.boolValue = newUseExact;
            }

            if (!newUseExact) return;

            EditorGUI.indentLevel++;
            var current = _exactCellProperty.vector2IntValue;
            int maxColumn = Mathf.Max(0, boardWidth - 1);
            int maxRow = Mathf.Max(0, boardHeight - 1);
            current.x = EditorGUILayout.IntSlider("Column (X)", current.x, 0, maxColumn);
            current.y = EditorGUILayout.IntSlider("Row From Top (Y)", current.y, 0, maxRow);
            _exactCellProperty.vector2IntValue = current;

            if (!IsExactCellValid(current, boardWidth, boardHeight))
            {
                EditorGUILayout.HelpBox($"Exact cell ({current.x}, {current.y}) lies outside the board hint bounds.", MessageType.Warning);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawColumnRangeControls(int boardWidth)
        {
            if (_columnRangeProperty == null) return;

            var columnRange = _columnRangeProperty.vector2IntValue;
            bool hasRange = columnRange.x >= 0 && columnRange.y >= columnRange.x;
            bool newHasRange = EditorGUILayout.Toggle("Limit Column Range", hasRange);
            int maxColumn = Mathf.Max(0, boardWidth - 1);

            if (!newHasRange)
            {
                if (hasRange)
                {
                    _columnRangeProperty.vector2IntValue = new Vector2Int(-1, -1);
                }
                return;
            }

            EditorGUI.indentLevel++;
            int minColumn = hasRange ? Mathf.Clamp(columnRange.x, 0, maxColumn) : 0;
            int maxColumnValue = hasRange ? Mathf.Clamp(columnRange.y, minColumn, maxColumn) : maxColumn;

            float min = minColumn;
            float max = maxColumnValue;
            EditorGUILayout.MinMaxSlider(new GUIContent("Columns"), ref min, ref max, 0, Mathf.Max(0, maxColumn));
            minColumn = Mathf.Clamp(Mathf.RoundToInt(min), 0, maxColumn);
            maxColumnValue = Mathf.Clamp(Mathf.RoundToInt(max), minColumn, maxColumn);

            minColumn = Mathf.Clamp(EditorGUILayout.IntField("Min Column", minColumn), 0, maxColumn);
            maxColumnValue = Mathf.Clamp(EditorGUILayout.IntField("Max Column", maxColumnValue), minColumn, maxColumn);

            _columnRangeProperty.vector2IntValue = new Vector2Int(minColumn, maxColumnValue);
            EditorGUI.indentLevel--;
        }

        private void DrawRowOffsetControls(int boardHeight)
        {
            if (_rowOffsetProperty == null) return;

            int rowOffset = _rowOffsetProperty.intValue;
            int optionsCount = Mathf.Clamp(boardHeight, 1, 8);
            string[] options = new string[optionsCount + 1];
            options[0] = "Top Rows (default)";
            for (int i = 1; i <= optionsCount; i++)
            {
                int offset = i - 1;
                options[i] = offset switch
                {
                    0 => "Top Row",
                    1 => "Second Row",
                    2 => "Third Row",
                    _ => $"Row {offset + 1} from Top"
                };
            }

            int currentIndex = rowOffset >= 0 ? Mathf.Clamp(rowOffset + 1, 0, optionsCount) : 0;
            int selected = EditorGUILayout.Popup("Preferred Row", currentIndex, options);
            if (selected == 0)
            {
                _rowOffsetProperty.intValue = -1;
            }
            else
            {
                int newOffset = Mathf.Clamp(selected - 1, 0, Mathf.Max(0, boardHeight - 1));
                _rowOffsetProperty.intValue = newOffset;
            }
        }

        private void DrawSpawnProfilePreview(int boardWidth, int boardHeight)
        {
            if (boardWidth <= 0 || boardHeight <= 0) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Placement Preview", EditorStyles.boldLabel);

            int shownRows = Mathf.Min(boardHeight, 6);
            float cellSize = 20f;

            bool useExact = _useExactCellProperty != null && _useExactCellProperty.boolValue;
            Vector2Int exactCell = _exactCellProperty != null ? _exactCellProperty.vector2IntValue : Vector2Int.zero;
            Vector2Int columnRange = _columnRangeProperty != null ? _columnRangeProperty.vector2IntValue : new Vector2Int(-1, -1);
            bool hasColumnRange = columnRange.x >= 0 && columnRange.y >= columnRange.x;
            int rowOffset = _rowOffsetProperty != null ? _rowOffsetProperty.intValue : -1;
            bool hasRowPreference = rowOffset >= 0;

            for (int y = 0; y < shownRows; y++)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x < boardWidth; x++)
                {
                    bool isExact = useExact && exactCell.x == x && exactCell.y == y;
                    bool inRange = hasColumnRange && x >= columnRange.x && x <= columnRange.y;
                    bool rowMatch = hasRowPreference && y == rowOffset;

                    Color prevColor = GUI.backgroundColor;
                    if (isExact)
                    {
                        GUI.backgroundColor = new Color(0.1f, 0.6f, 0.9f);
                    }
                    else if (inRange && rowMatch)
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                    }
                    else if (rowMatch)
                    {
                        GUI.backgroundColor = new Color(0.7f, 0.7f, 0.3f);
                    }
                    else if (inRange)
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.3f, 0.7f);
                    }

                    string label = isExact ? "X" : string.Empty;
                    if (GUILayout.Button(label, GUILayout.Width(cellSize), GUILayout.Height(cellSize)))
                    {
                        if (isExact)
                        {
                            _useExactCellProperty.boolValue = false;
                        }
                        else
                        {
                            _useExactCellProperty.boolValue = true;
                            _exactCellProperty.vector2IntValue = new Vector2Int(x, y);
                        }
                    }

                    GUI.backgroundColor = prevColor;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (boardHeight > shownRows)
            {
                EditorGUILayout.LabelField($"Preview truncated to top {shownRows} rows of {boardHeight}.", EditorStyles.miniLabel);
            }
        }

        private static bool IsExactCellValid(Vector2Int cell, int boardWidth, int boardHeight)
        {
            return boardWidth > 0 && boardHeight > 0 && cell.x >= 0 && cell.y >= 0 && cell.x < boardWidth && cell.y < boardHeight;
        }

#if UNITY_2023_1_OR_NEWER
        private static T FindAnyObject<T>() where T : Object => Object.FindFirstObjectByType<T>();
#else
        private static T FindAnyObject<T>() where T : Object => Object.FindObjectOfType<T>();
#endif

        private void GetBoardDimensionsHint(out int width, out int height)
        {
            width = 8;
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
                BoardController boardController = gridManager.boardController != null ? gridManager.boardController : gridManager.GetComponent<BoardController>();
                if (boardController != null)
                {
                    width = Mathf.Max(1, boardController.width);
                    height = Mathf.Max(1, boardController.height);
                }
            }
        }

        private void DrawLootModuleSection()
        {
            EditorGUILayout.LabelField("Loot Module", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(_lootModuleProperty);

            if (_lootModuleProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No loot emitter assigned. Create one to define drops for this enemy.", MessageType.Info);
                if (GUILayout.Button("Create Loot Emitter Module"))
                {
                    CreateLootModule();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create & Replace", GUILayout.Width(130)))
                {
                    CreateLootModule();
                }
                if (GUILayout.Button("Remove", GUILayout.Width(80)))
                {
                    RemoveLootModule();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();
                DrawLootModuleInspector();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawLootModuleInspector()
        {
            var module = _lootModuleProperty.objectReferenceValue;
            if (module == null) return;

            if (_lootModuleEditor == null || _lootModuleEditor.target != module)
            {
                if (_lootModuleEditor != null)
                {
                    DestroyImmediate(_lootModuleEditor);
                }
                _lootModuleEditor = CreateEditor(module);
            }

            EditorGUI.indentLevel++;
            _lootModuleEditor?.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }

        private void RemoveLootModule()
        {
            var module = _lootModuleProperty.objectReferenceValue;
            if (module == null) return;

            string modulePath = AssetDatabase.GetAssetPath(module);
            string enemyPath = AssetDatabase.GetAssetPath(target);
            bool isSubAsset = !string.IsNullOrEmpty(modulePath) && modulePath == enemyPath;

            _lootModuleProperty.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();

            if (_lootModuleEditor != null)
            {
                DestroyImmediate(_lootModuleEditor);
                _lootModuleEditor = null;
            }

            if (isSubAsset)
            {
                AssetDatabase.RemoveObjectFromAsset(module);
                AssetDatabase.SaveAssets();
            }
            else
            {
                DestroyImmediate(module, true);
            }
        }

        private void CreateLootModule()
        {
            var enemyDef = (EnemyDefinition)target;
            string assetPath = AssetDatabase.GetAssetPath(enemyDef);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("EnemyDefinition asset path not found. Save the asset before creating modules.");
                return;
            }

            var module = ScriptableObject.CreateInstance<EnemyLootEmitterModule>();
            module.name = $"{enemyDef.name}_LootEmitter";

            AssetDatabase.AddObjectToAsset(module, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);

            serializedObject.Update();
            _lootModuleProperty.objectReferenceValue = module;
            serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(module);
        }
    }
}
