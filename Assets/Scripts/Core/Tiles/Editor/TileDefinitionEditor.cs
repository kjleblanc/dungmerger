using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MergeDungeon.Core.Editor
{
    [CustomEditor(typeof(TileDefinition))]
    public class TileDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty _modulesProperty;
        private readonly Dictionary<UnityEngine.Object, UnityEditor.Editor> _moduleEditors = new();
        private string _newModuleName = string.Empty;

        private void OnEnable()
        {
            _modulesProperty = serializedObject.FindProperty("modules");
        }

        private void OnDisable()
        {
            foreach (var entry in _moduleEditors)
            {
                if (entry.Value != null)
                {
                    DestroyImmediate(entry.Value);
                }
            }
            _moduleEditors.Clear();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "m_Script", "modules");

            EditorGUILayout.Space();
            DrawModulesSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawModulesSection()
        {
            EditorGUILayout.LabelField("Modules", EditorStyles.boldLabel);
            if (_modulesProperty == null)
            {
                EditorGUILayout.HelpBox("Modules property not found.", MessageType.Error);
                return;
            }

            if (_modulesProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No modules assigned. Add modules to define tile behaviour.", MessageType.Info);
            }

            EditorGUI.indentLevel++;
            for (int i = 0; i < _modulesProperty.arraySize; i++)
            {
                SerializedProperty element = _modulesProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(element, GUIContent.none);

                using (new EditorGUI.DisabledScope(i == 0))
                {
                    if (GUILayout.Button("Up", GUILayout.Width(40)))
                    {
                        _modulesProperty.MoveArrayElement(i, i - 1);
                    }
                }

                using (new EditorGUI.DisabledScope(i == _modulesProperty.arraySize - 1))
                {
                    if (GUILayout.Button("Down", GUILayout.Width(50)))
                    {
                        _modulesProperty.MoveArrayElement(i, i + 1);
                    }
                }

                if (GUILayout.Button("Ping", GUILayout.Width(45)))
                {
                    EditorGUIUtility.PingObject(element.objectReferenceValue);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    RemoveModule(i, element.objectReferenceValue);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                if (element.objectReferenceValue != null)
                {
                    DrawModuleInspector(element.objectReferenceValue);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }
            EditorGUI.indentLevel--;

            DrawAddModuleControls();
        }

        private void DrawModuleInspector(UnityEngine.Object module)
        {
            if (!_moduleEditors.TryGetValue(module, out var editor) || editor == null)
            {
                editor = CreateEditor(module);
                _moduleEditors[module] = editor;
            }

            if (editor != null)
            {
                EditorGUI.indentLevel++;
                editor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawAddModuleControls()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Module", GUILayout.Width(110)))
            {
                ShowAddModuleMenu();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ShowAddModuleMenu()
        {
            var menu = new GenericMenu();
            var moduleTypes = TypeCache.GetTypesDerivedFrom<TileModule>();
            bool added = false;
            foreach (var type in moduleTypes)
            {
                if (type.IsAbstract) continue;
                added = true;
                string niceName = ObjectNames.NicifyVariableName(type.Name);
                menu.AddItem(new GUIContent(niceName), false, () => CreateModule(type));
            }

            if (!added)
            {
                menu.AddDisabledItem(new GUIContent("No module types found"));
            }

            menu.ShowAsContext();
        }

        private void CreateModule(Type moduleType)
        {
            if (moduleType == null) return;
            var tileDef = (TileDefinition)target;
            string assetPath = AssetDatabase.GetAssetPath(tileDef);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("TileDefinition asset path not found. Please save the asset before adding modules.");
                return;
            }

            var module = ScriptableObject.CreateInstance(moduleType);
            module.name = GenerateModuleName(moduleType);

            AssetDatabase.AddObjectToAsset(module, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath);

            serializedObject.Update();
            _modulesProperty.arraySize++;
            SerializedProperty newElement = _modulesProperty.GetArrayElementAtIndex(_modulesProperty.arraySize - 1);
            newElement.objectReferenceValue = module;
            serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(module);
        }

        private string GenerateModuleName(Type moduleType)
        {
            var tileDef = (TileDefinition)target;
            return $"{tileDef.name}_{moduleType.Name}";
        }

        private void RemoveModule(int index, UnityEngine.Object module)
        {
            if (index < 0 || index >= _modulesProperty.arraySize)
            {
                return;
            }

            var tileDef = (TileDefinition)target;

            var element = _modulesProperty.GetArrayElementAtIndex(index);
            if (element != null)
            {
                element.objectReferenceValue = null;
            }

            serializedObject.ApplyModifiedProperties();
            _modulesProperty.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();

            if (module != null && _moduleEditors.TryGetValue(module, out var editor) && editor != null)
            {
                DestroyImmediate(editor);
                _moduleEditors.Remove(module);
            }

            if (module != null)
            {
                string modulePath = AssetDatabase.GetAssetPath(module);
                string tilePath = AssetDatabase.GetAssetPath(tileDef);
                bool isSubAsset = !string.IsNullOrEmpty(modulePath) && modulePath == tilePath;

                if (isSubAsset)
                {
                    AssetDatabase.RemoveObjectFromAsset(module);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(tilePath);
                }
                else
                {
                    DestroyImmediate(module, true);
                    AssetDatabase.SaveAssets();
                }
            }

            EditorUtility.SetDirty(tileDef);
        }
    }
}



