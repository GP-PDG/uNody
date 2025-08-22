using System.Linq;
using UnityEngine;

namespace PuppyDragon.uNody.Utility
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(string))]
    [CreateNodeMenu(-5, true)]
    public class StringConcatNode : Node
    {
        [PortSettings(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<string> strings;
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<string> result = new(self => string.Concat((self as StringConcatNode).strings.Values));
    }
}
