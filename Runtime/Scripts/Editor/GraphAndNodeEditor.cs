﻿using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
#endif

namespace PuppyDragon.uNodyEditor {
    /// <summary> Override graph inspector to show an 'Open Graph' button at the top </summary>
    [CustomEditor(typeof(uNody.NodeGraph), true)]
#if ODIN_INSPECTOR
    public class GlobalGraphEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Edit graph", GUILayout.Height(40)))
                NodeEditorWindow.Open(serializedObject.targetObject as PuppyDragon.uNody.NodeGraph);

            base.OnInspectorGUI();
        }
    }
#else
    [CanEditMultipleObjects]
    public class GlobalGraphEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (GUILayout.Button("Edit graph", GUILayout.Height(40)))
                NodeEditorWindow.Open(serializedObject.targetObject as PuppyDragon.uNody.NodeGraph);

            GUILayout.Space(EditorGUIUtility.singleLineHeight);
            GUILayout.Label("Raw data", "BoldLabel");

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif

    [CustomEditor(typeof(uNody.Node), true)]
#if ODIN_INSPECTOR
    public class GlobalNodeEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Edit graph", GUILayout.Height(40)))
            {
                SerializedProperty graphProp = serializedObject.FindProperty("graph");
                NodeEditorWindow w = NodeEditorWindow.Open(graphProp.objectReferenceValue as PuppyDragon.uNody.NodeGraph);
                w.Home(); // Focus selected node
            }
            base.OnInspectorGUI();
        }
    }
#else
    [CanEditMultipleObjects]
    public class GlobalNodeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUILayout.BeginHorizontal();
            //GUILayout.Space(6);
            GUILayout.BeginVertical();

            if (GUILayout.Button("Open Graph", GUILayout.Height(30)))
            {
                SerializedProperty graphProp = serializedObject.FindProperty("graph");
                NodeEditorWindow w = NodeEditorWindow.Open(graphProp.objectReferenceValue as uNody.NodeGraph);
                w.Home(); // Focus selected node
            }

            NodePortDrawer.IsNeedUpdatePosition = false;
            DrawDefaultInspector();
            NodePortDrawer.IsNeedUpdatePosition = true;

            GUILayout.EndVertical();
            GUILayout.Space(18);
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}