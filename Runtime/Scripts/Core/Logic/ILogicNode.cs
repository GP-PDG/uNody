using System.Collections.Generic;

namespace PuppyDragon.uNody.Logic
{
    public interface ILogicNode
    {
        public NodePort PrevPort { get; }
        public NodePort NextPort { get; }

        public IEnumerable<ILogicNode> Prevs { get;} 
        public ILogicNode Next { get; }

        public void Execute();
    }
}
