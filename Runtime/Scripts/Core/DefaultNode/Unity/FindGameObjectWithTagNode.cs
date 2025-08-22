using System;
using UnityEngine;

namespace PuppyDragon.uNody.Unity
{
    [NodeWidth(NodeSize.Large)]
    [NodeIcon("GameObject Icon")]
    [CreateNodeMenu(true)]
    public class FindGameObjectWithTagNode : Node
    {
        [SerializeField]
        private OutputPort<GameObject> result = new(self => (self as FindGameObjectWithTagNode).Find());
        [SerializeField]
        private InputPort<string> tag;

        private GameObject Find()
        {
            try
            {
                return !string.IsNullOrEmpty(tag.Value) ? GameObject.FindWithTag(tag.Value) : null;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return null;
            }
        }
    }
}