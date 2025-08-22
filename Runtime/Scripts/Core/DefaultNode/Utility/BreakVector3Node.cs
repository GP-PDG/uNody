using UnityEngine;

namespace PuppyDragon.uNody.Utility
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(Vector3))]
    [CreateNodeMenu(-5, true)]
    public class BreakVector3Node : Node
    {
        [PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<Vector3> vector;
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<float> x = new(self => (self as BreakVector3Node).vector.Value.x);
        [SerializeField]
        private OutputPort<float> y = new(self => (self as BreakVector3Node).vector.Value.y);
        [SerializeField]
        private OutputPort<float> z = new(self => (self as BreakVector3Node).vector.Value.z);
    }
}
