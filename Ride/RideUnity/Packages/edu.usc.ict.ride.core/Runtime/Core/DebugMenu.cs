using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// A built-in implementation of <see cref="IDebugMenu"/> that provides runtime-accessible GUI panels for diagnostics,
    /// configuration, logging, and system introspection in RIDE-based Unity applications.
    /// 
    /// This class handles:
    /// - Registering and switching between multiple debug menus
    /// - Responsive IMGUI-based layout, with mobile safe area support
    /// - Viewing and scrolling system logs with color-coding
    /// - Real-time system configuration and platform metadata
    /// 
    /// It includes default menus for:
    /// <list type="bullet">
    /// <item><description><b>RideSystems</b> — active <c>IRideSystem</c> instances</description></item>
    /// <item><description><b>Config</b> — active configuration file and terrain key</description></item>
    /// <item><description><b>System/System2</b> — Unity, device, and graphics metadata</description></item>
    /// <item><description><b>iOS</b> — iOS-only hardware and API status (conditionally compiled)</description></item>
    /// </list>
    ///
    /// For internal layout utilities, see <see cref="IDebugMenu"/> and related <c>GUILayout</c> documentation:
    /// https://docs.unity3d.com/ScriptReference/GUILayout.html
    /// </summary>
    public class DebugMenu : RideSystemMonoBehaviour, IDebugMenu
    {
        const string OnGuiSystemsName = "RideSystems";
        const string OnGuiConfigName = "Config";
        const string OnGuiSystemName = "System";
        const string OnGuiSystem2Name = "System2";
        const string OnGuiIOSName = "iOS";

        [SerializeField] ApplicationLogMessageSystem m_applicationLogMessageSystem;
        [SerializeField] FramesPerSecondCounter m_framesPerSecondCounter;
        [SerializeField] bool m_showOnStartup = false;
        [SerializeField] bool m_useSafeArea = true;
        [SerializeField] float m_safeAreaFudge = 0;  // sometimes Screen.safeArea may not be 100% accurate, use a fudge factor to adjust if necessary.  in pixels

        [Header("Built-In Debug Menus")]

        [SerializeField] bool m_defaultMenuConfigOn = true;
        [SerializeField] bool m_defaultMenuRideSystemsOn = true;
        [SerializeField] bool m_defaultMenuSystemOn = true;
        [SerializeField] bool m_defaultMenuSystem2On = true;
        [SerializeField] bool m_defaultMenuIOSOn = true;

        public Action<int> OnSetMenu;

        bool m_debugMenuOn = false;

        List<(string name, Action callback)> m_debugMenus = new List<(string, Action)>();
        int m_debugMenuSelected = 0;

        Rect m_debugMenuSize = new Rect(0, 0, 0.4f, 1);  // unit coords (0..1), scaled to screen resolution
        Rect m_debugMenuSizeWide = new Rect(0, 0, 0.5f, 1);
        bool m_wideMode = false;

        Vector2 m_nativeSize = new Vector2(1920, 1080);

        int m_screenResolution = 0;
        FullScreenMode m_screenFullScreenMode;
        ScreenOrientation m_screenOrientation;

        bool m_showLog = false;
        List<string> m_logLines = new List<string>();
        string m_currentLogText = "";
        bool m_currentlogDirty = true;
        int m_maxLogLines = 20000;
        int m_maxLogCharacters = 16300;  // https://stackoverflow.com/questions/57915298/unity-editor-gui-text-size-limit
        Vector2 m_logScroll = new Vector2();
        bool m_logAutoScroll = true;
        bool m_logWrap = true;

        Vector2 m_rideSystemsScroll = new Vector2();

        bool m_terrainKeyCurrentlyChanging = false;
        string m_terrainKeyNew = "";

        string m_localIp;

        public GUIStyle GuiLabelStyle { get; protected set; }
        public GUIStyle GuiButtonStyle { get; protected set; }
        public GUIStyle GuiTextFieldStyle { get; protected set; }
        public GUIStyle GuiTextAreaStyle { get; protected set; }
        public GUIStyle GuiSliderStyle { get; protected set; }
        public GUIStyle GuiSliderThumbStyle { get; protected set; }
        public GUIStyle GuiToggleStyle { get; protected set; }
        public GUIStyle GuiBoxStyle { get; protected set; }
        public float SpaceHeight { get; protected set; }

        /// <inheritdoc />
        public override void SystemAwake()
        {
            base.SystemAwake();

            m_localIp = RideUtils.GetLocalIpAddress();

            // This property has a noticeable delay (as of 5.3.6f1) on the first time it's called.  Seems to be cached internally on subsequent calls. Pre-compute it in Awake() so that it's not visible.
            _ = SystemInfo.deviceUniqueIdentifier;

            m_screenResolution = RideUtils.FindCurrentResolutionIndex();
            m_screenFullScreenMode = Screen.fullScreenMode;
            m_screenOrientation = Screen.orientation;

            if (m_applicationLogMessageSystem != null)
                m_applicationLogMessageSystem.AddCallback(AddLogText);
            else
                AddLogText("Add a ApplicationLogMessageSystem to the DebugMenu.m_applicationLogMessageSystem inspector variable in order to see Console output here.", "", IApplicationLogMessageSystem.LogType.Warning);

            ShowMenu(m_showOnStartup);

            if (m_defaultMenuConfigOn)
                AddMenu(OnGuiConfigName, OnGUIConfig);
            if (m_defaultMenuRideSystemsOn)
                AddMenu(OnGuiSystemsName, OnGUIRideSystems);
            if (m_defaultMenuSystemOn)
                AddMenu(OnGuiSystemName, OnGUISystem);
            if (m_defaultMenuSystem2On)
                AddMenu(OnGuiSystem2Name, OnGUISystem2);
            if (m_defaultMenuIOSOn && RideUtils.IsIOS())
                AddMenu(OnGuiIOSName, OnGUIIOS);
        }

        /// <inheritdoc />
        void OnGUI()
        {
            if (!IsShowing())
                return;

            SetupGuiStylesIfNeeded();

            // https://forum.unity.com/threads/how-to-scale-my-gui-label-with-the-screensize.448997/
            int fontSize = (int)(22.0f * (Screen.height / m_nativeSize.y));
            float widgetHeight = 30 * (Screen.height / m_nativeSize.y);
            GuiLabelStyle.fontSize = fontSize;
            GuiButtonStyle.fontSize = fontSize;
            GuiTextFieldStyle.fontSize = fontSize;
            GuiTextAreaStyle.fontSize = fontSize;
            GuiSliderStyle.fontSize = fontSize;
            GuiSliderStyle.fixedHeight = widgetHeight;
            GuiSliderThumbStyle.fontSize = fontSize;
            GuiSliderThumbStyle.fixedHeight = widgetHeight;
            GuiToggleStyle.fontSize = fontSize;
            GuiBoxStyle.fontSize = fontSize;

            SpaceHeight = 15 * (Screen.height / m_nativeSize.y);

            Rect rectArea = IsWideMode() ? m_debugMenuSizeWide : m_debugMenuSize;
            using (new GUILayout.AreaScope(RideUtils.ScaleToRes(rectArea)))
            {
                using (new GUILayout.VerticalScope(GUI.skin.box))
                {
                    if (m_useSafeArea)
                        if (Screen.safeArea.y > 0)
                            GUILayout.Space(Math.Max(Screen.safeArea.y - 0, 0) + m_safeAreaFudge);

                    if (m_debugMenuSelected < m_debugMenus.Count)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            float buttonWidth1 = 0.05f * Screen.width;
                            float buttonWidth2 = 0.08f * Screen.width;
                            if (m_debugMenus.Count > 1) if (Button("<", buttonWidth1)) { PreviousMenu(); }
                            if (Button(m_debugMenus[m_debugMenuSelected].name)) { NextMenu(); }
                            if (m_debugMenus.Count > 1) if (Button(">", buttonWidth1)) { NextMenu(); }
                            if (Button(IsWideMode() ? "><" : "<>", buttonWidth2)) { ToggleWideMode(); }
                            if (Button(m_showLog ? "<<" : ">>", buttonWidth2)) { m_showLog = !m_showLog; }
                        }

                        Space();

                        // Draw the menu
                        m_debugMenus[m_debugMenuSelected].callback();
                    }
                }
            }

            if (m_showLog)
                DrawLog();
        }

        #region IDebugMenu

        /// <inheritdoc />
        public void ShowMenu(bool enable) => m_debugMenuOn = enable;

        /// <inheritdoc />
        public void ToggleMenu() => ShowMenu(!IsShowing());

        /// <inheritdoc />
        public bool IsShowing() => m_debugMenuOn;

        /// <inheritdoc />
        public void SetMenu(int menuIndex)
        {
            if (menuIndex >= 0 && menuIndex < m_debugMenus.Count)
            {
                m_debugMenuSelected = menuIndex;
                OnSetMenu?.Invoke(menuIndex);
            }
        }

        /// <inheritdoc />
        public void SetMenu(string menuName)
        {
            int index = m_debugMenus.FindIndex(m => m.name == menuName);
            if (index >= 0)
            {
                m_debugMenuSelected = index;
                OnSetMenu?.Invoke(index);
            }
        }

        /// <inheritdoc />
        public void NextMenu() => SetMenu(RideMath.IncrementWrap(m_debugMenuSelected, m_debugMenus.Count));

        /// <inheritdoc />
        public void PreviousMenu() => SetMenu(RideMath.DecrementWrap(m_debugMenuSelected, m_debugMenus.Count));

        /// <inheritdoc />
        public int GetCurrentMenu() => m_debugMenuSelected;

        /// <inheritdoc />
        public string GetCurrentMenuName() => GetMenuName(GetCurrentMenu());

        /// <inheritdoc />
        public int GetMenuCount() => m_debugMenus.Count;

        /// <inheritdoc />
        public string GetMenuName(int menuIndex) => (menuIndex >= 0 && menuIndex < m_debugMenus.Count) ? m_debugMenus[menuIndex].name : null;

        /// <inheritdoc />
        public IReadOnlyList<string> GetMenuNames() => m_debugMenus.Select(m => m.name).ToList();

        /// <inheritdoc />
        public void AddMenu(string name, Action callback) => m_debugMenus.Add((name, callback));

        /// <inheritdoc />
        public void AddMenuToFront(string name, Action callback) => m_debugMenus.Insert(0, (name, callback));

        /// <inheritdoc />
        public void InsertMenu(int index, string name, Action callback) => m_debugMenus.Insert(Math.Min(index, m_debugMenus.Count), (name, callback));

        /// <inheritdoc />
        public void RemoveMenu(string name)
        {
            int index = m_debugMenus.FindIndex(m => m.name == name);
            if (index >= 0)
                m_debugMenus.RemoveAt(index);
        }

        /// <inheritdoc />
        public void SetMenuSize(float x, float y, float width, float height) => m_debugMenuSize = new Rect(x, y, width, height);

        /// <inheritdoc />
        public void SetWideMenuSize(float x, float y, float width, float height) => m_debugMenuSizeWide = new Rect(x, y, width, height);

        /// <inheritdoc />
        public void ToggleWideMode() => m_wideMode = !m_wideMode;

        /// <inheritdoc />
        public bool IsWideMode() => m_wideMode;

        /// <inheritdoc />
        public void SetUseSafeArea(bool useSafeArea) => m_useSafeArea = useSafeArea;

        /// <inheritdoc />
        public void SetSafeAreaFudgeFactor(float factor) => m_safeAreaFudge = factor;


        // OnGUI() helper functions

        /// <inheritdoc />
        public void Space() => GUILayout.Space(SpaceHeight);

        /// <inheritdoc />
        public void Space(int pixels) => GUILayout.Space(pixels);

        /// <inheritdoc />
        public void FlexibleSpace() => GUILayout.FlexibleSpace();

        /// <inheritdoc />
        public void Label(string text) => GUILayout.Label(text, GuiLabelStyle);

        /// <inheritdoc />
        public void Label(string text, float width) => GUILayout.Label(text, GuiLabelStyle, GUILayout.Width(width));

        /// <inheritdoc />
        public bool Button(string text) => GUILayout.Button(text, GuiButtonStyle);

        /// <inheritdoc />
        public bool Button(string text, float width) => GUILayout.Button(text, GuiButtonStyle, GUILayout.Width(width));

        /// <inheritdoc />
        public bool Toggle(bool value, string text) => GUILayout.Toggle(value, text, GuiToggleStyle);

        /// <inheritdoc />
        public string TextField(string text) => GUILayout.TextField(text, GuiTextFieldStyle);

        /// <inheritdoc />
        public string TextField(string text, float width) => GUILayout.TextField(text, GuiTextFieldStyle, GUILayout.Width(width));

        /// <inheritdoc />
        public string TextArea(string text) => GUILayout.TextArea(text, GuiTextAreaStyle);

        /// <inheritdoc />
        public int SelectionGrid(int selection, string[] options, int xCount) => GUILayout.SelectionGrid(selection, options, xCount, GuiButtonStyle);

        /// <inheritdoc />
        public int SelectionGrid(int selection, string[] options, int xCount, float width) => GUILayout.SelectionGrid(selection, options, xCount, GuiButtonStyle, GUILayout.Width(width));

        /// <inheritdoc />
        public float HorizontalSlider(float value, float leftValue, float rightValue) => GUILayout.HorizontalSlider(value, leftValue, rightValue, GuiSliderStyle, GuiSliderThumbStyle);

        /// <inheritdoc />
        public IDisposable Horizontal() => new GUILayout.HorizontalScope();

        /// <inheritdoc />
        public IDisposable Vertical() => new GUILayout.VerticalScope();

        #endregion

        /// <summary>
        /// Initiates a check to verify the terrain key capability with the server.
        /// This method wraps the coroutine <see cref="TerrainKeyCheckServerInternal"/> and is triggered from UI buttons.
        /// </summary>
        public void TerrainKeyCheckServerButton()
        {
            StartCoroutine(TerrainKeyCheckServerInternal());
        }

        /// <summary>
        /// Initializes all GUI styles if they haven't been set yet.
        /// This ensures consistent layout and scaling across platforms and resolutions,
        /// and allows the debug menu to adapt to runtime resolution and platform UI skins.
        /// </summary>
        void SetupGuiStylesIfNeeded()
        {
            if (GuiLabelStyle == null)
            {
                GuiLabelStyle = new GUIStyle(GUI.skin.label);
                GuiLabelStyle.padding = new RectOffset(0, 0, 0, 0);
                GuiLabelStyle.alignment = TextAnchor.MiddleLeft;
            }

            if (GuiButtonStyle == null)
                GuiButtonStyle = new GUIStyle(GUI.skin.button);

            if (GuiTextFieldStyle == null)
                GuiTextFieldStyle = new GUIStyle(GUI.skin.textField);

            if (GuiTextAreaStyle == null)
                GuiTextAreaStyle = new GUIStyle(GUI.skin.textArea);

            if (GuiSliderStyle == null)
                GuiSliderStyle = new GUIStyle(GUI.skin.horizontalSlider);

            if (GuiSliderThumbStyle == null)
                GuiSliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);

            if (GuiToggleStyle == null)
                GuiToggleStyle = new GUIStyle(GUI.skin.button);

            if (GuiBoxStyle == null)
                GuiBoxStyle = new GUIStyle(GUI.skin.box);
        }

        /// <summary>
        /// Adds a log entry to the debug menu’s internal log buffer, applying color-coding based on log severity.
        /// Maintains a fixed-size log window and trims excess characters to fit Unity’s IMGUI limits.
        /// </summary>
        /// <param name="logString">The log message.</param>
        /// <param name="stackTrace">The stack trace (optional, not displayed in this view).</param>
        /// <param name="type">The log type (e.g., Error, Warning, Info).</param>
        void AddLogText(string logString, string stackTrace, IApplicationLogMessageSystem.LogType type)
        {
            string msg = WrapColor(logString, type);
            m_logLines.Add(msg);

            while (m_logLines.Count > m_maxLogLines)
                m_logLines.RemoveAt(0);

            m_currentlogDirty = true;

            if (m_logAutoScroll)
                m_logScroll.y = float.MaxValue;
        }

        /// <summary>
        /// Coroutine that sends a capability check request to AWS to verify terrain key permissions.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        IEnumerator TerrainKeyCheckServerInternal()
        {
            string capability = "valid-key";

            yield return RideAWSUtils.CheckCapability(this, capability, (ret) =>
            {
                if (string.IsNullOrEmpty(ret))
                    RideLog.LogWarning($"GetCapabilities() - error getting capability: {capability}");
                else
                    RideLog.LogWarning($"GetCapabilities() - success");
            });
        }

        /// <summary>
        /// Renders the runtime log viewer on the right side of the screen.
        /// Includes scrolling, wrapping, and filtering controls for debug output added via <see cref="AddLogText"/>.
        /// </summary>
        void DrawLog()
        {
            Rect debugMenuSize = IsWideMode() ? m_debugMenuSizeWide : m_debugMenuSize;
            Rect rectArea = new Rect(debugMenuSize.width - debugMenuSize.x, debugMenuSize.y, 1 - debugMenuSize.width, debugMenuSize.height);
            using (new GUILayout.AreaScope(RideUtils.ScaleToRes(rectArea)))
            {
                if (m_useSafeArea)
                    if (Screen.safeArea.y > 0)
                        GUILayout.Space(Math.Max(Screen.safeArea.y - 0, 0) + m_safeAreaFudge);

                using (new GUILayout.HorizontalScope())
                {
#if false
                    if (GUILayout.Button("Log", GuiButtonStyle))
                        Debug.LogFormat("Testing {0}", "one");
                    if (GUILayout.Button("Log10", GuiButtonStyle))
                    {
                        for (int i = 0; i < 10; i++)
                            Debug.LogFormat("Testing {0}", i);
                    }
                    if (GUILayout.Button("Log100", GuiButtonStyle))
                    {
                        for (int i = 0; i < 100; i++)
                            Debug.LogFormat("Testing a bunch of Log Messages - {0}", i + 1);
                    }
                    if (GUILayout.Button("LogLong", GuiButtonStyle))
                        Debug.LogError("124jh23k123l4h51kl3j5h1kl51 1jkl51lk4j5 h1lkj45h 1lk435h 1lk35h1lkj5h1lkj5h1lk5j 1klj45 1kl 51lkj54h 1l4k51klj45h1lkj54h1 l4k5 1kl4j5 1lk4j5 1lk45j 1l4kj51l k5 1lk4j5 1l45j1lkj51kl54jh1lkj51lk50998f890fa-0sd9fs0f90sdf 0s f0s df0sdf s90f8 0s9d8f s09df s09f s09 dfs90d fs09f8 09wer2 309523 l2k4j23l4 23l4j 2l34 2l3j4 2l4 2l34j 2l34 2l34l 2l4 2l");
                    if (GUILayout.Button("Warning", GuiButtonStyle))
                        Debug.LogWarningFormat("Testing {0}", "one");
                    if (GUILayout.Button("Error", GuiButtonStyle))
                        Debug.LogErrorFormat("Testing {0}", "one");
                    if (GUILayout.Button("Clear", GuiButtonStyle))
                        { m_logText.Clear(); m_currentLogText = ""; }
#endif

                    if (Button("Top"))
                        m_logScroll = Vector2.zero;

                    if (Button("Bottom"))
                        m_logScroll = new Vector2(0, float.MaxValue);

                    m_logWrap = Toggle(m_logWrap, "Wrap");
                    m_logAutoScroll = Toggle(m_logAutoScroll, "AutoScroll");
                }

                using (var scrollViewScope = new GUILayout.ScrollViewScope(m_logScroll))
                {
                    m_logScroll = scrollViewScope.scrollPosition;

                    // https://answers.unity.com/questions/360885/color-tags-in-rich-text-ongui.html?_ga=2.209355218.1783042878.1604897167-783155896.1580859010
                    GUIStyle textStyle = new GUIStyle(GuiTextAreaStyle);
                    textStyle.normal.textColor = Color.white;
                    textStyle.wordWrap = m_logWrap;
                    textStyle.richText = true;

                    string text = GetCurrentLogText();
                    GUILayout.TextArea(text, textStyle);
                }
            }
        }

        string GetCurrentLogText()
        {
            if (m_currentlogDirty)
            {
                m_currentLogText = string.Join("\n", m_logLines);

                // limit the amount of characters shown
                // https://stackoverflow.com/questions/57915298/unity-editor-gui-text-size-limit
                if (m_currentLogText.Length > m_maxLogCharacters)
                    m_currentLogText = m_currentLogText.Substring(m_currentLogText.Length - m_maxLogCharacters);

                m_currentlogDirty = false;
            }

            return m_currentLogText;
        }

        /// <summary>
        /// Draws a list of all registered RIDE systems implementing <c>IRideSystem</c>.
        /// Useful for introspection and verifying which systems are active at runtime.
        /// </summary>
        void OnGUIRideSystems()
        {
            using (var scrollViewScope = new GUILayout.ScrollViewScope(m_rideSystemsScroll))
            {
                m_rideSystemsScroll = scrollViewScope.scrollPosition;

                Label("<b>RIDE Systems:</b>");
                foreach (var system in Systems.Access.GetAddedSystems<IRideSystem>())
                    Label($"{system.GetType().ToString().Replace("Ride.", "")}");
            }
        }

        /// <summary>
        /// Draws the configuration panel, showing the current config file path, version, and terrain key.
        /// Supports resetting the configuration, opening the config folder, editing the config file,
        /// and verifying the terrain key format and server connectivity.
        /// </summary>
        void OnGUIConfig()
        {
            var config = Systems.Get<ConfigurationSystemUnity>();
            if (config == null)
                return;

            Label($"{config.path}");

            if (config.IsCorrectVersion())
            {
                Label($"Version: {config.config.version}");
            }
            else
            {
                Label($"<color=red>Config File Incorrect Version!</color>");
                Label($"Found: {config.config.version}");
                Label($"Expected: {RideConfig.Default.version}");
            }

            if (Button("Reset to Defaults"))
            {
                config.ResetConfig();
                config.Save();
            }

            if (RideUtils.IsWindows() || RideUtils.IsOSX())
            {
                if (Button("Open Folder Location"))
                {
                    if (RideUtils.IsWindows())
                        System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.GetDirectoryName(config.path));
                    else if (RideUtils.IsOSX())
                        System.Diagnostics.Process.Start("/usr/bin/open", $@"""{System.IO.Path.GetDirectoryName(config.path)}""");
                }

                if (Button("Edit Config"))
                {
                    if (RideUtils.IsWindows())
                        System.Diagnostics.Process.Start("notepad.exe", config.path);
                    else if (RideUtils.IsOSX())
                        System.Diagnostics.Process.Start("qlmanage", $@"-p ""{config.path}""");
                }
            }

            if (config.IsCorrectVersion())
            {
                Space();

                if (m_terrainKeyCurrentlyChanging)
                {
                    m_terrainKeyNew = TextField(m_terrainKeyNew);

                    using (new GUILayout.HorizontalScope())
                    {
                        if (Button("Back"))
                            m_terrainKeyCurrentlyChanging = false;

                        if (Button("Set"))
                        {
                            config.SetTerrainKey(m_terrainKeyNew.Trim());
                            config.Save();
                            m_terrainKeyCurrentlyChanging = false;
                        }

                        if (ConfigurationSystemUnity.IsTerrainKeyFormatValid(m_terrainKeyNew))
                            Label("<color=green>Key Valid</color>");
                        else
                            Label("<color=red>Key Invalid</color>");
                    }
                }
                else
                {
                    Label($"OWT Access Key: {config.GetTerrainKey()}");
                    Label($"AWS Region: {config.GetTerrainKeyRegion()}");

                    using (new GUILayout.HorizontalScope())
                    {
                        if (Button("Change"))
                        {
                            m_terrainKeyNew = config.GetTerrainKey();
                            m_terrainKeyCurrentlyChanging = true;
                        }

                        if (Button("Check Server"))
                            TerrainKeyCheckServerButton();
                    }
                }
            }
        }

        /// <summary>
        /// Displays runtime Unity system settings and environment state:
        /// screen resolution, fullscreen mode, screen orientation, frame timing, quality settings, and rendering info.
        /// Also allows modifying screen resolution, frame rate, and vSync via UI controls.
        /// </summary>
        void OnGUISystem()
        {
            Label($"T: {Time.time:f2} F: {Time.frameCount}");
            Label($"AVG: {m_framesPerSecondCounter.AverageFps:f0} FPS: {m_framesPerSecondCounter.Fps:f2} ({m_framesPerSecondCounter.MinFps:f0}/{m_framesPerSecondCounter.MaxFps:f0})");
#if UNITY_2022_2_OR_NEWER
            Label($"{Screen.width}x{Screen.height}x{Screen.currentResolution.refreshRateRatio.value:f0} ({RideUtils.GetCommonAspectText((float)Screen.width / Screen.height)}) {Screen.dpi:f0}dpi");
#else
            Label(string.Format("{0}x{1}x{2} ({3}) {4:f0}dpi", Screen.width, Screen.height, Screen.currentResolution.refreshRate, VHUtils.GetCommonAspectText((float)Screen.width / Screen.height), Screen.dpi));
#endif
            Label($"{Screen.fullScreenMode} - {Screen.orientation}");
            using (new GUILayout.HorizontalScope())
            {
                if (Screen.resolutions.Length > 0)
                {
                    if (Button("<", 25)) { m_screenResolution = RideMath.DecrementWrap(m_screenResolution, Screen.resolutions.Length); }
                    if (Button($"{Screen.resolutions[m_screenResolution]}")) { m_screenResolution = RideMath.IncrementWrap(m_screenResolution, Screen.resolutions.Length); }
                    if (Button(">", 25)) { m_screenResolution = RideMath.IncrementWrap(m_screenResolution, Screen.resolutions.Length); }
#if UNITY_2022_2_OR_NEWER
                    if (Button("Set", 60)) { Screen.SetResolution(Screen.resolutions[m_screenResolution].width, Screen.resolutions[m_screenResolution].height, Screen.fullScreenMode, Screen.resolutions[m_screenResolution].refreshRateRatio); }
#else
                    if (Button("Set", 60)) { Screen.SetResolution(Screen.resolutions[m_screenResolution].width, Screen.resolutions[m_screenResolution].height, Screen.fullScreenMode, Screen.resolutions[m_screenResolution].refreshRate); }
#endif
                }
                else
                {
                    Label($"Screen.resolutions.Length: {Screen.resolutions.Length}");
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (Button("<", 25)) { m_screenFullScreenMode = Enum.IsDefined(typeof(FullScreenMode), m_screenFullScreenMode - 1) ? m_screenFullScreenMode - 1 : Enum.GetValues(typeof(FullScreenMode)).Cast<FullScreenMode>().Max(); }
                if (Button($"{m_screenFullScreenMode}")) { m_screenFullScreenMode = Enum.IsDefined(typeof(FullScreenMode), m_screenFullScreenMode + 1) ? m_screenFullScreenMode + 1 : Enum.GetValues(typeof(FullScreenMode)).Cast<FullScreenMode>().Min(); }
                if (Button(">", 25)) { m_screenFullScreenMode = Enum.IsDefined(typeof(FullScreenMode), m_screenFullScreenMode + 1) ? m_screenFullScreenMode + 1 : Enum.GetValues(typeof(FullScreenMode)).Cast<FullScreenMode>().Min(); }
                if (Button("Set", 60)) { Screen.fullScreenMode = m_screenFullScreenMode; }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (Button("<", 25)) { m_screenOrientation = Enum.IsDefined(typeof(ScreenOrientation), m_screenOrientation - 1) ? m_screenOrientation - 1 : Enum.GetValues(typeof(ScreenOrientation)).Cast<ScreenOrientation>().Max(); }
                if (Button($"{m_screenOrientation}")) { m_screenOrientation = Enum.IsDefined(typeof(ScreenOrientation), m_screenOrientation + 1) ? m_screenOrientation + 1 : Enum.GetValues(typeof(ScreenOrientation)).Cast<ScreenOrientation>().Min(); }
                if (Button(">", 25)) { m_screenOrientation = Enum.IsDefined(typeof(ScreenOrientation), m_screenOrientation + 1) ? m_screenOrientation + 1 : Enum.GetValues(typeof(ScreenOrientation)).Cast<ScreenOrientation>().Min(); }
                if (Button("Set", 60)) { Screen.orientation = m_screenOrientation; }
            }

            Label($"TargetFR: {Application.targetFrameRate}    vSync: {QualitySettings.vSyncCount}");

            using (new GUILayout.HorizontalScope())
            {
                if (Button("-1")) { Application.targetFrameRate = -1; }
                if (Button("15")) { Application.targetFrameRate = 15; }
                if (Button("30")) { Application.targetFrameRate = 30; }
                if (Button("60")) { Application.targetFrameRate = 60; }
                if (Button("90")) { Application.targetFrameRate = 90; }
            }

            using (var scope = new GUILayout.HorizontalScope())
            {
                if (Button("0")) { QualitySettings.vSyncCount = 0; }
                if (Button("1")) { QualitySettings.vSyncCount = 1; }
                if (Button("2")) { QualitySettings.vSyncCount = 2; }
                if (Button("3")) { QualitySettings.vSyncCount = 3; }
                if (Button("4")) { QualitySettings.vSyncCount = 4; }
            }

            Label($"{RideUtils.SceneManagerActiveSceneName()}");
            using (new GUILayout.HorizontalScope())
            {
                Label("Quality:", 100);
                if (Button($"{QualitySettings.names[QualitySettings.GetQualityLevel()]}"))
                    QualitySettings.SetQualityLevel((QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length, true);
            }
            Label($"ColorSpace: {QualitySettings.activeColorSpace}");

            Camera camera = Camera.main;
            Label($"RenderPath: {camera.actualRenderingPath}");
            Label($"Unity: {Application.unityVersion}");
            Label($"Platform: {Application.platform}");

            Label($"App.streamingAssetsPath - '{Application.streamingAssetsPath}'");
            Label($"Dir.GetCurrentDirectory() - '{System.IO.Directory.GetCurrentDirectory()}'");
            Label($"App.dataPath - '{Application.dataPath}'");
            Label($"App.persistantDataPath - '{Application.persistentDataPath}'");
            Label($"Path.GetFullPath('.') - '{System.IO.Path.GetFullPath(".")}'");
        }

        /// <summary>
        /// Displays lower-level system information such as CPU, GPU, memory, device ID, and platform-specific paths.
        /// Also includes Unity memory profiling data like Mono heap and reserved memory.
        /// </summary>
        void OnGUISystem2()
        {
            Label($"{SystemInfo.operatingSystem}");
            Label($"{SystemInfo.processorCount} x {SystemInfo.processorType}");
            Label($"Mem: {SystemInfo.systemMemorySize / 1000.0f:f1}gb");
            Label($"{SystemInfo.graphicsDeviceName} - deviceID: {SystemInfo.graphicsDeviceID}");
            Label($"{SystemInfo.graphicsDeviceVendor} - vendorID: {SystemInfo.graphicsDeviceVendorID}");
            Label($"{SystemInfo.graphicsDeviceVersion}");
            Label($"VMem: {SystemInfo.graphicsMemorySize}mb");
            Label($"Shader Level: {SystemInfo.graphicsShaderLevel / 10.0f:f1}");
            Label($"deviceUniqueIdentifier: {SystemInfo.deviceUniqueIdentifier}");
            Label($"deviceName: {SystemInfo.deviceName}");
            Label($"deviceModel: {SystemInfo.deviceModel}");
            Label($"deviceType: {SystemInfo.deviceType}");
            Label($"UserName: {Environment.UserName}");
            Label($"IP: {m_localIp}");

            Label($"MonoHeap: {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong():N0}");
            Label($"TempAllocator: {UnityEngine.Profiling.Profiler.GetTempAllocatorSize():N0}");
            Label($"AllocatedMemory: {UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong():N0}");
            Label($"ReservedMemory: {UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong():N0}");
            Label($"UnusedReservedMemory: {UnityEngine.Profiling.Profiler.GetTotalUnusedReservedMemoryLong():N0}");
        }

        /// <summary>
        /// Displays iOS-specific device information such as identifiers, tracking flags, system version, and dimming behavior.
        /// Only active when running on iOS builds.
        /// </summary>
        void OnGUIIOS()
        {
            if (RideUtils.IsIOS())
            {
                Label("iOS specific info");
#if UNITY_IOS
                Label($"iOSDeviceGen: {UnityEngine.iOS.Device.generation}");  // The generation of the device.
                if (!RideUtils.IsEditor()) Label($"systemVersion: {UnityEngine.iOS.Device.systemVersion}");  // Causes 'Assertion failed on expression' error when run in editor on Windows  // iOS version.
                Label($"advertisingIdentifier: {UnityEngine.iOS.Device.advertisingIdentifier}");  // Advertising ID.
                Label($"advertisingTrackingEnabled: {UnityEngine.iOS.Device.advertisingTrackingEnabled}");  // Is advertising tracking enabled.
                Label($"vendorIdentifier: {UnityEngine.iOS.Device.vendorIdentifier}");  // Vendor ID.
                Label($"iosAppOnMac: {UnityEngine.iOS.Device.iosAppOnMac}");  // Specifies whether app built for iOS is running on Mac.
                Label($"lowPowerModeEnabled: {UnityEngine.iOS.Device.lowPowerModeEnabled}");  // Indicates whether Low Power Mode is enabled on the device.
                Label($"wantsSoftwareDimming: {UnityEngine.iOS.Device.wantsSoftwareDimming}");  // Indicates whether the screen may be dimmed lower than the hardware is normally capable of by emulating it in software.
                Label($"deferSystemGesturesMode: {UnityEngine.iOS.Device.deferSystemGesturesMode}");  // Defer system gestures until the second swipe on specific edges.
                Label($"hideHomeButton: {UnityEngine.iOS.Device.hideHomeButton}");  // Specifies whether the home button should be hidden in the iOS build of this application.
#endif
            }
        }

        /// <summary>
        /// Wraps a log message in Unity rich text color tags based on log type.
        /// Errors are red, warnings are yellow, and all other messages are unstyled.
        /// </summary>
        /// <param name="text">The original message text.</param>
        /// <param name="type">The log type (Error, Warning, Info).</param>
        /// <returns>The formatted message with rich text color tags.</returns>
        static string WrapColor(string text, IApplicationLogMessageSystem.LogType type)
        {
            if (type == IApplicationLogMessageSystem.LogType.Error) return $"<color=red>{text}</color>";
            if (type == IApplicationLogMessageSystem.LogType.Warning) return $"<color=yellow>{text}</color>";
            return text;
        }
    }
}
