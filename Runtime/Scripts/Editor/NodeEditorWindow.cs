using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System;
using PuppyDragon.uNody;

namespace PuppyDragon.uNodyEditor
{
    public class NodeEditorWindow : EditorWindow
    {
        [SerializeField]
        private NodeGraph viewingGraph;

        private NodeGraphEditor viewingGraphEditor;

        private void OnDestroy()
        {
            if (viewingGraphEditor != null)
                viewingGraphEditor.DrawTarget = null;
        }

        private void OnFocus()
        {
            wantsMouseMove = true;

            if (viewingGraphEditor != null)
            {
                viewingGraphEditor.OnFocus();
                if (NodeEditorPreferences.GetSettings(viewingGraph).autoSave)
                    AssetDatabase.SaveAssets();
            }
        }

        private void OnLostFocus()
        {
            if (viewingGraphEditor != null)
                viewingGraphEditor.OnFocusLost();
        }

        private void OnGUI()
        {
            ValidateGraphEditor();

            if (viewingGraphEditor != null)
            {
                viewingGraphEditor.OnGUI();

                if (string.IsNullOrEmpty(titleContent.text))
                    titleContent.text = viewingGraph.name;
            }
        }

        [InitializeOnLoadMethod]
        private static void OnLoad()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        /// <summary> Handle Selection Change events</summary>
        private static void OnSelectionChanged()
        {
            NodeGraph nodeGraph = Selection.activeObject as NodeGraph;
            if (nodeGraph && !AssetDatabase.Contains(nodeGraph))
            {
                if (NodeEditorPreferences.GetSettings().openOnCreate)
                    Open(nodeGraph);
            }
        }

        public void Home()
        {
            viewingGraphEditor.Home();
        }

        /// <summary> Make sure the graph editor is assigned and to the right object </summary>
        private void ValidateGraphEditor()
        {
            NodeGraphEditor graphEditor = NodeGraphEditor.GetEditor(viewingGraph);
            if (viewingGraphEditor != graphEditor && graphEditor != null)
            {
                graphEditor.Window = this;

                viewingGraphEditor = graphEditor;
                viewingGraphEditor.OnEnable();
            }

            if (viewingGraph == null)
            {
                viewingGraphEditor = null;
                Close();
            }
        }

        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line)
        {
            NodeGraph nodeGraph = EditorUtility.InstanceIDToObject(instanceID) as NodeGraph;
            if (nodeGraph != null)
            {
                Open(nodeGraph);
                return true;
            }
            return false;
        }

        /// <summary>Open the provided graph in the NodeEditor</summary>
        public static NodeEditorWindow Open(NodeGraph graph)
        {
            if (!graph)
                return null;

            var window = GetWindow(typeof(NodeEditorWindow), false, graph.name, true) as NodeEditorWindow;
            window.wantsMouseMove = true;
            window.viewingGraph = graph;
            window.ValidateGraphEditor();

            return window;
        }
    }
}
