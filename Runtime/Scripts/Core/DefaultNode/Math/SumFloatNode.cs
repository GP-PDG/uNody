using System.Linq;
using UnityEngine;

namespace PuppyDragon.uNody.Math
{
    [NodeWidth(NodeSize.Small)]
    [CreateNodeMenu(true)]
    [NodeHeaderTint(typeof(float))]
    public class SumFloatNode : Node
    {
        [SerializeField]
        [PortSettings(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        private InputPort<float> values;
        [SerializeField]
        [SpaceLine(-1)]
        private OutputPort<float> result = new(x => (x as SumFloatNode).Result);

        public float Result => values.Values.Sum();
    }
}