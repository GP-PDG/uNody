using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNody
{
    [NodeWidth(NodeSize.Large)]
    [CreateNodeMenu("Sub Graph", -10, true)]
    public class SubGraphNode : Node
    {
        [SerializeField]
        private NodeGraph subGraph;

        public NodeGraph SubGraph { get => subGraph; set => subGraph = value; }

        protected override void Initialize()
        {
            if (subGraph == null)
            {
                subGraph = ScriptableObject.CreateInstance(Graph.GetType()) as NodeGraph;
                subGraph.name = "Sub Graph Body";

#if UNITY_EDITOR
                if (AssetDatabase.IsMainAsset(Graph.Root ?? Graph))
                {
                    AssetDatabase.AddObjectToAsset(subGraph, Graph);
                    foreach (var requiredNode in subGraph.Nodes)
                        AssetDatabase.AddObjectToAsset(requiredNode, Graph);
                    AssetDatabase.SaveAssets();
                }
#endif
                subGraph.Parent = Graph;
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
                Destroy(subGraph);
            else
                DestroyImmediate(subGraph, true);
        }
    }
}
