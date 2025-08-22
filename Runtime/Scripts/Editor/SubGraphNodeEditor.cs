using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNodyEditor
{
    using PuppyDragon.uNody;
    using PuppyDragon.uNody.Logic;

    [CustomNodeEditor(typeof(SubGraphNode))]
    public class SubGraphNodeEditor : NodeEditor
    {
        public override void OnBodyGUI()
        {
            var node = target as SubGraphNode;
            if (GUILayout.Button("Open"))
            {
                var root = node.SubGraph.Root;

                NodeEditorWindow.Open(root);

                var rootEditor = NodeGraphEditor.GetEditor(root);
                rootEditor.DrawTarget = node.SubGraph;
            }

            var subGraphNodes = node.SubGraph.Nodes;
            var entryPointNode = subGraphNodes.FirstOrDefault(x => x.GetType() == typeof(EntryPointNode));
            var exitPointNode = subGraphNodes.FirstOrDefault(x => x.GetType() == typeof(ExitPointNode));
            var inPoints = subGraphNodes.Where(x => NodeReflection.IsInPoint(x, false) && x.Graph == node.SubGraph).ToArray();
            var outPoints = subGraphNodes.Where(x => NodeReflection.IsOutPoint(x, false) && x.Graph == node.SubGraph).ToArray();

            int entryPointCount = inPoints.Count();

            if (entryPointNode)
                entryPointCount += 1;

            GUILayout.BeginVertical(GUILayout.Height(entryPointCount * 20f));
            {
                if (entryPointNode)
                    DrawPoint(entryPointNode, "prevs");

                if (exitPointNode)
                {
                    if (entryPointNode)
                        GUILayout.Space(-20f);
                    DrawPoint(exitPointNode, "next");
                }

                DrawPoints(inPoints, "input");
                GUILayout.Space(-entryPointCount * 20f);
                DrawPoints(outPoints, "output");
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawPoints<T>(IEnumerable<T> points, string portName) where T : Node
        {
            foreach (var point in points)
                DrawPoint(point, portName);
        }

        private void DrawPoint<T>(T point, string portName) where T : Node
        {
            var pointEditor = NodeEditor.GetEditor(point);
            var portProperty = pointEditor.serializedObject.FindProperty(portName);
            EditorGUILayout.PropertyField(portProperty);
        }
    }
}
