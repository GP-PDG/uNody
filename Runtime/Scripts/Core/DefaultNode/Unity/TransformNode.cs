using UnityEngine;

namespace PuppyDragon.uNody.Unity
{
    [NodeWidth(NodeSize.Large)]
    [CreateNodeMenu(true)]
    public class TransformNode : Node
    {
        [SerializeField]
        private OutputPort<Transform> result = new(self => (self as TransformNode).GetTransform());
        [SerializeField]
        private InputPort<GameObject> gameObject;

        private Transform GetTransform()
            => gameObject.Value != null ? gameObject.Value.transform : null;
    }
}
