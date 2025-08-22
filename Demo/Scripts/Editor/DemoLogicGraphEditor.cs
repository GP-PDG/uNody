using UnityEngine;
using System;

namespace PuppyDragon.uNody.Logic.Demo
{
    using PuppyDragon.uNodyEditor.Logic;

    // Associate this editor with the TestGraph class
    [CustomNodeGraphEditor(typeof(DemoLogicGraph))]
    public class DemoLogicGraphEditor : LogicGraphEditor
    {
        public override string GetNodeMenuName(Type type)
        {
            // Only show nodes if their namespace is "Sample"
            if (type.Namespace != "PuppyDragon.uNody.Logic.Demo")
            {
                // Returning null hides the node from the context menu
                return null;
            }
            else
            {
                // Otherwise, use the default menu name
                return base.GetNodeMenuName(type);
            }
        }
    }
}
