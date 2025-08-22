using UnityEngine;

namespace PuppyDragon.uNody.Variable
{
    [NodeWidth(NodeSize.Medium)]
    [NodeHeaderTint(typeof(Vector3))]
    [CreateNodeMenu(-6, true)]
    public class Vector3Node : Node
    {
        [SerializeField]
        [PortSettings(true, ShowBackingValue.Always, ConnectionType.Multiple, TypeConstraint.Strict)]
        private OutputPort<Vector3> value;
    }
}