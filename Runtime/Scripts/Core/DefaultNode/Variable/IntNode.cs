using UnityEngine;

namespace PuppyDragon.uNody.Variable
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(int))]
    [CreateNodeMenu(-6, true)]
    public class IntNode : Node
    {
        [SerializeField]
        [PortSettings(true, ShowBackingValue.Always, ConnectionType.Multiple, TypeConstraint.Strict)]
        private OutputPort<int> value;
    }
}
