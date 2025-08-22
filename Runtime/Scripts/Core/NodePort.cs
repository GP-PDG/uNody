using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNody
{
    [System.Serializable]
    public abstract class NodePort
    {
        public enum IO { Input, Output }

        [SerializeField, HideInInspector]
        private Node ownerNode;
        [SerializeField, HideInInspector]
        private string guid;
        [SerializeField, HideInInspector]
        private string fieldName;
        [SerializeField, HideInInspector]
        private string elementName;
        [SerializeField, HideInInspector]
        private List<PortConnection> connections;
        [SerializeField, HideInInspector]
        private Rect rect;

        private int arrayIndex;
        private Node.ShowBackingValue showBackingValue;
        private Node.ConnectionType connectionType;
        private Node.TypeConstraint typeConstraint;

        public Node OwnerNode => ownerNode;
        public string GUID => guid;
        public string FieldName => fieldName;
        public string ElementName => elementName;
        public bool IsElement => !string.IsNullOrEmpty(elementName);
        public Rect Rect { get => rect; set => rect = value; }
        public abstract IO Direction { get; }
        public abstract object DynamicValue { get; set; }
        public abstract IEnumerable<object> DynamicValues { get; }

        public abstract Type ValueType { get; }

        public IReadOnlyList<PortConnection> Connections => connections ??= new();
        public bool IsConnected => Connections.Count > 0;
        public PortConnection Connection => IsConnected ? Connections[0] : null;
        public int ConnectionCount => Connections.Count;
        public Node.ShowBackingValue ShowBackingValueType => showBackingValue;
        public int ArrayIndex => arrayIndex; 

        protected abstract void ClearDefaultValue();

        public virtual void Setup(Node ownerNode, string fieldName, Node.PortSettingsAttribute settings)
        {
            if (string.IsNullOrEmpty(guid))
                guid = Guid.NewGuid().ToString();

            this.ownerNode = ownerNode;
            this.fieldName = fieldName;

            if (settings != null && settings.isOverrideSettings)
                SetSettings(settings.showBackingValue, settings.connectionType, settings.typeConstraint);
        }

        public virtual void Setup(Node ownerNode, string fieldName, int arrayIndex, Node.PortSettingsAttribute settings)
        {
            Setup(ownerNode, fieldName, settings);

            this.arrayIndex = arrayIndex;
            var newElementName = fieldName + "." + arrayIndex;
            if (elementName != newElementName)
                ClearConnections();

            var prevPort = ownerNode.GetPort(fieldName + "." + (arrayIndex - 1));
            if (prevPort != null && prevPort.GUID == guid)
                guid = Guid.NewGuid().ToString();

            elementName = newElementName;
        }

        protected void SetSettings(Node.ShowBackingValue showBackingValue, Node.ConnectionType connectionType, Node.TypeConstraint typeConstraint)
        {
            this.showBackingValue = showBackingValue;
            this.connectionType = connectionType;
            this.typeConstraint = typeConstraint;
        }

        public void VerifyConnections()
        {
            for (int i = Connections.Count - 1; i >= 0; i--)
            {
                var connection = connections[i];
                if (connection.Node != null &&
                    connection.Port != null)
                    continue;

                connections.RemoveAt(i);
            }
        }

        public void Connect(NodePort port)
        {
            if (port == null)
            {
                Debug.LogWarning("Cannot connect to null port");
                return;
            }

            if (port == this)
            {
                Debug.LogWarning("Cannot connect port to self.");
                return;
            }

            if (IsConnectedTo(port))
            {
                Debug.LogWarning("Port already connected. ");
                return;
            }

            if (Direction == port.Direction)
            {
                Debug.LogWarning("Cannot connect two " + (Direction == IO.Input ? "input" : "output") + " connections");
                return;
            }

#if UNITY_EDITOR
            Undo.RecordObject(OwnerNode, "Connect Port");
            Undo.RecordObject(port.OwnerNode, "Connect Port");
#endif
            if (port.connectionType == Node.ConnectionType.Override && port.ConnectionCount != 0)
                port.ClearConnections();

            if (connectionType == Node.ConnectionType.Override && ConnectionCount != 0)
                ClearConnections();

            connections.Add(new PortConnection(port));

            if (port.connections == null)
                port.connections = new List<PortConnection>();

            if (!port.IsConnectedTo(this))
                port.connections.Add(new PortConnection(this));

            OwnerNode.OnCreateConnection(this, port);
            port.OwnerNode.OnCreateConnection(this, port);

            port.ClearDefaultValue();
        }

        /// <summary> Disconnect this port from another port </summary>
        public void Disconnect(NodePort port)
        {
            if (port == null)
                return;

            connections.RemoveAll(x => x.Port == port);
            port.connections.RemoveAll(x => x.Port == this);

            // Trigger OnRemoveConnection
            OwnerNode.OnRemoveConnection(this);
        }

        /// <summary> Disconnect this port from another port </summary>
        public void Disconnect(int i)
        {
            // Remove the other ports connection to this port
            var otherPort = connections[i].Port;
            if (otherPort != null)
                otherPort.connections.RemoveAll(x => { return x.Port == this; });

            // Remove this ports connection to the other
            connections.RemoveAt(i);

            // Trigger OnRemoveConnection
            OwnerNode.OnRemoveConnection(this);
            if (otherPort != null)
                otherPort.OwnerNode.OnRemoveConnection(otherPort);
        }

        public void ClearConnections()
        {
            while (connections.Count > 0)
                Disconnect(connections[0].Port);
        }

        public void SwapConnections(NodePort targetPort)
        {
            int aConnectionCount = connections.Count;
            int bConnectionCount = targetPort.connections.Count;

            List<NodePort> portConnections = new();
            List<NodePort> targetPortConnections = new List<NodePort>();

            // Cache port connections
            for (int i = 0; i < aConnectionCount; i++)
                portConnections.Add(connections[i].Port);

            // Cache target port connections
            for (int i = 0; i < bConnectionCount; i++)
                targetPortConnections.Add(targetPort.connections[i].Port);

            ClearConnections();
            targetPort.ClearConnections();

            // Add port connections to targetPort
            for (int i = 0; i < portConnections.Count; i++)
                targetPort.Connect(portConnections[i]);

            // Add target port connections to this one
            for (int i = 0; i < targetPortConnections.Count; i++)
                Connect(targetPortConnections[i]);
        }

        // <summary> Get index of the connection connecting this and specified ports</summary>
        public int GetConnectionIndex(NodePort port)
            => connections.FindIndex(x => x.Port == port);

        public bool IsConnectedTo(NodePort port)
            => connections.Exists(x => x.Port == port);

        /// <summary> Returns true if this port can connect to specified port </summary>
        public bool CanConnectTo(NodePort port)
        {
            // If there isn't one of each, they can't connect
            if (Direction == port.Direction)
                return false;

            // Figure out which is input and which is output
            var input = Direction == IO.Input ? this : port;
            var output = Direction == IO.Output ? this : port;

            return IsVaildPort(input.typeConstraint, input, output) &&
                IsVaildPort(output.typeConstraint, input, output);
        }

        private bool IsVaildPort(Node.TypeConstraint typeConstraint, NodePort inputPort, NodePort outputPort)
        {
            switch (inputPort.typeConstraint)
            {
                case Node.TypeConstraint.Inherited:
                    return inputPort.ValueType.IsAssignableFrom(outputPort.ValueType);

                case Node.TypeConstraint.Strict:
                    return inputPort.ValueType == outputPort.ValueType;

                case Node.TypeConstraint.InheritedInverse:
                    return outputPort.ValueType.IsAssignableFrom(outputPort.ValueType);

                case Node.TypeConstraint.InheritedAny:
                    return inputPort.ValueType.IsAssignableFrom(outputPort.ValueType) && outputPort.ValueType.IsAssignableFrom(inputPort.ValueType);

                default:
                    return true;
            }
        }

        public PortConnection GetConnection(int i)
        {
            var connection = connections[i];
            if (connections[i].Port == null)
            {
                connections.RemoveAt(i);
                return null;
            }

            return connection;
        }

        public void Redirect(List<Node> oldNodes, List<Node> newNodes)
        {
            foreach (PortConnection connection in connections)
            {
                int index = oldNodes.IndexOf(connection.Node);
                if (index >= 0)
                    connection.Node = newNodes[index];
            }
        }

        [Serializable]
        public class PortConnection
        {
            [SerializeField]
            private Node node;
            [SerializeField]
            private string guid;
            [SerializeField]
            private List<Vector2> reroutes = new();

            public Node Node { get => node; set => node = value; }
            public NodePort Port => node.GetPortFromGUID(guid);
            public List<Vector2> Reroutes => reroutes;

            public PortConnection(NodePort port)
            {
                node = port.OwnerNode;
                guid = port.GUID;
            }

            public void AddReroute(Vector2 reroute)
                => reroutes.Add(reroute);

            public void InsertReroute(int index, Vector2 reroute)
                => reroutes.Insert(index, reroute);

            public void SetReroute(int index, Vector2 reroute)
                => reroutes[index] = reroute;

            public void AddReroutes(IEnumerable<Vector2> reroutes)
                => this.reroutes.AddRange(reroutes);

            public void RemoveReroute(int index)
                => reroutes.RemoveAt(index);
 
            public void ClearReroutes()
                => reroutes.Clear();
        }
    }

    [System.Serializable]
    public class InputPort<T> : NodePort
    {
        [SerializeField]
        private T defaultValue;

        public override IO Direction => IO.Input;
        public override object DynamicValue
        {
            get => Value;
            set => Value = (T)value;
        }
        public override IEnumerable<object> DynamicValues => IsConnected ? Connections.Select(x => x.Port.DynamicValue) : Enumerable.Repeat(DynamicValue, 1);
        public override Type ValueType => typeof(T);

        public T Value
        {
            get => IsConnected ? (T)Connection.Port.DynamicValue : defaultValue;
            set => defaultValue = value;
        }
        public IEnumerable<T> Values => IsConnected ? Connections.Select(x => (T)x.Port.DynamicValue) : Enumerable.Repeat(Value, 1);

        public InputPort(T defaultValue = default)
            => this.defaultValue = defaultValue;

        public override void Setup(Node ownerNode, string fieldName, Node.PortSettingsAttribute settings)
        {
            base.Setup(ownerNode, fieldName, settings);
            if (settings == null || !settings.isOverrideSettings)
                SetSettings(Node.ShowBackingValue.Unconnected, Node.ConnectionType.Multiple, Node.TypeConstraint.InheritedAny);
        }

        protected override void ClearDefaultValue()
        {
            defaultValue = default;
        }
    }

    [System.Serializable]
    public class OutputPort<T> : NodePort
    {
        [SerializeField]
        private T defaultValue;
        // for PropertyDrawer
        [SerializeField, HideInInspector]
        private bool hasFunc;

        private Func<Node, T> onGetDefaultValue;

        public override IO Direction => IO.Output;
        public override object DynamicValue
        {
            get => Value;
            set => Value = (T)value;
        }
        public override IEnumerable<object> DynamicValues => Enumerable.Repeat(DynamicValue, 1);
        public override Type ValueType => typeof(T);
        public T Value
        {
            get => hasFunc ? onGetDefaultValue.Invoke(OwnerNode) : defaultValue;
            set => defaultValue = value;
        }

        public OutputPort(T defaultValue = default)
        {
            this.defaultValue = defaultValue;
        }

        public OutputPort(Func<Node, T> onGetDefaultValue)
        {
            this.onGetDefaultValue = onGetDefaultValue;
        }

        public override void Setup(Node ownerNode, string fieldName, Node.PortSettingsAttribute settings)
        {
            base.Setup(ownerNode, fieldName, settings);
            if (settings == null || !settings.isOverrideSettings)
                SetSettings(Node.ShowBackingValue.Always, Node.ConnectionType.Multiple, Node.TypeConstraint.InheritedAny);

            hasFunc = onGetDefaultValue != null;
        }

        protected override void ClearDefaultValue()
        {
            defaultValue = default;
        }
    }
}