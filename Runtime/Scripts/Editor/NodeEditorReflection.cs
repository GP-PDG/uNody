using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNodyEditor
{
    using PuppyDragon.uNody;
    using PuppyDragon.uNody.Logic;

    /// <summary> Contains reflection-related extensions built for PuppyDragon.uNody </summary>
    public static class NodeEditorReflection
    {
        private static Dictionary<Type, Color> nodeHeaderTint;
        private static Dictionary<Type, Color> nodeBodyTint;
        private static Dictionary<Type, Color> nodeFooterTint;
        private static Dictionary<Type, int> nodeWidth;
        private static Dictionary<Type, Texture> nodeIcon;

        private static Type[] nodeTypes;
        private static Type[] nodeTypesWithoutLogic;

        /// <summary> All available node types </summary>
        public static Type[] NodeTypes => nodeTypes != null ? nodeTypes : nodeTypes = GetNodeTypes();
        public static Type[] NodeTypesWithoutLogic => nodeTypesWithoutLogic != null ? nodeTypesWithoutLogic : nodeTypesWithoutLogic = GetNodeTypesWithoutLogic();

        public static void ClearColorCache()
        {
            nodeHeaderTint = null;
            nodeBodyTint = null;
        }

        /// <summary> Return a delegate used to determine whether window is docked or not. It is faster to cache this delegate than run the reflection required each time. </summary>
        public static Func<bool> GetIsDockedDelegate(this EditorWindow window)
        {
            var fullBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var isDockedMethod = typeof(EditorWindow).GetProperty("docked", fullBinding).GetGetMethod(true);
            return (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), window, isDockedMethod);
        }

        //Get all classes deriving from Node via reflection
        public static Type[] GetNodeTypes()
            => GetDerivedTypes(typeof(Node));

        public static Type[] GetNodeTypesWithoutLogic()
            => NodeTypes.Where(x => !typeof(ILogicNode).IsAssignableFrom(x)).ToArray();

        public static bool TryGetAttributeHeaderTint(this Type nodeType, Node node, out Color tint)
        {
            if (nodeHeaderTint == null)
            {
                CacheAttributes<Color, Node.NodeHeaderTintAttribute>(ref nodeHeaderTint, x =>
                {
                    if (x.colorTargetType != null)
                        return NodeEditorPreferences.GetTypeColor(NodeGraphEditor.GetEditor(node.Graph), x.colorTargetType);
                    else
                        return x.color;
                });
            }

            return nodeHeaderTint.TryGetValue(nodeType, out tint);
        }

        public static bool TryGetAttributeFooterTint(this Type nodeType, Node node, out Color tint)
        {
            if (nodeFooterTint == null)
            {
                CacheAttributes<Color, Node.NodeFooterTintAttribute>(ref nodeFooterTint, x =>
                {
                    if (x.colorTargetType != null)
                        return NodeEditorPreferences.GetTypeColor(NodeGraphEditor.GetEditor(node.Graph), x.colorTargetType);
                    else
                        return x.color;
                });
            }

            return nodeFooterTint.TryGetValue(nodeType, out tint);
        }

        /// <summary> Custom node tint colors defined with [NodeColor(r, g, b)] </summary>
        public static bool TryGetAttributeBodyTint(this Type nodeType, out Color tint)
        {
            if (nodeBodyTint == null)
                CacheAttributes<Color, uNody.Node.NodeBodyTintAttribute>(ref nodeBodyTint, x => x.color);

            return nodeBodyTint.TryGetValue(nodeType, out tint);
        }

        /// <summary> Get custom node widths defined with [NodeWidth(width)] </summary>
        public static bool TryGetAttributeWidth(this Type nodeType, out int width)
        {
            if (nodeWidth == null)
                CacheAttributes<int, uNody.Node.NodeWidthAttribute>(ref nodeWidth, x => x.width);

            return nodeWidth.TryGetValue(nodeType, out width);
        }

        public static bool TryGetAttributeNodeIcon(this Type nodeType, out Texture icon)
        {
            if (nodeIcon == null)
            {
                CacheAttributes<Texture, Node.NodeIconAttribute>(ref nodeIcon, x =>
                {
                    if (x.path.EndsWith(".png"))
                        return AssetDatabase.LoadAssetAtPath<Texture>(x.path);
                    else
                        return EditorGUIUtility.IconContent(x.path).image as Texture;

                });
            }

            return nodeIcon.TryGetValue(nodeType, out icon);
        }

        private static void CacheAttributes<V, A>(ref Dictionary<Type, V> dict, Func<A, V> getter) where A : Attribute
        {
            dict = new Dictionary<Type, V>();
            for (int i = 0; i < NodeTypes.Length; i++)
            {
                var attribs = NodeTypes[i].GetCustomAttributes(typeof(A), true);
                if (attribs == null || attribs.Length == 0)
                    continue;

                var attrib = attribs[0] as A;
                dict.Add(NodeTypes[i], getter(attrib));
            }
        }



        /// <summary> Get FieldInfo of a field, including those that are private and/or inherited </summary>
        public static FieldInfo GetFieldInfo(this Type type, string fieldName)
        {
            // If we can't find field in the first run, it's probably a private field in a base class.
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Search base classes for private fields only. Public fields are found above
            while (field == null && (type = type.BaseType) != typeof(uNody.Node))
                field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            return field;
        }

        /// <summary> Get all classes deriving from baseType via reflection </summary>
        public static Type[] GetDerivedTypes(this Type baseType)
        {

            var types = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try { types.AddRange(assembly.GetTypes().Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t)).ToArray()); }
                catch (ReflectionTypeLoadException) { }
            }
            return types.ToArray();
        }

        /// <summary> Find methods marked with the [ContextMenu] attribute and add them to the context menu </summary>
        public static void AddCustomContextMenuItems(this AdvancedGenericMenu contextMenu, object obj)
        {
            var items = GetContextMenuMethods(obj);
            if (items.Length == 0)
                return;

            contextMenu.AddSeparator("");
            foreach (var item in items)
            {
                if (!item.Key.validate)
                    continue;

                contextMenu.AddItem(new GUIContent(item.Key.menuItem), false, () => item.Value.Invoke(obj, null));
            }
        }

        /// <summary> Call OnValidate on target </summary>
        public static void TriggerOnValidate(this UnityEngine.Object target)
        {
            MethodInfo onValidate = null;
            if (target != null)
            {
                onValidate = target.GetType().GetMethod("OnValidate", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (onValidate != null)
                    onValidate.Invoke(target, null);
            }
        }

        public static KeyValuePair<ContextMenu, MethodInfo>[] GetContextMenuMethods(object obj)
        {
            var type = obj.GetType();
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            var kvp = new List<KeyValuePair<ContextMenu, MethodInfo>>();
            foreach (var method in methods)
            { 
                var attribs = method.GetCustomAttributes(typeof(ContextMenu), true).Select(x => x as ContextMenu).ToArray();
                if (attribs == null || attribs.Length == 0)
                    continue;

                if (method.GetParameters().Length != 0)
                {
                    Debug.LogWarning("Method " + method.DeclaringType.Name + "." + method.Name + " has parameters and cannot be used for context menu commands.");
                    continue;
                }

                if (method.IsStatic) {
                    Debug.LogWarning("Method " + method.DeclaringType.Name + "." + method.Name + " is static and cannot be used for context menu commands.");
                    continue;
                }

                foreach (var attrib in attribs)
                    kvp.Add(new KeyValuePair<ContextMenu, MethodInfo>(attrib, method));
            }

            //Sort menu items
            kvp.Sort((x, y) => x.Key.priority.CompareTo(y.Key.priority));

            return kvp.ToArray();
        }

        /// <summary> Very crude. Uses a lot of reflection. </summary>
        public static void OpenPreferences()
            => SettingsService.OpenUserPreferences("Preferences/Node Editor");
    }
}
