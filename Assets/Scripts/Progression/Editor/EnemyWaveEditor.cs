using UnityEditor;
using UnityEngine;

namespace MergeDungeon.Core.Editor
{
    [CustomEditor(typeof(EnemyWave))]
    public class EnemyWaveEditor : UnityEditor.Editor
    {
        private SerializedProperty _spawnsProperty;
        private SerializedProperty _initialSpawnProperty;
        private SerializedProperty _perAdvanceSpawnProperty;

        private void OnEnable()
        {
            _spawnsProperty = serializedObject.FindProperty("spawns");
            _initialSpawnProperty = serializedObject.FindProperty("initialSpawn");
            _perAdvanceSpawnProperty = serializedObject.FindProperty("perAdvanceSpawn");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawSpawnsList();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_initialSpawnProperty);
            EditorGUILayout.PropertyField(_perAdvanceSpawnProperty);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSpawnsList()
        {
            if (_spawnsProperty == null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spawns"));
                return;
            }

            EditorGUILayout.LabelField("Spawns", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            for (int i = 0; i < _spawnsProperty.arraySize; i++)
            {
                var spawnProp = _spawnsProperty.GetArrayElementAtIndex(i);
                DrawSpawnEntry(spawnProp, i);
            }
            EditorGUILayout.Space();
            if (GUILayout.Button("Add Spawn"))
            {
                int index = _spawnsProperty.arraySize;
                _spawnsProperty.InsertArrayElementAtIndex(index);
                InitializeSpawn(_spawnsProperty.GetArrayElementAtIndex(index));
            }
        }

        private void DrawSpawnEntry(SerializedProperty spawnProp, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var label = new GUIContent($"Spawn {index + 1}");
            spawnProp.isExpanded = EditorGUILayout.Foldout(spawnProp.isExpanded, label, true);
            if (spawnProp.isExpanded)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(spawnProp.FindPropertyRelative("enemyDefinition"));
                EditorGUILayout.PropertyField(spawnProp.FindPropertyRelative("countMin"));
                EditorGUILayout.PropertyField(spawnProp.FindPropertyRelative("countMax"));
                EditorGUILayout.PropertyField(spawnProp.FindPropertyRelative("hpOverride"));
                EditorGUILayout.PropertyField(spawnProp.FindPropertyRelative("hpBonusPerFloor"));
                EditorGUILayout.PropertyField(spawnProp.FindPropertyRelative("bossFlagForAll"));

                EditorGUILayout.Space();
                DrawSpawnLayoutControls(spawnProp, index);

                if (GUILayout.Button("Remove Spawn"))
                {
                    _spawnsProperty.DeleteArrayElementAtIndex(index);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSpawnLayoutControls(SerializedProperty spawnProp, int index)
        {
            var layoutProp = spawnProp.FindPropertyRelative("spawnLayout");
            var anchorProp = spawnProp.FindPropertyRelative("layoutAnchor");
            var fillRemainderProp = spawnProp.FindPropertyRelative("fillRemainderWithRandom");

            EditorGUILayout.PropertyField(layoutProp);
            EditorGUILayout.PropertyField(anchorProp);
            EditorGUILayout.PropertyField(fillRemainderProp);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Layout Module"))
            {
                CreateLayoutModule(layoutProp, index);
            }
            GUI.enabled = layoutProp.objectReferenceValue != null;
            if (GUILayout.Button("Remove Layout Module"))
            {
                RemoveLayoutModule(layoutProp);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void CreateLayoutModule(SerializedProperty layoutProp, int index)
        {
            if (layoutProp.objectReferenceValue != null)
            {
                EditorUtility.DisplayDialog("Spawn Layout", "This spawn already has a layout assigned.", "OK");
                return;
            }

            var wave = (EnemyWave)target;
            string assetPath = AssetDatabase.GetAssetPath(wave);
            if (string.IsNullOrEmpty(assetPath))
            {
                EditorUtility.DisplayDialog("Spawn Layout", "Save the EnemyWave asset before creating layout modules.", "OK");
                return;
            }

            var module = ScriptableObject.CreateInstance<EnemySpawnLayoutModule>();
            module.name = $"{wave.name}_SpawnLayout_{index + 1}";

            AssetDatabase.AddObjectToAsset(module, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);

            layoutProp.objectReferenceValue = module;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(wave);

        }

        private void InitializeSpawn(SerializedProperty spawnProp)
        {
            if (spawnProp == null) return;
            spawnProp.FindPropertyRelative("enemyDefinition").objectReferenceValue = null;
            spawnProp.FindPropertyRelative("countMin").intValue = 1;
            spawnProp.FindPropertyRelative("countMax").intValue = 1;
            spawnProp.FindPropertyRelative("hpOverride").intValue = 0;
            spawnProp.FindPropertyRelative("hpBonusPerFloor").intValue = 0;
            spawnProp.FindPropertyRelative("bossFlagForAll").boolValue = false;
            spawnProp.FindPropertyRelative("spawnLayout").objectReferenceValue = null;
            spawnProp.FindPropertyRelative("layoutAnchor").vector2IntValue = Vector2Int.zero;
            spawnProp.FindPropertyRelative("fillRemainderWithRandom").boolValue = false;
        }

        private void RemoveLayoutModule(SerializedProperty layoutProp)
        {
            var module = layoutProp.objectReferenceValue as EnemySpawnLayoutModule;
            var wave = (EnemyWave)target;
            layoutProp.objectReferenceValue = null;
            serializedObject.ApplyModifiedProperties();

            if (module == null)
            {
                return;
            }

            EditorUtility.SetDirty(wave);

            string modulePath = AssetDatabase.GetAssetPath(module);
            string wavePath = AssetDatabase.GetAssetPath(wave);
            bool isSubAsset = !string.IsNullOrEmpty(modulePath) && modulePath == wavePath;

            if (isSubAsset)
            {
                AssetDatabase.RemoveObjectFromAsset(module);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(wavePath);
            }
            else
            {
                Object.DestroyImmediate(module, true);
                AssetDatabase.SaveAssets();
            }
        }
    }
}




