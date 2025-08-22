using PuppyDragon.uNody.BlackboardVariable;
using UnityEngine;

namespace PuppyDragon.uNody.Logic.BlackboardVariable
{
    [CreateNodeMenu(true, order = -7)]
    [NodeWidth(NodeSize.Medium)]
    public abstract class SetLocalValueNode<T> : LogicNode
    {
        [SerializeField]
        private InputPort<string> key;
        [SerializeField]
        private InputPort<T> set;
        [SpaceLine(-1)]
        [PortSettings(true)]
        [SerializeField]
        private OutputPort<T> value = new(self => (self as SetLocalValueNode<T>).set.Value);

        public override void Execute()
        {
            var graph = Graph.Root ?? Graph;
            if (graph.Blackboard == null)
                throw new MissingReferenceException();

            graph.Blackboard.SetLocalValue(graph, key.Value, set.Value);
        }
    }
}
