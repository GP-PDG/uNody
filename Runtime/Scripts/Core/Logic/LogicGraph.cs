
using System.Linq;
using UnityEngine;

namespace PuppyDragon.uNody.Logic
{

    [CreateAssetMenu(fileName = "Logic Graph", menuName = "uNody/Logic Graph")]
    [RequireNode(typeof(EntryPointNode), typeof(ExitPointNode))]
    public class LogicGraph : NodeGraph
    {
        private EntryPointNode entryPoint;
        private ExitPointNode exitPoint;

        private bool isAborting;

        public EntryPointNode EntryPoint => entryPoint ??= Nodes.First(x => x.GetType() == typeof(EntryPointNode)) as EntryPointNode;
        public ExitPointNode ExitPoint => exitPoint ??= Nodes.First(x => x.GetType() == typeof(ExitPointNode)) as ExitPointNode;

        public ILogicNode CurrentNode { get; private set; }

        public bool IsAborting => isAborting;

        public void Execute()
        {
            isAborting = false;

            CurrentNode = EntryPoint;
            while (!isAborting && (CurrentNode = CurrentNode.Next) != null)
                CurrentNode.Execute();
        }

        public void Step()
        {
            CurrentNode ??= EntryPoint;
            CurrentNode = CurrentNode.Next;
            CurrentNode.Execute();
        }

        public void Abort()
        {
            if (CurrentNode != null)
                isAborting = true;
        }
    }
}
