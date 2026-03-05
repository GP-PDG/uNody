using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNodyEditor
{
    using PuppyDragon.uNody;

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
                textAreaStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            }

            if (!string.IsNullOrEmpty(fieldName))
            {
                position.y += cachedSpace;

                var node = property.serializedObject.targetObject as Node;
                string elementName = GetCachedSerializedProperty(property, "elementName").stringValue;
                var port = string.IsNullOrEmpty(elementName) ? node.GetPort(fieldName) : node.GetPort(elementName);

                if (port != null)
                {
                    if (port.IsElement)
                        label.text = label.text.Replace("Element ", "");

                    var graphEditor = NodeGraphEditor.GetEditor(node.Graph);
                    var style = graphEditor.GetPortStyle(port);

                    var defaultValueProperty = GetCachedSerializedProperty(property, "defaultValue");
                    var hasFuncProperty = GetCachedSerializedProperty(property, "hasFunc");

                    bool isConnector = (port.Direction == NodePort.IO.Input && NodeReflection.IsInPoint(node)) ||
                                       (port.Direction == NodePort.IO.Output && NodeReflection.IsOutPoint(node));

                    if (isConnector)
                    {
                        label.text = node.name;
                        position.x += port.Direction == NodePort.IO.Input ? -3f : 3f;
                    }

                    Type nodeType = node.GetType();

                    // Apply standard TooltipAttribute if it exists
                    var attributes = NodeEditorUtilities.GetCachedPropertyAttribs(nodeType, fieldName) ?? new List<PropertyAttribute>();
                    var tooltipAttr = attributes.OfType<TooltipAttribute>().FirstOrDefault();
                    if (tooltipAttr != null)
                    {
                        label.tooltip = tooltipAttr.tooltip;
                    }

                    if (NodeEditorUtilities.GetCachedAttrib<Node.PortSettingsAttribute>(nodeType, fieldName, out var portSettings))
                    {
                        if (portSettings.isHideLabel)
                            label.text = string.Empty;
                    }

                    DrawPortValue(position, port, label, defaultValueProperty, hasFuncProperty, nodeType, fieldName, attributes);
                    DrawPortHandle(position, port, property, style, graphEditor, isConnector, node);
                }
            }

            EditorGUI.EndProperty();
        }

        private void DrawPortValue(Rect position, NodePort port, GUIContent label, SerializedProperty defaultValueProperty, SerializedProperty hasFuncProperty, Type nodeType, string fieldName, List<PropertyAttribute> attributes)
        {
            if (defaultValueProperty == null || (hasFuncProperty != null && hasFuncProperty.boolValue))
            {
                DrawLabelOnly(position, port, label);
                return;
            }

            switch (port.ShowBackingValueType)
            {
                case Node.ShowBackingValue.Unconnected:
                    if (port.IsConnected)
                    {
                        DrawLabelOnly(position, port, label);
                    }
                    else
                    {
                        DrawEditableProperty(position, label, defaultValueProperty, nodeType, fieldName, attributes);
                    }
                    break;

                case Node.ShowBackingValue.Never:
                    DrawLabelOnly(position, port, label);
                    break;

                case Node.ShowBackingValue.Always:
                    DrawEditableProperty(position, label, defaultValueProperty, nodeType, fieldName, attributes);
                    break;
            }
        }

        private void DrawLabelOnly(Rect position, NodePort port, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            GUIStyle labelStyle = port.Direction == NodePort.IO.Input ? EditorStyles.label : NodeEditorStyles.OutputPortLabel;
            EditorGUI.LabelField(position, label, labelStyle);
        }

        private Dictionary<string, PropertyDrawer> cachedCustomDrawers = new Dictionary<string, PropertyDrawer>();

        private PropertyDrawer GetCustomDrawer(Type nodeType, string fieldName, List<PropertyAttribute> attributes)
        {
            string key = $"{nodeType.FullName}.{fieldName}";
            if (cachedCustomDrawers.TryGetValue(key, out var drawer))
                return drawer;

            foreach (var attr in attributes)
            {
                Type drawerType = CustomPortAttributeDrawer.GetDrawerType(attr.GetType());
                if (drawerType != null)
                {
                    var fieldInfo = nodeType.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    drawer = CustomPortAttributeDrawer.CreateDrawerInstance(drawerType, attr, fieldInfo);
                    if (drawer != null)
                    {
                        cachedCustomDrawers[key] = drawer;
                        return drawer;
                    }
                }
            }

            cachedCustomDrawers[key] = null;
            return null;
        }

        private void DrawEditableProperty(Rect position, GUIContent label, SerializedProperty defaultValueProperty, Type nodeType, string fieldName, List<PropertyAttribute> attributes)
        {
            // Legacy fallback support for uNody's custom MultilineAttribute
            NodeEditorUtilities.GetCachedAttrib<Node.MultilineAttribute>(nodeType, fieldName, out var legacyMultiline);

            if (legacyMultiline != null)
            {
                var multilineRect = position;
                if (!string.IsNullOrEmpty(label.text))
                {
                    multilineRect.height = EditorGUIUtility.singleLineHeight;
                    EditorGUI.LabelField(multilineRect, label.text);
                    multilineRect.y += EditorGUIUtility.singleLineHeight;
                }
                multilineRect.height = EditorGUIUtility.singleLineHeight * legacyMultiline.line;
                defaultValueProperty.stringValue = EditorGUI.TextArea(multilineRect, defaultValueProperty.stringValue, textAreaStyle);
                return;
            }

            var customDrawer = GetCustomDrawer(nodeType, fieldName, attributes);
            if (customDrawer != null)
                customDrawer.OnGUI(position, defaultValueProperty, label);
            else
                EditorGUI.PropertyField(position, defaultValueProperty, label, true);
        }

        private void DrawPortHandle(Rect position, NodePort port, SerializedProperty property, GUIStyle style, NodeGraphEditor graphEditor, bool isConnector, Node node)
        {
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

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var node = property.serializedObject.targetObject as Node;
            string fieldName = GetCachedSerializedProperty(property, "fieldName").stringValue;
            string elementName = GetCachedSerializedProperty(property, "elementName").stringValue;
            
            if (!string.IsNullOrEmpty(fieldName) && string.IsNullOrEmpty(elementName))
            {
                var port = node.GetPort(fieldName);
                Type nodeType = node.GetType();

                cachedSpace = 0;
                if (NodeEditorUtilities.GetCachedAttrib<Node.SpaceLineAttribute>(nodeType, fieldName, out var spaceLine))
                    cachedSpace = spaceLine.height;

                var defaultValueProperty = GetCachedSerializedProperty(property, "defaultValue");
                var hasFuncProperty = GetCachedSerializedProperty(property, "hasFunc");
                float defaultHeight = 0f;

                var attributes = NodeEditorUtilities.GetCachedPropertyAttribs(nodeType, fieldName) ?? new List<PropertyAttribute>();
                NodeEditorUtilities.GetCachedAttrib<Node.MultilineAttribute>(nodeType, fieldName, out var legacyMultiline);

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
                    if (legacyMultiline != null)
                    {
                        defaultHeight = EditorGUIUtility.singleLineHeight * (legacyMultiline.line + 2);
                    }
                    else
                    {
                        PropertyDrawer customDrawer = GetCustomDrawer(nodeType, fieldName, attributes);
                        if (customDrawer != null)
                            defaultHeight = customDrawer.GetPropertyHeight(defaultValueProperty, label);
                        else
                            defaultHeight = EditorGUI.GetPropertyHeight(defaultValueProperty);
                    }
                }

                if (NodeEditorUtilities.GetCachedAttrib<Node.PortSettingsAttribute>(nodeType, fieldName, out var attribute))
                {
                    if (attribute.isHideLabel && !Mathf.Approximately(defaultHeight, EditorGUIUtility.singleLineHeight))
                        defaultHeight -= legacyMultiline != null ? EditorGUIUtility.singleLineHeight * 2f : EditorGUIUtility.singleLineHeight;
                }
                return defaultHeight + cachedSpace;
            }
            else
            {
                return EditorGUI.GetPropertyHeight(property);
            }
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