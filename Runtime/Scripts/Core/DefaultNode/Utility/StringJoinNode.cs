using UnityEngine;

namespace PuppyDragon.uNody.Utility
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(string))]
    [CreateNodeMenu(-5, true)]
    public class StringJoinNode : Node
    {
        [PortSettings(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<string> strings;
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<string> result = new(self => (self as StringJoinNode).Join());
        [SerializeField]
        private InputPort<string> sep;

        private string Join()
        {
            return string.Join(sep.Value, strings.Values);
        }
    }
}
