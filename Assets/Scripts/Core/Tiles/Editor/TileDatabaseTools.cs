using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MergeDungeon.Core.Editor
{
	public class TileDatabaseTools : EditorWindow
	{
		private TileDatabase _database;
		private Vector2 _scroll;

		[MenuItem("MergeDungeon/Tiles/Tile Database Tools")] 
		public static void Open()
		{
			var win = GetWindow<TileDatabaseTools>(true, "Tile Database Tools");
			win.minSize = new Vector2(420, 320);
			win.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Tile Database", EditorStyles.boldLabel);
			_database = (TileDatabase)EditorGUILayout.ObjectField("Asset", _database, typeof(TileDatabase), false);
			if (_database == null)
			{
				EditorGUILayout.HelpBox("Assign a TileDatabase asset.", MessageType.Info);
				return;
			}

			EditorGUILayout.Space();
			DrawAutoCollectSection();
			EditorGUILayout.Space();
			DrawRebuildSection();
			EditorGUILayout.Space();
			DrawValidateSection();
		}

		private void DrawAutoCollectSection()
		{
			EditorGUILayout.LabelField("Auto-Collect", EditorStyles.boldLabel);
			SerializedObject so = new SerializedObject(_database);
			var prop = so.FindProperty("editorSearchFolders");
			EditorGUILayout.PropertyField(prop, new GUIContent("Search Folders"), includeChildren: true);
			so.ApplyModifiedProperties();
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Add Selected Folder"))
			{
				string path = GetSelectedFolderPath();
				if (!string.IsNullOrEmpty(path))
				{
					prop.arraySize++;
					prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = path;
					so.ApplyModifiedProperties();
				}
			}
			if (GUILayout.Button("Collect Now"))
			{
				Undo.RecordObject(_database, "Collect Tile Definitions");
				_database.Rebuild();
				EditorUtility.SetDirty(_database);
			}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawRebuildSection()
		{
			EditorGUILayout.LabelField("Rebuild / Refresh", EditorStyles.boldLabel);
			if (GUILayout.Button("Rebuild Database"))
			{
				Undo.RecordObject(_database, "Rebuild Tile Database");
				_database.Rebuild();
				EditorUtility.SetDirty(_database);
			}
		}

		private void DrawValidateSection()
		{
			EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
			if (GUILayout.Button("Run Validation"))
			{
				Validate();
			}
			_scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(150));
			foreach (string msg in _messages)
			{
				EditorGUILayout.HelpBox(msg, MessageType.Info);
			}
			EditorGUILayout.EndScrollView();
		}

		private readonly List<string> _messages = new List<string>();
		private void Validate()
		{
			_messages.Clear();
			var ids = new HashSet<string>();
			var nulls = 0;
			foreach (var d in _database.All)
			{
				if (d == null) { nulls++; continue; }
				if (string.IsNullOrEmpty(d.Id))
				{
					_messages.Add($"Missing Id on definition: {d.name}");
				}
				else if (!ids.Add(d.Id))
				{
					_messages.Add($"Duplicate Id detected: {d.Id} ({d.name})");
				}
			}
			if (nulls > 0) _messages.Add($"Null entries in database list: {nulls}");
			if (_messages.Count == 0) _messages.Add("Validation passed: no issues found.");
		}

		private static string GetSelectedFolderPath()
		{
			string path = null;
			foreach (var obj in Selection.objects)
			{
				string p = AssetDatabase.GetAssetPath(obj);
				if (!string.IsNullOrEmpty(p) && AssetDatabase.IsValidFolder(p))
				{
					path = p;
					break;
				}
			}
			return path;
		}
	}
}


