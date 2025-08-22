using System.Linq;
using UnityEngine;

namespace PuppyDragon.uNody.Math
{
    [NodeWidth(NodeSize.Small)]
    [CreateNodeMenu(true)]
    [NodeHeaderTint(typeof(Vector3))]
    public class SubVector3Node : Node
    {
        [SerializeField]
        [PortSettings(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        private InputPort<Vector3> values;
        [SerializeField]
        [SpaceLine(-1)]
        private OutputPort<Vector3> result = new(x => (x as SubVector3Node).Result);

        public Vector3 Result => values.Values.Aggregate((current, next) => current - next);
    }
}
