using UnityEngine;

namespace PuppyDragon.uNody.Demo
{
    public class DemoNode : Node
    {
        [SerializeField]
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        private InputPort<float> inputValue;
        [SerializeField]
        [SpaceLine(-1)]
        private OutputPort<float> outputPort = new(Pow);
        // outputPort = new(x => (x as DemoNode).inputValue.Value * (x as DemoNode).inputValue.Value);
        // outputPort = new(x => (x as DemoNode).Pow());

        private static float Pow(Node node)
        {
            var demoNode = node as DemoNode;
            return demoNode.inputValue.Value * demoNode.inputValue.Value;
        }
    }
}
