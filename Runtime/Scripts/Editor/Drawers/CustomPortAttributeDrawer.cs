using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNodyEditor
{
    public static class CustomPortAttributeDrawer
    {
        private static Dictionary<Type, Type> attributeToDrawerType;

        public static Type GetDrawerType(Type attributeType)
        {
            if (attributeToDrawerType == null)
            {
                attributeToDrawerType = new Dictionary<Type, Type>();
                var customDrawers = TypeCache.GetTypesWithAttribute<CustomPropertyDrawer>();
                
                FieldInfo typeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var drawerType in customDrawers)
                {
                    var attributes = drawerType.GetCustomAttributes<CustomPropertyDrawer>();
                    foreach (var attr in attributes)
                    {
                        if (typeField != null)
                        {
                            var targetType = typeField.GetValue(attr) as Type;
                            if (targetType != null && typeof(PropertyAttribute).IsAssignableFrom(targetType))
                            {
                                if (!attributeToDrawerType.ContainsKey(targetType))
                                {
                                    attributeToDrawerType[targetType] = drawerType;
                                }
                            }
                        }
                    }
                }
            }

            attributeToDrawerType.TryGetValue(attributeType, out Type result);
            return result;
        }

        public static PropertyDrawer CreateDrawerInstance(Type drawerType, PropertyAttribute attribute, FieldInfo fieldInfo)
        {
            if (drawerType == null) return null;

            try
            {
                var drawer = (PropertyDrawer)Activator.CreateInstance(drawerType);
                
                var attrField = typeof(PropertyDrawer).GetField("m_Attribute", BindingFlags.NonPublic | BindingFlags.Instance);
                attrField?.SetValue(drawer, attribute);

                var fieldInfoField = typeof(PropertyDrawer).GetField("m_FieldInfo", BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfoField?.SetValue(drawer, fieldInfo);

                return drawer;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to create Custom PropertyDrawer instance for {drawerType}: {ex.Message}");
                return null;
            }
        }
    }
}