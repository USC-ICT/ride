using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

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
                "Other Unity projects’ caches will NOT be affected.",
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

            EditorGUILayout.Space();
            DrawBottomBar();
        }

        private void DrawTopBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reload", GUILayout.Width(90))) TryLoadDefault();

                if (GUILayout.Button("Load Default Path", GUILayout.Width(150))) TryLoadDefault();

                if (GUILayout.Button("Browse...", GUILayout.Width(100)))
                {
                    var dir = SafeDir(m_path);
                    var p = EditorUtility.OpenFilePanel("Select Ride Config", dir, "json");
                    if (!string.IsNullOrEmpty(p) && File.Exists(p))
                    {
                        try
                        {
                            m_config = ConfigurationSystemUnity.Load(p);
                            m_path = p;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Load failed: {ex}");
                            EditorUtility.DisplayDialog(TitleText, "Failed to load config. See Console.", "OK");
                        }
                    }
                }

                if (GUILayout.Button("Open Folder", GUILayout.Width(110)))
                {
                    try
                    {
                        var folder = Path.GetDirectoryName(m_path);
                        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                            EditorUtility.RevealInFinder(folder);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Open Folder failed: {ex}");
                    }
                }

                if (GUILayout.Button("Edit Config", GUILayout.Width(110)))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(m_path))
                            Application.OpenURL($"file:///{m_path.Replace("\\", "/")}");
                    }
                    catch (Exception ex) { Debug.LogError($"Edit Config failed: {ex}"); }
                }

                GUILayout.FlexibleSpace();

                bool newRevealAll = GUILayout.Toggle(m_revealAllSecrets, "Show all", "Button", GUILayout.Width(90));
                if (newRevealAll != m_revealAllSecrets)
                {
                    m_revealAllSecrets = newRevealAll;
                    Repaint();
                }

                if (GUILayout.Button("Expand all", GUILayout.Width(90)))
                    SetAllFoldouts(true);

                if (GUILayout.Button("Collapse all", GUILayout.Width(90)))
                    SetAllFoldouts(false);

                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Current Path:", GUILayout.Width(100));
                EditorGUILayout.SelectableLabel(m_path ?? "(none)", GUILayout.Height(18));
            }

            var verStyle = ConfigurationSystemUnity.IsCorrectVersion(m_config)
                ? EditorStyles.label
                : new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.red } };

            EditorGUILayout.LabelField($"Version: {m_config.version}", verStyle);
            if (!ConfigurationSystemUnity.IsCorrectVersion(m_config))
                EditorGUILayout.LabelField($"Config File Incorrect Version!", verStyle);
        }

        private void DrawBottomBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset All to Defaults", GUILayout.Width(180)))
                {
                    m_config = RideConfig.Default;
                    CommitImmediateUIChange();
                }

                var btnStyle = ConfigurationSystemUnity.IsCorrectVersion(m_config)
                    ? GUI.skin.button
                    : new GUIStyle(GUI.skin.button) { normal = { textColor = Color.red } };

                if (GUILayout.Button("Upgrade to Latest", btnStyle, GUILayout.Width(160)))
                    AttemptUpgrade();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Save As...", GUILayout.Width(100)))
                {
                    var dir = SafeDir(m_path);
                    var p = EditorUtility.SaveFilePanel("Save Ride Config As", dir, "ride.config.json", "json");
                    if (!string.IsNullOrEmpty(p)) TrySave(p);
                }

                if (GUILayout.Button("Save", GUILayout.Width(80)))
                    TrySave(m_path);

                if (GUILayout.Button("Save to Default Config", GUILayout.Width(180)))
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
                // Load the current file text if available; otherwise, upgrade from the in-memory config
                string json;
                if (!string.IsNullOrEmpty(m_path) && File.Exists(m_path))
                    json = File.ReadAllText(m_path);
                else
                    json = JsonUtility.ToJson(m_config); // best-effort fallback

                var upgraded = LoadMergedOntoDefaults(json, out string loadedVer);

                // Set to the latest default version explicitly as part of the upgrade
                upgraded.version = RideConfig.Default.version;

                m_config = upgraded;
                CommitImmediateUIChange();

                EditorUtility.DisplayDialog(TitleText, "Upgraded in memory to the latest schema.\nReview changes and Save.", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Upgrade failed: {ex}");
                EditorUtility.DisplayDialog(TitleText, "Failed to upgrade config. See Console.", "OK");
            }
        }

        private static RideConfig LoadMergedOntoDefaults(string json, out string loadedVersionText)
        {
            // 1) Start from current schema defaults so new fields remain populated
            var result = RideConfig.Default; // struct copy

            // 2) Forgiving deserialize of the user's file into a TEMP object
            //    (Unknown fields ignored; nulls preserved; nested objects created)
            var settings = new JsonSerializerSettings
            {
                MissingMemberHandling  = MissingMemberHandling.Ignore,
                NullValueHandling      = NullValueHandling.Include,   // keep nulls so we can treat them as "absent"
                DefaultValueHandling   = DefaultValueHandling.Populate,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                Error = (sender, args) => { args.ErrorContext.Handled = true; }
            };

            RideConfig temp;
            try
            {
                temp = JsonConvert.DeserializeObject<RideConfig>(json, settings);
            }
            catch
            {
                temp = default; // if it totally fails, overlay does nothing and defaults remain
            }

            // 3) Overlay temp -> result using our rule:
            //    - strings: only copy if NOT null/empty (so defaults survive otherwise)
            //    - numbers/bools: current LooksPresent logic applies
            object boxedDst = result; // box value type so SetValue writes into it
            OverlayRecursive(temp, boxedDst, typeof(RideConfig));
            result = (RideConfig)boxedDst;

            // 4) Version (for display only) and optional guardrails
            loadedVersionText = ExtractVersionFromJson(json);
            result = Sanitize(result);

            return result;
        }

        // Recursively overlay fields from src -> dst. 
        // Works with structs (boxed), nested structs, and simple leaves.
        private static void OverlayRecursive(object src, object dst, Type t)
        {
            if (src == null || dst == null) return;

            // Leaf-like cases are handled by parent (sets the field). Bail here.
            if (IsLeaf(t)) return;

            var flags = BindingFlags.Instance | BindingFlags.Public;
            var fields = t.GetFields(flags);
            for (int i = 0; i < fields.Length; i++)
            {
                var f = fields[i];
                var fType = f.FieldType;

                var srcVal = f.GetValue(src);
                var dstVal = f.GetValue(dst);

                if (IsLeaf(fType))
                {
                    if (LooksPresent(srcVal, fType))
                    {
                        // write into boxed struct/class
                        f.SetValue(dst, CoerceIfNeeded(srcVal, fType));
                    }
                }
                else
                {
                    // For nested structs/classes: recurse if srcVal is non-null (for classes)
                    // For structs, srcVal will always be a value; recurse anyway.
                    if (srcVal != null)
                    {
                        // If destination is null (class), create one so we can descend
                        if (dstVal == null && fType.IsClass)
                        {
                            dstVal = Activator.CreateInstance(fType);
                            f.SetValue(dst, dstVal);
                        }
                        OverlayRecursive(srcVal, dstVal, fType);

                        // write the (boxed) nested struct/class back into the parent
                        f.SetValue(dst, dstVal);
                    }
                }
            }

            // (Optional) public settable properties if you have any (your file is fields-only today)
            // var props = t.GetProperties(flags).Where(p => p.CanRead && p.CanWrite);
            // foreach (var p in props) { ... } 
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
            // RESTSettings.port is ushort; clamp already happens in CoerceIfNeeded,
            // but if you ever change it, you can enforce here too.
            if (cfg.rest.port > 65535) cfg.rest.port = 65535;   // defensive; port is ushort today :contentReference[oaicite:3]{index=3}
            if (cfg.rest.port < 0) cfg.rest.port = 0;

            return cfg;
        }
    }
}
