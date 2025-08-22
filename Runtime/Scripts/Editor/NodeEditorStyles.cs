using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNodyEditor {
    public static class NodeEditorStyles
    {
        private static NodeEditorStylesImpl impl;
        private static NodeEditorStylesImpl Impl => impl ??= new NodeEditorStylesImpl();

        public static GUIStyle InputDotPort => Impl.InputDotPort;
        public static GUIStyle OutputDotPort => Impl.OutputDotPort;
        public static GUIStyle InputArrowPort => Impl.InputArrowPort;
        public static GUIStyle OutputArrowPort => Impl.OutputArrowPort;
        public static GUIStyle OutputPortLabel => Impl.OutputPortLabel;
        public static GUIStyle NodeHeader => Impl.NodeHeader;
        public static GUIStyle NodeHeaderLabel => Impl.NodeHeaderLabel;
        public static GUIStyle NodeBody => Impl.NodeBody;
        public static GUIStyle NodeFooter => Impl.NodeFooter;
        public static GUIStyle NodeHighlight => Impl.NodeHighlight;
        public static GUIStyle Tooltip => Impl.Tooltip;

        public static Vector2 PortSize => impl.PortSize;

        public static Texture2D DotFilledTexture => Impl.DotFilledTexture;
        public static Texture2D DotEmptyTexture => Impl.DotEmptyTexture;
        public static Texture2D NodeHeaderTexture => Impl.NodeHeaderTexture;
        public static Texture2D NodeBodyTexture => Impl.NodeBodyTexture;
        public static Texture2D NodeHighlightTexture => Impl.NodeHighlightTexture;

        public static Texture2D GenerateGridTexture(Color line, Color bg)
            => Impl.GenerateGridTexture(line, bg);

        public static Texture2D GenerateCrossTexture(Color line)
            => Impl.GenerateCrossTexture(line);

        private class NodeEditorStylesImpl
        {
            public GUIStyle InputDotPort { get; private set; }
            public GUIStyle OutputDotPort { get; private set; }
            public GUIStyle InputArrowPort { get; private set; }
            public GUIStyle OutputArrowPort { get; private set; }
            public GUIStyle OutputPortLabel { get; private set; }
            public GUIStyle NodeHeader { get; private set; }
            public GUIStyle NodeHeaderLabel { get; private set; }
            public GUIStyle NodeBody { get; private set; }
            public GUIStyle NodeFooter { get; private set; }
            public GUIStyle NodeHighlight { get; private set; }
            public GUIStyle Tooltip { get; private set; }

            public Vector2 PortSize { get; private set; }

            public Texture2D DotFilledTexture { get; private set; }
            public Texture2D DotHoverTexture {get; private set; }
            public Texture2D DotEmptyTexture { get; private set; }
            public Texture2D DotArrowFilledTexture { get; private set; }
            public Texture2D DotArrowHoverTexture { get; private set; }
            public Texture2D DotArrowEmptyTexture { get; private set; }
            public Texture2D NodeHeaderTexture { get; private set; }
            public Texture2D NodeBodyTexture { get; private set; }
            public Texture2D NodeFooterTexture { get; private set; }
            public Texture2D NodeHighlightTexture { get; private set; }

            public NodeEditorStylesImpl()
            {
                GUIStyle baseStyle = new GUIStyle("Label");
                baseStyle.fixedHeight = 18;

                PortSize = new Vector2(11f, 11f);

                DotFilledTexture = Resources.Load<Texture2D>("uNody_Dot_Filled");
                DotHoverTexture = Resources.Load<Texture2D>("uNody_Dot_Hover");
                DotEmptyTexture = Resources.Load<Texture2D>("uNody_Dot_Empty");
                DotArrowFilledTexture = Resources.Load<Texture2D>("uNody_Dot_Arrow_Filled");
                DotArrowHoverTexture = Resources.Load<Texture2D>("uNody_Dot_Arrow_Hover");
                DotArrowEmptyTexture = Resources.Load<Texture2D>("uNody_Dot_Arrow_Empty");
                NodeHeaderTexture = Resources.Load<Texture2D>("uNody_Node_Header");
                NodeBodyTexture = Resources.Load<Texture2D>("uNody_Node_Body");
                NodeFooterTexture = Resources.Load<Texture2D>("uNody_Node_Footer");
                NodeHighlightTexture = Resources.Load<Texture2D>("uNody_Node_Highlight");

                InputDotPort = new GUIStyle(baseStyle);
                InputDotPort.alignment = TextAnchor.UpperLeft;
                InputDotPort.padding.top = 4;
                InputDotPort.active.background = DotFilledTexture;
                InputDotPort.hover.background = DotHoverTexture;
                InputDotPort.normal.background = DotEmptyTexture;

                OutputDotPort = new(baseStyle);
                OutputDotPort.alignment = TextAnchor.UpperRight;
                OutputDotPort.padding.top = 4;
                OutputDotPort.active.background = DotFilledTexture;
                OutputDotPort.hover.background = DotHoverTexture;
                OutputDotPort.normal.background = DotEmptyTexture;

                InputArrowPort = new GUIStyle(InputDotPort);
                InputArrowPort.active.background = DotArrowFilledTexture;
                InputArrowPort.hover.background = DotArrowHoverTexture;
                InputArrowPort.normal.background = DotArrowEmptyTexture;

                OutputArrowPort = new GUIStyle(OutputDotPort);
                OutputArrowPort.active.background = DotArrowFilledTexture;
                OutputArrowPort.hover.background = DotArrowHoverTexture;
                OutputArrowPort.normal.background = DotArrowEmptyTexture;

                OutputPortLabel = new GUIStyle(EditorStyles.label);
                OutputPortLabel.padding.top = 2;
                OutputPortLabel.alignment = TextAnchor.UpperRight;

                NodeHeader = new GUIStyle();
                NodeHeader.normal.background = NodeHeaderTexture;
                NodeHeader.border = new RectOffset(32, 32, 12, 1);
                NodeHeader.padding = new RectOffset(16, 16, 6, 0);

                NodeHeaderLabel = new GUIStyle();
                NodeHeaderLabel.padding = new RectOffset(4, 4, 4, 4);
                NodeHeaderLabel.alignment = TextAnchor.MiddleCenter;
                NodeHeaderLabel.fontStyle = FontStyle.Bold;
                NodeHeaderLabel.normal.textColor = Color.white;

                NodeBody = new GUIStyle();
                NodeBody.normal.background = NodeBodyTexture;
                NodeBody.border = new RectOffset(32, 32, 2, 2);
                NodeBody.padding = new RectOffset(16, 16, 4, 4);

                NodeFooter = new GUIStyle();
                NodeFooter.normal.background = NodeFooterTexture;
                NodeFooter.border = new RectOffset(32, 32, 1, 0);
                NodeFooter.padding = new RectOffset(16, 16, 0, 16);

                NodeHighlight = new GUIStyle();
                NodeHighlight.normal.background = NodeHighlightTexture;
                NodeHighlight.border = new RectOffset(32, 32, 16, 16);

                Tooltip = new GUIStyle("helpBox");
                Tooltip.alignment = TextAnchor.MiddleCenter;
            }

            public Texture2D GenerateGridTexture(Color line, Color bg)
            {
                Texture2D tex = new(64, 64);
                Color[] cols = new Color[64 * 64];
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        Color col = bg;
                        if (y % 16 == 0 || x % 16 == 0) col = Color.Lerp(line, bg, 0.65f);
                        if (y == 63 || x == 63) col = Color.Lerp(line, bg, 0.35f);
                        cols[(y * 64) + x] = col;
                    }
                }
                tex.SetPixels(cols);
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = FilterMode.Bilinear;
                tex.name = "Grid";
                tex.hideFlags = HideFlags.DontSave;
                tex.Apply();
                return tex;
            }

            public Texture2D GenerateCrossTexture(Color line)
            {
                Texture2D tex = new Texture2D(64, 64);
                Color[] cols = new Color[64 * 64];
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        Color col = line;
                        if (y != 31 && x != 31) col.a = 0;
                        cols[(y * 64) + x] = col;
                    }
                }
                tex.SetPixels(cols);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.hideFlags = HideFlags.DontSave;
                tex.name = "Grid";
                tex.Apply();
                return tex;
            }
        }
    }
}