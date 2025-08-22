using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.UIElements;

namespace PuppyDragon.uNodyEditor
{
    using Node = uNody.Node;
    using NodePort = uNody.NodePort;

    public partial class NodeGraphEditor
    {
        public static NodeGraphEditor current;

        public enum NodeActivity { Idle, HoldNode, DragNode, HoldGrid, DragGrid }

        public static Vector2[] dragOffset;
        public static Node[] copyBuffer;

        public List<RerouteReference> selectedReroutes = new();

        private static NodePort draggedOutput = null;
        private static NodePort draggedOutputTarget = null;
        private List<Vector2> draggedOutputReroutes = new();

        private Node hoveredNode = null;
        private NodePort hoveredPort = null;
        private NodePort autoConnectOutput = null;
        private RerouteReference hoveredReroute = null;

        private Vector2 dragBoxStart;
        private UnityEngine.Object[] preBoxSelection;
        private RerouteReference[] preBoxSelectionReroute;
        private Rect selectionBox;
        private bool isDoubleClick = false;
        private Vector2 lastMousePosition;

        public static NodeActivity CurrentActivity { get; private set; } = NodeActivity.Idle;
        public static bool IsPanning { get; private set; }
        private float DragThreshold => Math.Max(1f, DrawRect.width / 1000f);

        public bool IsDraggingPort => draggedOutput != null;
        public bool IsHoveringPort => hoveredPort != null;
        public bool IsHoveringNode => hoveredNode != null;
        public bool IsHoveringReroute => hoveredReroute != null;

        /// <summary> Return the dragged port or null if not exist </summary>
        public static NodePort DraggedOutputPort => draggedOutput;
        public static NodePort DraggedOutputPortTarget => draggedOutputTarget;

        /// <summary> Return the Hovered port or null if not exist </summary>
        public NodePort HoveredPort => hoveredPort;
        /// <summary> Return the Hovered node or null if not exist </summary>
        public Node HoveredNode => hoveredNode;

        public void UpdateControls()
        {
            Event e = Event.current;
            switch (e.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    OnDragPerforme();
                    break;

                case EventType.MouseMove:
                    lastMousePosition = e.mousePosition;
                    break;

                case EventType.ScrollWheel:
                    OnScrollWheel();
                    break;

                case EventType.MouseDrag:
                    OnMouseDrag();
                    break;

                case EventType.MouseDown:
                    OnMouseDown();
                    break;

                case EventType.MouseUp:
                    OnMouseUp();
                    lastMousePosition = e.mousePosition;
                    break;

                case EventType.KeyDown:
                    OnKeyDown();
                    break;

                case EventType.ValidateCommand:
                case EventType.ExecuteCommand:
                    OnExecuteCommand();
                    break;

                case EventType.Ignore:
                    // If release mouse outside window
                    if (e.rawType == EventType.MouseUp && CurrentActivity == NodeActivity.DragGrid)
                        CurrentActivity = NodeActivity.Idle;
                    break;
            }
        }

        private void RecalculateDragOffsets(Event current)
        {
            dragOffset = new Vector2[Selection.objects.Length + selectedReroutes.Count];

            // Selected nodes
            for (int i = 0; i < Selection.objects.Length; i++)
            {
                if (Selection.objects[i] is Node)
                {
                    var node = Selection.objects[i] as Node;
                    dragOffset[i] = node.NodePosition - WindowToGridPosition(current.mousePosition);
                }
            }

            // Selected reroutes
            for (int i = 0; i < selectedReroutes.Count; i++)
                dragOffset[Selection.objects.Length + i] = selectedReroutes[i].Value - WindowToGridPosition(current.mousePosition);
        }

        /// <summary> Puts all selected nodes in focus. If no nodes are present, resets view and zoom to to origin </summary>
        public void Home()
        {
            var nodes = Selection.objects.Where(o => o is Node).Cast<Node>().ToList();
            if (nodes.Count > 0)
            {
                Vector2 minPos = nodes.Select(x => x.NodePosition).Aggregate((x, y) => new Vector2(Mathf.Min(x.x, y.x), Mathf.Min(x.y, y.y)));
                Vector2 maxPos = nodes.Select(x => x.NodePosition + (nodeSizes.ContainsKey(x) ? nodeSizes[x] : Vector2.zero)).Aggregate((x, y) => new Vector2(Mathf.Max(x.x, y.x), Mathf.Max(x.y, y.y)));
                PanOffset = -(minPos + (maxPos - minPos) / 2f);
            }
            else
            {
                Zoom = 1;
                PanOffset = Vector2.zero;
            }
        }

        /// <summary> Remove nodes in the graph in Selection.objects</summary>
        public void RemoveSelectedNodes()
        {
            selectedReroutes = selectedReroutes.OrderByDescending(x => x.RerouteIndex).ToList();
            for (int i = 0; i < selectedReroutes.Count; i++)
                selectedReroutes[i].RemovePoint();

            selectedReroutes.Clear();

            foreach (UnityEngine.Object obj in Selection.objects)
            {
                if (obj is Node node)
                    RemoveNode(node);
            }
        }

        /// <summary> Initiate a rename on the currently selected node </summary>
        public void RenameSelectedNode()
        {
            if (Selection.objects.Length == 1 && Selection.activeObject is Node)
            {
                var node = Selection.activeObject as Node;
                if (nodeSizes.TryGetValue(node, out var size))
                    RenamePopup.Show(Selection.activeObject, size.x);
                else
                    RenamePopup.Show(Selection.activeObject);
            }
        }

        /// <summary> Duplicate selected nodes and select the duplicates </summary>
        public void DuplicateSelectedNodes()
        {
            // Get selected nodes which are part of this graph
            var selectedNodes = Selection.objects.Where(x => x is Node node && node.Graph == target).Cast<Node>().ToArray();
            if (selectedNodes == null || selectedNodes.Length == 0)
                return;

            // Get top left node position
            Vector2 topLeftNode = selectedNodes.Select(x => x.NodePosition).Aggregate((x, y) => new Vector2(Mathf.Min(x.x, y.x), Mathf.Min(x.y, y.y)));
            InsertDuplicateNodes(selectedNodes, topLeftNode + new Vector2(30, 30));
        }

        public void CopySelectedNodes()
        {
            copyBuffer = Selection.objects.Where(x => x is Node node && node.Graph == target).Cast<Node>().ToArray();
        }

        public void PasteNodes(Vector2 pos)
        {
            InsertDuplicateNodes(copyBuffer, pos);
        }

        private void InsertDuplicateNodes(Node[] nodes, Vector2 topLeft)
        {
            if (nodes == null || nodes.Length == 0) return;

            // Get top-left node
            Vector2 topLeftNode = nodes.Select(x => x.NodePosition).Aggregate((x, y) => new Vector2(Mathf.Min(x.x, y.x), Mathf.Min(x.y, y.y)));
            Vector2 offset = topLeft - topLeftNode;

            Node[] newNodes = new Node[nodes.Length];
            Dictionary<Node, Node> substitutes = new Dictionary<Node, Node>();
            for (int i = 0; i < nodes.Length; i++)
            {
                Node srcNode = nodes[i];
                if (srcNode == null)
                    continue;

                // Check if user is allowed to add more of given node type
                Node.DisallowMultipleNodesAttribute disallowAttrib;
                Type nodeType = srcNode.GetType();
                if (NodeEditorUtilities.GetAttrib(nodeType, out disallowAttrib))
                {
                    int typeCount = target.Nodes.Count(x => x.GetType() == nodeType);
                    if (typeCount >= disallowAttrib.max)
                        continue;
                }

                Node newNode = CopyNode(srcNode);
                substitutes.Add(srcNode, newNode);
                newNode.NodePosition = srcNode.NodePosition + offset;
                newNodes[i] = newNode;
            }

            // Walk through the selected nodes again, recreate connections, using the new nodes
            for (int i = 0; i < nodes.Length; i++)
            {
                Node srcNode = nodes[i];
                if (srcNode == null) continue;
                foreach (var port in srcNode.Ports)
                {
                    for (int c = 0; c < port.ConnectionCount; c++)
                    {
                        var inputPort = port.Direction == NodePort.IO.Input ? port : port.GetConnection(c).Port;
                        var outputPort = port.Direction == NodePort.IO.Output ? port : port.GetConnection(c).Port;

                        if (substitutes.TryGetValue(inputPort.OwnerNode, out var newNodeIn) && substitutes.TryGetValue(outputPort.OwnerNode, out var newNodeOut))
                        {
                            inputPort = newNodeIn.GetPort(inputPort.FieldName);
                            outputPort = newNodeOut.GetPort(outputPort.FieldName);
                        }

                        if (!inputPort.IsConnectedTo(outputPort))
                            inputPort.Connect(outputPort);
                    }
                }
            }
            EditorUtility.SetDirty(target);
            // Select the new nodes
            Selection.objects = newNodes;
        }

        /// <summary> Draw a connection as we are dragging it </summary>
        public void DrawDraggedConnection()
        {
            if (IsDraggingPort)
            {
                var gradient = GetNoodleGradient(draggedOutput, null);
                float thickness = GetNoodleThickness(draggedOutput, null);

                var gridPoints = new List<Vector2>();
                gridPoints.Add(draggedOutput.Rect.center);

                for (int i = 0; i < draggedOutputReroutes.Count; i++)
                    gridPoints.Add(draggedOutputReroutes[i]);
                
                gridPoints.Add(WindowToGridPosition(Event.current.mousePosition));

                DrawNoodle(draggedOutput, null, gradient, thickness, gridPoints);

                var portStyle = GetPortStyle(draggedOutput);
                Color bgcol = Color.black;
                Color frcol = gradient.colorKeys[0].color;
                bgcol.a = 0.6f;
                frcol.a = 0.6f;

                // Loop through reroute points again and draw the points
                for (int i = 0; i < draggedOutputReroutes.Count; i++)
                {
                    // Draw reroute point at position
                    Rect rect = new Rect(draggedOutputReroutes[i], new Vector2(16, 16));
                    rect.position = new Vector2(rect.position.x - 8, rect.position.y - 8);
                    rect = GridToWindowRect(rect);

                    Color col = GUI.color;
                    GUI.color = bgcol;
                    GUI.DrawTexture(rect, portStyle.normal.background);
                    GUI.color = frcol;
                    GUI.DrawTexture(rect, portStyle.active.background);
                    GUI.color = col;
                }
            }
        }

        bool IsHoveringTitle(Node node)
        {
            Vector2 mousePos = Event.current.mousePosition;
            //Get node position
            Vector2 nodePos = GridToWindowPosition(node.NodePosition);
            float width;
            Vector2 size;
            if (nodeSizes.TryGetValue(node, out size)) width = size.x;
            else width = 200;
            Rect windowRect = new Rect(nodePos, new Vector2(width / Zoom, 30 / Zoom));

            return windowRect.Contains(mousePos);
        }

        /// <summary> Attempt to connect dragged output to target node </summary>
        public void AutoConnect(Node node)
        {
            if (autoConnectOutput == null) return;

            // Find compatible input port
            PuppyDragon.uNody.NodePort inputPort = node.Ports.FirstOrDefault(x => x.Direction == NodePort.IO.Input && CanConnect(autoConnectOutput, x));
            if (inputPort != null)
                autoConnectOutput.Connect(inputPort);

            // Save changes
            EditorUtility.SetDirty(target);
            if (NodeEditorPreferences.GetSettings(target).autoSave)
                AssetDatabase.SaveAssets();
            autoConnectOutput = null;
        }

        private void OnDragPerforme()
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                OnDropObjects(DragAndDrop.objectReferences);
            }
        }

        private void OnScrollWheel()
        {
            float oldZoom = Zoom;
            if (Event.current.delta.y > 0)
                Zoom += 0.1f * Zoom;
            else
                Zoom -= 0.1f * Zoom;

            if (NodeEditorPreferences.GetSettings().zoomToMouse)
                PanOffset += (1 - oldZoom / Zoom) * (WindowToGridPosition(Event.current.mousePosition) + PanOffset);
        }

        private void OnMouseDrag()
        {
            var eCurrent = Event.current;
            if (eCurrent.button == 0)
            {
                if (IsDraggingPort)
                {
                    // Set target even if we can't connect, so as to prevent auto-conn menu from opening erroneously
                    if (IsHoveringPort && hoveredPort.Direction == NodePort.IO.Input && !draggedOutput.IsConnectedTo(hoveredPort))
                        draggedOutputTarget = hoveredPort;
                    else
                        draggedOutputTarget = null;
                }
                else
                {
                    switch (CurrentActivity)
                    {
                        case NodeActivity.HoldNode:
                            RecalculateDragOffsets(Event.current);
                            CurrentActivity = NodeActivity.DragNode;
                            break;

                        case NodeActivity.DragNode:
                            // Holding ctrl inverts grid snap
                            bool gridSnap = NodeEditorPreferences.GetSettings(this).gridSnap;
                            if (Event.current.control)
                                gridSnap = !gridSnap;
                            
                            Vector2 mousePos = WindowToGridPosition(eCurrent.mousePosition);
                            // Move selected nodes with offset
                            for (int i = 0; i < Selection.objects.Length; i++)
                            {
                                var node = Selection.objects[i] as Node;
                                if (node == null)
                                    continue;

                                Undo.RecordObject(node, "Moved Node");

                                Vector2 initial = node.NodePosition;
                                node.NodePosition = mousePos + dragOffset[i];
                                if (gridSnap)
                                {
                                    node.NodePosition = new Vector2(
                                        (Mathf.Round((node.NodePosition.x + 8) / 16) * 16) - 8,
                                        (Mathf.Round((node.NodePosition.y + 8) / 16) * 16) - 8);
                                }
                            }

                            for (int i = 0; i < selectedReroutes.Count; i++)
                            {
                                Vector2 pos = mousePos + dragOffset[Selection.objects.Length + i];
                                if (gridSnap)
                                {
                                    pos.x = (Mathf.Round(pos.x / 16) * 16);
                                    pos.y = (Mathf.Round(pos.y / 16) * 16);
                                }
                                selectedReroutes[i].SetPoint(pos);
                            }
                            break;

                        case NodeActivity.HoldGrid:
                            CurrentActivity = NodeActivity.DragGrid;
                            preBoxSelection = Selection.objects;
                            preBoxSelectionReroute = selectedReroutes.ToArray();
                            dragBoxStart = WindowToGridPosition(eCurrent.mousePosition);
                            break;

                        case NodeActivity.DragGrid:
                            Vector2 boxStartPos = GridToWindowPosition(dragBoxStart);
                            Vector2 boxSize = eCurrent.mousePosition - boxStartPos;
                            if (boxSize.x < 0)
                            {
                                boxStartPos.x += boxSize.x;
                                boxSize.x = Mathf.Abs(boxSize.x);
                            }
                            if (boxSize.y < 0)
                            {
                                boxStartPos.y += boxSize.y;
                                boxSize.y = Mathf.Abs(boxSize.y);
                            }
                            selectionBox = new Rect(boxStartPos, boxSize);
                            break;
                    }
                }
            }
            else if (eCurrent.button == 1 || eCurrent.button == 2)
            {
                //check drag threshold for larger screens
                if (eCurrent.delta.magnitude > DragThreshold)
                {
                    PanOffset += eCurrent.delta * Zoom;
                    IsPanning = true;
                }
            }
        }

        private void OnMouseDown()
        {
            var eCurrent = Event.current;
            if (eCurrent.button == 0)
            {
                draggedOutputReroutes.Clear();

                if (IsHoveringPort)
                {
                    if (hoveredPort.Direction == NodePort.IO.Output)
                    {
                        draggedOutput = hoveredPort;
                        autoConnectOutput = hoveredPort;
                    }
                    else
                    {
                        hoveredPort.VerifyConnections();
                        autoConnectOutput = null;
                        if (hoveredPort.IsConnected)
                        {
                            var node = hoveredPort.OwnerNode;
                            var output = hoveredPort.Connection.Port;
                            int outputConnectionIndex = output.GetConnectionIndex(hoveredPort);
                            draggedOutputReroutes = output.GetConnection(outputConnectionIndex).Reroutes;
                            hoveredPort.Disconnect(output);
                            draggedOutput = output;
                            draggedOutputTarget = hoveredPort;

                            if (NodeEditor.onUpdateNode != null)
                                NodeEditor.onUpdateNode(node);
                        }
                    }
                }
                else if (IsHoveringNode && IsHoveringTitle(hoveredNode))
                {
                    // If mousedown on node header, select or deselect
                    if (!Selection.Contains(hoveredNode))
                    {
                        SelectNode(hoveredNode, eCurrent.control || eCurrent.shift);
                        if (!eCurrent.control && !eCurrent.shift)
                            selectedReroutes.Clear();
                    }
                    else if (eCurrent.control || eCurrent.shift)
                        DeselectNode(hoveredNode);

                    // Cache double click state, but only act on it in MouseUp - Except ClickCount only works in mouseDown.
                    isDoubleClick = (eCurrent.clickCount == 2);

                    eCurrent.Use();

                    CurrentActivity = NodeActivity.HoldNode;
                }
                else if (IsHoveringReroute)
                {
                    // If reroute isn't selected
                    if (!selectedReroutes.Contains(hoveredReroute))
                    {
                        // Add it
                        if (eCurrent.control || eCurrent.shift)
                            selectedReroutes.Add(hoveredReroute);
                        // Select it
                        else
                        {
                            selectedReroutes = new List<RerouteReference>() { hoveredReroute };
                            Selection.activeObject = null;
                        }

                    }
                    // Deselect
                    else if (eCurrent.control || eCurrent.shift)
                        selectedReroutes.Remove(hoveredReroute);

                    eCurrent.Use();

                    CurrentActivity = NodeActivity.HoldNode;
                }
                // If mousedown on grid background, deselect all
                else if (!IsHoveringNode)
                {
                    CurrentActivity = NodeActivity.HoldGrid;
                    if (!eCurrent.control && !eCurrent.shift)
                    {
                        selectedReroutes.Clear();
                        Selection.activeObject = null;
                    }
                }
            }
        }

        private void OnMouseUp()
        {
            var eCurrent = Event.current;
            if (eCurrent.button == 0)
            {
                //Port drag release
                if (IsDraggingPort)
                {
                    // If connection is valid, save it
                    if (draggedOutputTarget != null && CanConnect(draggedOutput, draggedOutputTarget))
                    {
                        var node = draggedOutputTarget.OwnerNode;
                        if (target.Nodes.Count != 0)
                            draggedOutput.Connect(draggedOutputTarget);

                        // ConnectionIndex can be -1 if the connection is removed instantly after creation
                        int connectionIndex = draggedOutput.GetConnectionIndex(draggedOutputTarget);
                        if (connectionIndex != -1)
                        {
                            draggedOutput.GetConnection(connectionIndex).AddReroutes(draggedOutputReroutes);

                            if (NodeEditor.onUpdateNode != null)
                                NodeEditor.onUpdateNode(node);

                            EditorUtility.SetDirty(target);
                        }
                    }
                    // Open context menu for auto-connection if there is no target node
                    else if (draggedOutputTarget == null && NodeEditorPreferences.GetSettings(this).dragToCreate && autoConnectOutput != null)
                    {
                        var menu = new AdvancedGenericMenu("Connect");
                        AddContextMenuItems(menu, draggedOutput.ValueType);
                        menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                    }

                    //Release dragged connection
                    draggedOutput = null;
                    draggedOutputTarget = null;

                    EditorUtility.SetDirty(target);
                    if (NodeEditorPreferences.GetSettings(target).autoSave)
                        AssetDatabase.SaveAssets();
                }
                else if (CurrentActivity == NodeActivity.DragNode)
                {
                    var nodes = Selection.objects.Where(x => x is Node).Cast<Node>();
                    foreach (var node in nodes)
                        EditorUtility.SetDirty(node);

                    if (NodeEditorPreferences.GetSettings(target).autoSave)
                        AssetDatabase.SaveAssets();
                }
                else if (!IsHoveringNode)
                {
                    // If click outside node, release field focus
                    if (!IsPanning)
                    {
                        EditorGUI.FocusTextInControl(null);
                        EditorGUIUtility.editingTextField = false;
                    }

                    if (NodeEditorPreferences.GetSettings(target).autoSave)
                        AssetDatabase.SaveAssets();
                }
                // If click node header, select it.
                else if (CurrentActivity == NodeActivity.HoldNode && !(eCurrent.control || eCurrent.shift))
                {
                    selectedReroutes.Clear();
                    SelectNode(hoveredNode, false);

                    // Double click to center node
                    if (isDoubleClick)
                    {
                        Vector2 nodeDimension = nodeSizes.ContainsKey(hoveredNode) ? nodeSizes[hoveredNode] / 2 : Vector2.zero;
                        PanOffset = -hoveredNode.NodePosition - nodeDimension;
                    }
                }

                if (IsHoveringReroute && !(eCurrent.control || eCurrent.shift))
                {
                    selectedReroutes = new List<RerouteReference>() { hoveredReroute };
                    Selection.activeObject = null;
                }

                CurrentActivity = NodeActivity.Idle;
            }
            else if (eCurrent.button == 1 || eCurrent.button == 2)
            {
                if (!IsPanning)
                {
                    if (IsDraggingPort)
                    {
                        draggedOutputReroutes.Add(WindowToGridPosition(eCurrent.mousePosition));
                    }
                    else if (CurrentActivity == NodeActivity.DragNode && Selection.activeObject == null && selectedReroutes.Count == 1)
                    {
                        selectedReroutes[0].InsertPoint(selectedReroutes[0].Value);
                        selectedReroutes[0] = new RerouteReference(selectedReroutes[0].Port, selectedReroutes[0].ConnectionIndex, selectedReroutes[0].RerouteIndex + 1);
                    }
                    else if (IsHoveringReroute)
                    {
                        ShowRerouteContextMenu(hoveredReroute);
                    }
                    else if (IsHoveringPort)
                    {
                        ShowPortContextMenu(hoveredPort);
                    }
                    else if (IsHoveringNode && IsHoveringTitle(hoveredNode))
                    {
                        if (!Selection.Contains(hoveredNode))
                            SelectNode(hoveredNode, false);

                        autoConnectOutput = null;
                        var menu = new AdvancedGenericMenu(hoveredNode.name);
                        NodeEditor.GetEditor(hoveredNode).AddContextMenuItems(menu);
                        menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                        eCurrent.Use(); // Fixes copy/paste context menu appearing in Unity 5.6.6f2 - doesn't occur in 2018.3.2f1 Probably needs to be used in other places.
                    }
                    else if (!IsHoveringNode)
                    {
                        autoConnectOutput = null;

                        var menu = new AdvancedGenericMenu("New Node");
                        AddContextMenuItems(menu);
                        menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                    }
                }
                IsPanning = false;
            }
            // Reset DoubleClick
            isDoubleClick = false;
        }

        private void OnKeyDown()
        {
            var eCurrent = Event.current;
            if (EditorGUIUtility.editingTextField || GUIUtility.keyboardControl != 0)
                return;

            switch (eCurrent.keyCode)
            {
                case KeyCode.F:
                    Home();
                    break;

                case KeyCode.Return:
                    if (NodeEditorUtilities.IsMac())
                        RenameSelectedNode();
                    break;

                case KeyCode.F2:
                    RenameSelectedNode();
                    break;

                case KeyCode.A:
                    if (Selection.objects.Any(x => target.Nodes.Contains(x as Node)))
                    {
                        foreach (Node node in target.Nodes)
                            DeselectNode(node);
                    }
                    else
                    {
                        foreach (Node node in target.Nodes)
                            SelectNode(node, true);
                    }
                    break;

                default:
                    break;
            }
        }

        private void OnExecuteCommand()
        {
            var eCurrent = Event.current;

            if (eCurrent.commandName == "SoftDelete")
            {
                if (eCurrent.type == EventType.ExecuteCommand)
                    RemoveSelectedNodes();
                eCurrent.Use();
            }
            else if (NodeEditorUtilities.IsMac() && eCurrent.commandName == "Delete")
            {
                if (eCurrent.type == EventType.ExecuteCommand)
                    RemoveSelectedNodes();
                eCurrent.Use();
            }
            else if (eCurrent.commandName == "Duplicate")
            {
                if (eCurrent.type == EventType.ExecuteCommand)
                    DuplicateSelectedNodes();
                eCurrent.Use();
            }
            else if (eCurrent.commandName == "Copy")
            {
                if (!EditorGUIUtility.editingTextField)
                {
                    if (eCurrent.type == EventType.ExecuteCommand)
                        CopySelectedNodes();
                    eCurrent.Use();
                }
            }
            else if (eCurrent.commandName == "Paste")
            {
                if (!EditorGUIUtility.editingTextField)
                {
                    if (eCurrent.type == EventType.ExecuteCommand)
                        PasteNodes(WindowToGridPosition(lastMousePosition));
                    eCurrent.Use();
                }
            }
        }
    }
}
