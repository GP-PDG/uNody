using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PuppyDragon.uNody.Utility
{
    [NodeWidth(NodeSize.Small)]
    [NodeHeaderTint(typeof(bool))]
    [CreateNodeMenu(-5, true)]
    public abstract class ComparisonNode<T> : Node where T : IComparable
    {
        public enum Method { Less, LessOrEqual, Equal, GreaterOrEqual, Greater }

        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
        [SerializeField]
        private OutputPort<bool> result = new(self => (self as ComparisonNode<T>).GetResult());
        [PortSettings(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.InheritedAny)]
        [SpaceLine(-1)]
        [SerializeField]
        private InputPort<T> a;
        [NodeEnum(true)]
        [SerializeField]
        private Method op;
        [PortSettings(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.InheritedAny)]
        [SerializeField]
        private InputPort<T> b;

        private bool GetResult()
        {
            int value = a.Value.CompareTo(b.Value);
            return op switch
            {
                Method.Less => value < 0,
                Method.LessOrEqual => value <= 0,
                Method.Equal => value == 0,
                Method.GreaterOrEqual => value >= 0,
                Method.Greater => value > 0,
                _ => throw new InvalidCastException(),
            };
        }
    }
}
