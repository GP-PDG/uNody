using System.Collections.Generic;
using UnityEngine;

namespace PuppyDragon.uNody.Logic
{
    [NodeWidth(NodeSize.Mini)]
    [NodeIcon(NodeStyles.Icon.Stop)]
    [CreateNodeMenu(-8, true)]
    public class AbortNode : Node, ILogicNode
    {
        [ArrowPort, PortSettings(true, ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        [SerializeField]
        private InputPort<ILogicNode> prevs;

        public NodePort PrevPort => prevs;
        public NodePort NextPort => null;

        public IEnumerable<ILogicNode> Prevs => prevs.Values;
        public ILogicNode Next => null;

        public void Execute()
        {
            (Graph as LogicGraph).Abort();
        }
    }
}
