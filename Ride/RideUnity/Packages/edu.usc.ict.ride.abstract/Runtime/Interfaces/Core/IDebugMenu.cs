using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Provides a unified interface for managing and rendering runtime debug menus using Unity's IMGUI system.
    /// This interface supports layout helpers, multiple menu views, and system configuration tools,
    /// and is commonly used for in-game diagnostics, testing, or live debugging on desktop and mobile platforms.
    ///
    /// See <see cref="DebugMenu"/> for the default implementation.
    ///
    /// For Unity IMGUI basics, see:
    /// https://docs.unity3d.com/Manual/GUIScriptingGuide.html
    /// </summary>
    public interface IDebugMenu : IRideSystem
    {
        /// <summary>
        /// Controls the visibility of the debug menu.
        /// </summary>
        /// <param name="enable">Whether the menu should be shown.</param>
        void ShowMenu(bool enable);

        /// <summary>
        /// Toggles the visibility of the debug menu.
        /// </summary>
        void ToggleMenu();

        /// <summary>
        /// Checks whether the debug menu is currently visible.
        /// </summary>
        /// <returns>True if the debug menu is showing, false otherwise.</returns>
        bool IsShowing();

        /// <summary>
        /// Sets the current menu by index.
        /// </summary>
        /// <param name="menuIndex">Index of the menu to activate.</param>
        void SetMenu(int menuIndex);

        /// <summary>
        /// Sets the current menu by name.
        /// </summary>
        /// <param name="menuName">Name of the menu to activate.</param>
        void SetMenu(string menuName);

        /// <summary>
        /// Cycles to the next available menu.
        /// </summary>
        void NextMenu();

        /// <summary>
        /// Cycles to the previous menu.
        /// </summary>
        void PreviousMenu();

        /// <summary>
        /// Returns the index of the currently active menu.
        /// </summary>
        /// <returns>The current menu index.</returns>
        int GetCurrentMenu();

        /// <summary>
        /// Returns the name of the currently active debug menu.
        /// </summary>
        /// <returns>The name of the current menu, or null if no menu is active.</returns>
        string GetCurrentMenuName();

        /// <summary>
        /// Returns the total number of menus registered.
        /// </summary>
        /// <returns>The number of menus.</returns>
        int GetMenuCount();

        /// <summary>
        /// Gets the name of a menu by index.
        /// </summary>
        /// <param name="menuIndex">Index of the menu.</param>
        /// <returns>The name of the menu.</returns>
        string GetMenuName(int menuIndex);

        /// <summary>
        /// Gets the list of all menu names.
        /// </summary>
        /// <returns>A read-only list of menu names.</returns>
        IReadOnlyList<string> GetMenuNames();

        /// <summary>
        /// Adds a debug menu to the end of the list.
        /// </summary>
        /// <param name="name">Menu name.</param>
        /// <param name="callback">Callback to invoke when the menu is rendered.</param>
        void AddMenu(string name, Action callback);

        /// <summary>
        /// Adds a debug menu to the front of the list.
        /// </summary>
        /// <param name="name">Menu name.</param>
        /// <param name="callback">Callback to invoke when the menu is rendered.</param>
        void AddMenuToFront(string name, Action callback);

        /// <summary>
        /// Inserts a debug menu at a specific index.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="name">Menu name.</param>
        /// <param name="callback">Callback to invoke when the menu is rendered.</param>
        void InsertMenu(int index, string name, Action callback);

        /// <summary>
        /// Removes a debug menu entry.
        /// </summary>
        /// <param name="name">Menu name.</param>
        /// <param name="callback">Callback associated with the menu to remove.</param>
        void RemoveMenu(string name);

        /// <summary>
        /// Sets the position and size of the debug menu.
        /// </summary>
        /// <param name="x">X coordinate (normalized 0-1).</param>
        /// <param name="y">Y coordinate (normalized 0-1).</param>
        /// <param name="width">Width (normalized 0-1).</param>
        /// <param name="height">Height (normalized 0-1).</param>
        void SetMenuSize(float x, float y, float width, float height);

        /// <summary>
        /// Sets the position and size of the debug menu when wide mode is enabled.
        /// </summary>
        /// <param name="x">X coordinate (normalized 0-1).</param>
        /// <param name="y">Y coordinate (normalized 0-1).</param>
        /// <param name="width">Width (normalized 0-1).</param>
        /// <param name="height">Height (normalized 0-1).</param>
        void SetWideMenuSize(float x, float y, float width, float height);

        /// <summary>
        /// Toggles wide mode, allowing the menu to occupy more horizontal space.
        /// </summary>
        void ToggleWideMode();

        /// <summary>
        /// Checks whether the debug menu is currently in wide mode.
        /// </summary>
        /// <returns>True if wide mode is active.</returns>
        bool IsWideMode();

        /// <summary>
        /// Enables or disables using Unity's screen safe area to avoid notches.
        /// </summary>
        /// <param name="useSafeArea">True to enable the safe area.</param>
        void SetUseSafeArea(bool useSafeArea);

        /// <summary>
        /// Adjusts the safe area boundary with a pixel-based fudge factor.
        /// </summary>
        /// <param name="factor">Fudge factor in pixels.</param>
        void SetSafeAreaFudgeFactor(float factor);

        #region OnGUI() helper functions

        /// <summary>
        /// Adds a standard space between UI elements.
        /// </summary>
        void Space();

        /// <summary>
        /// Adds a fixed-size vertical space between UI elements.
        /// </summary>
        /// <param name="pixels">The height of the space in pixels.</param>
        void Space(int pixels);

        /// <summary>
        /// Adds a flexible vertical space that expands to fill available room.
        /// </summary>
        void FlexibleSpace();

        /// <summary>
        /// Displays a text label in the debug menu.
        /// </summary>
        /// <param name="text">The text to display.</param>
        void Label(string text);

        /// <summary>
        /// Displays a fixed-width text label in the debug menu.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="width">The fixed width of the label.</param>
        void Label(string text, float width);

        /// <summary>
        /// Renders a clickable button.
        /// </summary>
        /// <param name="text">Button label.</param>
        /// <returns>True if clicked.</returns>
        bool Button(string text);

        /// <summary>
        /// Renders a fixed-width clickable button.
        /// </summary>
        /// <param name="text">Button label.</param>
        /// <param name="width">Fixed width of the button.</param>
        /// <returns>True if clicked.</returns>
        bool Button(string text, float width);

        /// <summary>
        /// Renders a toggle control (checkbox).
        /// </summary>
        /// <param name="value">Current toggle value.</param>
        /// <param name="text">Label next to the toggle.</param>
        /// <returns>Updated toggle value.</returns>
        bool Toggle(bool value, string text);

        /// <summary>
        /// Renders a single-line text input field.
        /// </summary>
        /// <param name="text">The current text.</param>
        /// <returns>The modified text.</returns>
        string TextField(string text);

        /// <summary>
        /// Renders a fixed-width single-line text input field.
        /// </summary>
        /// <param name="text">The current text.</param>
        /// <param name="width">The fixed width of the input field.</param>
        /// <returns>The modified text.</returns>
        string TextField(string text, float width);

        /// <summary>
        /// Renders a multi-line text input field.
        /// </summary>
        /// <param name="text">The current text.</param>
        /// <returns>The modified text.</returns>
        string TextArea(string text);

        /// <summary>
        /// Renders a grid of selectable options.
        /// </summary>
        /// <param name="selection">Current selection index.</param>
        /// <param name="options">Available options.</param>
        /// <param name="xCount">Number of columns in the grid.</param>
        /// <returns>Selected option index.</returns>
        int SelectionGrid(int selection, string[] options, int xCount);

        /// <summary>
        /// Renders a fixed-width grid of selectable options.
        /// </summary>
        /// <param name="selection">Current selection index.</param>
        /// <param name="options">Available options.</param>
        /// <param name="xCount">Number of columns in the grid.</param>
        /// <param name="width">Fixed width of the entire grid.</param>
        /// <returns>Selected option index.</returns>
        int SelectionGrid(int selection, string[] options, int xCount, float width);

        /// <summary>
        /// Renders a horizontal slider control.
        /// </summary>
        /// <param name="value">Current value.</param>
        /// <param name="leftValue">Minimum value.</param>
        /// <param name="rightValue">Maximum value.</param>
        /// <returns>Updated value.</returns>
        float HorizontalSlider(float value, float leftValue, float rightValue);

        /// <summary>
        /// Begins a horizontal layout block. Must be used with a using statement.
        /// </summary>
        /// <returns>A disposable scope for layout management.</returns>
        IDisposable Horizontal();

        /// <summary>
        /// Begins a vertical layout block. Must be used with a using statement.
        /// </summary>
        /// <returns>A disposable scope for layout management.</returns>
        IDisposable Vertical();

        #endregion
    }
}
