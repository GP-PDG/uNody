using UnityEngine;

namespace PuppyDragon.uNody.Unity
{
    [NodeWidth(NodeSize.Large)]
    [NodeIcon("GameObject Icon")]
    [CreateNodeMenu(true)]
    public class FindGameObjectNode : Node
    {
        [SerializeField]
        private OutputPort<GameObject> result = new(self => (self as FindGameObjectNode).Find());
        [SerializeField]
        private InputPort<string> goName;

        private GameObject Find()
            => !string.IsNullOrEmpty(goName.Value) ? GameObject.FindWithTag(goName.Value) : null;
    }
}
