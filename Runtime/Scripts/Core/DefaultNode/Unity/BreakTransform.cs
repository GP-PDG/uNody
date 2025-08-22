using UnityEngine;

namespace PuppyDragon.uNody.Unity
{
    [NodeWidth(NodeSize.Large)]
    [NodeHeaderTint("#00FF85")]
    [CreateNodeMenu(true)]
    public class BreakTransform : Node
    {
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private InputPort<Transform> transform;
        [SerializeField]
        private OutputPort<Vector3> position = new(self => (self as BreakTransform).GetPosition());
        [SerializeField]
        private OutputPort<Vector3> eulerAngles = new(self => (self as BreakTransform).GetEulerAngles());
        [SerializeField]
        private OutputPort<Vector3> localEulerAngles = new(self => (self as BreakTransform).GetLocalEulerAngles());
        [SerializeField]
        private OutputPort<Vector3> lossyScale = new(self => (self as BreakTransform).GetLocalScale());
        [SerializeField]
        private OutputPort<Vector3> localScale = new(self => (self as BreakTransform).GetLossyScale());

        private Vector3 GetPosition()
            => transform.Value ? transform.Value.position : Vector3.zero;

        private Vector3 GetEulerAngles()
            => transform.Value ? transform.Value.eulerAngles : Vector3.zero;

        private Vector3 GetLocalEulerAngles()
            => transform.Value ? transform.Value.localEulerAngles : Vector3.zero;

        private Vector3 GetLocalScale()
            => transform.Value ? transform.Value.localScale : Vector3.zero;

        private Vector3 GetLossyScale()
            => transform.Value ? transform.Value.lossyScale : Vector3.zero;
    }
}
