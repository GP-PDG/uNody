using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace PuppyDragon.uNodyEditor
{
    using PuppyDragon.uNody;
    using System.Collections.Generic;
    using System.Linq;

    [CustomEditor(typeof(Blackboard))]
    public class BlackboardEditor : Editor
    {
        private ReorderableList globalVars;
        private ReorderableList localVars;

        private Dictionary<SerializedObject, Dictionary<string, SerializedProperty>> cachedPropertiesByObject = new();
        private Dictionary<Object, SerializedObject> serializedElementByObject = new();

        private void OnEnable()
        {
            globalVars = CreateVarList(serializedObject, serializedObject.FindProperty("globalVars"));
            localVars = CreateVarList(serializedObject, serializedObject.FindProperty("localVars"));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            globalVars.DoLayoutList();
            EditorGUILayout.Space();
            localVars.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private ReorderableList CreateVarList(SerializedObject serializedObject, SerializedProperty property)
        {
            var list = new ReorderableList(serializedObject, property);

            list.drawHeaderCallback += (Rect rect) =>
            {
                EditorGUI.LabelField(rect, property.displayName);
            };

            list.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);

                var lineRect = rect;
                lineRect.position = new Vector2(lineRect.position.x, lineRect.position.y + 2);
                lineRect.height = EditorGUIUtility.singleLineHeight;

                if (!serializedElementByObject.TryGetValue(element.objectReferenceValue, out var serializedElement))
                {
                    serializedElement = new SerializedObject(element.objectReferenceValue);
                    serializedElementByObject[element.objectReferenceValue] = serializedElement;
                }

                serializedElement.Update();

                EditorGUI.PropertyField(lineRect, GetCachedSerializedProperty(serializedElement, "key"));

                var valueProperty = GetCachedSerializedProperty(serializedElement, "value");

                lineRect.position = new Vector2(lineRect.position.x, lineRect.position.y + EditorGUIUtility.singleLineHeight + 2);
                lineRect.height = EditorGUI.GetPropertyHeight(valueProperty);

                EditorGUI.PropertyField(lineRect, valueProperty);

                serializedElement.ApplyModifiedProperties();

                if (rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.StartDrag("blackboardVar");
                    DragAndDrop.objectReferences = new Object[] { element.objectReferenceValue };
                }
            };

            list.onRemoveCallback += (ReorderableList list) =>
            {
                var removeTarget = list.serializedProperty.GetArrayElementAtIndex(list.index);
                var targetObject = removeTarget.objectReferenceValue;
                list.serializedProperty.DeleteArrayElementAtIndex(list.index);
                AssetDatabase.RemoveObjectFromAsset(targetObject);
                AssetDatabase.SaveAssets();
            };

            list.onAddDropdownCallback += (Rect buttonRect, ReorderableList list) =>
            {
                var menu = new GenericMenu();
                var types = TypeCache.GetTypesDerivedFrom<BlackboardVar>().Where(x => !x.IsGenericType);
                foreach (var type in types)
                {
                    string name = type.Name.Replace("Blackboard", "");
                    menu.AddItem(new GUIContent(name), false, () =>
                    {
                        var newVar = CreateInstance(type);
                        newVar.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                        AssetDatabase.AddObjectToAsset(newVar, serializedObject.targetObject);

                        var lastIndex = list.serializedProperty.arraySize++;
                        list.serializedProperty.GetArrayElementAtIndex(lastIndex).objectReferenceValue = newVar;

                        serializedObject.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                    });
                }
                menu.DropDown(buttonRect);
            };

            list.elementHeightCallback += (index) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                var serializedElement = new SerializedObject(element.objectReferenceValue);
                return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(serializedElement.FindProperty("value")) + 4;
            };

            return list;
        }

        private SerializedProperty GetCachedSerializedProperty(SerializedObject property, string fieldName)
        {
            if (!cachedPropertiesByObject.TryGetValue(property, out var cachedProperties))
            {
                cachedProperties = new Dictionary<string, SerializedProperty>();
                cachedPropertiesByObject[property] = cachedProperties;
            }

            if (!cachedProperties.TryGetValue(fieldName, out var cachedProperty))
            {
                cachedProperty = property.FindProperty(fieldName);
                cachedProperties[fieldName] = cachedProperty;
            }

            return cachedProperty;
        }
    }
}
