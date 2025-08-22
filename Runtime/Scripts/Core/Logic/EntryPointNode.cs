using System.Collections.Generic;
using UnityEngine;

namespace PuppyDragon.uNody.Logic
{
    [DisallowMultipleNodes]
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint("#F25C05")]
    [NodeIcon(NodeStyles.Icon.RightArrow)]
    public class EntryPointNode : Node, ILogicNode, ILogicConnector
    {
        [ArrowPort, PortSettings(true, ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        [HideInInspector]
        [SerializeField]
        private InputPort<ILogicNode> prevs;
        [ArrowPort, PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
        [SerializeField]
        private OutputPort<ILogicNode> enter = new(self => self as ILogicNode);

        public NodePort PrevPort => prevs;
        public NodePort NextPort => enter;

        public IEnumerable<ILogicNode> Prevs => prevs.Values;
        public ILogicNode Next
        {
            get
            {
                var node = NextPort.Connection?.Node as ILogicNode;
                while (node != null && node is ILogicConnector)
                    node = node.Next;
                return node;
            }
        }

        public void Execute() { }
    }
}
