using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNodyEditor
{
    using PuppyDragon.uNody;
    using PuppyDragon.uNody.Logic;
    using PuppyDragon.uNody.Variable;
    using Object = UnityEngine.Object;

    /// <summary> Base class to derive custom Node Graph editors from. Use this to override how graphs are drawn in the editor. </summary>
    [CustomNodeGraphEditor(typeof(NodeGraph))]
    public partial class NodeGraphEditor : Internal.NodeEditorBase<NodeGraphEditor, NodeGraphEditor.CustomNodeGraphEditorAttribute, uNody.NodeGraph>
    {
        private Rect[] rects;
        private NodeGraph drawTarget;
        private NodeGraphEditor drawGraphEditor;
        private List<NodeGraph> subGraphPath = new();

        private Editor blackboardEditor;
        private SerializedProperty blackboardProperty;

        private Dictionary<Node, Vector2> nodeSizes = new();        

        public Dictionary<Node, Vector2> NodeSizes => nodeSizes;

        public Vector2 PanOffset
        {
            get => target.PanOffset;
            set => target.PanOffset = value;
        }
        public float Zoom
        {
            get => target.Zoom;
            set => target.Zoom = Mathf.Clamp(value, NodeEditorPreferences.GetSettings(this).minZoom, NodeEditorPreferences.GetSettings(this).maxZoom);
        }

        public NodeGraph DrawTarget
        {
            get => drawTarget;
            set
            {
                if (drawTarget == value)
                    return;

                drawTarget = value == null ? target : value;
                drawGraphEditor = drawTarget != target ? GetEditor(drawTarget) : this;
                drawGraphEditor.Window = Window;

                subGraphPath.Clear();
            }
        }

        protected NodeGraphEditor DrawGraphEditor => drawGraphEditor;

        /// <summary> Are we currently renaming a node? </summary>
        protected bool isRenaming;

        public virtual void OnGUI()
        {
            blackboardProperty ??= serializedObject.FindProperty("blackboard");

            serializedObject.Update();

            if (DrawTarget == null)
                DrawTarget = target;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.Height(18));
                {
                    DrawNavigation(EditorStyles.toolbarButton);
                    GUILayout.FlexibleSpace();
                    DrawToolbarGUI();
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    DrawGraphEditor.DrawGrid();
                }
                GUILayout.EndVertical();

                if (blackboardProperty.isExpanded)
                {
                    GUILayout.BeginVertical(GUILayout.Width(300f));
                    EditorGUILayout.PropertyField(blackboardProperty);
                    if (blackboardProperty.objectReferenceValue != null)
                    {
                        GUILayout.Space(4);
                        Editor.CreateCachedEditor(blackboardProperty.objectReferenceValue, null, ref blackboardEditor);
                        blackboardEditor.OnInspectorGUI();
                    }
                    GUILayout.EndVertical();
                }
            }
            GUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        public void DrawNavigation(GUIStyle buttonStyle)
        {
            var parentGraph = DrawTarget.Parent;

            if (subGraphPath.Count == 0)
            {
                while (parentGraph != null)
                {
                    subGraphPath.Add(parentGraph);
                    parentGraph = parentGraph.Parent;
                }
            }

            for (int i = subGraphPath.Count - 1; i >= 0; i--)
            {
                parentGraph = subGraphPath[i];
                var buttonName = new GUIContent(parentGraph.name);
                var buttonSize = EditorStyles.toolbarButton.CalcSize(buttonName);
                if (GUILayout.Button(buttonName, buttonStyle, GUILayout.Width(buttonSize.x)))
                {
                    DrawTarget = parentGraph;
                    break;
                }
            }
        }

        private void DrawToolbarGUI()
        {
            OnToolbarGUI();

            if (GUILayout.Button("Blackboard", EditorStyles.toolbarButton))
                blackboardProperty.isExpanded = !blackboardProperty.isExpanded;
        }

        public virtual void OnToolbarGUI()
        {
        }

        /// <summary> Called when opened by NodeEditorWindow </summary>
        public virtual void OnEnable() { }

        /// <summary> Called when NodeEditorWindow gains focus </summary>
        public virtual void OnFocus() { }

        /// <summary> Called when NodeEditorWindow loses focus </summary>
        public virtual void OnFocusLost() { }

        public void SelectNode(Node node, bool isAdd)
        {
            if (isAdd)
            {
                var selections = new List<Object>(Selection.objects);
                selections.Add(node);
                Selection.objects = selections.ToArray();
            }
            else
                Selection.objects = new Object[] { node };
        }

        public void DeselectNode(Node node)
        {
            var selection = new List<Object>(Selection.objects);
            selection.Remove(node);
            Selection.objects = selection.ToArray();
        }

        public virtual Texture2D GetGridLargeLineTexture() {
            return NodeEditorPreferences.GetSettings(this).GridLargeLineTexture;
        }

        public virtual Texture2D GetGridSmallLineTexture() {
            return NodeEditorPreferences.GetSettings(this).GridSmallLineTexture;
        }

        /// <summary> Return default settings for this graph type. This is the settings the user will load if no previous settings have been saved. </summary>
        public virtual NodeEditorPreferences.Settings CreateDefaultPreferences()
            => new NodeEditorPreferences.Settings();

        /// <summary> Returns context node menu path. Null or empty strings for hidden nodes. </summary>
        public virtual string GetNodeMenuName(Type type)
        {
            //Check if type has the CreateNodeMenuAttribute
            if (NodeEditorUtilities.GetAttrib<Node.CreateNodeMenuAttribute>(type, out var attrib)) // Return custom path
                return attrib.menuName;
            else // Return generated path
                return NodeEditorUtilities.NodeDefaultPath(type);
        }

        /// <summary> The order by which the menu items are displayed. </summary>
        public virtual int GetNodeMenuOrder(Type type) {
            //Check if type has the CreateNodeMenuAttribute
            if (NodeEditorUtilities.GetAttrib<Node.CreateNodeMenuAttribute>(type, out var attrib)) // Return custom path
                return attrib.order;
            else
                return 0;
        }

        public virtual bool IsForceAddNodeMenu(Type type)
        {
            if (NodeEditorUtilities.GetAttrib<Node.CreateNodeMenuAttribute>(type, out var attrib)) // Return custom path
                return attrib.isForce;
            else
                return false;
        }

        /// <summary>
        /// Called before connecting two ports in the graph view to see if the output port is compatible with the input port
        /// </summary>
        public virtual bool CanConnect(uNody.NodePort output, uNody.NodePort input)
            => output.CanConnectTo(input);

        /// <summary>
        /// Add items for the context menu when right-clicking this node.
        /// Override to add custom menu items.
        /// </summary>
        /// <param name="menu"></param>
        /// <param name="compatibleType">Use it to filter only nodes with ports value type, compatible with this type</param>
        /// <param name="direction">Direction of the compatiblity</param>
        public virtual void AddContextMenuItems(AdvancedGenericMenu menu, Type compatibleType = null, uNody.NodePort.IO direction = uNody.NodePort.IO.Input)
        {
            EditorGUIUtility.SetIconSize(new Vector2(32f, 32f));
            Vector2 nodePosition = WindowToGridPosition(Event.current.mousePosition);

            var targetType = target.GetType();
            var filterTypes = typeof(LogicGraph).IsAssignableFrom(targetType) ? NodeEditorReflection.NodeTypes : NodeEditorReflection.NodeTypesWithoutLogic;

            List<Type> nodeTypes;
            if (compatibleType != null && NodeEditorPreferences.GetSettings(this).createFilter)
                nodeTypes = NodeEditorUtilities.GetCompatibleNodesTypes(filterTypes, compatibleType, direction).OrderBy(GetNodeMenuOrder).ToList();
            else
                nodeTypes = filterTypes.OrderBy(GetNodeMenuOrder).ToList();

            List<Type> nodyTypes = new List<Type>();
            List<Type> customTypes = new List<Type>();
            for (int i = 0; i < nodeTypes.Count; i++)
            {
                var type = nodeTypes[i];
                if (!string.IsNullOrEmpty(type.Namespace) && type.Namespace.StartsWith("PuppyDragon"))
                    nodyTypes.Add(type);
                else
                    customTypes.Add(type);
            }

            if (customTypes.Count > 0)
            {
                AddContextMenuItem(menu, customTypes, nodePosition);
                menu.AddSeparator("");
            }

            AddContextMenuItem(menu, nodeTypes, nodePosition);
            
            menu.AddSeparator("");

            if (copyBuffer != null && copyBuffer.Length > 0)
                menu.AddItem(new GUIContent("Paste"), false, () => PasteNodes(nodePosition));
            else
                menu.AddDisabledItem(new GUIContent("Paste"));
            menu.AddItem(new GUIContent("Preferences"), false, () => NodeEditorReflection.OpenPreferences());
            menu.AddCustomContextMenuItems(target);
        }

        private void AddContextMenuItem(AdvancedGenericMenu menu, IReadOnlyList<Type> types, Vector2 nodePosition)
        {
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];

                string path = null;
                if (IsForceAddNodeMenu(type))
                {
                    //Check if type has the CreateNodeMenuAttribute
                    if (NodeEditorUtilities.GetAttrib<Node.CreateNodeMenuAttribute>(type, out var attrib) && !string.IsNullOrEmpty(attrib.menuName)) // Return custom path
                        path = attrib.menuName;
                    else // Return generated path
                        path = NodeEditorUtilities.NodeDefaultPath(type);
                }
                else
                {
                    //Get node context menu path
                    path = GetNodeMenuName(type);
                    if (path == null)
                        continue;
                }

                path = path.Replace("Puppy Dragon/u Nody/", "");

                // Check if user is allowed to add more of given node type
                Node.DisallowMultipleNodesAttribute disallowAttrib;
                bool disallowed = false;
                if (NodeEditorUtilities.GetAttrib(type, out disallowAttrib))
                {
                    int typeCount = target.Nodes.Count(x => x.GetType() == type);
                    if (typeCount >= disallowAttrib.max)
                        disallowed = true;
                }


                type.TryGetAttributeNodeIcon(out var icon);
                // Add node entry to context menu
                if (!disallowed)
                {
                    menu.AddItem(new GUIContent(path, icon), false, () =>
                    {
                        var node = CreateNode(type, nodePosition);
                        if (node != null)
                            AutoConnect(node); // handle null nodes to avoid nullref exceptions
                    });
                }
            }
        }

        /// <summary>
        /// The returned Style is used to configure the paddings and icon texture of the ports.
        /// Use these properties to customize your port style.
        ///
        /// The properties used is:
        /// <see cref="GUIStyle.padding"/>[Left and Right], <see cref="GUIStyle.normal"/> [Background] = border texture,
        /// and <seealso cref="GUIStyle.active"/> [Background] = dot texture;
        /// </summary>
        /// <param name="port">the owner of the style</param>
        /// <returns></returns>
        public virtual GUIStyle GetPortStyle(NodePort port)
        {
            var portFieldInfo = port.OwnerNode.GetType().GetFieldInfo(port.FieldName);
            var attribute = portFieldInfo.GetCustomAttribute<Node.ArrowPortAttribute>(true);
            if (port.Direction == NodePort.IO.Input)
                return attribute != null ? NodeEditorStyles.InputArrowPort : NodeEditorStyles.InputDotPort;
            else
                return attribute != null ? NodeEditorStyles.OutputArrowPort : NodeEditorStyles.OutputDotPort;
        }

        /// <summary> Returned color is used to color ports </summary>
        public virtual Color GetPortFilledColor(NodePort port)
            => GetTypeColor(port.ValueType);

        /// <summary> The returned color is used to color the background of the door.
        /// Usually used for outer edge effect </summary>
        public virtual Color GetPortEmptyColor(NodePort port)
            => GetTypeColor(port.ValueType);

        /// <summary> Returns generated color for a type. This color is editable in preferences </summary>
        public virtual Color GetTypeColor(Type type)
            => NodeEditorPreferences.GetTypeColor(this, type);

        /// <summary> Override to display custom tooltips </summary>
        public virtual string GetPortTooltip(NodePort port)
        {
            var portType = port.ValueType;
            string tooltip = portType.PrettyName();
            tooltip += " = ";

            var objects = port.DynamicValues;
            if (objects.Count() == 1)
            {
                object obj = objects.First();
                tooltip += (obj != null ? obj.ToString() : "null");
            }
            else
            {
                tooltip += "[";
                tooltip += string.Join(',', objects);
                tooltip += "]";
            }

            return tooltip;
        }

        /// <summary> Deal with objects dropped into the graph through DragAndDrop </summary>
        public virtual void OnDropObjects(Object[] objects)
        {
        }

        /// <summary> Create a node and save it in the graph asset </summary>
        public virtual Node CreateNode(Type type, Vector2 position) {
            Undo.RecordObject(target, "Create Node");

            Node node = target.AddNode(type);
            SetDirtyFlagParents();
            
            // handle null nodes to avoid nullref exceptions
            if (node == null)
                return null;

            Undo.RegisterCreatedObjectUndo(node, "Create Node");

            node.NodePosition = position;

            if (node.name == null || node.name.Trim() == "")
                node.name = NodeEditorUtilities.NodeDefaultName(type);

            var root = target.Root ?? target;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(root)))
                AssetDatabase.AddObjectToAsset(node, root);

            if (NodeEditorPreferences.GetSettings(this).autoSave)
                AssetDatabase.SaveAssets();

            return node;
        }

        /// <summary> Creates a copy of the original node in the graph </summary>
        public virtual uNody.Node CopyNode(uNody.Node original) {
            Undo.RecordObject(target, "Duplicate Node");
            uNody.Node node = target.CopyNode(original);
            Undo.RegisterCreatedObjectUndo(node, "Duplicate Node");
            node.name = original.name;

            (new SerializedObject(node)).ApplyModifiedProperties();

            var root = target.Root ?? target;
            if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(root)))
                AssetDatabase.AddObjectToAsset(node, root);

            if (node is SubGraphNode subGraphNode)
            {
                foreach (var nodeInSubGraph in subGraphNode.SubGraph.Nodes)
                    AssetDatabase.AddObjectToAsset(nodeInSubGraph, root);
            }

            if (NodeEditorPreferences.GetSettings(this).autoSave)
                AssetDatabase.SaveAssets();

            return node;
        }

        /// <summary> Return false for nodes that can't be removed </summary>
        public virtual bool CanRemove(uNody.Node node)
        {
            // Check graph attributes to see if this node is required
            var graphType = target.GetType();
            uNody.NodeGraph.RequireNodeAttribute[] attribs = Array.ConvertAll(
                graphType.GetCustomAttributes(typeof(uNody.NodeGraph.RequireNodeAttribute), true), x => x as uNody.NodeGraph.RequireNodeAttribute);
            if (attribs.Any(x => x.Requires(node.GetType())))
            {
                if (target.Nodes.Count(x => x.GetType() == node.GetType()) <= 1)
                    return false;
            }
            return true;
        }

        /// <summary> Safely remove a node and all its connections. </summary>
        public virtual bool RemoveNode(Node node)
        {
            if (!CanRemove(node))
                return false;

            // Remove the node
            Undo.RecordObject(node, "Delete Node");
            Undo.RecordObject(target, "Delete Node");

            foreach (var port in node.Ports)
                foreach (var connection in port.Connections)
                    Undo.RecordObject(connection.Node, "Delete Node");

            target.RemoveNode(node);
            SetDirtyFlagParents();

            Undo.DestroyObjectImmediate(node);

            if (NodeEditorPreferences.GetSettings(this).autoSave)
                AssetDatabase.SaveAssets();

            return true;
        }

        private void SetDirtyFlagParents()
        {
            var parentGraph = target.Parent;
            while (parentGraph != null)
            {
                EditorUtility.SetDirty(parentGraph);
                parentGraph = parentGraph.Parent;
            }
        }

        public void Save()
        {
            if (AssetDatabase.Contains(target))
            {
                EditorUtility.SetDirty(target);
                if (NodeEditorPreferences.GetSettings(this).autoSave)
                    AssetDatabase.SaveAssets();
            }
            else
                SaveAs();
        }

        public void SaveAs()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save NodeGraph", "NewNodeGraph", "asset", "");
            if (string.IsNullOrEmpty(path))
                return;
            else
            {
                uNody.NodeGraph existingGraph = AssetDatabase.LoadAssetAtPath<uNody.NodeGraph>(path);
                if (existingGraph != null)
                    AssetDatabase.DeleteAsset(path);

                AssetDatabase.CreateAsset(target, path);
                EditorUtility.SetDirty(target);

                if (NodeEditorPreferences.GetSettings(this).autoSave)
                    AssetDatabase.SaveAssets();
            }
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class CustomNodeGraphEditorAttribute : Attribute, INodeEditorAttrib {
            public Type inspectedType;
            public bool isRegisterToPreferences;
            /// <summary> Tells a NodeGraphEditor which Graph type it is an editor for </summary>
            /// <param name="inspectedType">Type that this editor can edit</param>
            /// <param name="editorPrefsKey">Define unique key for unique layout settings instance</param>
            public CustomNodeGraphEditorAttribute(Type inspectedType, bool isRegisterToPreferences = false) {
                this.inspectedType = inspectedType;
                this.isRegisterToPreferences = isRegisterToPreferences;
            }

            public Type GetInspectedType() {
                return inspectedType;
            }
        }
    }
}