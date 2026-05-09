using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.UI
{
    /// <summary>
    /// Represents the UI prompt that allows the user to confirm or cancel exiting the current scenario.
    /// </summary>
    public interface IExitPromptMenu
    {
        /// <summary>Occurs when the exit prompt is opened and becomes visible to the user.</summary>
        event EventHandler onOpenPrompt;

        /// <summary>Occurs when the exit prompt is closed after being visible to the user.</summary>
        event EventHandler onClosePrompt;

        /// <summary>
        /// Sets whether opening the prompt should pause the simulation while the prompt is active.
        /// </summary>
        /// <param name="pausable">True to pause the simulation while the prompt is open; otherwise, false.</param>
        void SetPausable(bool pausable);

        /// <summary>
        /// Sets the level that should be loaded if the user confirms the exit action.
        /// </summary>
        /// <param name="levelToLoad">The level name to load, or an empty value to quit the application instead.</param>
        void SetLevelToLoad(string levelToLoad);

        /// <summary>Opens the exit confirmation prompt if it is not already visible.</summary>
        void OpenPrompt();

        /// <summary>
        /// Determines whether the exit prompt is currently open.
        /// </summary>
        /// <returns>True if the prompt is open; otherwise, false.</returns>
        bool IsPromptOpen();

        /// <summary>Handles the negative response for the prompt and closes it without exiting the scenario.</summary>
        void OnNo();
    }
}
