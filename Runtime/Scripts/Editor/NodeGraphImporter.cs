using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using PuppyDragon.uNody;

namespace PuppyDragon.uNodyEditor {
    /// <summary> Deals with modified assets </summary>
    class NodeGraphImporter : AssetPostprocessor {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            foreach (string path in importedAssets) {
                // Skip processing anything without the .asset extension
                if (Path.GetExtension(path) != ".asset") continue;

                // Get the object that is requested for deletion
                NodeGraph graph = AssetDatabase.LoadAssetAtPath<NodeGraph>(path);
                if (graph == null) continue;

                // Get attributes
                var requiredNodes = graph.AddRequired();
                foreach (var requiredNode in requiredNodes)
                    AssetDatabase.AddObjectToAsset(requiredNode, graph);
            }
        }
    }
}