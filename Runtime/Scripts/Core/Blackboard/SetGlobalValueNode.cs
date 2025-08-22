using UnityEngine;

namespace PuppyDragon.uNody.Logic.BlackboardVariable
{
    [CreateNodeMenu(true, order = -7)]
    [NodeWidth(NodeSize.Medium)]
    public abstract class SetGlobalValueNode<T> : LogicNode
    {
        [SerializeField]
        private InputPort<string> key;
        [SerializeField]
        private InputPort<T> set;
        [PortSettings(true)]
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<T> value = new(self => (self as SetGlobalValueNode<T>).set.Value);

        public override void Execute()
        {
            var graph = Graph.Root ?? Graph;
            if (graph.Blackboard == null)
                throw new MissingReferenceException();

            graph.Blackboard.SetGlobalValue(key.Value, set.Value);
        }
    }
}
