using UnityEngine;

namespace PuppyDragon.uNody.Variable
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(string))]
    [CreateNodeMenu(-6, true)]
    public class StringNode : Node
    {
        [SerializeField]
        [PortSettings(true, ShowBackingValue.Always, ConnectionType.Multiple, TypeConstraint.Strict)]
        private OutputPort<string> value;
    }
}