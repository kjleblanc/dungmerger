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
                controller.BuildBoard(controller.cellPrefab);
                controller.RecomputeGridCellSize(force: true);
            }
        }
    }
}
