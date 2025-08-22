using UnityEngine;

namespace PuppyDragon.uNody.Variable
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(Vector2))]
    [CreateNodeMenu(-6, true)]
    public class Vector2Node : Node
    {
        [SerializeField]
        [PortSettings(true, ShowBackingValue.Always, ConnectionType.Multiple, TypeConstraint.Strict)]
        private OutputPort<Vector2> value;
    }
}
