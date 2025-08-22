using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace PuppyDragon.uNody
{
    [CreateAssetMenu(fileName = "Node Graph", menuName = "uNody/Node Graph")]
    public class NodeGraph : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField]
        private Blackboard blackboard;
        [SerializeField]
        private Vector2 panOffset;
        [SerializeField]
        private float zoom = 1f;
        [SerializeField]
        private List<Node> nodes = new();

        [Header("Graph")]
        [SerializeField]
        private NodeGraph parent;
        [SerializeField]
        private List<NodeGraph> children = new();
        [SerializeField]
        private List<Node> inPoints = new();
        [SerializeField]
        private List<Node> outPoints = new();

        private List<Node> collectedNodes = new();

        public IReadOnlyList<Node> Nodes
        {
            get
            {
                collectedNodes.Clear();
                CollectNodes(this, collectedNodes);
                return collectedNodes;
            }
        }
        public IReadOnlyList<Node> InPoints => inPoints;
        public IReadOnlyList<Node> OutPoints => outPoints;

        public Blackboard Blackboard
        {
            get
            {
                var currentGraph = this;
                while (currentGraph.parent != null)
                    currentGraph = currentGraph.parent;
                return currentGraph.blackboard;
            }
        }

        public Vector2 PanOffset
        {
            get => panOffset;
            set => panOffset = value;
        }
        public float Zoom
        {
            get => zoom;
            set => zoom = value;
        }

        public NodeGraph Root
        {
            get
            {
                if (Parent == null)
                    return null;

                var root = Parent;
                while (root.Parent != null)
                    root = root.Parent;
                return root;
            }
        }
        public NodeGraph Parent
        {
            get => parent;
            set
            {
                if (parent != null)
                    parent.children.Remove(this);

                parent = value;

                if (parent != null)
                    parent.children.Add(this);
            }
        }

        public IReadOnlyList<NodeGraph> Children => children;

        protected virtual void OnEnable()
        {
            AddRequired();
        }

        protected virtual void OnDestroy()
        {
            if (Parent)
                Parent.children.Remove(this);

            if (blackboard)
                blackboard.DeleteLocalVars(this);

            // Remove all nodes prior to graph destruction
            Clear();
        }

        public void SetInValue(string pointName, object value)
            => inPoints.Find(x => x.name == pointName).GetPort("input").DynamicValue = value;

        public T GetInValue<T>(string pointName)
            => (T)inPoints.Find(x => x.name == pointName).GetPort("input").DynamicValue;

        public object GetInValue(string pointName)
            => inPoints.Find(x => x.name == pointName).GetPort("input").DynamicValue;

        public T GetOutValue<T>(string pointName)
            => (T)outPoints.Find(x => x.name == pointName).GetPort("output").DynamicValue;

        public object GetOutValue(string pointName)
            => outPoints.Find(x => x.name == pointName).GetPort("output").DynamicValue;

        public int IndexOf(Node node)
            => nodes.IndexOf(node);

        public bool Contains(Node node)
            => nodes.Contains(node);

        private void CollectNodes(NodeGraph rootGraph, List<Node> collectedNodes)
        {
            collectedNodes.AddRange(rootGraph.nodes);
            foreach (var subGraph in rootGraph.Children)
                CollectNodes(subGraph, collectedNodes);
        }

        /// <summary> Add a node to the graph by type (convenience method - will call the System.Type version) </summary>
        public T AddNode<T>() where T : Node
            => AddNode(typeof(T)) as T;

        /// <summary> Add a node to the graph by type </summary>
        public virtual Node AddNode(Type type)
        {
            Node.graphHotfix = this;
            Node node = ScriptableObject.CreateInstance(type) as Node;
            nodes.Add(node);

            if (NodeReflection.IsInPoint(type))
                inPoints.Add(node);
            else if (NodeReflection.IsOutPoint(type))
                outPoints.Add(node);

            return node;
        }

        /// <summary> Creates a copy of the original node in the graph </summary>
        public virtual Node CopyNode(Node original) {
            Node.graphHotfix = this;
            Node node = ScriptableObject.Instantiate(original);
            node.ClearConnections();
            nodes.Add(node);

            if (NodeReflection.IsInPoint(node))
                inPoints.Add(node);
            else if (NodeReflection.IsOutPoint(node))
                outPoints.Add(node);

            if (node is SubGraphNode subGraphNode)
            {
                subGraphNode.SubGraph = subGraphNode.SubGraph.Copy();
                subGraphNode.SubGraph.Parent = parent;
            }

            return node;
        }

        /// <summary> Safely remove a node and all its connections </summary>
        /// <param name="node"> The node to remove </param>
        public virtual void RemoveNode(Node node)
        {
            nodes.Remove(node);

            if (node != null)
            {
                node.ClearConnections();

                if (NodeReflection.IsInPoint(node))
                    inPoints.Remove(node);
                else if (NodeReflection.IsOutPoint(node))
                    outPoints.Remove(node);

                if (Application.isPlaying)
                    Destroy(node);
            }
            else
            {
                inPoints.Remove(node);
                outPoints.Remove(node);
            }
        }

        public virtual void RemoveNodeAll(Func<Node, bool> predicate)
        {
            foreach (var node in nodes.ToArray())
            {
                if (predicate(node))
                    RemoveNode(node);
            }
        }

        public void RemoveNodeAllDirectely(Node node)
        {
            nodes.Remove(node);
            inPoints.Remove(node);
            outPoints.Remove(node);

            if (Application.isPlaying && node != null)
                Destroy(node);
        }

        /// <summary> Remove all nodes and connections from the graph </summary>
        public virtual void Clear()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null)
                {
                    if (Application.isPlaying)
                        Destroy(nodes[i]);
                    else
                        DestroyImmediate(nodes[i], true);
                }
            }

            nodes.Clear();
        }

        /// <summary> Create a new deep copy of this graph </summary>
        public virtual NodeGraph Copy()
        {
            // Instantiate a new nodegraph instance
            NodeGraph graph = Instantiate(this);
            graph.name = name;
            graph.children.Clear();

            var inPoints = new List<Node>();
            var outPoints = new List<Node>();

            // Instantiate all nodes inside the graph
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] == null)
                    continue;

                Node.graphHotfix = graph;
                Node node = Instantiate(nodes[i]) as Node;
                node.name = nodes[i].name;
                graph.nodes[i] = node;

                if (node is SubGraphNode subGraphNode)
                {
                    subGraphNode.SubGraph = subGraphNode.SubGraph.Copy();
                    subGraphNode.SubGraph.Parent = graph;
                }
                else if (NodeReflection.IsInPoint(node))
                    inPoints.Add(node);
                else if (NodeReflection.IsOutPoint(node))
                    outPoints.Add(node);
            }

            graph.inPoints = inPoints;
            graph.outPoints = outPoints;

            // Redirect all connections
            foreach (var node in graph.nodes)
            {
                foreach (var port in node.Ports)
                {
                    port.Redirect(nodes, graph.nodes);

                    // Redirection inPoint connections
                    if (port.Connections.Any(x => x.Node.Graph != graph))
                    {
                        for (int i = 0; i < graph.children.Count; i++)
                            port.Redirect(children[i].inPoints, graph.children[i].inPoints);
                    }
                }
            }

            // Redirection sub to parent connections
            foreach (var subGraph in graph.children)
            {
                foreach (var inPoint in subGraph.inPoints)
                {
                    foreach (var port in inPoint.Inputs)
                        port.Redirect(nodes, graph.nodes);
                }

                foreach (var outPoint in subGraph.outPoints)
                {
                    foreach (var port in outPoint.Outputs)
                        port.Redirect(nodes, graph.nodes);
                }
            }

            return graph;
        }

        public Node[] AddRequired()
        {
            Vector2 position = Vector2.zero;
            var requiredNodeAttr = GetType().GetCustomAttribute<RequireNodeAttribute>();
            var requiredNodes = new List<Node>();
            if (requiredNodeAttr != null)
            {
                foreach (var type in requiredNodeAttr.Types)
                {
                    var requiredNode = AddRequired(type, ref position);
                    if (requiredNode != null)
                        requiredNodes.Add(requiredNode);
                }
            }
            return requiredNodes.ToArray();
        }

        private Node AddRequired(Type type, ref Vector2 position)
        {
            if (!nodes.Any(x => x.GetType() == type))
            {
                var node = AddNode(type);
                node.NodePosition = position;
                position.x += 200;

                if (node.name == null || node.name.Trim() == "")
                {
                    var typeName = type.Name;
                    if (typeName.EndsWith("Node"))
                        typeName = typeName.Substring(0, typeName.LastIndexOf("Node"));
                    typeName = ObjectNames.NicifyVariableName(typeName);

                    node.name = typeName;
                }

                return node;
            }
            return null;
        }

        public virtual void OnBeforeSerialize()
        {
            // Fix null object creation for unknown reasons.
            if (nodes.Count > 0 && nodes[nodes.Count - 1] == null)
                nodes.RemoveAt(nodes.Count - 1);
        }

        public virtual void OnAfterDeserialize() { }

        #region Attributes
        /// <summary> Automatically ensures the existance of a certain node type, and prevents it from being deleted. </summary>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class RequireNodeAttribute : Attribute
        {
            private Type[] types;

            public Type[] Types => types;

            /// <summary> Automatically ensures the existance of a certain node type, and prevents it from being deleted </summary>
            public RequireNodeAttribute(Type[] types) => this.types = types;
            public RequireNodeAttribute(Type type1) : this(new[] { type1 }) { }
            public RequireNodeAttribute(Type type1, Type type2) : this(new[] { type1, type2 }) { }
            public RequireNodeAttribute(Type type1, Type type2, Type type3) : this(new[] { type1, type2, type3 }) { }

            public bool Requires(Type type)
                => type == null ? false : types.Contains(type);
        }
#endregion
    }
}