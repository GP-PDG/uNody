using UnityEngine;

namespace PuppyDragon.uNody.Convert
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(int))]
    [NodeIcon(NodeStyles.Icon.Exchange)]
    [CreateNodeMenu(true)]
    public class FloatToIntNode : Node
    {
        [SerializeField]
        [PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        private InputPort<float> from;
        [PortSettings(true)]
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<int> to = new(self => (int)(self as FloatToIntNode).from.Value);
    }
}