using UnityEngine;

namespace PuppyDragon.uNody.Logic
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint("#C66D00")]
    [CreateNodeMenu(-8, true)]
    [NodeIcon(NodeStyles.Icon.Bug)]
    public class PrintNode : LogicNode
    {
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.None)]
        [SerializeField]
        private InputPort<object> value;

        public override void Execute()
        {
            Debug.Log(value.Value);
        }
    }
}