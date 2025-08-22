using UnityEngine;

namespace PuppyDragon.uNody
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint("#45C4B0")]
    [CreateNodeMenu(-8, true)]
    [NodeIcon(NodeStyles.Icon.RightArrow)]
    public abstract class OutPointNode<T> : Node
    {
        [PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)]
        [SerializeField]
        private InputPort<T> input;
        [SerializeField, HideInInspector]
        private OutputPort<T> output = new(self => (self as OutPointNode<T>).input.Value);
    }
}
