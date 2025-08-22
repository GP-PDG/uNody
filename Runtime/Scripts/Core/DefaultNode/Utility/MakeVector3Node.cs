using UnityEngine;

namespace PuppyDragon.uNody.Utility
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(Vector3))]
    [CreateNodeMenu(-5, true)]
    public class MakeVector3Node : Node
    {
        [SerializeField]
        private OutputPort<Vector3> result = new(self => (self as MakeVector3Node).MakeVector3());
        [SerializeField]
        private InputPort<float> x;
        [SerializeField]
        private InputPort<float> y;
        [SerializeField]
        private InputPort<float> z;

        private Vector3 MakeVector3()
            => new(x.Value, y.Value, z.Value);
    }
}
