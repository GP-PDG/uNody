using UnityEngine;

namespace PuppyDragon.uNody.Variable
{
    [NodeWidth(NodeSize.Large)]
    [NodeHeaderTint(typeof(string))]
    [CreateNodeMenu(-6, true)]
    public class MultiLineStringNode : Node
    {
        [SerializeField]
        [Multiline, PortSettings(true, ShowBackingValue.Always, ConnectionType.Multiple, TypeConstraint.Strict)]
        private OutputPort<string> value;
    }
}