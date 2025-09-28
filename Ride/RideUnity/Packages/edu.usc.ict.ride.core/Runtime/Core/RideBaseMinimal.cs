using System;
using UnityEngine;
using Ride;
using Ride.IO;
using Ride.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Ride.Examples
{
    /// <summary>
    /// Base class for all RIDE examples. Provides helper functionality, GUI, etc
    /// </summary>
    public class RideBaseMinimal : RideMonoBehaviour
    {
        [Tooltip("Tick if the simulation can be paused")]
        public bool m_simulationIsPausable = true;

        [Tooltip("The keyboard key that shows/hides the debug menu")]
        public RideKeyCode m_toggleDebugMenu = RideKeyCode.F11;

        protected DebugMenu m_debugMenu;

        [SerializeField] GameObject m_exitPromptMenuPrefab;
        IExitPromptMenu m_exitPromptMenu;
        bool m_wasDebugMenuActiveOnExitPrompt;

        protected override void Start()
        {
            base.Start();

            if (m_exitPromptMenuPrefab != null)
                m_exitPromptMenu = m_exitPromptMenuPrefab.GetComponent<IExitPromptMenu>();
            if (m_exitPromptMenu == null)
            {
                var coreUISystem = Globals.api.GetAddedSystemOfType<ICoreUISystem>();
                if (coreUISystem != null)
                    m_exitPromptMenu = coreUISystem.CreateExitPromptMenu();
            }

            if (m_exitPromptMenu != null)
            {
                m_exitPromptMenu.onOpenPrompt += ExitPrompt_onOpenPrompt;
                m_exitPromptMenu.onClosePrompt += ExitPrompt_onClosePrompt;
                m_exitPromptMenu.SetPausable(m_simulationIsPausable);
            }

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();

            var config = Globals.api.GetSystem<ConfigurationSystemUnity>();
            if (config != null)
            {
                if (!config.IsCorrectVersion())
                    RideLog.LogWarning("Warning: RIDE config file is out of date. Reset in Debug menu or LevelSelect scene.");
            }
        }

        protected override void Update()
        {
            base.Update();
#if ENABLE_INPUT_SYSTEM && (!ENABLE_LEGACY_INPUT_MANAGER || PREFER_NEW_INPUT)
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame)
                    ActivateExitMenu();
                if (kb.f11Key.wasPressedThisFrame)
                    ToggleDebugMenu();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Globals.api.inputSystem.GetKeyDown(RideKeyCode.Escape, RideInputLayer.System))
                ActivateExitMenu();
            if (Globals.api.inputSystem.GetKeyDown(m_toggleDebugMenu, RideInputLayer.System))
                ToggleDebugMenu();
#endif
        }

        protected void SetExitMenuSceneToLoad(string sceneToLoad)
        {
            m_exitPromptMenu.SetLevelToLoad(sceneToLoad);
        }

        protected void SetExitMenuCloseCallback(Action<object, EventArgs> callback)
        {
            m_exitPromptMenu.onClosePrompt += new EventHandler(callback);
        }

        protected void ActivateExitMenu()
        {
            m_exitPromptMenu.OpenPrompt();
        }

        protected void CloseExitMenu()
        {
            m_exitPromptMenu.OnNo();
        }

        protected bool IsExitMenuActivated()
        {
            return m_exitPromptMenu.IsPromptOpen();
        }

        protected void ToggleDebugMenu()
        {
            if (m_debugMenu != null)
                m_debugMenu.ToggleMenu();
        }

        protected void ShowDebugMenu(bool show)
        {
            if (m_debugMenu != null)
                m_debugMenu.ShowMenu(show);
        }

        protected bool IsDebugMenuShowing()
        {
            return m_debugMenu != null ? m_debugMenu.IsShowing() : false;
        }

        protected void AddDebugMenu(string menuName, Action cb)
        {
            if (m_debugMenu != null)
                m_debugMenu.AddMenu(menuName, cb);
        }

        protected void SetDebugMenu(int menu)
        {
            if (m_debugMenu != null)
                m_debugMenu.SetMenu(menu);
        }

        protected int GetDebugMenuCount()
        {
            return m_debugMenu ? m_debugMenu.GetMenuCount() : -1;
        }

        protected void NextMenu()
        {
            m_debugMenu.NextMenu();
        }

        protected void BeginHorizontal()
        {
            GUILayout.BeginHorizontal();
        }

        protected void EndHorizontal()
        {
            GUILayout.EndHorizontal();
        }

        protected RideVector2 BeginScrollView(RideVector2 scroll, float height)
        {
            return GUILayout.BeginScrollView(scroll, GUILayout.Height(height));
        }

        protected void EndScrollView()
        {
            GUILayout.EndScrollView();
        }

        public void DrawGUISpace()
        {
            GUILayout.Space(m_debugMenu.SpaceHeight);
        }

        protected void DrawGUILabel(string text)
        {
            GUILayout.Label(text, m_debugMenu.GuiLabelStyle);
        }

        protected void DrawGUILabel(string text, float width)
        {
            GUILayout.Label(text, m_debugMenu.GuiLabelStyle, GUILayout.Width(width));
        }

        protected bool DrawGUIButton(string text)
        {
            return GUILayout.Button(string.Format(text), m_debugMenu.GuiButtonStyle);
        }

        protected bool DrawGUIButton(string text, float width)
        {
            return GUILayout.Button(string.Format(text), m_debugMenu.GuiButtonStyle, GUILayout.Width(width));
        }

        protected bool DrawGUIToggle(bool value, string text)
        {
            return GUILayout.Toggle(value, text, m_debugMenu.GuiToggleStyle);
        }

        protected string DrawGUITextField(string text)
        {
            return GUILayout.TextField(text, m_debugMenu.GuiTextFieldStyle);
        }

        protected string DrawGUITextField(string text, float width)
        {
            return GUILayout.TextField(text, m_debugMenu.GuiTextFieldStyle, GUILayout.Width(width));
        }

        protected string DrawGUITextField(string text, float width, float height)
        {
            return GUILayout.TextField(text, m_debugMenu.GuiTextFieldStyle, GUILayout.Width(width), GUILayout.Height(height));
        }

        protected string DrawGUITextArea(string text)
        {
            return GUILayout.TextArea(text, m_debugMenu.GuiTextAreaStyle);
        }

        protected int DrawGUISelectionGrid(int selection, string[] options, int xCount)
        {
            return GUILayout.SelectionGrid(selection, options, xCount, m_debugMenu.GuiButtonStyle);
        }

        protected int DrawGUISelectionGrid(int selection, string[] options, int xCount, float width)
        {
            return GUILayout.SelectionGrid(selection, options, xCount, m_debugMenu.GuiButtonStyle, GUILayout.Width(width));
        }

        protected float DrawGUIHorizontalSlider(float value, float leftValue, float rightValue)
        {
            return GUILayout.HorizontalSlider(value, leftValue, rightValue, m_debugMenu.GuiSliderStyle, m_debugMenu.GuiSliderThumbStyle);
        }

        protected void DrawSpace()
        {
            DrawSpace(m_debugMenu.SpaceHeight);
        }

        protected void DrawSpace(float pixels)
        {
            GUILayout.Space(pixels);
        }

        void ExitPrompt_onClosePrompt(object sender, EventArgs e)
        {
            if (m_debugMenu != null)
                m_debugMenu.ShowMenu(m_wasDebugMenuActiveOnExitPrompt);
        }

        private void ExitPrompt_onOpenPrompt(object sender, EventArgs e)
        {
            if (m_debugMenu != null)
            {
                m_wasDebugMenuActiveOnExitPrompt = m_debugMenu.IsShowing();
                m_debugMenu.ShowMenu(false);
            }
        }
    }
}
