using UnityEditor;
using UnityEngine;
using System.IO;

namespace PuppyDragon.uNodyEditor {
    /// <summary> Deals with modified assets </summary>
    class NodeEditorAssetModProcessor : AssetModificationProcessor {

        /// <summary> Automatically delete Node sub-assets before deleting their script.
        /// This is important to do, because you can't delete null sub assets.
        /// <para/> For another workaround, see: https://gitlab.com/RotaryHeart-UnityShare/subassetmissingscriptdelete </summary> 
        private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            // Skip processing anything without the .cs extension
            if (Path.GetExtension(path) != ".cs")
                return AssetDeleteResult.DidNotDelete;
            
            // Get the object that is requested for deletion
            Object obj = AssetDatabase.LoadAssetAtPath<Object> (path);

            // If we aren't deleting a script, return
            if (!(obj is MonoScript))
                return AssetDeleteResult.DidNotDelete;

            // Check script type. Return if deleting a non-node script
            var script = obj as MonoScript;
            System.Type scriptType = script.GetClass ();
            if ((scriptType == null || (scriptType != typeof(uNody.Node)) &&
                !scriptType.IsSubclassOf(typeof(uNody.Node))))
                return AssetDeleteResult.DidNotDelete;

            // Find all ScriptableObjects using this script
            string[] guids = AssetDatabase.FindAssets ("t:" + scriptType);
            for (int i = 0; i < guids.Length; i++)
            {
                string assetpath = AssetDatabase.GUIDToAssetPath (guids[i]);
                var objs = AssetDatabase.LoadAllAssetRepresentationsAtPath (assetpath);
                for (int k = 0; k < objs.Length; k++)
                {
                    uNody.Node node = objs[k] as uNody.Node;
                    if (node.GetType() == scriptType)
                    {
                        if (node != null && node.Graph != null)
                        {
                            // Delete the node and notify the user
                            Debug.LogWarning (node.name + " of " + node.Graph + " depended on deleted script and has been removed automatically.", node.Graph);
                            node.Graph.RemoveNode(node);
                            AssetDatabase.RemoveObjectFromAsset(node);
                        }
                    }
                }
            }
            // We didn't actually delete the script. Tell the internal system to carry on with normal deletion procedure
            return AssetDeleteResult.DidNotDelete;
        }

        /// <summary> Automatically re-add loose node assets to the Graph node list </summary>
        // [InitializeOnLoadMethod]
        // private static void OnReloadEditor ()
        // {
        //     // Find all NodeGraph assets
        //     var guids = AssetDatabase.FindAssets ("t:" + typeof (uNody.NodeGraph));
        //     for (int i = 0; i < guids.Length; i++)
        //     {
        //         string assetpath = AssetDatabase.GUIDToAssetPath(guids[i]);
        //         uNody.NodeGraph graph = AssetDatabase.LoadAssetAtPath(assetpath, typeof(uNody.NodeGraph)) as uNody.NodeGraph;
        //         RelinkNodes(graph);
        //     }
        // }

        // private static void RelinkNodes(uNody.NodeGraph rootGraph)
        // {
        //     rootGraph.RemoveNodeAll(x => x == null); //Remove null items
        //     foreach (var node in rootGraph.Nodes)
        //     {
        //         if (!rootGraph.Contains(node))
        //             rootGraph.AddNodeDirectely(node);

        //         if (node.Graph != rootGraph)
        //             node.Graph = rootGraph;
        //     }

        //     foreach (var child in rootGraph.Children)
        //         RelinkNodes(child);
        // }
    }
}