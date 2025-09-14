using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MergeDungeon.EditorTools
{
    public static class SceneHierarchyMarkdownExporter
    {
        [MenuItem("Tools/Export/Scene Hierarchy to Markdown")] 
        public static void ExportActiveSceneHierarchyToMarkdown()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded)
            {
                EditorUtility.DisplayDialog("Export Scene Hierarchy", "No active scene is loaded.", "OK");
                return;
            }

            // Ensure scene is saved to reflect current state if user wants to
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            string dir = "Assets/SceneExports";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_Scene_{Sanitize(scene.name)}_Hierarchy.md";
            string path = Path.Combine(dir, fileName);

            int goCount = 0;
            int scriptCount = 0;
            int missingScriptCount = 0;

            var sb = new StringBuilder(64 * 1024);
            sb.AppendLine($"# Scene: {scene.name}");
            sb.AppendLine();
            sb.AppendLine($"- Unity: {Application.unityVersion}");
            sb.AppendLine($"- Exported: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            foreach (var root in scene.GetRootGameObjects())
            {
                AppendGameObject(root, 0, sb, ref goCount, ref scriptCount, ref missingScriptCount);
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine($"Total GameObjects: {goCount}");
            sb.AppendLine($"Total Scripts: {scriptCount}");
            sb.AppendLine($"Missing Scripts: {missingScriptCount}");

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            AssetDatabase.Refresh();
            Debug.Log($"Scene hierarchy exported to: {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static void AppendGameObject(GameObject go, int depth, StringBuilder sb, ref int goCount, ref int scriptCount, ref int missingScriptCount)
        {
            goCount++;
            string indent = new string(' ', depth * 2);

            // GameObject line
            string active = go.activeInHierarchy ? "true" : "false";
            sb.Append(indent).Append("- ").Append(go.name).Append(" (active: ").Append(active).Append(")").AppendLine();

            // Scripts (MonoBehaviours)
            var monos = go.GetComponents<MonoBehaviour>();
            if (monos != null && monos.Length > 0)
            {
                sb.Append(indent).Append("  - Scripts:").AppendLine();
                foreach (var m in monos)
                {
                    if (m == null)
                    {
                        missingScriptCount++;
                        sb.Append(indent).Append("    - [Missing Script]").AppendLine();
                        continue;
                    }
                    scriptCount++;
                    var ms = MonoScript.FromMonoBehaviour(m);
                    string scriptPath = ms != null ? AssetDatabase.GetAssetPath(ms) : string.Empty;
                    string typeName = m.GetType().FullName;
                    string status = (m is Behaviour b && !b.enabled) ? " (disabled)" : string.Empty;
                    if (!string.IsNullOrEmpty(scriptPath))
                        sb.Append(indent).Append("    - ").Append(typeName).Append(status).Append(" — ").Append(scriptPath).AppendLine();
                    else
                        sb.Append(indent).Append("    - ").Append(typeName).Append(status).AppendLine();
                }
            }

            // Children
            var t = go.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                AppendGameObject(t.GetChild(i).gameObject, depth + 1, sb, ref goCount, ref scriptCount, ref missingScriptCount);
            }
        }

        private static string Sanitize(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}

