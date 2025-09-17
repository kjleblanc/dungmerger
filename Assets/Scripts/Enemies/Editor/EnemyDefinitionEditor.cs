using UnityEditor;
using UnityEngine;

namespace MergeDungeon.Core.Editor
{
    [CustomEditor(typeof(EnemyDefinition))]
    public class EnemyDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _lootModuleProperty;
        private UnityEditor.Editor _lootModuleEditor;

        private void OnEnable()
        {
            _lootModuleProperty = serializedObject.FindProperty("lootModule");
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

            DrawPropertiesExcluding(serializedObject, "m_Script", "lootModule");

            EditorGUILayout.Space();
            DrawLootModuleSection();

            serializedObject.ApplyModifiedProperties();
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
