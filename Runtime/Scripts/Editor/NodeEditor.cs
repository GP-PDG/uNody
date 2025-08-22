using PuppyDragon.uNody;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
#endif

namespace PuppyDragon.uNodyEditor {
    /// <summary> Base class to derive custom Node editors from. Use this to create your own custom inspectors and editors for your nodes. </summary>
    [CustomNodeEditor(typeof(uNody.Node))]
    public class NodeEditor : Internal.NodeEditorBase<NodeEditor, NodeEditor.CustomNodeEditorAttribute, uNody.Node> {

        /// <summary> Fires every whenever a node was modified through the editor </summary>
        public readonly static Dictionary<uNody.NodePort, Vector2> portPositions = new();

        public static Action<uNody.Node> onUpdateNode;
        public static Node currentDrawingTarget;

#if ODIN_INSPECTOR
        protected internal static bool inNodeEditor = false;
#endif

        public virtual void OnHeaderGUI()
        {
            NodeEditorReflection.TryGetAttributeNodeIcon(target.GetType(), out var icon);
            if (icon != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(-14f);
                EditorGUIUtility.SetIconSize(new Vector2(18f, 18f));
                GUILayout.Label(new GUIContent(target.name, icon), NodeEditorStyles.NodeHeaderLabel, GUILayout.Height(24));
                EditorGUIUtility.SetIconSize(Vector2.zero);
                GUILayout.EndHorizontal();
            }
            else
                GUILayout.Label(new GUIContent(target.name, icon), NodeEditorStyles.NodeHeaderLabel, GUILayout.Height(24));
        }

        /// <summary> Draws standard field editors for all public fields </summary>
        public virtual void OnBodyGUI()
        {
#if ODIN_INSPECTOR
            inNodeEditor = true;
#endif
            // Unity specifically requires this to save/update any serial object.
            // serializedObject.Update(); must go at the start of an inspector gui, and
            // serializedObject.ApplyModifiedProperties(); goes at the end.
            serializedObject.Update();
            string[] excludes = { "m_Script" };

#if ODIN_INSPECTOR
            try
            {
#if ODIN_INSPECTOR_3
                objectTree.BeginDraw( true );
#else
                InspectorUtilities.BeginDrawPropertyTree(objectTree, true);
#endif
            }
            catch ( ArgumentNullException )
            {
#if ODIN_INSPECTOR_3
                objectTree.EndDraw();
#else
                InspectorUtilities.EndDrawPropertyTree(objectTree);
#endif
                NodeEditor.DestroyEditor(this.target);
                return;
            }

            GUIHelper.PushLabelWidth( 84 );
            objectTree.Draw( true );
#if ODIN_INSPECTOR_3
            objectTree.EndDraw();
#else
            InspectorUtilities.EndDrawPropertyTree(objectTree);
#endif
            GUIHelper.PopLabelWidth();
#else

            // Iterate through serialized properties and draw them like the Inspector (But with ports)
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (excludes.Contains(iterator.name))
                    continue;
                EditorGUILayout.PropertyField(iterator, true);
            }
#endif
            serializedObject.ApplyModifiedProperties();

#if ODIN_INSPECTOR
            // Call repaint so that the graph window elements respond properly to layout changes coming from Odin
            if (GUIHelper.RepaintRequested) {
                GUIHelper.ClearRepaintRequest();
                window.Repaint();
            }
#endif

#if ODIN_INSPECTOR
            inNodeEditor = false;
#endif
        }

        public virtual int GetWidth()
        {
            Type type = target.GetType();
            return type.TryGetAttributeWidth(out var width) ? width : (int)Node.NodeSize.Medium;
        }

        public virtual Color GetHeaderTint()
        {
            var type = target.GetType();
            if (!type.TryGetAttributeHeaderTint(target, out var color))
                color = GetBodyTint();

            color *= 0.75f;
            color.a = 1f;
            return color;
        }

        /// <summary> Returns color for target node </summary>
        public virtual Color GetBodyTint()
        {
            // Try get color from [NodeTint] attribute
            var type = target.GetType();
            return type.TryGetAttributeBodyTint(out var color) ?
                color : NodeEditorPreferences.GetSettings(NodeGraphEditor.GetEditor(target.Graph)).tintColor;
        }

        public virtual Color GetFooterTint()
        {
            var type = target.GetType();
            if (!type.TryGetAttributeFooterTint(target, out var color))
                color = GetHeaderTint();
            else
            {
                color *= 0.75f;
                color.a = 1f;
            }
            return color;
        }

        public virtual GUIStyle GetHeaderStyle()
            => NodeEditorStyles.NodeHeader;

        public virtual GUIStyle GetBodyStyle()
            => NodeEditorStyles.NodeBody;

        public virtual GUIStyle GetFooterStyle()
            => NodeEditorStyles.NodeFooter;
        
        public virtual GUIStyle GetBodyHighlightStyle()
            => NodeEditorStyles.NodeHighlight;

        /// <summary> Override to display custom node header tooltips </summary>
        public virtual string GetHeaderTooltip()
            => null;

        /// <summary> Add items for the context menu when right-clicking this node. Override to add custom menu items. </summary>
        public virtual void AddContextMenuItems(AdvancedGenericMenu menu)
        {
            bool canRemove = true;
            var graphEditor = NodeGraphEditor.GetEditor(target.Graph);

            Node node = null;
            // Actions if only one node is selected
            if (Selection.count == 1 && Selection.objects[0] is Node)
            {
                node = Selection.objects[0] as Node;
                menu.AddItem(new GUIContent("Open Script"), false, () => {
                    var iterator = new SerializedObject(node).GetIterator();
                    iterator.NextVisible(true);
                    AssetDatabase.OpenAsset(iterator.objectReferenceInstanceIDValue, 0);
                    });

                menu.AddItem(new GUIContent("Rename"), false, graphEditor.RenameSelectedNode);
                
                canRemove = NodeGraphEditor.GetEditor(node.Graph).CanRemove(node);
            }

            // Add actions to any number of selected nodes
            //if (NodeSelection.Selection.GetType() != typeof(SubGraphNode))
            //{
            //    menu.AddItem(new GUIContent("Copy"), false, graphEditor.CopySelectedNodes);
            //    menu.AddItem(new GUIContent("Duplicate"), false, graphEditor.DuplicateSelectedNodes);
            //}

            menu.AddItem(new GUIContent("Copy"), false, graphEditor.CopySelectedNodes);
            menu.AddItem(new GUIContent("Duplicate"), false, graphEditor.DuplicateSelectedNodes);

            if (canRemove)
                menu.AddItem(new GUIContent("Remove"), false, graphEditor.RemoveSelectedNodes);
            else
                menu.AddItem(new GUIContent("Remove"), false, null);

            // Custom sctions if only one node is selected
            if (node != null)
                menu.AddCustomContextMenuItems(node);
        }

        /// <summary> Rename the node asset. This will trigger a reimport of the node. </summary>
        public void Rename(string newName)
        {
            if (newName == null || newName.Trim() == "")
                newName = NodeEditorUtilities.NodeDefaultName(target.GetType());

            target.name = newName;
            OnRename();

            EditorUtility.SetDirty(target);

            string assetPath = AssetDatabase.GetAssetPath(target);
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            EditorUtility.SetDirty(mainAsset);
            AssetDatabase.SaveAssetIfDirty(mainAsset);
        }

        /// <summary> Called after this node's name has changed. </summary>
        public virtual void OnRename() { }

        [AttributeUsage(AttributeTargets.Class)]
        public class CustomNodeEditorAttribute : Attribute, INodeEditorAttrib
        {
            private Type inspectedType;

            /// <summary> Tells a NodeEditor which Node type it is an editor for </summary>
            /// <param name="inspectedType">Type that this editor can edit</param>
            public CustomNodeEditorAttribute(Type inspectedType)
            {
                this.inspectedType = inspectedType;
            }

            public Type GetInspectedType()
                => inspectedType;
        }
    }
}