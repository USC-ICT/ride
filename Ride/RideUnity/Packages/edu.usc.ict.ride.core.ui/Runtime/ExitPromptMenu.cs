using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VHAssets;
using Ride.IO;

namespace Ride.UI
{
    /// <summary>
    /// Displays a confirmation prompt for leaving the current scenario and coordinates the related
    /// pause, input-layer, cursor, and scene-transition behavior.
    /// </summary>
    public class ExitPromptMenu : MenuUnity, IExitPromptMenu
    {
        [Tooltip("Button that closes the prompt and keeps the current scenario running.")]
        [SerializeField]
        protected RideButton noButton = null;       // The button that cancels exiting the scenario.
        [Tooltip("Button that confirms the exit action and quits or loads the configured level.")]
        [SerializeField]
        protected RideButton yesButton = null;      // The button that exits the scenario.
        [Tooltip("Button that opens the exit prompt, primarily for touch-based interfaces.")]
        [SerializeField]
        protected RideButton exitButton = null;     // The button that opens the prompt. Intended for touch users.
        [Tooltip("Panel GameObject that contains the exit confirmation UI and is shown or hidden when the prompt opens or closes.")]
        [SerializeField]
        protected GameObject promptPanel = null;    // This reference is for toggling the prompt on and off.


        protected IPauseSystem pauseSystem;
        protected IInputSystem inputSystem;
        protected string m_levelToLoad = "LevelSelect";     // The level to load upon user confirmation of a scenario exit.
        protected bool simulationPausable = true;           // If false, we should not pause the simulation when the prompt is active.
        protected bool cursorWasLocked = false;             // Handles relocking mouse input if the containing scene had a locked mouse.

        public event EventHandler onOpenPrompt;
        public event EventHandler onClosePrompt;


        /// <summary>
        /// Initializes button listeners, locates dependent systems, and configures the initial prompt visibility.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            noButton.GetComponent<Button>().onClick.AddListener(() => { OnNo(); });
            yesButton.GetComponent<Button>().onClick.AddListener(() => { OnYes(); });
            exitButton.GetComponent<Button>().onClick.AddListener(() => { OpenPrompt(); });

            pauseSystem = Systems.Get<IPauseSystem>();

            StartCoroutine(SetupListeners());

            promptPanel.SetActive(false);

#if UNITY_IOS || UNITY_ANDROID
            // LevelSelect.unity's main script derives off ExampleBase.cs, so this is to hide the exit button.
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByName(m_levelToLoad).isLoaded)
            {
                exitButton.gameObject.SetActive(false);
            }
            else
            {
                exitButton.gameObject.SetActive(true);
            }
#else
            exitButton.gameObject.SetActive(false);
#endif
        }

        /// <summary>
        /// Sets whether the simulation should pause while the exit prompt is visible.
        /// </summary>
        /// <param name="pausable">True to pause the simulation while the prompt is open; otherwise, false.</param>
        public void SetPausable(bool pausable) => simulationPausable = pausable;

        /// <summary>
        /// Opens the prompt, pauses simulation if configured, and disables gameplay input while the prompt is active.
        /// </summary>
        public virtual void OpenPrompt()
        {
            if (!promptPanel.activeSelf)
            {
                promptPanel.SetActive(true);
                if (simulationPausable)
                    pauseSystem?.PauseSimulation(yesButton.id, null);

                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    cursorWasLocked = true;
                }

                inputSystem.SetInputLayer(RideInputLayer.Player, false);
                inputSystem.SetInputLayer(RideInputLayer.Camera, false);
                inputSystem.SetInputLayer(RideInputLayer.System, false);
                onOpenPrompt?.Invoke(this, null);

                EventSystem.current.SetSelectedGameObject(VHUtils.FindChildRecursive(promptPanel, "No"));
            }
        }

        /// <summary>
        /// Closes the prompt, restores simulation and input state, and raises the close notification event.
        /// </summary>
        protected virtual void ClosePrompt()
        {
            promptPanel.SetActive(false);
            if (simulationPausable)
                pauseSystem?.ResumeSimulation(null);

            if (cursorWasLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                cursorWasLocked = false;
            }

            inputSystem.SetInputLayer(RideInputLayer.Player, true);
            inputSystem.SetInputLayer(RideInputLayer.Camera, true);
            inputSystem.SetInputLayer(RideInputLayer.System, true);
            onClosePrompt?.Invoke(this,null);
        }

        /// <summary>
        /// Determines whether the prompt panel is currently active.
        /// </summary>
        /// <returns>True if the prompt is open; otherwise, false.</returns>
        public bool IsPromptOpen() => promptPanel.activeSelf;

        /// <summary>
        /// Sets the level to load when the user confirms the exit action.
        /// </summary>
        /// <param name="levelToLoad">The level name to load, or an empty value to quit the application instead.</param>
        public void SetLevelToLoad(string levelToLoad) => m_levelToLoad = levelToLoad;

        /// <summary>
        /// Handles the negative response to the prompt by closing it and leaving the current scenario running.
        /// </summary>
        public void OnNo() => ClosePrompt();

        /// <summary>
        /// Handles the affirmative response to the prompt by closing it and then quitting or loading the configured level.
        /// </summary>
        public void OnYes()
        {
            ClosePrompt();
            if (string.IsNullOrEmpty(m_levelToLoad))
                RideUtils.QuitApplication();
            else
                RideUtils.LoadScene(m_levelToLoad);
        }

        /// <summary>
        /// Waits for the Ride API to become available and then caches the input system reference used by the prompt.
        /// </summary>
        /// <returns>An enumerator for the delayed setup coroutine.</returns>
        protected IEnumerator SetupListeners()
        {
            yield return new WaitUntil(() => Systems.WorldState != null);
            inputSystem = Systems.Input;
            //Globals.api.worldStateSystem.AddListener<AgentAddedEvent>(WorldEvent.agentCreated, HandleAgentCreation);
        }
    }
}
