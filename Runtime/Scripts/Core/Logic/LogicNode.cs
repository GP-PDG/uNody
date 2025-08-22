using System.Collections.Generic;
using UnityEngine;

namespace PuppyDragon.uNody.Logic
{
    public abstract class LogicNode : Node, ILogicNode
    {
        [ArrowPort, PortSettings(true, ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        [SerializeField]
        private InputPort<ILogicNode> prevs;
        [ArrowPort, PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<ILogicNode> next = new(self => self as ILogicNode);

        public NodePort PrevPort => prevs;
        public NodePort NextPort => next;

        public virtual IEnumerable<ILogicNode> Prevs => prevs.Values;
        public virtual ILogicNode Next => NextPort.Connection?.Node as ILogicNode;

        public abstract void Execute();
    }
}
