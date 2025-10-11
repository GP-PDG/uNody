using PuppyDragon.uNody;
using UnityEditor;
using UnityEngine;
using System;

namespace PuppyDragon.uNodyEditor
{
    [CustomEditor(typeof(SceneGraph), true)]
    public class SceneGraphEditor : Editor
    {
        private SceneGraph sceneGraph;
        private bool isReallyRemove;
        private Type graphType;

        public override void OnInspectorGUI()
        {
            if (sceneGraph.graph == null)
            {
                if (GUILayout.Button("New Graph", GUILayout.Height(40)))
                {
                    if (graphType == null)
                    {
                        var graphTypes = NodeEditorReflection.GetDerivedTypes(typeof(NodeGraph));
                        var menu = new GenericMenu();

                        for (int i = 0; i < graphTypes.Length; i++)
                        {
                            Type graphType = graphTypes[i];
                            menu.AddItem(new GUIContent(graphType.Name), false, () => CreateGraph(graphType));
                        }

                        menu.ShowAsContext();
                    }
                    else
                        CreateGraph(graphType);
                }
            }
            else
            {
                if (GUILayout.Button("Open Graph", GUILayout.Height(40)))
                    NodeEditorWindow.Open(sceneGraph.graph);

                if (isReallyRemove)
                {
                    GUILayout.BeginHorizontal();
                    {
                        GUILayout.Label("Really remove graph?");

                        GUI.color = new Color(1, 0.8f, 0.8f);
                        if (GUILayout.Button("Remove"))
                        {
                            isReallyRemove = false;
                            Undo.RecordObject(sceneGraph, "Removed graph");
                            sceneGraph.graph = null;
                        }

                        GUI.color = Color.white;
                        if (GUILayout.Button("Cancel"))
                        {
                            isReallyRemove = false;
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUI.color = new Color(1, 0.8f, 0.8f);
                    if (GUILayout.Button("Remove graph"))
                        isReallyRemove = true;
                    GUI.color = Color.white;
                }
            }
            DrawDefaultInspector();
        }

        private void OnEnable()
        {
            sceneGraph = target as SceneGraph;

            var sceneGraphType = sceneGraph.GetType();
            if (sceneGraphType == typeof(SceneGraph))
                graphType = null;
            else
            {
                var baseType = sceneGraphType.BaseType;
                if (baseType.IsGenericType)
                    graphType = sceneGraphType = baseType.GetGenericArguments()[0];
            }
        }

        public void CreateGraph(Type type)
        {
            Undo.RecordObject(sceneGraph, "Create Graph");
            sceneGraph.graph = ScriptableObject.CreateInstance(type) as NodeGraph;
            sceneGraph.graph.name = sceneGraph.name;
        }
    }
}
