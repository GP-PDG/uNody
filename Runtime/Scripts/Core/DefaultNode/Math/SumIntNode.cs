using System.Linq;
using UnityEngine;

namespace PuppyDragon.uNody.Math
{
    [NodeWidth(NodeSize.Small)]
    [CreateNodeMenu(true)]
    [NodeHeaderTint(typeof(int))]
    public class SumIntNode : Node
    {
        [SerializeField]
        [PortSettings(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        private InputPort<int> values;
        [SerializeField]
        [SpaceLine(-1)]
        private OutputPort<int> result = new(self => (self as SumIntNode).Result);

        public int Result => values.Values.Sum();
    }
}
