using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ride
{
    /// <summary>
    /// RideConfigWindow (Editor)
    /// Reflection-driven Unity Editor window for viewing and editing <c>RideConfig</c>
    /// with support for per-field defaults, secret masking (Show/Hide and Show all), and
    /// expand/collapse of nested objects. Values are loaded/saved via <c>ConfigurationSystemUnity</c>
    /// to the user's JSON config file.
    /// </summary>
    /// <remarks>
    /// <para><b>Purpose</b></para>
    /// <para>
    /// Provide a single place for developers to enter and persist their local configuration
    /// (API keys, endpoints, ports, options) without hard-coding secrets in source. The window
    /// mirrors the shape of <c>RideConfig</c> via reflection and writes changes back to the JSON
    /// file managed by <c>ConfigurationSystemUnity</c>.
    /// </para>
    ///
    /// <para><b>Menu</b></para>
    /// <para>
    /// Unity: <c>Ride/Config...</c>
    /// </para>
    ///
    /// <para><b>Key Features</b></para>
    /// <list type="bullet">
    ///   <item><description><b>Struct-safe editing:</b> Edits occur on a boxed copy of <c>RideConfig</c>; values are written back and unboxed each frame.</description></item>
    ///   <item><description><b>Defaults:</b> Per-field "Default" buttons pull from <c>RideConfig.Default</c>. "Reset All to Defaults" restores the entire config.</description></item>
    ///   <item><description><b>Secrets:</b> Fields that look like secrets (key/token/password/etc.) use a password field with per-field Show/Hide and a global "Show all".</description></item>
    ///   <item><description><b>Navigation:</b> Nested objects render as foldouts with "Expand all" / "Collapse all".</description></item>
    ///   <item><description><b>Version:</b> Displays the current config version and warns if it is incorrect using <c>ConfigurationSystemUnity.IsCorrectVersion</c>.</description></item>
    ///   <item><description><b>Convenience:</b> "Browse...", "Open Folder", and "Edit Config" (open in OS) are provided.</description></item>
    /// </list>
    ///
    /// <para><b>Persistence</b></para>
    /// <para>
    /// Default path is provided by <c>ConfigurationSystemUnity.GetDefaultPath()</c>. Use "Save" to write to the current path
    /// or "Save As..." to write elsewhere. The JSON structure is defined by <c>RideConfig</c> and consumed by runtime systems.
    /// </para>
    ///
    /// <para><b>Safety & Security</b></para>
    /// <list type="bullet">
    ///   <item><description>No real secrets are stored in <c>RideConfig.Default</c>; defaults should be placeholders only.</description></item>
    ///   <item><description>Secret fields are masked by default and can be revealed explicitly by the user.</description></item>
    /// </list>
    ///
    /// <para><b>Assumptions</b></para>
    /// <list type="bullet">
    ///   <item><description><c>RideConfig</c> is defined in <c>ride.abstract</c> and exposes <c>public static RideConfig Default</c>.</description></item>
    ///   <item><description><c>ConfigurationSystemUnity</c> exists in <c>ride.core</c> with <c>GetDefaultPath()</c>, <c>Load(path)</c>, and <c>Save(config, path)</c>.</description></item>
    ///   <item><description>Nested reference types in <c>RideConfig</c> have parameterless constructors; otherwise adjust <c>CreateDefault</c>.</description></item>
    /// </list>
    ///
    /// <para><b>Limitations</b></para>
    /// <list type="bullet">
    ///   <item><description>Collections (arrays/lists) are not edited in this version.</description></item>
    ///   <item><description>Types without obvious editors fall back to read-only text.</description></item>
    /// </list>
    ///
    /// <para><b>Performance</b></para>
    /// <para>
    /// The window uses reflection each OnGUI call. This is acceptable for editor tooling. If you
    /// notice slowdowns with very large configs, consider caching field/property metadata.
    /// </para>
    ///
    /// <para><b>Usage</b></para>
    /// <list type="number">
    ///   <item><description>Open Unity menu: <c>Ride/Config...</c>.</description></item>
    ///   <item><description>Click "Load Default Path" or "Browse..." to select an existing JSON file.</description></item>
    ///   <item><description>Edit values. Use "Default" per field or "Reset All to Defaults".</description></item>
    ///   <item><description>Click "Save" or "Save As..." to persist.</description></item>
    /// </list>
    ///
    /// <para><b>Change Log (developer notes)</b></para>
    /// <list type="bullet">
    ///   <item><description>Initial version: struct-safe editing, per-field defaults, secret masking, expand/collapse, version display.</description></item>
    /// </list>
    /// </remarks>
    public class RideConfigWindow : EditorWindow
    {
        private const string TitleText = "Ride Config";
        private const float LabelWidth = 240f;

        private const float ButtonWidthSmall = 50f;
        private const float ButtonWidthDefault = 70f;
        private static readonly GUILayoutOption[] TextFieldOptions =
            new GUILayoutOption[] { GUILayout.MinWidth(200f), GUILayout.ExpandWidth(true) };

        // RideConfig is a struct (value type).
        private RideConfig m_config;        // current working copy
        private RideConfig m_savedConfig;   // snapshot at last load or save, for dirty detection
        private bool m_isDirty;
        private string m_path;              // current file path
        private bool m_loaded;
        private Vector2 m_scroll;
        private bool m_revealAllSecrets; // global "Show all" toggle

        // foldout + secret reveal
        private readonly Dictionary<string, bool> _foldout = new Dictionary<string, bool>();
        private readonly Dictionary<string, bool> _reveal = new Dictionary<string, bool>();


        [MenuItem("Ride/Config...", priority = 100)]
        public static void Open()
        {
            var w = GetWindow<RideConfigWindow>(false, TitleText);
            w.minSize = new Vector2(680, 480);
            w.Show();
        }

        [MenuItem("Ride/Cache Clear (This Project)", priority = 140)]
        public static void ClearAllCaches()
        {
            if (!EditorUtility.DisplayDialog(
                "Clear cached AssetBundles?",
                "This will delete all AssetBundle cache used by this project (" + Application.productName + ") on this machine.\n\n" +
                "Other Unity projects' caches will NOT be affected.",
                "Clear Cache",
                "Cancel"))
                return;

            bool success = Caching.ClearCache();

            Debug.Log(success
                ? "[Ride] Caching.ClearCache() completed successfully."
                : "[Ride][Warning] Caching.ClearCache() returned false.");
        }

        private void OnEnable()
        {
            TryLoadDefault();
        }

        private void TryLoadDefault()
        {
            try
            {
                m_path = ConfigurationSystemUnity.GetDefaultPath();
                m_config = File.Exists(m_path)
                    ? ConfigurationSystemUnity.Load(m_path)
                    : RideConfig.Default;

                m_loaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RideConfigWindow load failed: {ex}");
                m_config = RideConfig.Default;
                m_path = ConfigurationSystemUnity.GetDefaultPath();
                m_loaded = true;
            }
            SetClean();
        }

        private void TryReloadCurrent()
        {
            try
            {
                m_config = (!string.IsNullOrEmpty(m_path) && File.Exists(m_path))
                    ? ConfigurationSystemUnity.Load(m_path)
                    : RideConfig.Default;

                m_loaded = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"RideConfigWindow reload failed: {ex}");
            }
            SetClean();
        }

        private void SetClean()
        {
            m_savedConfig = m_config;
            m_isDirty = false;
        }

        private void OnGUI()
        {
            DrawTopBar();

            if (!m_loaded)
            {
                EditorGUILayout.HelpBox("Config not loaded.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space();
            DrawTreeToolbar();
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUIUtility.labelWidth = LabelWidth;

                // Edit the boxed root and unbox back to RideConfig
                object boxed = m_config;
                object boxedDefaults = RideConfig.Default;

                // Directly draw RideConfig's fields without a root foldout
                var t = typeof(RideConfig);

#pragma warning disable IDE0079  // Remove unnecessary suppression
#pragma warning disable UNT0018  // System.Reflection usage detected in performance critical message 'OnGUI'.
                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
#pragma warning restore IDE0079, UNT0018  // System.Reflection usage detected in performance critical message 'OnGUI'.

                foreach (var f in fields)
                {
                    var cur = f.GetValue(boxed);
                    var def = f.GetValue(boxedDefaults);
                    var newVal = IsLeaf(f.FieldType)
                        ? DrawLeaf(f.Name, cur, def, f.FieldType)
                        : EditObjectRecursive(cur ?? CreateDefault(f.FieldType), def, f.FieldType, f.Name);
                    f.SetValue(boxed, newVal);
                }

                var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                .Where(p => p.CanRead && p.CanWrite);
                foreach (var p in props)
                {
                    var cur = p.GetValue(boxed, null);
                    var def = p.GetValue(boxedDefaults, null);
                    var newVal = IsLeaf(p.PropertyType)
                        ? DrawLeaf(p.Name, cur, def, p.PropertyType)
                        : EditObjectRecursive(cur ?? CreateDefault(p.PropertyType), def, p.PropertyType, p.Name);
                    p.SetValue(boxed, newVal, null);
                }

                m_config = (RideConfig)boxed;
            }
            EditorGUILayout.EndScrollView();

            // Only recompute dirty when something actually changed this frame.
            if (GUI.changed)
                m_isDirty = JsonConvert.SerializeObject(m_config)
                         != JsonConvert.SerializeObject(m_savedConfig);

            EditorGUILayout.Space();
            DrawBottomBar();
        }

        private void DrawTopBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Reload", "Re-read the currently loaded file from disk, discarding any unsaved changes"), GUILayout.Width(90)))
                    TryReloadCurrent();

                if (GUILayout.Button(new GUIContent("Open File...", "Browse for a different config JSON file to load"), GUILayout.Width(100)))
                {
                    var dir = SafeDir(m_path);
                    var p = EditorUtility.OpenFilePanel("Select Ride Config", dir, "json");
                    if (!string.IsNullOrEmpty(p) && File.Exists(p))
                    {
                        try
                        {
                            m_config = ConfigurationSystemUnity.Load(p);
                            m_path = p;
                            SetClean();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Load failed: {ex}");
                            EditorUtility.DisplayDialog(TitleText, "Failed to load config. See Console.", "OK");
                        }
                    }
                }

                if (GUILayout.Button(new GUIContent("Show in Explorer", "Reveal this config file in the OS file browser"), GUILayout.Width(130)))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(m_path) && File.Exists(m_path))
                            EditorUtility.RevealInFinder(m_path);
                        else
                        {
                            var folder = Path.GetDirectoryName(m_path);
                            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                                EditorUtility.RevealInFinder(folder);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Show in Explorer failed: {ex}");
                    }
                }

                if (GUILayout.Button(new GUIContent("Open in Editor", "Open this config file in your system default text editor"), GUILayout.Width(120)))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(m_path))
                            Application.OpenURL($"file:///{m_path.Replace("\\", "/")}");
                    }
                    catch (Exception ex) { Debug.LogError($"Open in Editor failed: {ex}"); }
                }

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                bool isDefault = string.Equals(
                    m_path, ConfigurationSystemUnity.GetDefaultPath(),
                    StringComparison.OrdinalIgnoreCase);

                // Bold the prefix label when a non-default path is active to draw attention.
                // Avoid passing custom styles to SelectableLabel — it uses different height
                // metrics internally and can shift IMGUI layout rects for controls below.
                var prefixStyle = isDefault ? EditorStyles.label : EditorStyles.boldLabel;
                EditorGUILayout.LabelField("Current Path:", prefixStyle, GUILayout.Width(100));

                var pathText = (m_path ?? "(none)") + (isDefault ? "  (default)" : string.Empty);
                var pathStyle = isDefault
                    ? EditorStyles.label
                    : new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
                EditorGUILayout.SelectableLabel(pathText, pathStyle, GUILayout.Height(18));
            }

            var verStyle = ConfigurationSystemUnity.IsCorrectVersion(m_config)
                ? EditorStyles.label
                : new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.red } };

            EditorGUILayout.LabelField($"Version: {m_config.version}", verStyle);
            if (!ConfigurationSystemUnity.IsCorrectVersion(m_config))
                EditorGUILayout.LabelField($"Config File Incorrect Version!", verStyle);
        }

        private void DrawTreeToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Expand All", "Open all service sections"), GUILayout.Width(90)))
                    SetAllFoldouts(true);

                if (GUILayout.Button(new GUIContent("Collapse All", "Close all service sections"), GUILayout.Width(90)))
                    SetAllFoldouts(false);

                GUILayout.Space(8);

                bool newRevealAll = GUILayout.Toggle(
                    m_revealAllSecrets,
                    new GUIContent("Reveal Secrets", "Globally show or hide all API key and password fields"),
                    "Button",
                    GUILayout.Width(110));
                if (newRevealAll != m_revealAllSecrets)
                {
                    m_revealAllSecrets = newRevealAll;
                    Repaint();
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void DrawBottomBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Destructive / schema actions
                if (GUILayout.Button(new GUIContent("Reset to Defaults", "Restore every field to its factory default in memory - does not save. Use Save to write defaults to the current file."), GUILayout.Width(150)))
                {
                    m_config = RideConfig.Default;
                    CommitImmediateUIChange();
                }

                var upgradeStyle = ConfigurationSystemUnity.IsCorrectVersion(m_config)
                    ? GUI.skin.button
                    : new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red } };

                if (GUILayout.Button(new GUIContent("Upgrade Schema", "Migrate this config to the current schema version - new fields get defaults, removed fields are dropped. Review changes and Save when done."), upgradeStyle, GUILayout.Width(130)))
                    AttemptUpgrade();

                GUILayout.FlexibleSpace();

                // Load / Save group
                if (GUILayout.Button(new GUIContent("Load Default", "Switch to the default config path and reload from disk"), GUILayout.Width(110)))
                    TryLoadDefault();

                if (GUILayout.Button(new GUIContent("Save As...", "Save the current config to a new file location"), GUILayout.Width(90)))
                {
                    var dir = SafeDir(m_path);
                    var p = EditorUtility.SaveFilePanel("Save Ride Config As", dir, "ride.config.json", "json");
                    if (!string.IsNullOrEmpty(p)) TrySave(p);
                }

                var saveStyle = m_isDirty
                    ? new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold }
                    : GUI.skin.button;
                var saveLabel = m_isDirty ? "Save *" : "Save";
                if (GUILayout.Button(new GUIContent(saveLabel, "Save the current config to the current file path"), saveStyle, GUILayout.Width(70)))
                    TrySave(m_path);

                if (GUILayout.Button(new GUIContent("Save to Default", "Save the current config to the default config path and switch to it"), GUILayout.Width(130)))
                {
                    TrySave(null);
                    TryLoadDefault();
                }
            }
        }

        private void TrySave(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                    path = ConfigurationSystemUnity.GetDefaultPath();

                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
                ConfigurationSystemUnity.Save(m_config, path);
                m_path = path;
                SetClean();
                EditorUtility.DisplayDialog(TitleText, "Configuration saved.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Save failed: {ex}");
                EditorUtility.DisplayDialog(TitleText, "Failed to save. See Console.", "OK");
            }
        }

        private static string SafeDir(string p)
        {
            try { return Path.GetDirectoryName(p) ?? ""; } catch { return ""; }
        }

        // ==========================
        // Struct-safe reflection UI
        // ==========================
        private object EditObjectRecursive(object boxedObj, object boxedDefaults, Type t, string groupName)
        {
            if (IsLeaf(t))
                return DrawLeaf(groupName, boxedObj, boxedDefaults, t);

            var key = "fold:" + groupName;
            if (!_foldout.ContainsKey(key)) _foldout[key] = true;
            _foldout[key] = EditorGUILayout.Foldout(_foldout[key], groupName, true);
            if (!_foldout[key]) return boxedObj;

            EditorGUI.indentLevel++;

            // Prefer public instance fields; properties optional
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);
            foreach (var f in fields)
            {
                var cur = f.GetValue(boxedObj);
                var def = boxedDefaults != null ? f.GetValue(boxedDefaults) : null;

                var newVal = IsLeaf(f.FieldType)
                    ? DrawLeaf(f.Name, cur, def, f.FieldType)
                    : EditObjectRecursive(cur ?? CreateDefault(f.FieldType), def, f.FieldType, ObjectDisplay(f));

                // Set back into the *boxed* parent (works for structs)
                f.SetValue(boxedObj, newVal);
            }

            // (Optional) public properties with getter/setter
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => p.CanRead && p.CanWrite);
            foreach (var p in props)
            {
                var cur = p.GetValue(boxedObj, null);
                var def = boxedDefaults != null ? p.GetValue(boxedDefaults, null) : null;

                var newVal = IsLeaf(p.PropertyType)
                    ? DrawLeaf(p.Name, cur, def, p.PropertyType)
                    : EditObjectRecursive(cur ?? CreateDefault(p.PropertyType), def, p.PropertyType, ObjectDisplay(p));

                p.SetValue(boxedObj, newVal, null);
            }

            EditorGUI.indentLevel--;
            return boxedObj;
        }

        private object DrawLeaf(string label, object cur, object def, Type t)
        {
            EditorGUI.BeginChangeCheck();

            if (t == typeof(Version))
            {
                var v = cur as Version;
                using (new EditorGUILayout.HorizontalScope())
                {
                    var verStyle = ConfigurationSystemUnity.IsCorrectVersion(m_config)
                        ? EditorStyles.label
                        : new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.red } };

                    EditorGUILayout.LabelField(label, verStyle, GUILayout.Width(LabelWidth));
                    EditorGUILayout.LabelField(v != null ? v.ToString() : "(null)", verStyle, GUILayout.ExpandWidth(true));
                }
                return cur; // unchanged
            }
            else if (t == typeof(string))
            {
                var isSecret = LooksSecret(label);
                var revealKey = "secret:" + label;
                var reveal = m_revealAllSecrets || (_reveal.ContainsKey(revealKey) && _reveal[revealKey]);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
                    var s = cur as string ?? string.Empty;

                    if (isSecret && !reveal)
                        s = EditorGUILayout.PasswordField(s, TextFieldOptions);
                    else
                        s = EditorGUILayout.TextField(s, TextFieldOptions);

                    if (isSecret)
                    {
                        if (GUILayout.Button(reveal ? "Hide" : "Show", GUILayout.Width(ButtonWidthSmall)))
                            _reveal[revealKey] = !reveal;
                    }

                    if (GUILayout.Button("Default", GUILayout.Width(ButtonWidthDefault)))
                    {
                        s = def as string ?? string.Empty;
                        CommitImmediateUIChange();
                    }

                    if (EditorGUI.EndChangeCheck()) return s;
                    return cur;
                }
            }
            else if (t == typeof(bool))
            {
                var v = cur is bool b ? b : false;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
                    v = EditorGUILayout.Toggle(v, GUILayout.Width(18));
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Default", GUILayout.Width(ButtonWidthDefault)))
                    {
                        v = (def is bool db) && db;
                        CommitImmediateUIChange();
                    }
                }

                if (EditorGUI.EndChangeCheck()) return v;
                return cur;
            }
            else if (t == typeof(int))
            {
                var v = cur is int i ? i : 0;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
                    v = EditorGUILayout.IntField(v, TextFieldOptions);

                    if (GUILayout.Button("Default", GUILayout.Width(ButtonWidthDefault)))
                    {
                        v = def is int di ? di : 0;
                        CommitImmediateUIChange();
                    }
                }

                if (EditorGUI.EndChangeCheck()) return v;
                return cur;
            }
            else if (t == typeof(ushort))
            {
                ushort v = cur is ushort us ? us : (ushort)0;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
                    int temp = EditorGUILayout.IntField((int)v, TextFieldOptions);

                    if (GUILayout.Button("Default", GUILayout.Width(ButtonWidthDefault)))
                    {
                        temp = def is ushort dus ? dus : 0;
                        CommitImmediateUIChange();
                    }

                    temp = Mathf.Clamp(temp, 0, 65535);
                    v = (ushort)temp;
                }

                if (EditorGUI.EndChangeCheck()) return v;
                return cur;
            }
            else if (t == typeof(float))
            {
                var v = cur is float f ? f : 0f;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
                    v = EditorGUILayout.FloatField(v, TextFieldOptions);

                    if (GUILayout.Button("Default", GUILayout.Width(ButtonWidthDefault)))
                    {
                        v = def is float df ? df : 0f;
                        CommitImmediateUIChange();
                    }
                }

                if (EditorGUI.EndChangeCheck()) return v;
                return cur;
            }
            else if (t == typeof(double))
            {
                var s = (cur is double d ? d : 0d).ToString(System.Globalization.CultureInfo.InvariantCulture);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
                    s = EditorGUILayout.TextField(s, TextFieldOptions);

                    if (GUILayout.Button("Default", GUILayout.Width(ButtonWidthDefault)))
                    {
                        s = (def is double dd ? dd : 0d).ToString(System.Globalization.CultureInfo.InvariantCulture);
                        CommitImmediateUIChange();
                    }
                }

                if (EditorGUI.EndChangeCheck() && double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    return parsed;
                return cur;
            }
            else if (t.IsEnum)
            {
                var e = cur ?? Activator.CreateInstance(t);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(label, GUILayout.Width(LabelWidth));
                    e = EditorGUILayout.EnumPopup((Enum)e, GUILayout.ExpandWidth(true));

                    if (GUILayout.Button("Default", GUILayout.Width(ButtonWidthDefault)))
                    {
                        e = def ?? Activator.CreateInstance(t);
                        CommitImmediateUIChange();
                    }
                }

                if (EditorGUI.EndChangeCheck()) return e;
                return cur;
            }
            else
            {
                Debug.Log($"Unknown type in config file: {label} - {t.AssemblyQualifiedName}");
            }


            // Fallback
            EditorGUILayout.LabelField(label, cur != null ? cur.ToString() : "(null)");
            EditorGUI.EndChangeCheck();
            return cur;
        }

        private static bool IsLeaf(Type t)
        {
            if (t.IsPrimitive) return true;
            if (t == typeof(string)) return true;
            if (t.IsEnum) return true;
            if (t == typeof(Version)) return true;

            return false;
        }

        private void SetAllFoldouts(bool expand)
        {
            var keys = _foldout.Keys.ToList();
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i].StartsWith("fold:"))
                    _foldout[keys[i]] = expand;
            }
            Repaint();
        }

        private void CommitImmediateUIChange()
        {
            GUI.FocusControl(null);
            EditorGUIUtility.keyboardControl = 0;
            GUI.changed = true;
            Repaint();
        }

        private static string ObjectDisplay(MemberInfo mi) => string.IsNullOrEmpty(mi.Name) ? "(Object)" : mi.Name;

        private static object CreateDefault(Type t)
        {
            if (t == typeof(string)) return string.Empty;
            if (t.IsValueType || t.IsClass) return Activator.CreateInstance(t);
            return null;
        }

        private static bool LooksSecret(string name)
        {
            var n = (name ?? "").ToLowerInvariant();
            return n.Contains("secret") || n.Contains("token") || n.Contains("password") ||
                   n.Contains("apikey") || n.Contains("api_key") || n.Contains("accesskey") ||
                   n.Contains("access_key") || n.Contains("endpointkey") || n.Contains("bearer") ||
                   n.EndsWith("key");
        }


        // ===== MERGE LOADER =====

        private void AttemptUpgrade()
        {
            try
            {
                string json;
                if (!string.IsNullOrEmpty(m_path) && File.Exists(m_path))
                    json = File.ReadAllText(m_path);
                else
                    json = JsonConvert.SerializeObject(m_config); // was JsonUtility.ToJson — mismatched serializer

                var upgraded = LoadMergedOntoDefaults(json, out string loadedVer, out var upgradeErrors);
                upgraded.version = RideConfig.Default.version;

                m_config = upgraded;
                m_isDirty = true;
                CommitImmediateUIChange();

                var fromVer = string.IsNullOrEmpty(loadedVer) ? "unknown" : loadedVer;
                var msg = $"Upgraded in memory from version {fromVer} to {RideConfig.Default.version}.\n\nReview changes and Save. Click Reload to discard the upgrade.";
                if (upgradeErrors.Count > 0)
                    msg += $"\n\n{upgradeErrors.Count} field(s) could not be migrated and kept their defaults:\n• " + string.Join("\n• ", upgradeErrors);

                EditorUtility.DisplayDialog(TitleText, msg, "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Upgrade failed: {ex}");
                EditorUtility.DisplayDialog(TitleText, "Failed to upgrade config. See Console.", "OK");
            }
        }

        private static RideConfig LoadMergedOntoDefaults(string json, out string loadedVersionText, out List<string> errors)
        {
            errors = new List<string>();
            loadedVersionText = ExtractVersionFromJson(json);

            var result = RideConfig.Default;

            // Parse to JObject first so OverlayRecursive knows exactly which keys were present
            // in the old JSON. Without this, fields absent from the old schema get CLR defaults
            // (false for bool, 0 for ints) which then overwrite the new schema's real defaults.
            JObject sourceJson;
            try
            {
                sourceJson = JObject.Parse(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Upgrade: JSON could not be parsed - {ex.Message}");
                errors.Add("JSON could not be parsed — all fields kept at defaults");
                return result;
            }

            var capturedErrors = errors;
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling    = MissingMemberHandling.Ignore,
                NullValueHandling        = NullValueHandling.Include,
                DefaultValueHandling     = DefaultValueHandling.Populate,
                ObjectCreationHandling   = ObjectCreationHandling.Replace,
                // Explicitly ignore $id/$ref/$type written by RideIO.JsonSerialize
                // (PreserveReferencesHandling.Objects boxes every struct and stamps $id on it).
                // Without this, Newtonsoft silently tries to process them and any resulting
                // errors are swallowed by the error handler below.
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                Error = (sender, args) =>
                {
                    capturedErrors.Add($"{args.ErrorContext.Path}: {args.ErrorContext.Error.Message}");
                    Debug.LogWarning($"Upgrade field error at '{args.ErrorContext.Path}': {args.ErrorContext.Error.Message}");
                    args.ErrorContext.Handled = true;
                }
            };

            RideConfig temp;
            try
            {
                temp = JsonConvert.DeserializeObject<RideConfig>(json, settings);
            }
            catch (Exception ex)
            {
                // Total failure: don't run OverlayRecursive on a zeroed temp — bools would all
                // pass LooksPresent (it can't distinguish absent from explicit false) and would
                // overwrite any true defaults in result.
                Debug.LogError($"Upgrade: deserialization failed - {ex.Message}");
                errors.Add("Config deserialization failed — all fields kept at defaults");
                return result;
            }

            object boxedDst = result;
            OverlayRecursive(temp, boxedDst, typeof(RideConfig), sourceJson);
            result = (RideConfig)boxedDst;
            result = Sanitize(result);

            return result;
        }

        // Recursively overlay fields from src -> dst.
        // sourceJson is the JObject for the current level — used to check which fields actually
        // existed in the old JSON, so we never overwrite a new default with a CLR zero-value for
        // a field that was simply absent from the old schema.
        private static void OverlayRecursive(object src, object dst, Type t, JObject sourceJson)
        {
            if (src == null || dst == null) return;
            if (IsLeaf(t)) return;

            var flags = BindingFlags.Instance | BindingFlags.Public;
            var fields = t.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                var fType = f.FieldType;

                // Resolve the JSON key name, respecting [JsonProperty] if present.
                var jsonKey = (f.GetCustomAttributes(typeof(JsonPropertyAttribute), false)
                                 .FirstOrDefault() as JsonPropertyAttribute)?.PropertyName ?? f.Name;

                // Skip fields not present in the source JSON — this is the only reliable way to
                // avoid new bool/numeric fields (whose defaults may be true/non-zero) from being
                // clobbered by the CLR zero-value that Newtonsoft places in temp for absent fields.
                if (sourceJson != null && !sourceJson.ContainsKey(jsonKey))
                    continue;

                var srcVal = f.GetValue(src);
                var dstVal = f.GetValue(dst);

                if (IsLeaf(fType))
                {
                    if (LooksPresent(srcVal, fType))
                        f.SetValue(dst, CoerceIfNeeded(srcVal, fType));
                }
                else if (srcVal != null)
                {
                    if (dstVal == null && fType.IsClass)
                    {
                        dstVal = Activator.CreateInstance(fType);
                        f.SetValue(dst, dstVal);
                    }

                    var childJson = sourceJson?[jsonKey] as JObject;
                    OverlayRecursive(srcVal, dstVal, fType, childJson);
                    f.SetValue(dst, dstVal);
                }
            }
        }

        // Decide whether an incoming value looks "present" (i.e., should overwrite defaults).
        private static bool LooksPresent(object val, Type t)
        {
            if (val == null) return false;
            if (t == typeof(string)) return !string.IsNullOrEmpty((string)val);
            if (t.IsEnum) return true;

            if (t == typeof(bool))
            {
                // Caveat: cannot distinguish "absent" vs "explicit false" with JsonUtility.
                // Best-effort policy: apply false as well.
                return true;
            }
            if (t == typeof(ushort)) return (ushort)val != default(ushort);
            if (t == typeof(int))    return (int)val != default;
            if (t == typeof(float))  return Math.Abs((float)val - default(float)) > float.Epsilon;
            if (t == typeof(double)) return Math.Abs((double)val - default(double)) > double.Epsilon;
            if (t == typeof(Version)) return val != null; // we normally won't get this via JsonUtility

            // Value-types (structs) are handled by recursion, not here
            return true;
        }

        // Handle minor type coercions (e.g., clamp ports, convert numerics if needed)
        private static object CoerceIfNeeded(object val, Type t)
        {
            if (t == typeof(ushort))
            {
                // JsonUtility maps JSON numbers to the exact field type; still, clamp to be safe
                int asInt = Convert.ToInt32(val);
                if (asInt < 0) asInt = 0;
                if (asInt > 65535) asInt = 65535;
                return (ushort)asInt;
            }
            return val;
        }

        [Serializable] private struct VersionNumber { public int Major, Minor, Build, Revision; }
        [Serializable] private struct VersionProbe { public VersionNumber version; }

        private static string ExtractVersionFromJson(string json)
        {
            try
            {
                var probe = JsonConvert.DeserializeObject<VersionProbe>(json);
                return $"{probe.version.Major}.{probe.version.Minor}.{probe.version.Build}.{probe.version.Revision}";
            }
            catch
            {
                return "(unknown)";
            }
        }

        // Example guardrails you might apply after merge
        private static RideConfig Sanitize(RideConfig cfg)
        {
            cfg.rest.port = Math.Clamp(cfg.rest.port, 0, 65535);

            return cfg;
        }
    }
}
