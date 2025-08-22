using UnityEngine;

namespace PuppyDragon.uNody.Variable
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(float))]
    [CreateNodeMenu(-6, true)]
    public class FloatNode : Node
    {
        [SerializeField]
        [PortSettings(true, ShowBackingValue.Always, ConnectionType.Multiple, TypeConstraint.Strict)]
        private OutputPort<float> value;
    }
}
