using UnityEngine;

namespace PuppyDragon.uNody.Utility
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(Vector2))]
    [CreateNodeMenu(-5, true)]
    public class MakeVector2Node : Node
    {
        [SerializeField]
        private OutputPort<Vector2> result = new(self => (self as MakeVector2Node).MakeVector2());
        [SerializeField]
        private InputPort<float> x;
        [SerializeField]
        private InputPort<float> y;

        private Vector2 MakeVector2()
            => new(x.Value, y.Value);
    }
}
