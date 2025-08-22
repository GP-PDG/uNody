using System.Linq;
using UnityEngine;

namespace PuppyDragon.uNody.Math
{
    [NodeWidth(NodeSize.Small)]
    [CreateNodeMenu(true)]
    [NodeHeaderTint(typeof(int))]
    public class SubIntNode : Node
    {
        [SerializeField]
        [PortSettings(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        private InputPort<int> values;
        [SerializeField]
        [SpaceLine(-1)]
        private OutputPort<int> result = new(x => (x as SubIntNode).Result);

        public int Result => values.Values.Aggregate((current, next) => current - next);
    }
}
