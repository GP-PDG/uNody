using UnityEngine;
using UnityEditor;

namespace PuppyDragon.uNodyEditor.Logic
{
    using PuppyDragon.uNody.Logic;

    [CustomNodeGraphEditor(typeof(LogicGraph))]
    public class LogicGraphEditor : NodeGraphEditor
    {
        public override void OnToolbarGUI()
        {
            if (GUILayout.Button("Run", EditorStyles.toolbarButton))
            {
                var logicGraph = target as LogicGraph;
                logicGraph.Execute();
                logicGraph.Blackboard?.ClearRuntimeVars();
            }
        }
    }
}