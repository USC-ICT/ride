using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VHAssets;
using Ride.IO;

namespace Ride.UI
{
    public class ExitPromptMenu : MenuMono, IExitPromptMenu
    {
        [SerializeField]
        protected RideButton noButton = null;       // The button that cancels exiting the scenario.
        [SerializeField]
        protected RideButton yesButton = null;      // The button that exits the scenario.
        [SerializeField]
        protected RideButton exitButton = null;     // The button that opens the prompt. Intended for touch users.
        [SerializeField]
        protected GameObject promptPanel = null;    // This reference is for toggling the prompt on and off.


        protected IPauseSystem pauseSystem;
        protected IInputSystem inputSystem;
        protected string m_levelToLoad = "LevelSelect";     // The level to load upon user confirmation of a scenario exit.
        protected bool simulationPausable = true;           // If false, we should not pause the simulation when the prompt is active.
        protected bool cursorWasLocked = false;             // Handles relocking mouse input if the containing scene had a locked mouse.

        public event EventHandler onOpenPrompt;
        public event EventHandler onClosePrompt;


        protected override void Start()
        {
            base.Start();

            noButton.GetComponent<Button>().onClick.AddListener(() => { OnNo(); });
            yesButton.GetComponent<Button>().onClick.AddListener(() => { OnYes(); });
            exitButton.GetComponent<Button>().onClick.AddListener(() => { OpenPrompt(); });

            // TODO - Ride Refactor - add a GetSystem() variant that doesn't fail with an error. Alternatively, HasSystem().
            //pauseSystem = Globals.api.GetSystem<IPauseSystem>();
            pauseSystem = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<IPauseSystem>().FirstOrDefault();

            StartCoroutine(SetupListeners());

            promptPanel.SetActive(false);

            #if UNITY_IOS || UNITY_ANDROID
            // LevelSelect.unity's main script derives off ExampleBase.cs, so this is to hide the exit button.
            if (UnityEngine.SceneManagement.SceneManager.GetSceneByName(m_levelToLoad).isLoaded) {
                exitButton.gameObject.SetActive(false);
            }
            else{
                exitButton.gameObject.SetActive(true);
            }
            #else
            exitButton.gameObject.SetActive(false);
            #endif
        }

        public void SetPausable(bool pausable)
        {
            simulationPausable = pausable;
        }

        public virtual void OpenPrompt()
        {
            if (!promptPanel.activeSelf)
            {
                promptPanel.SetActive(true);
                if (simulationPausable)
                {
                    pauseSystem?.PauseSimulation(yesButton.id, null);
                }

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

        protected virtual void ClosePrompt()
        {
            promptPanel.SetActive(false);
            if (simulationPausable)
            {
                pauseSystem?.ResumeSimulation(null);
            }

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

        public bool IsPromptOpen()
        {
            return promptPanel.activeSelf;
        }

        public void SetLevelToLoad(string levelToLoad)
        {
            this.m_levelToLoad = levelToLoad;
        }

        public void OnNo()
        {
            ClosePrompt();
        }

        public void OnYes()
        {
            ClosePrompt();
            if (string.IsNullOrEmpty(m_levelToLoad))
                RideUtils.QuitApplication();
            else
                RideUtils.LoadScene(m_levelToLoad);
        }

        protected IEnumerator SetupListeners()
        {
            yield return new WaitUntil(() => Globals.api != null && Globals.api.worldStateSystem != null);
            inputSystem = Globals.api.inputSystem;
            //Globals.api.worldStateSystem.AddListener<AgentAddedEvent>(WorldEvent.agentCreated, HandleAgentCreation);
        }
    }
}
