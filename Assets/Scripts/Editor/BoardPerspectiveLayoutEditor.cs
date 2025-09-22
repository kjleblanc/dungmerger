using UnityEditor;
using UnityEngine;

namespace MergeDungeon.Core.Editor
{
    [CustomEditor(typeof(BoardPerspectiveLayout))]
    [CanEditMultipleObjects]
    public class BoardPerspectiveLayoutEditor : UnityEditor.Editor

    {
        private SerializedProperty _tiltProp;
        private SerializedProperty _rowDepthProp;
        private SerializedProperty _scaleCurveProp;
        private SerializedProperty _skewProp;
        private SerializedProperty _rowZProp;

        private void OnEnable()
        {
            _tiltProp = serializedObject.FindProperty("tiltAngle");
            _rowDepthProp = serializedObject.FindProperty("rowDepthOffset");
            _scaleCurveProp = serializedObject.FindProperty("perRowScale");
            _skewProp = serializedObject.FindProperty("horizontalSkew");
            _rowZProp = serializedObject.FindProperty("rowZOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Perspective Controls", EditorStyles.boldLabel);

            DrawTiltSlider();
            DrawRowDepthSlider();
            DrawHorizontalSkew();
            EditorGUILayout.PropertyField(_scaleCurveProp, new GUIContent("Per Row Scale"));
            DrawRowZOffset();

            EditorGUILayout.Space();
            if (GUILayout.Button("Preview Perspective"))
            {
                ApplyLayoutsDelayed();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                ApplyLayoutsDelayed();
            }
        }

        private void DrawTiltSlider()
        {
            float value = _tiltProp.floatValue;
            value = EditorGUILayout.Slider(new GUIContent("Tilt Angle (deg)"), value, -80f, 80f);
            _tiltProp.floatValue = value;
        }

        private void DrawRowDepthSlider()
        {
            float value = _rowDepthProp.floatValue;
            value = EditorGUILayout.Slider(new GUIContent("Row Depth Offset"), value, -400f, 400f);
            _rowDepthProp.floatValue = value;
        }

        private void DrawHorizontalSkew()
        {
            float value = _skewProp.floatValue;
            value = EditorGUILayout.Slider(new GUIContent("Horizontal Skew"), value, -400f, 400f);
            _skewProp.floatValue = value;
        }

        private void DrawRowZOffset()
        {
            float value = _rowZProp.floatValue;
            value = EditorGUILayout.Slider(new GUIContent("Row Z Offset"), value, -400f, 400f);
            _rowZProp.floatValue = value;
        }

        private void ApplyLayoutsDelayed()
        {
#if UNITY_EDITOR
            EditorApplication.delayCall += ApplyLayouts;
#else
            ApplyLayouts();
#endif
        }

        private void ApplyLayouts()
        {
            foreach (var targetObj in targets)
            {
                if (targetObj is BoardPerspectiveLayout layout && layout != null)
                {
                    layout.ApplyLayout();
                }
            }
        }
    }
}
