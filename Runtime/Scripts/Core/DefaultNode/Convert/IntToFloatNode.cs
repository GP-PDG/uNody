using UnityEngine;

namespace PuppyDragon.uNody.Convert
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(float))]
    [NodeIcon(NodeStyles.Icon.Exchange)]
    [CreateNodeMenu(true)]


    public class IntToFloatNode : Node
    {
        [SerializeField]
        [PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        private InputPort<int> from;
        [PortSettings(true)]
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<float> to = new(self => (self as IntToFloatNode).from.Value);
    }
}