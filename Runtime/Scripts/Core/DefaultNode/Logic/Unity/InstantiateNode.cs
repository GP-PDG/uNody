using UnityEngine;

namespace PuppyDragon.uNody.Logic.Unity
{
    [NodeWidth(NodeSize.Large)]
    [CreateNodeMenu(-6, true)]
    public class InstantiateNode : LogicNode
    {
        [PortSettings(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<GameObject> gameObject;
        [PortSettings(true, ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SpaceLine(-1)]
        [SerializeField]
        private OutputPort<GameObject> clone;
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<Transform> parent;
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<Vector3> position;
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<Vector3> rotation;
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<Vector3> scale;

        public override void Execute()
        {
            var gameObject = this.gameObject.Value;
            Vector3 position = this.position.Connection != null ? this.position.Value : gameObject.transform.position;
            Quaternion rotation = this.rotation.Connection != null ? Quaternion.Euler(this.rotation.Value) : gameObject.transform.rotation;
            Vector3 scale = this.scale.Connection != null ? this.scale.Value : gameObject.transform.localScale;
            
            var clone = Instantiate(gameObject, position, rotation, parent.Value);
            clone.transform.localScale = scale;

            this.clone.DynamicValue = clone;
        }
    }
}
