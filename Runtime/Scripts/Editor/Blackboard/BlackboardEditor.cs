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

        private readonly Dictionary<int, SerializedObject> serializedElementById = new();
        private readonly Dictionary<int, Dictionary<string, SerializedProperty>> cachedPropertiesById = new();

        private void OnEnable()
        {
            globalVars = CreateVarList(serializedObject, serializedObject.FindProperty("globalVars"));
            localVars = CreateVarList(serializedObject, serializedObject.FindProperty("localVars"));
        }

        private void OnDisable()
        {
            foreach (var so in serializedElementById.Values)
            {
                so?.Dispose();
            }
            serializedElementById.Clear();
            cachedPropertiesById.Clear();
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
                var targetObj = element.objectReferenceValue;

                if (targetObj == null)
                {
                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.PropertyField(rect, element, new GUIContent("Missing or Null Reference"));
                    return;
                }

                int instanceId = targetObj.GetInstanceID();

                var lineRect = rect;
                lineRect.y += 2;
                lineRect.height = EditorGUIUtility.singleLineHeight;

                if (!serializedElementById.TryGetValue(instanceId, out var serializedElement) || serializedElement == null)
                {
                    serializedElement = new SerializedObject(targetObj);
                    serializedElementById[instanceId] = serializedElement;
                }

                serializedElement.Update();

                var keyProperty = GetCachedSerializedProperty(serializedElement, "key");
                var valueProperty = GetCachedSerializedProperty(serializedElement, "value");

                if (keyProperty != null)
                    EditorGUI.PropertyField(lineRect, keyProperty);

                if (valueProperty != null)
                {
                    lineRect.y += EditorGUIUtility.singleLineHeight + 2;
                    lineRect.height = EditorGUI.GetPropertyHeight(valueProperty);
                    EditorGUI.PropertyField(lineRect, valueProperty);
                }

                serializedElement.ApplyModifiedProperties();

                if (rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDrag)
                {
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.StartDrag("blackboardVar");
                    DragAndDrop.objectReferences = new Object[] { targetObj };
                }
            };

            list.onRemoveCallback += (ReorderableList list) =>
            {
                var removeTarget = list.serializedProperty.GetArrayElementAtIndex(list.index);
                var targetObject = removeTarget.objectReferenceValue;

                list.serializedProperty.DeleteArrayElementAtIndex(list.index);

                if (targetObject != null)
                {
                    int id = targetObject.GetInstanceID();
                    if (serializedElementById.TryGetValue(id, out var so)) so?.Dispose();
                    serializedElementById.Remove(id);
                    cachedPropertiesById.Remove(id);

                    AssetDatabase.RemoveObjectFromAsset(targetObject);
                    AssetDatabase.SaveAssets();
                }
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
                var targetObj = element.objectReferenceValue;

                if (targetObj == null)
                    return EditorGUIUtility.singleLineHeight + 4;

                int instanceId = targetObj.GetInstanceID();

                if (!serializedElementById.TryGetValue(instanceId, out var serializedElement) || serializedElement == null)
                {
                    serializedElement = new SerializedObject(targetObj);
                    serializedElementById[instanceId] = serializedElement;
                }

                var valueProperty = GetCachedSerializedProperty(serializedElement, "value");
                if (valueProperty == null) return EditorGUIUtility.singleLineHeight + 4;

                return EditorGUIUtility.singleLineHeight + EditorGUI.GetPropertyHeight(valueProperty) + 4;
            };

            return list;
        }

        private SerializedProperty GetCachedSerializedProperty(SerializedObject serializedObj, string fieldName)
        {
            if (serializedObj == null || serializedObj.targetObject == null)
                return null;

#if UNITY_6000_3_OR_NEWER
            int instanceId = serializedObj.targetObject.GetInstanceID();
#else
            int instanceId = serializedObj.targetObject.GetEntityId();
#endif

            if (!cachedPropertiesById.TryGetValue(instanceId, out var cachedProperties))
            {
                cachedProperties = new Dictionary<string, SerializedProperty>();
                cachedPropertiesById[instanceId] = cachedProperties;
            }

            if (!cachedProperties.TryGetValue(fieldName, out var cachedProperty))
            {
                cachedProperty = serializedObj.FindProperty(fieldName);
                cachedProperties[fieldName] = cachedProperty;
            }

            return cachedProperty;
        }
    }
}
