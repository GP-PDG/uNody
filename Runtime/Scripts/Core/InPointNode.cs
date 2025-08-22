using UnityEngine;

namespace PuppyDragon.uNody
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint("#F25C05")]
    [CreateNodeMenu(-9, true)]
    [NodeIcon(NodeStyles.Icon.RightArrow)]
    public abstract class InPointNode<T> : Node
    {
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
        [SerializeField, HideInInspector]
        private InputPort<T> input;
        [PortSettings(true)]
        [SerializeField]
        private OutputPort<T> output = new(self => (self as InPointNode<T>).input.Value);
    }
}
