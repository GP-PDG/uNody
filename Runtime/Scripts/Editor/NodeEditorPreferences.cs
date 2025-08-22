using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PuppyDragon.uNodyEditor
{
    using Codice.Client.BaseCommands;
    using PuppyDragon.uNody;
    using PuppyDragon.uNodyEditor.Logic;

    public static class NodeEditorPreferences
    {
        private const string kProviderRootKey = "Preferences/uNody";
        private const string kDefaultSettingsKey = "PuppyDragon.uNody.Settings";

        private static Dictionary<string, Settings> settings = new();
        private static Dictionary<string, Settings> defaultSettingsByKey = new();
        private static Dictionary<Type, string> keysByType = new();
        private static Settings defaultSetting;

        private static Settings DefaultSettings
        {
            get
            {
                if (defaultSetting == null)
                    settings.TryGetValue(kDefaultSettingsKey, out defaultSetting);
                return defaultSetting;
            }
        }

        private static string GetTypeKey(Type type)
            => keysByType.TryGetValue(type, out string key) ? key : keysByType[type] = $"{kDefaultSettingsKey}.{type.FullName}";

        private static void CacheDefaultSettings()
        {
            if (defaultSettingsByKey.Count != 0)
                return;

            defaultSettingsByKey[kDefaultSettingsKey] = new Settings();

            var types = TypeCache.GetTypesWithAttribute<NodeGraphEditor.CustomNodeGraphEditorAttribute>();
            foreach (var type in types)
            {
                var attribute = type.GetCustomAttribute<NodeGraphEditor.CustomNodeGraphEditorAttribute>(true);
                if (!attribute.isRegisterToPreferences)
                    continue;

                defaultSettingsByKey[GetTypeKey(attribute.inspectedType)] = (Activator.CreateInstance(type) as NodeGraphEditor).CreateDefaultPreferences();
            }
        }

        public static Settings GetSettings(NodeGraph graph)
            => GetSettings(NodeGraphEditor.GetEditor(graph));

        /// <summary> Get settings of current active editor </summary>
        public static Settings GetSettings(NodeGraphEditor graphEditor = null)
        {
            string key;
            if (graphEditor != null)
                key = GetTypeKey(graphEditor.target.GetType());
            else
                key = kDefaultSettingsKey;
;
            if (!settings.ContainsKey(key))
                VerifyLoaded(key);

            return settings[key];
        }

        [SettingsProviderGroup]
        public static SettingsProvider[] NodeSettingsProvider()
        {
            var proviers = new List<SettingsProvider>();
            var types = TypeCache.GetTypesWithAttribute<NodeGraphEditor.CustomNodeGraphEditorAttribute>();
            var namespaces = new Dictionary<string, SettingsProvider>();

            var provider = new SettingsProvider(kProviderRootKey, SettingsScope.User)
            {
                guiHandler = searchContext => PreferencesGUI(kDefaultSettingsKey),
                keywords = new HashSet<string>(new[] { "PuppyDragon.uNody", "node", "editor", "graph", "connections", "noodles", "ports" })
            };

            namespaces["Null"] = provider;

            proviers.Add(provider);

            foreach (var type in types)
            {
                if (type == typeof(NodeGraphEditor) || type == typeof(LogicGraphEditor))
                    continue;

                var attribute = type.GetCustomAttribute<NodeGraphEditor.CustomNodeGraphEditorAttribute>(true);
                if (!attribute.isRegisterToPreferences)
                    continue;

                string key = GetTypeKey(attribute.inspectedType);
                string typeNamespace = string.IsNullOrEmpty(attribute.inspectedType.Namespace) ? "Null" : attribute.inspectedType.Namespace;
                var rootKey = kProviderRootKey;

                if (typeNamespace != "Null")
                    rootKey += "/" + typeNamespace.Replace("PuppyDragon.uNody.", "").Replace(".", "/");

                if (!namespaces.ContainsKey(typeNamespace))
                {
                    provider = new SettingsProvider(rootKey, SettingsScope.User)
                    {
                        keywords = new HashSet<string>(new[] { "PuppyDragon.uNody", "node", "editor", "graph", "connections", "noodles", "ports" })
                    };

                    namespaces[typeNamespace] = provider;
                    proviers.Add(provider);
                }

                provider = new SettingsProvider(rootKey + "/" + attribute.inspectedType.Name, SettingsScope.User)
                {
                    guiHandler = searchContext => PreferencesGUI(key),
                    keywords = new HashSet<string>(new[] { "PuppyDragon.uNody", "node", "editor", "graph", "connections", "noodles", "ports" })
                };

                proviers.Add(provider);
            }

            return proviers.ToArray();
        }

        private static void PreferencesGUI(string key)
        {
            VerifyLoaded(key);

            var settings = NodeEditorPreferences.settings[key];

            EditorGUIUtility.labelWidth = Screen.width * 0.35f;
            NodeSettingsGUI(key, settings);
            GridSettingsGUI(key, settings);
            SystemSettingsGUI(key, settings);
            TypeColorsGUI(key, settings);
            EditorGUIUtility.labelWidth = 0;

            if (GUILayout.Button(new GUIContent("Set Default", "Reset all values to default"), GUILayout.Width(120)))
                ResetPrefs(key);
        }

        private static void GridSettingsGUI(string key, Settings settings) {
            //Label
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            settings.gridSnap = EditorGUILayout.Toggle(new GUIContent("Snap", "Hold CTRL in editor to invert"), settings.gridSnap);
            settings.zoomToMouse = EditorGUILayout.Toggle(new GUIContent("Zoom to Mouse", "Zooms towards mouse position"), settings.zoomToMouse);
            EditorGUILayout.LabelField("Zoom");
            EditorGUI.indentLevel++;
            settings.maxZoom = EditorGUILayout.FloatField(new GUIContent("Max", "Upper limit to zoom"), settings.maxZoom);
            settings.minZoom = EditorGUILayout.FloatField(new GUIContent("Min", "Lower limit to zoom"), settings.minZoom);
            EditorGUI.indentLevel--;
            settings.gridLargeLineColor = EditorGUILayout.ColorField("Large Line", settings.gridLargeLineColor);
            settings.GridSmallLineColor = EditorGUILayout.ColorField("Small Line", settings.GridSmallLineColor);
            if (EditorGUI.EndChangeCheck())
                SavePrefs(key, settings);
            EditorGUILayout.EndVertical();

        }

        private static void SystemSettingsGUI(string key, Settings settings) {
            //Label
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("System", EditorStyles.boldLabel);
            settings.autoSave = EditorGUILayout.Toggle(new GUIContent("Autosave", "Disable for better editor performance"), settings.autoSave);
            settings.openOnCreate = EditorGUILayout.Toggle(new GUIContent("Open Editor on Create", "Disable to prevent openening the editor when creating a new graph"), settings.openOnCreate);
            if (GUI.changed)
                SavePrefs(key, settings);
            EditorGUILayout.EndVertical();
        }

        private static void NodeSettingsGUI(string key, Settings settings) {
            //Label
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Node", EditorStyles.boldLabel);
            settings.tintColor = EditorGUILayout.ColorField("Tint", settings.tintColor);
            settings.highlightColor = EditorGUILayout.ColorField("Selection", settings.highlightColor);
            settings.noodleThickness = EditorGUILayout.FloatField(new GUIContent("Noodle thickness", "Noodle Thickness of the node connections"), settings.noodleThickness);
            settings.portTooltips = EditorGUILayout.Toggle("Port Tooltips", settings.portTooltips);
            settings.dragToCreate = EditorGUILayout.Toggle(new GUIContent("Drag to Create", "Drag a port connection anywhere on the grid to create and connect a node"), settings.dragToCreate);
            settings.createFilter = EditorGUILayout.Toggle(new GUIContent("Create Filter", "Only show nodes that are compatible with the selected port"), settings.createFilter);
            EditorGUILayout.EndVertical();

            //END
            if (GUI.changed)
                SavePrefs(key, settings);
        }

        private static void TypeColorsGUI(string key, Settings settings) {
            //Label
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Types", EditorStyles.boldLabel);

            bool isNeedSave = false;
            //Clone keys so we can enumerate the dictionary and make changes.
            //Display type colors. Save them if they are edited by the user
            foreach (var typeColorKey in settings.typeColors.Keys.ToArray())
            {
                Color color = settings.typeColors[typeColorKey];

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.BeginHorizontal();
                {
                    color = EditorGUILayout.ColorField(typeColorKey, color);
                }
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck()) {
                    isNeedSave = true;

                    if (settings.typeColors.ContainsKey(typeColorKey))
                        settings.typeColors[typeColorKey] = color;
                    else
                        settings.typeColors.Add(typeColorKey, color);
                }
            }
            EditorGUILayout.EndVertical();

            if (isNeedSave)
            {
                NodeEditorReflection.ClearColorCache();
                SavePrefs(key, settings);
            }
        }

        /// <summary> Load prefs if they exist. Create if they don't </summary>
        private static Settings LoadPrefs(string key)
        {
            CacheDefaultSettings();

            // Create settings if it doesn't exist
            if (!EditorPrefs.HasKey(key))
            {
                if (defaultSettingsByKey.TryGetValue(key, out var settings))
                    EditorPrefs.SetString(key, JsonUtility.ToJson(settings));
                else
                    EditorPrefs.SetString(key, JsonUtility.ToJson(new Settings()));
            }

            return JsonUtility.FromJson<Settings>(EditorPrefs.GetString(key));
        }

        /// <summary> Delete all prefs </summary>
        public static void ResetPrefs(string key)
        {
            if (EditorPrefs.HasKey(key))
                EditorPrefs.DeleteKey(key);
            
            settings.Remove(key);
            defaultSettingsByKey.Remove(key);

            VerifyLoaded(key);
        }

        /// <summary> Save preferences in EditorPrefs </summary>
        private static void SavePrefs(string key, Settings settings)
        {
            EditorPrefs.SetString(key, JsonUtility.ToJson(settings));
        }

        /// <summary> Check if we have loaded settings for given key. If not, load them </summary>
        private static void VerifyLoaded(string key)
        {
            if (!settings.ContainsKey(key))
                settings.Add(key, LoadPrefs(key));
        }

        /// <summary> Return color based on type </summary>
        public static Color GetTypeColor(NodeGraphEditor graphEditor, Type type)
        {
            string key = GetTypeKey(graphEditor.target.GetType());

            VerifyLoaded(key);

            var typeName = string.Empty;
            if (type.IsGenericType)
                typeName = type.GetGenericArguments()[0].PrettyName();
            else if (type.IsArray)
                typeName = type.GetElementType().PrettyName();
            else
                typeName = type.PrettyName();

            var settings = NodeEditorPreferences.settings[key];
            var typeColors = settings.typeColors;

            if (!typeColors.TryGetValue(typeName, out var color))
            {
                UnityEngine.Random.State oldState = UnityEngine.Random.state;
                UnityEngine.Random.InitState(typeName.GetHashCode());

                color = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
                typeColors.Add(typeName, color);

                UnityEngine.Random.state = oldState;

                SavePrefs(key, settings);
            }

            if (!DefaultSettings?.typeColors.ContainsKey(typeName) ?? false)
            {
                DefaultSettings.typeColors.Add(typeName, color);
                SavePrefs(kDefaultSettingsKey, DefaultSettings);
            }

            return color;
        }

        [System.Serializable]
        public class Settings : ISerializationCallbackReceiver
        {
            public Color32 gridLargeLineColor = new Color32(14, 14, 14, 255);
            public Color32 gridSmallLineColor = new Color32(41, 41, 41, 255);

            public Color32 tintColor = new Color32(51, 51, 51, 255);
            public Color32 highlightColor = new Color32(255, 255, 255, 255);

            public bool gridSnap = true;
            public bool autoSave = true;
            public bool openOnCreate = true;
            public bool dragToCreate = true;
            public bool createFilter = true;
            public bool zoomToMouse = true;
            public bool portTooltips = true;

            public float maxZoom = 2f;
            public float minZoom = 0.5f;

            private Texture2D gridLargeLineTexture;
            private Texture2D gridSmallLineTexture;

            public float noodleThickness = 4f;

            [SerializeField]
            private string typeColorsData = string.Empty;

            public Dictionary<string, Color> typeColors = new()
            {
                { "object", new Color32(101, 66, 255, 255) },
                { "bool", new Color32(156, 0, 255, 255) },
                { "string", new Color32(255, 163, 0, 255) },
                { "float", new Color32(151, 255, 0, 255) },
                { "int", new Color32(0, 186, 255, 255) },
                { "UnityEngine.Vector2", new Color32(255, 143, 234, 255) },
                { "UnityEngine.Vector3", new Color32(255, 143, 234, 255) },
                { "UnityEngine.GameObject", new Color32(0, 255, 255, 255) },
                { "UnityEngine.Transform", new Color32(0, 255, 133, 255) },
                { "PuppyDragon.uNody.Logic.ILogicNode", Color.white }
            };

            public Color32 GridLargeLineColor
            {
                get => gridLargeLineColor;
                set
                {
                    gridLargeLineColor = value;
                    gridLargeLineTexture = null;
                    gridSmallLineTexture = null;
                }
            }

            public Color32 GridSmallLineColor
            {
                get => gridSmallLineColor;
                set
                {
                    gridSmallLineColor = value;
                    gridSmallLineTexture = null;
                }
            }

            public Texture2D GridLargeLineTexture => gridLargeLineTexture ??= NodeEditorStyles.GenerateGridTexture(gridLargeLineColor, gridSmallLineColor);
            public Texture2D GridSmallLineTexture => gridSmallLineTexture ??= NodeEditorStyles.GenerateCrossTexture(gridLargeLineColor);

            public void OnAfterDeserialize()
            {
                // Deserialize typeColorsData
                var datas = typeColorsData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < datas.Length; i += 2)
                {
                    if (ColorUtility.TryParseHtmlString("#" + datas[i + 1], out var col))
                        typeColors[datas[i]] = col;
                }
            }

            public void OnBeforeSerialize()
            {
                // Serialize typeColors
                typeColorsData = string.Empty;
                foreach (var item in typeColors)
                    typeColorsData += item.Key + "," + ColorUtility.ToHtmlStringRGB(item.Value) + ",";

            }
        }
    }
}