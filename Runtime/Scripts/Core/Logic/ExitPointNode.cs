using System.Collections.Generic;
using UnityEngine;

namespace PuppyDragon.uNody.Logic
{
    [DisallowMultipleNodes]
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint("#45C4B0")]
    [NodeIcon(NodeStyles.Icon.RightArrow)]
    public class ExitPointNode : Node, ILogicNode, ILogicConnector
    {
        [ArrowPort, PortSettings(true, ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        [SerializeField]
        private InputPort<ILogicNode> exit;
        [ArrowPort, PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
        [HideInInspector]
        [SerializeField]
        private OutputPort<ILogicNode> next = new(self => self as ILogicNode);

        public NodePort PrevPort => exit;
        public NodePort NextPort => next;

        public IEnumerable<ILogicNode> Prevs => exit.Values;
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
            (Graph as LogicGraph).Abort();
        }
    }
}
