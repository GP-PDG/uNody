using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using System.Reflection;

namespace PuppyDragon.uNodyEditor
{
    using PuppyDragon.uNody;
    using PuppyDragon.uNody.Logic;
    using System.Linq;

    public partial class NodeGraphEditor
    {
        private static readonly Vector3[] polyLineTempArray = new Vector3[2];

        private readonly List<Vector2> worldGridPoints = new();
        private readonly HashSet<UnityEngine.Object> selectionCache = new();
        private readonly HashSet<Node> culledNodes = new();
        private readonly List<Node> drawTargetNodes = new();
        private readonly HashSet<NodePort> flowPorts = new();

        private AnimFloat flowAnim;

        private EditorWindow window;

        public EditorWindow Window { get; set; }

        public Rect DrawRect { get; private set; }

        /// <summary> Executed after all other window GUI. Useful if Zoom is ruining your day. Automatically resets after being run.</summary>
        public event Action onLateGUI;

        protected void DrawGrid()
        {
            current = this;

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint)
                DrawRect = GUILayoutUtility.GetLastRect();

            DrawGrid(DrawRect, Zoom, PanOffset);

            GUILayout.BeginArea(DrawRect);
            {
                UpdateControls();

                UpdateAnim();

                CollectFlowPorts();
                DrawDraggedConnection();
                DrawConnections();
                DrawNodes();
                DrawSelectionBox();
                DrawTooltip();

                // Run and reset onLateGUI
                if (onLateGUI != null)
                {
                    onLateGUI();
                    onLateGUI = null;
                }
            }
            GUILayout.EndArea();
        }

        public Vector2 WindowToGridPosition(Vector2 windowPosition)
            => (windowPosition - (DrawRect.center) - (PanOffset / Zoom)) * Zoom;

        public Vector2 GridToWindowPosition(Vector2 gridPosition)
            => (DrawRect.center) + (PanOffset / Zoom) + (gridPosition / Zoom);

        public Rect GridToWindowRectNoClipped(Rect gridRect)
        {
            gridRect.position = GridToWindowPositionNoClipped(gridRect.position);
            return gridRect;
        }

        public Rect GridToWindowRect(Rect gridRect)
        {
            gridRect.position = GridToWindowPosition(gridRect.position);
            gridRect.size /= Zoom;
            return gridRect;
        }

        public Vector2 GridToWindowPositionNoClipped(Vector2 gridPosition)
        {
            Vector2 center = DrawRect.center;
            // UI Sharpness complete fix - Round final offset not panOffset
            float xOffset = Mathf.Round(center.x * Zoom + (PanOffset.x + gridPosition.x));
            float yOffset = Mathf.Round(center.y * Zoom + (PanOffset.y + gridPosition.y));
            return new Vector2(xOffset, yOffset);
        }

        /// <summary> Returned gradient is used to color noodles </summary>
        /// <param name="output"> The output this noodle comes from. Never null. </param>
        /// <param name="input"> The output this noodle comes from. Can be null if we are dragging the noodle. </param>
        public virtual Gradient GetNoodleGradient(uNody.NodePort output, uNody.NodePort input)
        {
            Gradient grad = new Gradient();

            // If dragging the noodle, draw solid, slightly transparent
            if (input == null)
            {
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }
            // If normal, draw gradient fading from one input color to the other
            else
            {
                Color a = GetTypeColor(output.ValueType);
                Color b = GetTypeColor(input.ValueType);

                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
                );
            }

            return grad;
        }

        /// <summary> Returned float is used for noodle thickness </summary>
        /// <param name="output"> The output this noodle comes from. Never null. </param>
        /// <param name="input"> The output this noodle comes from. Can be null if we are dragging the noodle. </param>
        public virtual float GetNoodleThickness(uNody.NodePort output, uNody.NodePort input)
            => NodeEditorPreferences.GetSettings(this).noodleThickness;

        public void BeginZoom(Rect rect, float zoom)
        {
            rect.position = Vector2.zero;
            EditorGUIScaler.BeginScale(ref rect, rect.center, zoom, false, false);
        }

        public void EndZoom()
        {
            EditorGUIScaler.EndScale();
        }

        public void DrawGrid(Rect rect, float zoom, Vector2 panOffset)
        {
            Vector2 center = rect.center;
            Texture2D gridTex = GetGridLargeLineTexture();
            Texture2D crossTex = GetGridSmallLineTexture();

            // Offset from origin in tile units
            float xOffset = -(center.x * zoom + panOffset.x) / gridTex.width;
            float yOffset = ((center.y - rect.size.y) * zoom + panOffset.y) / gridTex.height;

            Vector2 tileOffset = new Vector2(xOffset, yOffset);

            // Amount of tiles
            float tileAmountX = Mathf.Round(rect.size.x * zoom) / gridTex.width;
            float tileAmountY = Mathf.Round(rect.size.y * zoom) / gridTex.height;

            Vector2 tileAmount = new Vector2(tileAmountX, tileAmountY);

            // Draw tiled background
            GUI.DrawTextureWithTexCoords(rect, gridTex, new Rect(tileOffset, tileAmount));
            GUI.DrawTextureWithTexCoords(rect, crossTex, new Rect(tileOffset + new Vector2(0.5f, 0.5f), tileAmount));
        }

        public void DrawSelectionBox()
        {
            if (CurrentActivity == NodeActivity.DragGrid)
            {
                Vector2 curPos = WindowToGridPosition(Event.current.mousePosition);
                Vector2 size = curPos - dragBoxStart;
                Rect r = new Rect(dragBoxStart, size);
                r.position = GridToWindowPosition(r.position);
                r.size /= Zoom;
                Handles.DrawSolidRectangleWithOutline(r, new Color(0, 0, 0, 0.1f), new Color(1, 1, 1, 0.6f));
            }
        }

        public static bool DropdownButton(string name, float width)
        {
            return GUILayout.Button(name, EditorStyles.toolbarDropDown, GUILayout.Width(width));
        }

        /// <summary> Show right-click context menu for hovered reroute </summary>
        void ShowRerouteContextMenu(RerouteReference reroute)
        {
            var contextMenu = new GenericMenu();
            contextMenu.AddItem(new GUIContent("Remove"), false, () => reroute.RemovePoint());
            contextMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));

            if (NodeEditorPreferences.GetSettings().autoSave)
                AssetDatabase.SaveAssets();
        }

        /// <summary> Show right-click context menu for hovered port </summary>
        void ShowPortContextMenu(NodePort hoveredPort)
        {
            var contextMenu = new AdvancedGenericMenu("Port", new AdvancedDropdownState());

            foreach (var connection in hoveredPort.Connections)
            {
                var name = connection.Port.OwnerNode.name;
                var index = hoveredPort.GetConnectionIndex(connection.Port);
                contextMenu.AddItem(new GUIContent(string.Format("Disconnect({0})", name)), false, () => hoveredPort.Disconnect(index));
            }
            contextMenu.AddItem(new GUIContent("Clear Connections"), false, () => hoveredPort.ClearConnections());
            //Get compatible nodes with this port
            if (NodeEditorPreferences.GetSettings().createFilter)
            {
                contextMenu.AddSeparator("");

                if (hoveredPort.Direction == uNody.NodePort.IO.Input)
                    AddContextMenuItems(contextMenu, hoveredPort.ValueType, uNody.NodePort.IO.Output);
                else
                    AddContextMenuItems(contextMenu, hoveredPort.ValueType, uNody.NodePort.IO.Input);
            }
            contextMenu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
            if (NodeEditorPreferences.GetSettings(target).autoSave) AssetDatabase.SaveAssets();
        }

        static Vector2 CalculateBezierPoint(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float u = 1 - t;
            float tt = t * t, uu = u * u;
            float uuu = uu * u, ttt = tt * t;
            return new Vector2(
                (uuu * p0.x) + (3 * uu * t * p1.x) + (3 * u * tt * p2.x) + (ttt * p3.x),
                (uuu * p0.y) + (3 * uu * t * p1.y) + (3 * u * tt * p2.y) + (ttt * p3.y)
            );
        }

        /// <summary> Draws a line segment without allocating temporary arrays </summary>
        static void DrawAAPolyLineNonAlloc(float thickness, Vector2 p0, Vector2 p1)
        {
            polyLineTempArray[0].x = p0.x;
            polyLineTempArray[0].y = p0.y;
            polyLineTempArray[1].x = p1.x;
            polyLineTempArray[1].y = p1.y;
            Handles.DrawAAPolyLine(thickness, polyLineTempArray);
        }

        /// <summary> Draw a bezier from output to input in grid coordinates </summary>
        public void DrawNoodle(NodePort outputPort, NodePort inputPort, Gradient gradient, float thickness, List<Vector2> gridPoints)
        {
            // convert grid points to window points
            for (int i = 0; i < gridPoints.Count; ++i)
                worldGridPoints.Add(GridToWindowPosition(gridPoints[i]));

            Color originalHandlesColor = Handles.color;
            Handles.color = gradient.Evaluate(0f);
            int length = gridPoints.Count;
            bool isFlowTarget = flowPorts.Contains(outputPort);
   
            var outputNode = GetDrawerNode(outputPort, NodePort.IO.Output);
            if (!NodeSizes.TryGetValue(outputNode, out Vector2 outputNodeSize))
                return;

            Vector2 inputNodeSize = Vector2.zero;
            Node inputNode = null;
            if (inputPort != null)
            {
                inputNode = GetDrawerNode(inputPort, NodePort.IO.Input);
                NodeSizes.TryGetValue(inputNode, out inputNodeSize);
            }

            var bezierPositions = new List<Vector2>();

            for (int i = 0; i < length - 1; i++)
            {
                Vector2 startPoint = worldGridPoints[i];
                Vector2 endPoint = worldGridPoints[i + 1];
                float distance = Vector2.Distance(startPoint, endPoint);
                int division = Mathf.RoundToInt(0.2f * distance);
                float zoomCoef = 50 / Zoom;
                
                if (startPoint.x < endPoint.x)
                {
                    Vector2 outputTangent = Zoom * distance * 0.01f * Vector2.right;
                    Vector2 inputTangent = Zoom * distance * 0.01f * Vector2.left;

                    // Calculates the tangents for the bezier's curves.
                    Vector2 tangentA = startPoint + outputTangent * zoomCoef;
                    Vector2 tangentB = endPoint + inputTangent * zoomCoef;

                    // Coloring and bezier drawing.
                    Vector2 bezierPrevious = startPoint;
                    bezierPositions.Add(bezierPrevious);
                    for (int j = 0; j <= division; j++)
                    {
                        float timePoint = j / (float)division;
                        float gradientPoint = ((i * division) + j) / (float)(division * (length - 1));

                        if (inputPort != null)
                            Handles.color = gradient.Evaluate(gradientPoint);

                        Vector2 bezierNext = CalculateBezierPoint(startPoint, tangentA, tangentB, endPoint, timePoint);
                        bezierPositions.Add(bezierNext);
                        DrawAAPolyLineNonAlloc(thickness, bezierPrevious, bezierNext);

                        bezierPrevious = bezierNext;
                    }
                }
                else
                {
                    Vector2 upNodeBottomPosition = gridPoints[i].y > gridPoints[i + 1].y ?
                        ((inputPort == null || i + 1 != length - 1) ? gridPoints[i + 1] : inputNode.NodePosition + inputNodeSize) :
                        (i != 0 ? gridPoints[i] : outputNode.NodePosition + outputNodeSize);
                    Vector2 downNodeTopPosition = gridPoints[i].y > gridPoints[i + 1].y ?
                            (i != 0 ? gridPoints[i] : outputNode.NodePosition) :
                            ((inputPort == null || i + 1 != length - 1) ? gridPoints[i + 1] : inputNode.NodePosition);

                    Vector2 midPosition = (upNodeBottomPosition + downNodeTopPosition) * 0.5f;
                    float distanceY = (downNodeTopPosition.y - upNodeBottomPosition.y) / 35 * 0.05f;
                    float tangentX = 35f * (1 + distanceY);
                    Vector2 wayPoint1 = new Vector2(gridPoints[i].x, midPosition.y);
                    wayPoint1 = GridToWindowPosition(wayPoint1);
                    Vector2 wayPoint2 = GridToWindowPosition(gridPoints[i + 1]);
                    wayPoint2.y = wayPoint1.y;

                    Vector2 bezierPrevious = startPoint;
                    bezierPositions.Add(bezierPrevious);
                    Vector2 p1 = Vector2.zero;
                    Vector2 tangentA = Vector2.zero;
                    Vector2 tangentB = Vector2.zero;
                    Vector2 p4 = Vector2.zero;
                    int sequenceIndex = 0;

                    for (int j = 0; j <= division; j++)
                    {
                        float timePoint = j / (float)division;
                        float splitTimePoint = timePoint;
                        float gradientPoint = ((i * division) + j) / (float)(division * (length - 1));

                        if (inputPort != null)
                            Handles.color = gradient.Evaluate(gradientPoint);

                        if (splitTimePoint <= 0.33f)
                        {
                            if (sequenceIndex == 0)
                            {
                                p1 = startPoint;
                                tangentA = GridToWindowPosition(gridPoints[i] + new Vector2(tangentX, 0f));
                                tangentB = new Vector2(gridPoints[i].x + tangentX, midPosition.y);
                                tangentB = GridToWindowPosition(tangentB);
                                p4 = wayPoint1;

                                sequenceIndex++;
                            }
                        }
                        else if (splitTimePoint <= 0.66f)
                        {
                            splitTimePoint -= 0.33f;

                            if (sequenceIndex == 1)
                            {
                                p1 = wayPoint1;
                                tangentA = wayPoint1;
                                tangentB = wayPoint2;
                                p4 = wayPoint2;

                                sequenceIndex++;
                            }
                        }
                        else
                        {
                            splitTimePoint -= 0.66f;

                            if (sequenceIndex == 2)
                            {
                                p1 = wayPoint2;
                                tangentA = new Vector2(gridPoints[i + 1].x - tangentX, midPosition.y);
                                tangentA = GridToWindowPosition(tangentA);
                                tangentB = GridToWindowPosition(gridPoints[i + 1] - new Vector2(tangentX, 0f));
                                p4 = endPoint;

                                sequenceIndex = 0;
                            }
                        }

                        Vector2 bezierNext = CalculateBezierPoint(p1, tangentA, tangentB, p4, splitTimePoint / 0.33f);
                        bezierPositions.Add(bezierNext);
                        DrawAAPolyLineNonAlloc(thickness, bezierPrevious, bezierNext);
                        bezierPrevious = bezierNext;
                    }
                }
            }

            bezierPositions.Add(worldGridPoints[worldGridPoints.Count - 1]);

            if (isFlowTarget)
            {
                float index = (bezierPositions.Count - 1) * flowAnim.value;
                var bezierPosition = bezierPositions[(int)index];
                Handles.color = gradient.Evaluate(index / (bezierPositions.Count - 1));
                DrawFlowDot(new Rect(bezierPosition - (new Vector2(4, 4) / Zoom), new Vector2(8, 8) / Zoom));
            }

            Handles.color = originalHandlesColor;

            worldGridPoints.Clear();
        }

        public Node GetDrawerNode(NodePort port, NodePort.IO io)
        {
            if ((io == NodePort.IO.Input && NodeReflection.IsInPoint(port.OwnerNode)) ||
                (io == NodePort.IO.Output && NodeReflection.IsOutPoint(port.OwnerNode)))
            {
                var subGraph = port.OwnerNode.Graph;
                if (subGraph != target)
                    return target.Nodes.First(x => x is SubGraphNode subGraphNode && subGraphNode.SubGraph == subGraph);
            }
            
            return port.OwnerNode;
        }

        private void DrawFlowDot(Rect rect)
        {
            var portStyle = NodeEditorStyles.InputDotPort;
            Color guiColor = GUI.color;
            GUI.color = Handles.color;
            GUI.DrawTexture(rect, portStyle.active.background);
            GUI.color = guiColor;
        }

        public void CollectFlowPorts()
        {
            flowPorts.Clear();

            if (Selection.count > 0)
            {
                foreach (var obj in Selection.objects)
                {
                    var node = obj as Node;
                    if (node == null)
                        continue;

                    if (node is ILogicNode logicNode)
                    {
                        if (logicNode.PrevPort != null)
                            CollectFlowInputPort(logicNode.PrevPort, flowPorts);

                        if (logicNode.NextPort != null)
                            CollectFlowOutputPort(logicNode.NextPort, flowPorts);
                    }
                    else if (node is SubGraphNode subGraphNode && subGraphNode.SubGraph is LogicGraph subLogicGraph)
                    {
                        CollectFlowInputPort(subLogicGraph.EntryPoint.Inputs.FirstOrDefault(), flowPorts);
                        CollectFlowOutputPort(subLogicGraph.ExitPoint.Outputs.FirstOrDefault(), flowPorts);
                    }
                    else
                    {
                        foreach (var input in node.Inputs)
                            CollectFlowInputPort(input, flowPorts);

                        foreach (var output in node.Outputs)
                            CollectFlowOutputPort(output, flowPorts);
                    }
                }
            }

            if (hoveredPort != null)
            {
                if (hoveredPort.Direction == NodePort.IO.Input)
                    CollectFlowInputPort(hoveredPort, flowPorts);
                else
                    CollectFlowOutputPort(hoveredPort, flowPorts);
            }
        }

        private void CollectFlowInputPort(NodePort port, HashSet<NodePort> flowPorts, Type valueTypeFiler = null)
        {
            if (port == null)
                return;

            foreach (var connection in port.Connections)
            {
                if (connection.Node.Graph != target && connection.Node is ExitPointNode)
                {
                    flowPorts.Add(connection.Port);
                    port = (connection.Node.Graph as LogicGraph).EntryPoint.Inputs.First();
                    CollectFlowInputPort(port, flowPorts, valueTypeFiler);
                    continue;
                }

                if (connection.Node.Graph != target ||
                    (valueTypeFiler != null && connection.Port.ValueType != valueTypeFiler))
                    continue;

                flowPorts.Add(connection.Port);

                if (connection.Node is ILogicNode logicNode)
                    CollectFlowInputPort(logicNode.PrevPort, flowPorts, valueTypeFiler);
                else
                {
                    foreach (var input in connection.Node.Inputs)
                        CollectFlowInputPort(input, flowPorts, valueTypeFiler);
                }
            }
        }

        private void CollectFlowOutputPort(NodePort port, HashSet<NodePort> flowPorts, Type valueTypeFiler = null)
        {
            if (port == null)
                return;

            flowPorts.Add(port);
            foreach (var connection in port.Connections)
            {
                if (connection.Node.Graph != target && connection.Node is EntryPointNode)
                {
                    port = (connection.Node.Graph as LogicGraph).ExitPoint.Outputs.First();
                    CollectFlowOutputPort(port, flowPorts, valueTypeFiler);
                    continue;
                }

                if (connection.Node.Graph != target ||
                    (valueTypeFiler != null && connection.Port.ValueType != valueTypeFiler))
                    continue;

                if (connection.Node is ILogicNode logicNode)
                    CollectFlowOutputPort(logicNode.NextPort, flowPorts, valueTypeFiler);
                else
                {
                    foreach (var output in connection.Node.Outputs)
                        CollectFlowOutputPort(output, flowPorts, valueTypeFiler);
                }
            }
        }

        /// <summary> Draws all connections </summary>
        public void DrawConnections()
        {
            Vector2 mousePos = Event.current.mousePosition;
            List<RerouteReference> selections = preBoxSelectionReroute != null ? new List<RerouteReference>(preBoxSelectionReroute) : new List<RerouteReference>();
            hoveredReroute = null;

            var gridPoints = new List<Vector2>();

            Color col = GUI.color;
            foreach (var node in drawTargetNodes)
            {
                //If a null node is found, return. This can happen if the nodes associated script is deleted. It is currently not possible in Unity to delete a null asset.
                if (node == null)
                    continue;

                if (NodeReflection.IsOutPoint(node))
                {
                    if (node.Graph.Parent != target)
                        continue;
                    else if (node.Graph == target)
                        continue;
                }
                else if (node.Graph != target)
                    continue;

                foreach (var output in node.Outputs)
                {
                    Color portColor = GetPortFilledColor(output);
                    GUIStyle portStyle = GetPortStyle(output);
                    
                    for (int k = 0; k < output.ConnectionCount; k++)
                    {
                        var connection = output.GetConnection(k);
                        if (connection == null)
                            continue;

                        var input = connection.Port;
                        if (input == null)
                            continue;

                        if (!input.IsConnectedTo(output))
                            input.Connect(output);

                        var noodleGradient = GetNoodleGradient(output, input);
                        float noodleThickness = GetNoodleThickness(output, input);

                        if (!input.IsConnectedTo(output))
                            input.Connect(output);

                        gridPoints.Clear();
                        gridPoints.Add(output.Rect.center);
                        gridPoints.AddRange(connection.Reroutes);
                        gridPoints.Add(input.Rect.center);


                        DrawNoodle(output, input, noodleGradient, noodleThickness, gridPoints);

                        for (int i = 0; i < connection.Reroutes.Count; i++)
                        {
                            var rerouteReference = new RerouteReference(output, k, i);

                            // Draw reroute point at position
                            Rect rect = new Rect(connection.Reroutes[i], new Vector2(12, 12));
                            rect.position = new Vector2(rect.position.x - 6, rect.position.y - 6);
                            rect = GridToWindowRect(rect);

                            // Draw selected reroute points with an outline
                            if (selectedReroutes.Contains(rerouteReference))
                            {
                                GUI.color = NodeEditorPreferences.GetSettings().highlightColor;
                                GUI.DrawTexture(rect, portStyle.normal.background);
                            }

                            GUI.color = portColor;
                            GUI.DrawTexture(rect, portStyle.active.background);

                            if (rect.Overlaps(selectionBox))
                                selections.Add(rerouteReference);

                            if (rect.Contains(mousePos))
                                hoveredReroute = rerouteReference;
                        }
                    }
                }
            }

            GUI.color = col;

            if (Event.current.type != EventType.Layout && CurrentActivity == NodeActivity.DragGrid)
                selectedReroutes = selections;
        }

        private void UpdateAnim()
        {
            if (flowAnim == null)
            {
                flowAnim = new AnimFloat(0);
                flowAnim.speed = 1f;
                flowAnim.valueChanged.AddListener(() => Window?.Repaint());
            }

            if (!flowAnim.isAnimating)
            {
                flowAnim.value = 0f;
                flowAnim.target = 1f;
            }
        }

        private void DrawNodes()
        {
            Event eCurrent = Event.current;

            if (eCurrent.type == EventType.Layout)
            {
                selectionCache.Clear();
                var objs = Selection.objects;
                selectionCache.EnsureCapacity(objs.Length);
                foreach (var obj in objs)
                    selectionCache.Add(obj);
            }

            MethodInfo onValidate = null;
            if (Selection.activeObject != null && Selection.activeObject is Node)
            {
                onValidate = Selection.activeObject.GetType().GetMethod("OnValidate");
                if (onValidate != null)
                    EditorGUI.BeginChangeCheck();
            }

            BeginZoom(DrawRect, Zoom);

            Vector2 mousePos = Event.current.mousePosition;

            if (eCurrent.type != EventType.Layout)
            {
                hoveredNode = null;
                hoveredPort = null;
            }

            var preSelection = preBoxSelection != null ? new List<UnityEngine.Object>(preBoxSelection) : new List<UnityEngine.Object>();

            // Selection box stuff
            Vector2 boxStartPos = GridToWindowPositionNoClipped(dragBoxStart);
            Vector2 boxSize = mousePos - boxStartPos;
            if (boxSize.x < 0) { boxStartPos.x += boxSize.x; boxSize.x = Mathf.Abs(boxSize.x); }
            if (boxSize.y < 0) { boxStartPos.y += boxSize.y; boxSize.y = Mathf.Abs(boxSize.y); }
            Rect selectionBox = new Rect(boxStartPos, boxSize);

            //Save guiColor so we can revert it
            Color guiColor = GUI.color;

            var removeEntries = new List<NodePort>();

            if (eCurrent.type == EventType.Layout)
            {
                culledNodes.Clear();

                drawTargetNodes.Clear();
                drawTargetNodes.AddRange(target.Nodes);
                foreach (var subGraph in target.Children)
                {
                    drawTargetNodes.AddRange(subGraph.InPoints);
                    drawTargetNodes.AddRange(subGraph.OutPoints);
                }
            }

            foreach (var node in drawTargetNodes)
            {
                // Skip null nodes. The user could be in the process of renaming scripts, so removing them at this point is not advisable.
                if (node == null)
                    continue;

                if (node.Graph == target)
                {
                    // Culling
                    if (eCurrent.type == EventType.Layout)
                    {
                        // Cull unselected nodes outside view
                        if (!Selection.Contains(node) && ShouldBeCulled(node))
                        {
                            culledNodes.Add(node);
                            continue;
                        }
                    }
                    else if (culledNodes.Contains(node))
                        continue;

                    if (eCurrent.type == EventType.Repaint)
                        removeEntries.Clear();

                    NodeEditor nodeEditor = NodeEditor.GetEditor(node);

                    NodeEditor.portPositions.Clear();

                    //Get node position
                    Vector2 nodePos = GridToWindowPositionNoClipped(node.NodePosition);
                    Vector2 nodeSize = Vector2.zero;

                    bool selected = selectionCache.Contains(node);

                    float nodeWidth = nodeEditor.GetWidth();
                    GUILayout.BeginArea(new Rect(nodePos, new Vector2(nodeWidth, 4000)));
                    {
                        if (selected)
                        {
                            GUI.color = NodeEditorPreferences.GetSettings(this).highlightColor;
                            GUILayout.BeginVertical(nodeEditor.GetBodyHighlightStyle());
                        }

                        GUI.color = nodeEditor.GetHeaderTint();
                        GUILayout.BeginVertical(nodeEditor.GetHeaderStyle());
                        {
                            GUI.color = Color.white;
                            nodeEditor.OnHeaderGUI();
                        }
                        GUILayout.EndVertical();

                        nodeSize = GUILayoutUtility.GetLastRect().size;

                        GUILayout.Space(-0.01f);

                        GUI.color = nodeEditor.GetBodyTint();
                        GUILayout.BeginVertical(nodeEditor.GetBodyStyle());
                        {
                            GUI.color = Color.white;
                            EditorGUI.BeginChangeCheck();

                            EditorGUIUtility.labelWidth = nodeWidth * 0.4f;
                            //Draw node contents
                            nodeEditor.OnBodyGUI();
                            EditorGUIUtility.labelWidth = 0;
                            //If user changed a value, notify other scripts through onUpdateNode
                            if (EditorGUI.EndChangeCheck())
                            {
                                if (NodeEditor.onUpdateNode != null)
                                    NodeEditor.onUpdateNode(node);
                                EditorUtility.SetDirty(node);
                                nodeEditor.serializedObject.ApplyModifiedProperties();
                            }
                        }
                        GUILayout.EndVertical();

                        nodeSize.y += GUILayoutUtility.GetLastRect().size.y;

                        GUI.color = nodeEditor.GetFooterTint();
                        GUILayout.BeginVertical(nodeEditor.GetFooterStyle(), GUILayout.Height(12));
                        GUILayout.EndVertical();
                        GUI.color = Color.white;

                        nodeSize.y += GUILayoutUtility.GetLastRect().size.y;

                        if (selected)
                        {
                            GUILayout.Space(-2.3f);
                            GUILayout.EndVertical();
                        }

                        GUI.color = guiColor;

                        //Cache data about the node for next frame
                        if (eCurrent.type == EventType.Repaint)
                        {
                            nodeSizes[node] = nodeSize;
                        }

                        if (eCurrent.type != EventType.Layout && nodeSizes.TryGetValue(node, out nodeSize))
                        {
                            //Check if we are hovering this node
                            Rect windowRect = new Rect(nodePos, nodeSize);
                            if (windowRect.Contains(mousePos))
                                hoveredNode = node;

                            //If dragging a selection box, add nodes inside to selection
                            if (CurrentActivity == NodeActivity.DragGrid)
                            {
                                if (windowRect.Overlaps(selectionBox))
                                    preSelection.Add(node);
                            }
                        }
                    }
                    GUILayout.EndArea();
                }

                if (node.Graph == target ||
                    NodeReflection.IsInPoint(node) ||
                    NodeReflection.IsOutPoint(node))
                {
                    if (eCurrent.type != EventType.Layout)
                    {
                        //Check if we are hovering any of this nodes ports
                        //Check input ports
                        foreach (var input in node.Inputs)
                        {
                            Rect r = GridToWindowRectNoClipped(input.Rect);
                            if (r.Contains(mousePos))
                                hoveredPort = input;
                        }

                        //Check all output ports
                        foreach (var output in node.Outputs)
                        {
                            Rect r = GridToWindowRectNoClipped(output.Rect);
                            if (r.Contains(mousePos))
                                hoveredPort = output;
                        }
                    }
                }
            }

            if (eCurrent.type != EventType.Layout && CurrentActivity == NodeActivity.DragGrid)
                Selection.objects = preSelection.ToArray();

            EndZoom();

            //If a change in is detected in the selected node, call OnValidate method.
            //This is done through reflection because OnValidate is only relevant in editor,
            //and thus, the code should not be included in build.
            if (onValidate != null && EditorGUI.EndChangeCheck())
                onValidate.Invoke(Selection.activeObject, null);
        }

        private bool ShouldBeCulled(uNody.Node node)
        {
            Vector2 nodePos = GridToWindowPositionNoClipped(node.NodePosition);

            if (nodePos.x / Zoom > DrawRect.width) return true; // Right
            else if (nodePos.y / Zoom > DrawRect.height) return true; // Bottom
            else if (nodeSizes.ContainsKey(node))
            {
                Vector2 size = nodeSizes[node];
                if (nodePos.x + size.x < 0) return true; // Left
                else if (nodePos.y + size.y + 23f < 0) return true; // Top
            }
            return false;
        }

        private void DrawTooltip()
        {
            if (!NodeEditorPreferences.GetSettings(this).portTooltips)
                return;

            if (hoveredPort != null && (hoveredPort.ValueType == typeof(ILogicNode)))
                return;

            string tooltip = null;
            if (hoveredPort != null)
            {
                tooltip = GetPortTooltip(hoveredPort);
            }
            else if (hoveredNode != null && IsHoveringNode && IsHoveringTitle(hoveredNode))
            {
                tooltip = NodeEditor.GetEditor(hoveredNode).GetHeaderTooltip();
            }
            if (string.IsNullOrEmpty(tooltip)) return;
            GUIContent content = new GUIContent(tooltip);
            Vector2 size = NodeEditorStyles.Tooltip.CalcSize(content);
            size.x += 8;
            Rect rect = new Rect(Event.current.mousePosition, size);
            if (hoveredPort != null)
            {
                if (hoveredPort.Direction == uNody.NodePort.IO.Input)
                    rect.position -= size;
                else
                    rect.position = new Vector2(rect.position.x, rect.position.y - size.y);
            }
            EditorGUI.LabelField(rect, content, NodeEditorStyles.Tooltip);
        }
    }
}