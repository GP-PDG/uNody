using UnityEngine;

namespace PuppyDragon.uNody.Convert
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(string))]
    [NodeIcon(NodeStyles.Icon.Exchange)]
    [CreateNodeMenu(true)]
    public class ToStringNode : Node
    {
        [PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)]
        [SerializeField]
        private InputPort<object> from;
        [PortSettings(true)]
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<string> to = new(self => (self as ToStringNode).from.Value?.ToString());
    }
}