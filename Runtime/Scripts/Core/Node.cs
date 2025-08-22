using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace PuppyDragon.uNody
{
    [Serializable]
    public abstract class Node : ScriptableObject, ISerializationCallbackReceiver
    {
        public enum NodeSize { Mini = 100,  Small = 132, Medium = 180, Large = 228, Big = 260 }

        /// <summary> Used by <see cref="InputAttribute"/> and <see cref="OutputAttribute"/> to determine when to display the field value associated with a <see cref="NodePort"/> </summary>
        public enum ShowBackingValue
        {
            /// <summary> Never show the backing value </summary>
            Never,
            /// <summary> Show the backing value only when the port does not have any active connections </summary>
            Unconnected,
            /// <summary> Always show the backing value </summary>
            Always
        }

        public enum ConnectionType
        {
            /// <summary> Allow multiple connections</summary>
            Multiple,
            /// <summary> always override the current connection </summary>
            Override,
        }

        /// <summary> Tells which types of input to allow </summary>
        public enum TypeConstraint
        {
            /// <summary> Allow all types of input</summary>
            None,
            /// <summary> Allow connections where input value type is assignable from output value type (eg. ScriptableObject --> Object)</summary>
            Inherited,
            /// <summary> Allow only similar types </summary>
            Strict,
            /// <summary> Allow connections where output value type is assignable from input value type (eg. Object --> ScriptableObject)</summary>
            InheritedInverse,
            /// <summary> Allow connections where output value type is assignable from input value or input value type is assignable from output value type</summary>
            InheritedAny
        }

        public enum PortStyle
        {
            Dot,
            Arrow
        }

        /// <summary> Used during node instantiation to fix null/misconfigured graph during OnEnable/Init. Set it before instantiating a node. Will automatically be unset during OnEnable </summary>
        public static NodeGraph graphHotfix;

        /// <summary> Parent <see cref="NodeGraph"/> </summary>
        [SerializeField, HideInInspector]
        private NodeGraph graph;
        /// <summary> Position on the <see cref="NodeGraph"/> </summary>
        [SerializeField, HideInInspector]
        private Vector2 nodePosition;

        /// <summary> It is recommended not to modify these at hand. Instead, see <see cref="InputAttribute"/> and <see cref="OutputAttribute"/> </summary>
        private Dictionary<string, NodePort> portsByField;
        private Dictionary<string, NodePort[]> arrayPortsByField;

        private Dictionary<string, NodePort> PortsByField
        {
            get
            {
                if (portsByField == null)
                    CollectPorts();

                return portsByField;
            }
        }

        private Dictionary<string, NodePort[]> ArrayPortsByField
        {
            get
            {
                if (arrayPortsByField == null)
                    CollectPorts();

                return arrayPortsByField;
            }
        }

        public IEnumerable<NodePort> Ports => PortsByField.Values;
        public IEnumerable<NodePort> Inputs => PortsByField.Values.Where(x => x.Direction == NodePort.IO.Input);
        public IEnumerable<NodePort> Outputs => PortsByField.Values.Where(x => x.Direction == NodePort.IO.Output);

        public bool HasArrayPort => ArrayPortsByField.Count > 0;

        public NodeGraph Graph => graph;

        public Vector2 NodePosition { get => nodePosition; set => nodePosition = value; }

        protected void OnEnable()
        {
            if (graphHotfix != null)
                graph = graphHotfix;

            graphHotfix = null;
            CollectPorts();
            Initialize();
        }

        /// <summary> Initialize node. Called on enable. </summary>
        protected virtual void Initialize() { }

        /// <summary> Checks all connections for invalid references, and removes them. </summary>
        public void VerifyConnections()
        {
            foreach (NodePort port in PortsByField.Values)
                port.VerifyConnections();
        }

#region Ports
        public void CollectPorts()
        {
            portsByField ??= new();
            arrayPortsByField ??= new();

            NodeDataCache.FillPorts(this, portsByField, arrayPortsByField);

            VerifyConnections();
        }

        /// <summary> Returns port which matches fieldName </summary>
        public NodePort GetPort(string fieldName)
            => PortsByField.TryGetValue(fieldName, out var port) ? port : null;

        public NodePort GetPort(string fieldName, int index)
            => PortsByField.TryGetValue(fieldName + "." + index, out var port) ? port : null;

        public NodePort GetPortFromGUID(string guid)
            => PortsByField.Values.FirstOrDefault(x => x.GUID == guid);

        public IEnumerable<NodePort> GetPorts(string fieldName)
            => ArrayPortsByField.TryGetValue(fieldName, out var ports) ? ports.AsEnumerable() : null;
 
        public bool Contains(string fieldName)
            => PortsByField.ContainsKey(fieldName);

        public object GetValue(string fieldName)
            => GetPort(fieldName).DynamicValue;
#endregion

        /// <summary> Called after a connection between two <see cref="NodePort"/>s is created </summary>
        /// <param name="from">Output</param> <param name="to">Input</param>
        public virtual void OnCreateConnection(NodePort from, NodePort to) { }

        /// <summary> Called after a connection is removed from this port </summary>
        /// <param name="port">Output or Input</param>
        public virtual void OnRemoveConnection(NodePort port) { }

        /// <summary> Disconnect everything from this node </summary>
        public void ClearConnections()
        {
            foreach (var port in PortsByField.Values)
                port.ClearConnections();
        }

        public virtual void OnBeforeSerialize()
        {
            CollectPorts();
        }

        public virtual void OnAfterDeserialize()
        {
        }

        #region Attributes
        /// <summary> Manually supply node class with a context menu path </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class CreateNodeMenuAttribute : Attribute
        {
            public string menuName;
            public int order;
            public bool isForce;

            /// <summary> If true, Path will be added to the Conext Menu regardless of custom editor settings. </summary>
            /// <param name="isForce"> If true, Path will be added to the Conext Menu regardless of custom editor settings. </param>
            public CreateNodeMenuAttribute(bool isForce)
            {
                this.isForce = isForce;
            }

            /// <summary> The order by which the menu items are displayed </summary>
            /// <param name="isForce"> If true, Path will be added to the Conext Menu regardless of custom editor settings. </param>
            public CreateNodeMenuAttribute(int order, bool isForce = false)
            {
                this.order = order;
                this.isForce = isForce;
            }

            /// <summary> Manually supply node class with a context menu path </summary>
            /// <param name="menuName"> Path to this node in the context menu. Null or empty hides it. </param>
            /// <param name="isForce"> If true, Path will be added to the Conext Menu regardless of custom editor settings. </param>
            public CreateNodeMenuAttribute(string menuName, bool isForce = false)
            {
                this.menuName = menuName;
                this.isForce = isForce;
            }

            /// <summary> Manually supply node class with a context menu path </summary>
            /// <param name="menuName"> Path to this node in the context menu. Null or empty hides it. </param>
            /// <param name="order"> The order by which the menu items are displayed. </param>
            /// <param name="isForce"> If true, Path will be added to the Conext Menu regardless of custom editor settings. </param>
            public CreateNodeMenuAttribute(string menuName, int order, bool isForce = false)
            {
                this.menuName = menuName;
                this.order = order;
                this.isForce = isForce;
            }
        }

        /// <summary> Prevents Node of the same type to be added more than once (configurable) to a NodeGraph </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class DisallowMultipleNodesAttribute : Attribute
        {
            // TODO: Make inheritance work in such a way that applying [DisallowMultipleNodes(1)] to type NodeBar : Node
            //       while type NodeFoo : NodeBar exists, will let you add *either one* of these nodes, but not both.
            public int max;
            /// <summary> Prevents Node of the same type to be added more than once (configurable) to a NodeGraph </summary>
            /// <param name="max"> How many nodes to allow. Defaults to 1. </param>
            public DisallowMultipleNodesAttribute(int max = 1)
            {
                this.max = max;
            }
        }

        /// <summary> Specify a color for this node type </summary>
        public abstract class NodeTintAttribute : Attribute
        {
            public Color color;
            /// <summary> Specify a color for this node type </summary>
            /// <param name="r"> Red [0.0f .. 1.0f] </param>
            /// <param name="g"> Green [0.0f .. 1.0f] </param>
            /// <param name="b"> Blue [0.0f .. 1.0f] </param>
            public NodeTintAttribute(float r, float g, float b)
            {
                color = new Color(r, g, b);
            }

            /// <summary> Specify a color for this node type </summary>
            /// <param name="hex"> HEX color value </param>
            public NodeTintAttribute(string hex)
            {
                ColorUtility.TryParseHtmlString(hex, out color);
            }

            /// <summary> Specify a color for this node type </summary>
            /// <param name="r"> Red [0 .. 255] </param>
            /// <param name="g"> Green [0 .. 255] </param>
            /// <param name="b"> Blue [0 .. 255] </param>
            public NodeTintAttribute(byte r, byte g, byte b)
            {
                color = new Color32(r, g, b, byte.MaxValue);
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class NodeBodyTintAttribute : NodeTintAttribute
        {
            public NodeBodyTintAttribute(float r, float g, float b) : base(r, g, b) { }

            /// <summary> Specify a color for this node type </summary>
            /// <param name="hex"> HEX color value </param>
            public NodeBodyTintAttribute(string hex) : base(hex) { }

            /// <summary> Specify a color for this node type </summary>
            /// <param name="r"> Red [0 .. 255] </param>
            /// <param name="g"> Green [0 .. 255] </param>
            /// <param name="b"> Blue [0 .. 255] </param>
            public NodeBodyTintAttribute(byte r, byte g, byte b) : base(r, g, b) { }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class NodeHeaderTintAttribute : NodeTintAttribute
        {
            public Type colorTargetType;

            public NodeHeaderTintAttribute(float r, float g, float b) : base(r, g, b) { }

            /// <summary> Specify a color for this node type </summary>
            /// <param name="hex"> HEX color value </param>
            public NodeHeaderTintAttribute(string hex) : base(hex) { }

            /// <summary> Specify a color for this node type </summary>
            /// <param name="r"> Red [0 .. 255] </param>
            /// <param name="g"> Green [0 .. 255] </param>
            /// <param name="b"> Blue [0 .. 255] </param>
            public NodeHeaderTintAttribute(byte r, byte g, byte b) : base(r, g, b) { }

            public NodeHeaderTintAttribute(Type type) : base(0, 0, 0)
                => colorTargetType = type;
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class NodeFooterTintAttribute : NodeTintAttribute
        {
            public Type colorTargetType;

            public NodeFooterTintAttribute(float r, float g, float b) : base(r, g, b) { }

            /// <summary> Specify a color for this node type </summary>
            /// <param name="hex"> HEX color value </param>
            public NodeFooterTintAttribute(string hex) : base(hex) { }

            /// <summary> Specify a color for this node type </summary>
            /// <param name="r"> Red [0 .. 255] </param>
            /// <param name="g"> Green [0 .. 255] </param>
            /// <param name="b"> Blue [0 .. 255] </param>
            public NodeFooterTintAttribute(byte r, byte g, byte b) : base(r, g, b) { }

            public NodeFooterTintAttribute(Type type) : base(0, 0, 0)
                => colorTargetType = type;
        }

        /// <summary> Specify a width for this node type </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class NodeWidthAttribute : Attribute
        {
            public int width;
            /// <summary> Specify a width for this node type </summary>
            /// <param name="width"> Width </param>
            public NodeWidthAttribute(int width)
            {
                this.width = width;
            }

            public NodeWidthAttribute(NodeSize size)
            {
                width = (int)size;
            }
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class NodeIconAttribute : Attribute
        {
            public string path;
            /// <summary> Specify a width for this node type </summary>
            /// <param name="width"> Width </param>
            public NodeIconAttribute(string path)
            {
                this.path = path;
            }

            public NodeIconAttribute(NodeStyles.Icon icon)
            {
                path = NodeStyles.GetIconPath(icon);
            }
        }

        [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
        public class SpaceLineAttribute : SpaceAttribute
        {
            public SpaceLineAttribute(int direction = 1) : base(20f * direction) { }
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class PortSettingsAttribute : PropertyAttribute
        {
            public bool isHideLabel;
            public bool isOverrideSettings;
            public ShowBackingValue showBackingValue;
            public ConnectionType connectionType;
            public TypeConstraint typeConstraint;

            /// <summary> Mark a serializable field as an input port. You can access this through <see cref="GetInputPort(string)"/> </summary>
            /// <param name="backingValue">Should we display the backing value for this port as an editor field? </param>
            /// <param name="connectionType">Should we allow multiple connections? </param>
            /// <param name="typeConstraint">Constrains which input connections can be made to this port </param>
            /// <param name="dynamicPortList">If true, will display a reorderable list of inputs instead of a single port. Will automatically add and display values for lists and arrays </param>
            public PortSettingsAttribute(ShowBackingValue showBackingValue, ConnectionType connectionType, TypeConstraint typeConstraint)
            {
                isOverrideSettings = true;
                this.showBackingValue = showBackingValue;
                this.connectionType = connectionType;
                this.typeConstraint = typeConstraint;
            }

            public PortSettingsAttribute(bool isHideLabel)
            {
                isOverrideSettings = false;
                this.isHideLabel = isHideLabel;
            }

            public PortSettingsAttribute(bool isHideLabel, ShowBackingValue showBackingValue, ConnectionType connectionType, TypeConstraint typeConstraint)
            {
                isOverrideSettings = true;
                this.isHideLabel = isHideLabel;
                this.showBackingValue = showBackingValue;
                this.connectionType = connectionType;
                this.typeConstraint = typeConstraint;
            }
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class ArrowPortAttribute : PropertyAttribute
        {
        }

        [AttributeUsage(AttributeTargets.Field)]
        public class MultilineAttribute : PropertyAttribute
        {
            public int line;

            public MultilineAttribute(int line = 3)
                => this.line = line;
        }
        #endregion
    }
}
