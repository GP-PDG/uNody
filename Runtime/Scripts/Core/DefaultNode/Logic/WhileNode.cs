using System;
using UnityEngine;

namespace PuppyDragon.uNody.Logic
{
    [CreateNodeMenu(-8, true)]
    public class WhileNode : LogicNode
    {
        [SerializeField]
        private InputPort<int> startIndex;
        [SerializeField]
        private InputPort<int> count;
        [ArrowPort]
        [PortSettings(ShowBackingValue.Always, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private OutputPort<ILogicNode> body = new(GetBodyNode);
        [PortSettings(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
        [SerializeField]
        private OutputPort<int> crrentIndex;

        public override void Execute()
        {
            if (count.Value < 0)
            {
                for (int i = startIndex.Value; i > startIndex.Value + count.Value; i--)
                {
                    if ((Graph as LogicGraph).IsAborting)
                        break;

                    OnBody(i);
                }
            }
            else if (count.Value > 0)
            {
                for (int i = startIndex.Value; i < startIndex.Value + count.Value; i++)
                {
                    if ((Graph as LogicGraph).IsAborting)
                        break;

                    OnBody(i);
                }
            }
            crrentIndex.Value = 0;
        }

        private void OnBody(int index)
        {
            var next = body.Value;
            while (next != null && typeof(ILogicConnector).IsAssignableFrom(next.GetType()))
                next = next.Next;

            if (next == null)
                return;

            crrentIndex.Value = index;

            if (next == null)
                return;

            do
            {
                next.Execute();
            }
            while ((next = next.Next) != null);
        }

        private static ILogicNode GetBodyNode(Node node)
            => (node as WhileNode).body.Connection?.Node as ILogicNode;
    }
}