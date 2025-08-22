using System.Collections.Generic;
using UnityEngine;

namespace PuppyDragon.uNody.Logic
{
    [NodeWidth(NodeSize.Small)]
    [CreateNodeMenu(-8, true)]
    public class IfNode : Node, ILogicNode, ILogicConnector
    {
        [ArrowPort, PortSettings(true, ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        [SerializeField]
        private InputPort<ILogicNode> prevs;
        [ArrowPort, PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<ILogicNode> True = new(self => self as ILogicNode);
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<bool> condition;
        [ArrowPort, PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<ILogicNode> False = new(self => self as ILogicNode);

        public NodePort PrevPort => prevs;
        public NodePort NextPort => condition.Value ? True : False;

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

        public void Execute()
        {
        }
    }
}
