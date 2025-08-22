using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;

namespace PuppyDragon.uNody
{
    [CreateAssetMenu(fileName = "Blackboard", menuName = "uNody/Blackboard")]
    public class Blackboard : ScriptableObject
    {
        [SerializeField]
        private List<BlackboardVar> globalVars;

        [SerializeField]
        private List<BlackboardVar> localVars;

        private List<BlackboardVar> runtimeGlobalVars;
        private Dictionary<NodeGraph, List<BlackboardVar>> runtimeLocalVarsByGraph;

        public void SetLocalValue(NodeGraph graph, string key, object value)
        {
            VerifyLocalVars(graph);

            runtimeLocalVarsByGraph[graph].Find(x => x.Key == key).Value = value;
        }

        public T GetLocalValue<T>(NodeGraph graph, string key)
        {
            VerifyLocalVars(graph);

            return (T)runtimeLocalVarsByGraph[graph].Find(x => x.Key == key).Value;
        }

        public bool TryGetLocalValue<T>(NodeGraph graph, string key, out T value)
        {
            VerifyLocalVars(graph);

            var localVars = runtimeLocalVarsByGraph[graph];
            int findedIndex = localVars.FindIndex(x => x.Key == key);
            if (findedIndex >= 0)
            {
                value = (T)localVars[findedIndex].Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        public void SetGlobalValue(string key, object value)
        {
            VerifyGlobalVars();
            runtimeGlobalVars.Find(x => x.Key == key).Value = value;
        }

        public T GetGlobalValue<T>(string key)
        {
            VerifyGlobalVars();
            return (T)runtimeGlobalVars.Find(x => x.Key == key).Value;
        }

        public bool TryGetGlobalValue<T>(string key, out T value)
        {
            VerifyGlobalVars();

            int findedIndex = runtimeGlobalVars.FindIndex(x => x.Key == key);
            if (findedIndex >= 0)
            {
                value = (T)runtimeGlobalVars[findedIndex].Value;
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }

        private void VerifyLocalVars(NodeGraph graph)
        {
            if (runtimeLocalVarsByGraph == null)
                runtimeLocalVarsByGraph = new();

            if (!runtimeLocalVarsByGraph.TryGetValue(graph, out var localVars) ||
                (localVars.Count > 0 && localVars[0] == null))
            {
                localVars = new List<BlackboardVar>();
                foreach (var var in this.localVars)
                    localVars.Add(Instantiate(var));

                runtimeLocalVarsByGraph[graph] = localVars;
            }
        }

        private void VerifyGlobalVars()
        {
            if (runtimeGlobalVars == null)
            {
                runtimeGlobalVars = new();
                foreach (var var in globalVars)
                    runtimeGlobalVars.Add(Instantiate(var));
            }
        }

        /// <summary> Clear global and local vars </summary>
        public void ClearRuntimeVars()
        {
            if (runtimeGlobalVars != null)
            {
                foreach (var var in runtimeGlobalVars)
                {
                    if (Application.isPlaying)
                        Destroy(var);
                    else
                        DestroyImmediate(var);
                }
            }

            if (runtimeLocalVarsByGraph != null)
            {
                foreach (var pair in runtimeLocalVarsByGraph)
                {
                    foreach (var var in pair.Value)
                    {
                        if (Application.isPlaying)
                            Destroy(var);
                        else
                            DestroyImmediate(var);
                    }

                }
            }

            runtimeGlobalVars = null;
            runtimeLocalVarsByGraph = null;
        }

        public void DeleteLocalVars(NodeGraph graph)
        {
            if (runtimeLocalVarsByGraph == null)
                return;

            if (!runtimeLocalVarsByGraph.TryGetValue(graph, out var localVars))
            {
                if (localVars == null)
                    runtimeLocalVarsByGraph.Remove(graph);
                else
                {
                    foreach (var var in localVars)
                    {
                        if (Application.isPlaying)
                            Destroy(var);
                        else
                            DestroyImmediate(var);
                    }

                    runtimeLocalVarsByGraph.Remove(graph);
                }
            }
        }
    }
}
