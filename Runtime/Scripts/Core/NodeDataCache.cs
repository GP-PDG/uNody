using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

namespace PuppyDragon.uNody {
    public static class NodeDataCache
    {
        private static Dictionary<Type, List<FieldInfo>> portFieldsByType= new();

        public static IReadOnlyList<FieldInfo> GetPortFields(Node node)
            => GetPortFields(node.GetType());

        public static IReadOnlyList<FieldInfo> GetPortFields(Type nodeType)
        {
            if (!portFieldsByType.TryGetValue(nodeType, out var portFields))
            {
                portFields = new();

                var rootType = nodeType;
                while (rootType != typeof(Node))
                {
                    var fields = rootType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(x => 
                        {
                            var checkField = x.FieldType.IsArray ? x.FieldType.GetElementType() : x.FieldType;
                            return checkField.IsGenericType &&
                            (checkField.GetGenericTypeDefinition() == typeof(InputPort<>) ||
                            checkField.GetGenericTypeDefinition() == typeof(OutputPort<>));
                        });

                    portFields.AddRange(fields);

                    rootType = rootType.BaseType;
                }

                portFieldsByType[nodeType] = portFields;
            }

            return portFields;
        }

        public static void FillPorts(Node node, Dictionary<string, NodePort> portsByField, Dictionary<string, NodePort[]> arrayPortsByField)
        {
            if (portsByField != null)
                portsByField.Clear();

            if (arrayPortsByField != null)
                arrayPortsByField.Clear();

            var portFields = GetPortFields(node);
            foreach (var portField in portFields)
            {
                var portSettings = portField.GetCustomAttribute<Node.PortSettingsAttribute>();

                if (!portField.FieldType.IsArray)
                {
                    var nodePort = (NodePort)portField.GetValue(node);
                    if (nodePort != null)
                    {
                        portsByField.Add(portField.Name, nodePort);
                        nodePort.Setup(node, portField.Name, portSettings);
                    }
                }
                else
                {
                    var nodePorts = (NodePort[])portField.GetValue(node);
                    if (nodePorts != null)
                    {
                        arrayPortsByField[portField.Name] = nodePorts;
                        for (int i = 0; i < nodePorts.Length; i++)
                        {
                            var port = nodePorts[i];
                            portsByField.Add(portField.Name + "." + i, port);
                            port.Setup(node, portField.Name, i, portSettings);
                        }
                    }
                }
            }
        }
    }
}
