using UnityEngine;

namespace PuppyDragon.uNody.Logic.Demo
{
    public class LogicGraphController : MonoBehaviour
    {
        [SerializeField]
        private LogicGraph logicGraph;

        void Start()
        {
            logicGraph.Execute();
        }
    }
}