using UnityEngine;

namespace PuppyDragon.uNody.BlackboardVariable
{
    [CreateNodeMenu(true, order = -7)]
    [NodeWidth(NodeSize.Medium)]
    public abstract class GetGlobalValueNode<T> : Node
    {
        [SerializeField]
        private OutputPort<T> value = new(self => (self as GetGlobalValueNode<T>).GetValue());
        [SerializeField]
        private InputPort<string> key;

        public T GetValue()
        {
            if (Graph.Blackboard == null)
                return default;

            Graph.Blackboard.TryGetGlobalValue(key.Value, out T value);
            
            return value;
        }
    }
}
