using UnityEngine;

namespace PuppyDragon.uNody.Demo
{

    public class NodeGraphController : MonoBehaviour
    {
        [SerializeField]
        private NodeGraph nodeGraph;

        void Start()
        {
            nodeGraph.SetInValue("inputValue", 20f);
            var result = nodeGraph.GetOutValue<float>("outputValue");

            Debug.Log("NodeGraph Result: " + result);
        }

    }
}
