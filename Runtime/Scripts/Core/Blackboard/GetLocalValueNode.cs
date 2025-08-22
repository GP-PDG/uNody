using UnityEngine;

namespace PuppyDragon.uNody.BlackboardVariable
{
    [CreateNodeMenu(true, order = -7)]
    [NodeWidth(NodeSize.Medium)]
    public abstract class GetLocalValueNode<T> : Node
    {
        [SerializeField]
        private OutputPort<T> value = new(self => (self as GetLocalValueNode<T>).GetValue());
        [SerializeField]
        private InputPort<string> key;

        public T GetValue()
        {
            if (Graph.Blackboard == null)
                return default;

            Graph.Blackboard.TryGetLocalValue(Graph, key.Value, out T value);
            return value;
        }
    }
}
