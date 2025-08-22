using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNodyEditor
{
    using PuppyDragon.uNody;
    using System.Collections.Generic;

    [CustomPropertyDrawer(typeof(NodePort), true)]
    public class NodePortDrawer : PropertyDrawer
    {
        public static bool IsNeedUpdatePosition { get; set; } = true;

        private Dictionary<SerializedProperty, Dictionary<string, SerializedProperty>> cachedPropertiesByObject = new();
        private float cachedSpace = 0;
        private GUIStyle textAreaStyle;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string fieldName = GetCachedSerializedProperty(property, "fieldName").stringValue;

            if (textAreaStyle == null)
            {
                textAreaStyle = new GUIStyle(EditorStyles.textArea);
                textAreaStyle.wordWrap = true;
            }

            if (!string.IsNullOrEmpty(fieldName))
            {
                position.y += cachedSpace;

                var node = (property.serializedObject.targetObject as Node);
                string elementName = GetCachedSerializedProperty(property, "elementName").stringValue;
                var port = string.IsNullOrEmpty(elementName) ? node.GetPort(fieldName) : node.GetPort(elementName);
                if (port != null)
                {
                    if (port.IsElement)
                        label.text = label.text.Replace("Element ", "");

                    var graphEditor = NodeGraphEditor.GetEditor((property.serializedObject.targetObject as Node).Graph);
                    var style = graphEditor.GetPortStyle(port);

                    var defaultValueProperty = GetCachedSerializedProperty(property, "defaultValue");
                    var hasFuncProperty = GetCachedSerializedProperty(property, "hasFunc");

                    bool isConnector = (port.Direction == NodePort.IO.Input && NodeReflection.IsInPoint(node)) ||
                        (port.Direction == NodePort.IO.Output && NodeReflection.IsOutPoint(node));

                    if (isConnector)
                    {
                        label.text = node.name;
                        if (port.Direction == NodePort.IO.Input)
                            position.x -= 3f;
                        else
                            position.x += 3f;
                    }
                    
                    var fieldNames = fieldName.Split('.')[0];
                    if (NodeEditorUtilities.GetCachedAttrib<Node.PortSettingsAttribute>(
                        property.serializedObject.targetObject.GetType(), fieldName, out var attribute))
                    {
                        if (attribute.isHideLabel)
                            label.text = string.Empty;
                    }

                    NodeEditorUtilities.GetCachedAttrib<Node.MultilineAttribute>(
                                    property.serializedObject.targetObject.GetType(), fieldName, out var multilineAttribute);

                    if (defaultValueProperty != null && (hasFuncProperty == null || !hasFuncProperty.boolValue))
                    {
                        switch (port.ShowBackingValueType)
                        {
                            case Node.ShowBackingValue.Unconnected:
                                // Display a label if port is connected
                                if (port.IsConnected)
                                {
                                    position.height = EditorGUIUtility.singleLineHeight;
                                    if (port.Direction == NodePort.IO.Input)
                                        EditorGUI.LabelField(position, label);
                                    else
                                        EditorGUI.LabelField(position, label, NodeEditorStyles.OutputPortLabel);
                                }
                                // Display an editable property field if port is not connected
                                else
                                {
                                    if (multilineAttribute != null)
                                    {
                                        var multilineRect = position;

                                        if (!string.IsNullOrEmpty(label.text))
                                        {
                                            multilineRect.height = EditorGUIUtility.singleLineHeight;
                                            EditorGUI.LabelField(multilineRect, label.text);
                                            multilineRect.y += EditorGUIUtility.singleLineHeight;
                                        }

                                        multilineRect.height = EditorGUIUtility.singleLineHeight * multilineAttribute.line;
                                        defaultValueProperty.stringValue = EditorGUI.TextArea(multilineRect, defaultValueProperty.stringValue, textAreaStyle);
                                    }
                                    else
                                    {
                                        EditorGUI.PropertyField(position, defaultValueProperty, label, true);
                                    }
                                }
                                break;

                            case Node.ShowBackingValue.Never:
                                position.height = EditorGUIUtility.singleLineHeight;
                                // Display a label
                                if (port.Direction == NodePort.IO.Input)
                                    EditorGUI.LabelField(position, label);
                                else
                                    EditorGUI.LabelField(position, label, NodeEditorStyles.OutputPortLabel);
                                break;

                            case Node.ShowBackingValue.Always:
                                if (multilineAttribute != null)
                                {
                                    var multilineRect = position;

                                    if (!string.IsNullOrEmpty(label.text))
                                    {
                                        multilineRect.height = EditorGUIUtility.singleLineHeight;
                                        EditorGUI.LabelField(multilineRect, label.text);
                                        multilineRect.y += EditorGUIUtility.singleLineHeight;
                                    }

                                    multilineRect.height = EditorGUIUtility.singleLineHeight * multilineAttribute.line;
                                    defaultValueProperty.stringValue = EditorGUI.TextArea(multilineRect, defaultValueProperty.stringValue, textAreaStyle);
                                }
                                else
                                {
                                    EditorGUI.PropertyField(position, defaultValueProperty, label, true);
                                }
                                break;
                        }
                    }
                    else
                    {
                        position.height = EditorGUIUtility.singleLineHeight;
                        if (port.Direction == NodePort.IO.Input)
                            EditorGUI.LabelField(position, label);
                        else
                            EditorGUI.LabelField(position, label, NodeEditorStyles.OutputPortLabel);
                    }

                    // If property is an input, display a regular property field and put a port handle on the left side
                    if (port.Direction == NodePort.IO.Input)
                    {
                        float indentLevel = EditorGUI.indentLevel;
                        if (property.propertyPath.Contains("Array.data"))
                            indentLevel += 1.9f;

                        position.x -= (NodeEditorStyles.PortSize.x * (1.4f * (indentLevel + 1)));
                        position.y += style.padding.top;
                    }
                    else
                    {
                        position.x += position.width + (NodeEditorStyles.PortSize.x * 0.5f) - 1;
                        position.y += style.padding.top;
                    }

                    position.size = NodeEditorStyles.PortSize;

                    Color emptyColor = graphEditor.GetPortEmptyColor(port);
                    Color filledColor = graphEditor.GetPortFilledColor(port);
                    var portStyle = graphEditor.GetPortStyle(port);

                    Color col = GUI.color;
                    if (port.IsConnected)
                    {
                        GUI.color = filledColor;
                        GUI.DrawTexture(position, portStyle.active.background);
                    }
                    else if (NodeGraphEditor.DraggedOutputPort == port || NodeGraphEditor.DraggedOutputPortTarget == port)
                    {
                        GUI.color = emptyColor;
                        GUI.DrawTexture(position, portStyle.hover.background);
                    }
                    else
                    {
                        GUI.color = emptyColor;
                        GUI.DrawTexture(position, portStyle.normal.background);
                    }
                    GUI.color = col;

                    if (isConnector)
                    {
                        var root = node.Graph.Parent ?? node.Graph;
                        node = root.Nodes.First(x => (x is SubGraphNode subGraphNode) && subGraphNode.SubGraph == node.Graph);
                    }

                    if (Event.current.type == EventType.Repaint && IsNeedUpdatePosition)
                    {
                        Vector2 portPosition = node.NodePosition + position.center;
                        port.Rect = new Rect(portPosition.x - 8, portPosition.y - 8, 16, 16);
                    }
                }
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var node = (property.serializedObject.targetObject as Node);
            string fieldName = GetCachedSerializedProperty(property, "fieldName").stringValue;
            string elementName = GetCachedSerializedProperty(property, "elementName").stringValue;
            if (!string.IsNullOrEmpty(fieldName) && string.IsNullOrEmpty(elementName))
            {
                var port = string.IsNullOrEmpty(elementName) ? node.GetPort(fieldName) : node.GetPort(elementName);

                if (NodeEditorUtilities.GetCachedAttrib<Node.SpaceLineAttribute>(
                    property.serializedObject.targetObject.GetType(), fieldName, out var spaceLine))
                    cachedSpace = spaceLine.height;
                else
                    cachedSpace = 0;

                var defaultValueProperty = GetCachedSerializedProperty(property, "defaultValue");
                var hasFuncProperty = GetCachedSerializedProperty(property, "hasFunc");
                float defaultHeight = 0f;

                NodeEditorUtilities.GetCachedAttrib<Node.MultilineAttribute>(
                        property.serializedObject.targetObject.GetType(), fieldName, out var multilineAttribute);

                if (port == null ||
                    defaultValueProperty == null ||
                    (hasFuncProperty != null && hasFuncProperty.boolValue) ||
                    port.ShowBackingValueType == Node.ShowBackingValue.Never ||
                    (port.ShowBackingValueType == Node.ShowBackingValue.Unconnected && port.IsConnected))
                {
                    defaultHeight = EditorGUIUtility.singleLineHeight;
                }
                else
                {
                    if (multilineAttribute != null)
                        defaultHeight = EditorGUIUtility.singleLineHeight * (multilineAttribute.line + 2);
                    else
                        defaultHeight = EditorGUI.GetPropertyHeight(defaultValueProperty);
                }

                if (NodeEditorUtilities.GetCachedAttrib<Node.PortSettingsAttribute>(
                    property.serializedObject.targetObject.GetType(), fieldName, out var attribute))
                {
                    if (attribute.isHideLabel && !Mathf.Approximately(defaultHeight, EditorGUIUtility.singleLineHeight))
                        defaultHeight -= (multilineAttribute != null) ? EditorGUIUtility.singleLineHeight * 2f : EditorGUIUtility.singleLineHeight;
                }
                return defaultHeight + cachedSpace;
            }
            else
                return EditorGUI.GetPropertyHeight(property);
        }

        private SerializedProperty GetCachedSerializedProperty(SerializedProperty property, string fieldName)
        {
            if (!cachedPropertiesByObject.TryGetValue(property, out var cachedProperties))
            {
                cachedProperties = new Dictionary<string, SerializedProperty>();
                cachedPropertiesByObject[property] = cachedProperties;
            }

            if (!cachedProperties.TryGetValue(fieldName, out var cachedProperty))
            {
                cachedProperty = property.FindPropertyRelative(fieldName);
                cachedProperties[fieldName] = cachedProperty;
            }

            return cachedProperty;
        }
    }
}
