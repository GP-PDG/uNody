using UnityEngine;
using System;
using PuppyDragon.uNody.Logic;

namespace PuppyDragon.uNody
{
    public static class NodeReflection
    {
        public static bool IsInPoint(Node node, bool isIncludeEntry = true)
            => IsInPoint(node.GetType(), isIncludeEntry);
        public static bool IsInPoint(Type type, bool isIncludeEntry = true)
            => (isIncludeEntry && type == typeof(EntryPointNode)) || IsSubclassOf(type, typeof(InPointNode<>));

        public static bool IsOutPoint(Node node, bool isIncludeExit = true)
            => IsOutPoint(node.GetType(), isIncludeExit);
        public static bool IsOutPoint(Type type, bool isIncludeExit = true)
            => (isIncludeExit && type == typeof(ExitPointNode)) || IsSubclassOf(type, typeof(OutPointNode<>));

        private static bool IsSubclassOf(Type derivedType, Type genericBaseType)
        {
            while (derivedType != null && derivedType != typeof(object))
            {
                if (derivedType.IsGenericType && derivedType.GetGenericTypeDefinition() == genericBaseType)
                    return true;

                derivedType = derivedType.BaseType;
            }

            return false;
        }
    }
}
