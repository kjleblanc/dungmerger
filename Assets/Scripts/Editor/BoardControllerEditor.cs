using UnityEditor;
using UnityEngine;

namespace MergeDungeon.Core
{
    [CustomEditor(typeof(BoardController))]
    public class BoardControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var controller = (BoardController)target;
            if (GUILayout.Button("Rebuild Board"))
            {
                // Delay to next editor tick to avoid rebuild during inspector GUI events
                EditorApplication.delayCall += () =>
                {
                    if (controller == null) return;
                    controller.BuildBoard(controller.cellPrefab);
                    controller.RecomputeGridCellSize(force: true);
                };
            }
        }
    }
}
