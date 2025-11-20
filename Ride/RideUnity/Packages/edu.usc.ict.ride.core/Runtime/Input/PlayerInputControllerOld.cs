#define DEPRECATED_CAMERA

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Ride.WorldState;
//using TMPro;
using Ride.Entities;
using System.Linq;

namespace Ride.IO
{
    [ExecuteInEditMode]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class PlayerInputControllerOld : RideMonoBehaviour, IPlayerInputController
    {
        #region PlayerInputActionBindingLink
        [Serializable]
        public class PlayerInputActionBindingLink
        {
            public string hostActionName;
            public string[] linkedActionNames;
        }
        #endregion

        #region delegates and events
#if ENABLE_INPUT_SYSTEM
        private delegate void PlayerInputCallback(InputValue value);
#endif

#pragma warning disable CS0067  //The event '' is never used
        public event EventHandler onMoveCall;
        public event EventHandler onLookCall;
        public event EventHandler onRotateCall;
        public event EventHandler onViewChangeCall;
        public event EventHandler onViewZoomChangeCall;

        public event EventHandler onFirePressCall;
        public event EventHandler onFireReleaseCall;
        public event EventHandler onFireAimCall;
        public event EventHandler onFireAimReleaseCall;
        public event EventHandler onCrouchToggleCall;
        public event EventHandler onProneToggleCall;
        public event EventHandler onMenuOptionsCall;
        public event EventHandler onJumpInputCall;
        public event EventHandler onSprintCall;
        public event EventHandler onSprintReleaseCall;
        public event EventHandler onReloadCall;
        public event EventHandler onToggleFiringModeCall;
        public event EventHandler onThrowCall;
#pragma warning restore CS0067

#if ENABLE_INPUT_SYSTEM
        private event PlayerInputCallback onDeactivateCall;
#endif
        #endregion

        #region variables
        [SerializeField]
        Transform cameraParent = null;

        [SerializeField]
        Camera playerCam = null;

        [SerializeField]
        float lookSensitivity = 100.0f;

        float minimumY = -90.0f;
        float maximumY = 90.0f;

        float maximumXRot = 10.0f;

        Vector2 rotationDelta;
        float rotationX = 0.0f;
        float rotationY = 0.0f;
        float startYrot = 0.0f;

        float zoom = 0.0f;
        Vector3 camLocalZPosition;

        [SerializeField]
        float zoomSpeed = 1.0f;

        Quaternion originalRotation;

        [SerializeField]
        Vector2 minMaxZoom;

        [SerializeField]
        GameObject crosshair = null;

        public string bindingListDirectory;

        [SerializeField]
        PlayerInputActionBindingLink[] actionBindingLinks = null;

        [SerializeField]
        PlayerView startingPlayerView = PlayerView.FirstPerson;
        int playerViewOptionCount = 15;

        [HideInInspector]
        public bool[] playerViewExcludeToggles = new bool[Enum.GetValues(typeof(PlayerView)).Length];
        [HideInInspector]
        public int playerViewRestrictions = 0;
        [HideInInspector]
        public bool[] playerViewIncludeCrosshair = new bool[Enum.GetValues(typeof(PlayerView)).Length];
        [HideInInspector]
        public int playerViewsWithCrosshair = 0;

        [SerializeField]
        private float startingCameraDepth = 0;

        //[SerializeField]
        //private TextMeshPro ammoText = null; // TODO: TEMPORARY UI; REMOVE LATER

        private Transform agentTransform = null;

        bool active = false;
        bool init = false;
        #endregion

        #region Getters/Setters
        public void SetMinMaxZoom(RideVector2 zoom)
        {
            minMaxZoom = zoom;
        }
        public void SetCameraParent(Transform transform)
        {
            cameraParent = transform;
        } 
        public void SetPlayerCam(Camera camera)
        {
            playerCam = camera;
        }    
        public void SetActionBindings(PlayerInputActionBindingLink[] actionBindings)
        {
            actionBindingLinks = actionBindings.ToArray();
        }
        public void SetCrosshair(GameObject crosshair)
        {
            this.crosshair = crosshair;
        }
        //public void SetAmmoText(TextMeshPro ammoText)
        //{
        //    this.ammoText = ammoText;
        //}
        #endregion

        #region properties and mono funcs
        public PlayerView playerView { get; private set; }

        public RideID m_rideID { get; private set; }

        public float cameraDepth
        {
            get
            {
                return playerCam.depth;
            }
            set
            {
                playerCam.depth = value;
            }
        }

        public bool Active
        {
            get { return active && init; }
            set
            {
                Cursor.visible = !value;
                Cursor.lockState = (value) ? CursorLockMode.Locked : CursorLockMode.None;
                active = value;
#if ENABLE_INPUT_SYSTEM
                onDeactivateCall?.Invoke(new InputValue());
#endif
                rotationDelta = Vector2.zero;
                zoom = 0.0f;
                UpdatePlayerView();

                if (!init)
                    enabled = true;

                if (active)
                    CaptureMainCamera(true);
            }
        }

        public bool CrosshairActive
        {
            get { return (crosshair != null) ? crosshair.activeSelf : false; }
            set { if (crosshair != null) crosshair.SetActive(value); }
        }

        public RideRay cameraRay
        {
            get { return new RideRay(playerCam.ScreenPointToRay(new RideVector3(Screen.width / 2, Screen.height / 2, 0))); }
        }

        public bool ObserverMode { get; set; } = false;

        private void Awake()
        {
            agentTransform = transform;

            StartCoroutine(RegisterPlayerInputController());
        }

        protected override void Start()
        {
            base.Start();

            StartCoroutine(Initialize());
        }

        private IEnumerator RegisterPlayerInputController()
        {
            // RideID and transform
            yield return new WaitUntil(() => Globals.api != null);
#if false
            yield return new WaitUntil(() => AgentMonoSetup() || m_rideID != RideID.Null);
            if (m_rideID == RideID.Null)
                m_rideID = GetComponent<AgentMono>().id;
            agentTransform = transform;
#else
            Debug.LogError($"PlayerInputControllerOld.RegisterPlayerInputController() - TODO - RIDE Modularization - needs to be refactored");
#endif

            yield return new WaitUntil(() => Globals.api != null && Globals.api.inputSystem != null && m_rideID != RideID.Null);
            Globals.api.inputSystem.AttachPlayerInputController(m_rideID, this);
        }

        // Temporary for handling AgentMono; TODO: EVENTUALLY NEEDS TO BE REMOVED
#if false
        private bool AgentMonoSetup()
        {
            AgentMono agent = GetComponent<AgentMono>();
            if (agent != null)
            {
                if (agent.id != RideID.Null)
                    return true;
            }
            return false;
        }
#endif

        private IEnumerator Initialize()
        {
            if (!Application.isPlaying)
                yield break;

            // RideID
            yield return new WaitUntil(() => m_rideID != RideID.Null);

            // Turn off mouse cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

#if ENABLE_INPUT_SYSTEM
            // Setup deactivation
            onDeactivateCall += OnFireAimRelease;
            onDeactivateCall += OnFireRelease;
            onDeactivateCall += OnSprintRelease;
#endif

            // Setup camera and views
            startYrot = (agentTransform.rotation.eulerAngles.y != 0.0f) ? 360.0f - agentTransform.rotation.eulerAngles.y : 0.0f;
            playerCam.transform.SetParent(cameraParent);
            playerView = startingPlayerView;
            UpdatePlayerView();
            originalRotation = agentTransform.localRotation;
            playerViewOptionCount = (int)Mathf.Pow(2, Enum.GetValues(typeof(PlayerView)).Length) - 1;
            cameraDepth = startingCameraDepth;

            // Bind min max zoom parameters
            if (minMaxZoom.y <= minMaxZoom.x)
                minMaxZoom = new Vector2(0.5f, 5.0f);

            // Set player character layer to Ignore Raycasts (for camera functions)
            SetLayerRecursively(transform, 2);

            // Setup input and binding system (and binding links)
#if ENABLE_INPUT_SYSTEM
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
#if false
                playerInput.enabled = true;
                PlayerInputBindingSystem.SetupBindingSystem(playerInput, bindingListDirectory);
                PlayerInputBindingSystem.LoadBindingOverrides("current");

                if (actionBindingLinks != null)
                {
                    foreach (PlayerInputActionBindingLink actionBindingLink in actionBindingLinks)
                        PlayerInputBindingSystem.LinkBindings(actionBindingLink.hostActionName, actionBindingLink.linkedActionNames);
                }
#else
                Debug.LogError($"PlayerInputControllerOld.Initialize() - TODO - RIDE Modularization - needs to be refactored");
#endif
            }
#endif

            // Setup listeners
            Globals.api.worldStateSystem.AddListener<AgentStateChangedEvent>(WorldEvent.agentStateChanged, HandleAgentStateChange);
            Globals.api.worldStateSystem.AddListener<WeaponEvent>(WorldEvent.weaponFired, HandleWeaponFired);
            Globals.api.worldStateSystem.AddListener<InputLayerModifiedEvent>(WorldEvent.inputLayersModified, HandleInputLayerModified);

            // Activate PlayerInputController
            init = true;
            Active = true;
            CaptureMainCamera(true);
        }

        protected override void Update()
        {
            base.Update();

#if ENABLE_INPUT_SYSTEM
#if false
            if (PlayerInputBindingSystem.IsRebinding)
                PlayerInputBindingSystem.CheckForMouseUpdate();
#else
            Debug.LogError($"PlayerInputControllerOld.Update() - TODO - RIDE Modularization - needs to be refactored");
#endif
#endif

            if (!Application.isPlaying)
                return;

            if (!Active)
                return;

            HandleRotation(rotationDelta);
            HandleZoom();

            UpdateAmmoUI();
        }

        void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (GetComponent<PlayerInput>() != null)
            {
                GetComponent<PlayerInput>().enabled = false;
            }
#endif
            Active = false;
            CaptureMainCamera(false);
            EnableCharacterRenderer(true);
        }

        void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            if (GetComponent<PlayerInput>() != null)
                GetComponent<PlayerInput>().enabled = true && init;
#endif
            Active = true && init;
            CaptureMainCamera(true && init);
        }

        void OnDestroy()
        {
            Active = false;
            Globals.api?.worldStateSystem?.RemoveListener<AgentStateChangedEvent>(WorldEvent.agentStateChanged, HandleAgentStateChange);
            Globals.api?.worldStateSystem?.RemoveListener<WeaponEvent>(WorldEvent.weaponFired, HandleWeaponFired);
#if false
            if (GetComponent<AgentMono>() != null) id = GetComponent<AgentMono>().id;
#else
            Debug.LogError($"PlayerInputControllerOld.OnDestroy() - TODO - RIDE Modularization - needs to be refactored");
#endif
            Globals.api?.worldStateSystem.DispatchEvent(WorldEvent.entityDataDestroyed, new EntityEvent(id));
        }
        #endregion

        #region Player Input action func calls
#if ENABLE_INPUT_SYSTEM
        void OnMove(InputValue value)
        {
            if (!Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player)) {
                onMoveCall?.Invoke(this, new PlayerInputControllerEventArgs(new RideVector2(0,0)));
                return;
            }

            if (!Active || ObserverMode)
                return;

            Vector2 moveInput = value.Get<Vector2>();

            onMoveCall?.Invoke(this, new PlayerInputControllerEventArgs(new RideVector2(moveInput)));
        }

        void OnLook(InputValue value)
        {
            if (!Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player)) {
                rotationDelta = default;
                onLookCall?.Invoke(this, new PlayerInputControllerEventArgs(rotationDelta));
                return;
            }

            if (!Active || ObserverMode)
                return;

            Vector2 lookInput = value.Get<Vector2>();
            lookInput.y *= (RideUtils.GetBoolPreference(RideDefines.InvertedMousePref) ? -1 : 1);
            if (lookInput.magnitude <= 1.0f)
                rotationDelta = new Vector2(lookInput.x * Mathf.Abs(lookInput.x), lookInput.y * Mathf.Abs(lookInput.y));
            else
                rotationDelta = lookInput * 0.025f;

            onLookCall?.Invoke(this, new PlayerInputControllerEventArgs(rotationDelta));
        }

        void OnViewChange(InputValue value)
        {
            if (!Active && !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            SwitchView();
            onViewChangeCall?.Invoke(this, new PlayerInputControllerEventArgs(playerView));
        }

        void OnViewZoom(InputValue value)
        {
            if (!Active || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            Vector2 zoomInput = value.Get<Vector2>();

            zoom = zoomInput.y * zoomSpeed * 0.1f;

            onViewZoomChangeCall?.Invoke(this, new PlayerInputControllerEventArgs(new RideVector2(zoomInput)));
        }

        void OnFirePress(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onFirePressCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnFireRelease(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onFireReleaseCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnFireAim(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onFireAimCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnFireAimRelease(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onFireAimReleaseCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnCrouchToggle(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onCrouchToggleCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnProneToggle(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onProneToggleCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnMenuOptions(InputValue value)
        {
            if (!Active || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onMenuOptionsCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnJump(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onJumpInputCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnSprint(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onSprintCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnSprintRelease(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onSprintReleaseCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnReload(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onReloadCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnToggleFiringMode(InputValue value)
        {
            if (!Active || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onToggleFiringModeCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }

        void OnThrow(InputValue value)
        {
            if (!Active || ObserverMode || !Globals.api.inputSystem.GetInputLayerActive(RideInputLayer.Player))
                return;

            onThrowCall?.Invoke(this, new PlayerInputControllerEventArgs());
        }
#endif
        #endregion

        #region PlayerInputController funcs
        public void SetupData(RideID id, object data)
        {
#if false
            if (data is PlayerInputControllerData playerInputControllerData)
            {
                this.lookSensitivity = playerInputControllerData.lookSensitivity;
                this.zoomSpeed = playerInputControllerData.zoomSpeed;
                this.minMaxZoom = playerInputControllerData.minMaxZoom;
                this.bindingListDirectory = playerInputControllerData.bindingListDirectory;
                this.startingPlayerView = playerInputControllerData.startingPlayerView;
                this.cameraParent = playerInputControllerData.cameraParent;
                this.playerCam = playerInputControllerData.playerCam;
                this.ammoText = playerInputControllerData.ammoText;
                this.actionBindingLinks = playerInputControllerData.actionBindingLinksOld;
                this.crosshair = playerInputControllerData.crosshair;
                this.playerViewRestrictions = playerInputControllerData.playerViewRestrictions;
                this.playerViewsWithCrosshair = playerInputControllerData.playerViewsWithCrosshair;

#if ENABLE_INPUT_SYSTEM
                if (playerInputControllerData.actionAsset != null)
                {
                    PlayerInput playerInput = GetComponent<PlayerInput>();
                    if (playerInput == null)
                        playerInput = gameObject.AddComponent<PlayerInput>();

                    playerInput.actions = playerInputControllerData.actionAsset;
                    playerInput.defaultActionMap = (!string.IsNullOrEmpty(playerInputControllerData.defaultMap)) ? playerInputControllerData.defaultMap : playerInput.defaultActionMap;
                }
#endif
            }

            // Handle AgentMono; TODO: EVENTUALLY NEEDS TO BE REMOVED
            IComponentSystem<UnityEngine.Component> compSystem = Globals.api.systemAccessSystem.GetSystem<IComponentSystem<UnityEngine.Component>>();
            Entities.AgentMono agent = compSystem.GetComponent<Entities.AgentMono>(id);
            agent.id = id;
            if (!Globals.api.agentSystem.AgentExists(agent.id))
                Globals.api.agentSystem.AddAgentExisting(agent);
#else
            Debug.LogError($"PlayerInputControllerOld.SetupData() - TODO - RIDE Modularization - needs to be refactored");
#endif

            m_rideID = id;

            UpdatePlayerView();
        }

        public void StopCamera()
        {
            CaptureMainCamera(false);
        }

        void CaptureMainCamera(bool enable)
        {
#if DEPRECATED_CAMERA
            if (cameraParent != null)
                cameraParent.gameObject.SetActive(enable);

            if (playerCam != null)
            {
                playerCam.gameObject.SetActive(enable);
                playerCam.enabled = enable;
            }

            SetLayerRecursively(transform, (enable) ? 2 : 0);
#endif
        }

        float yRotation = 0.0f;
        void HandleRotation(Vector2 rotDelta)
        {
            RideVector2 finalRotDelta = RideVector2.zero;
            float mouseX = rotDelta.x * lookSensitivity * Time.deltaTime;
            float mouseY = rotDelta.y * lookSensitivity * Time.deltaTime;

            yRotation -= mouseY;
            yRotation = Mathf.Clamp(yRotation, minimumY, maximumY);

            mouseX = Mathf.Clamp(mouseX, -maximumXRot, maximumXRot);

            if (playerView == PlayerView.ThirdPersonFree)
            {
                rotationX += rotDelta.x * lookSensitivity * Time.deltaTime;
                rotationY += rotDelta.y * lookSensitivity * Time.deltaTime;
                Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                Quaternion xQuaternionZero = Quaternion.AngleAxis(startYrot, Vector3.up);
                Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.left);
                cameraParent.localRotation = originalRotation * xQuaternion * xQuaternionZero * yQuaternion;
            }
            else
            {
                finalRotDelta = new RideVector2(mouseX, yRotation);
                cameraParent.localRotation = Quaternion.Euler(yRotation, 0.0f, 0.0f);
            }

            if (playerView != PlayerView.ThirdPersonFree)
                onRotateCall?.Invoke(this, new PlayerInputControllerEventArgs(finalRotDelta));
        }

        void HandleZoom()
        {
            if (playerView == PlayerView.FirstPerson || playerView == PlayerView.ThirdPersonOverTheShoulder)
                return;

            float newZoom = camLocalZPosition.z + zoom;
            newZoom = Mathf.Clamp(newZoom, -minMaxZoom.y, -minMaxZoom.x);
            camLocalZPosition = playerCam.transform.localPosition = new Vector3(0.0f, 0.0f, newZoom);

            HandleCameraCollision();
        }

        void HandleAgentStateChange(WorldEventMarker simulationEvent, AgentStateChangedEvent e)
        {
            if (e.agent == m_rideID)
            {
                //Debug.Log("Player Input Controller State Change: " + e.fromState + " to " + e.toState);
#if false
                if (e.toState == StateMachineUtils.DeadState)
                {
                    onMoveCall?.Invoke(this, new PlayerInputControllerEventArgs(RideVector2.zero));
                    onRotateCall?.Invoke(this, new PlayerInputControllerEventArgs(RideVector2.zero));
                }
                if (enabled)
                {
                    if (e.toState == StateMachineUtils.DeadState)
                        Active = false;
                    else if (e.fromState == StateMachineUtils.DeadState)
                        Active = true;
                }
                //Active = !(e.toState == StateMachineUtils.DeadState) && enabled;
#else
                Debug.LogError($"PlayerInputControllerOld.HandleAgentStateChange() - TODO - RIDE Modularization - needs to be refactored");
#endif
            }
        }

        void HandleWeaponFired(WorldEventMarker simulationEvent, WeaponEvent e)
        {
            if (!ObserverMode && e.weapon == Globals.api.agentSystem.GetPrimaryWeapon(m_rideID))
                HandleRotation(new Vector2(0.0f, Globals.api.equipmentSystem.GetWeapon(e.weapon).recoilEffect));
        }

        /// <summary>
        /// Prevents input from 'lingering' when the player input layer is toggled off.
        /// Otherwise, held input would continue to be held when the input should have been ignored.
        /// </summary>
        /// <param name="simulationEvent"></param>
        /// <param name="e"></param>
        void HandleInputLayerModified(WorldEventMarker simulationEvent, InputLayerModifiedEvent e) {
            if(e.layer == RideInputLayer.Player && !e.isOn) {
                // Trigger inputs to reset them to neutral states.

#if ENABLE_INPUT_SYSTEM
                OnMove(new InputValue());
                OnLook(new InputValue());
#endif
            }
        }

        void HandleCameraCollision()
        {
            if (playerView == PlayerView.FirstPerson || playerView == PlayerView.ThirdPersonOverTheShoulder)
                return;

            Vector3 camToHeadVector = playerCam.transform.position - cameraParent.position;
            Ray camRay = new Ray(cameraParent.position, camToHeadVector.normalized);
            if (Physics.Raycast(camRay, out RaycastHit hitInfo, camToHeadVector.magnitude))
                playerCam.transform.position = hitInfo.point;
        }

        void SwitchView()
        {
#if DEPRECATED_CAMERA
            if (playerViewRestrictions == playerViewOptionCount)
                return;

            playerView = (playerView != PlayerView.ThirdPersonFree) ? (PlayerView)((int)playerView << 1) : PlayerView.FirstPerson;

            int viewPermissible = (int)playerView & playerViewRestrictions;
            if (viewPermissible != 0)
                SwitchView();
            else
                UpdatePlayerView();
#endif
        }

        void UpdatePlayerView(bool requireActivation = true)
        {
#if DEPRECATED_CAMERA
            if (Active || !requireActivation)
            {
                if (playerView == PlayerView.FirstPerson)
                {
                    playerCam.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                    cameraParent.transform.localRotation = Quaternion.identity;

                    EnableCharacterRenderer(false);
                }
                else
                {
                    if (playerView == PlayerView.ThirdPersonOverTheShoulder)
                    {
                        playerCam.transform.SetLocalPositionAndRotation(new Vector3(-0.3f, 0.0f, -0.4f), Quaternion.identity);
                        cameraParent.transform.localRotation = Quaternion.identity;
                    }
                    else if (playerView == PlayerView.ThirdPersonLocked)
                    {
                        playerCam.transform.SetLocalPositionAndRotation(new Vector3(0.0f, 0.0f, -3.0f), Quaternion.identity);
                        cameraParent.transform.localRotation = Quaternion.identity;
                    }
                    else
                        cameraParent.transform.localRotation = Quaternion.identity;

                    EnableCharacterRenderer(true);
                }
            }
            else
                EnableCharacterRenderer(true);
            onRotateCall?.Invoke(this, new PlayerInputControllerEventArgs());
            if (playerCam != null)
                camLocalZPosition = playerCam.transform.localPosition;

            UpdatePlayerViewCrosshairs();
#endif
        }

        void UpdatePlayerViewCrosshairs()
        {
            int viewWithCrosshairs = (int)playerView & playerViewsWithCrosshair;
            CrosshairActive = (viewWithCrosshairs != 0) && Active;
        }

        void UpdateAmmoUI()
        {
#if false
            if (ammoText != null && Globals.api.agentSystem.AgentExists(m_rideID))
            {
                List<RideID> weaponsList = new List<RideID>(Globals.api.agentSystem.GetWeapons(m_rideID));
                if (weaponsList.Count < 1)
                {
                    ammoText.text = string.Empty;
                    return;
                }

                IWeapon weapon = Globals.api.equipmentSystem.GetWeapon(weaponsList[0]);
                string ammoCountStr = "Ammo:\n|";
                List<RideID> magazines = new List<RideID>(Globals.api.equipmentSystem.GetItemsFromAgent<IMagazine>(m_rideID));
                if (weapon is WeaponMono weaponMono)
                {
                    foreach (RideID mag in magazines)
                    {
                        if (mag != weaponMono.attachedMagazine)
                            ammoCountStr += ((IMagazine)Globals.api.equipmentSystem.GetItem(mag)).ammoCount.ToString("D2") + "|";
                    }

                    ammoCountStr += " <color=red>" + (weaponMono.isReloading ? "RELOAD" : weapon.currentMagazineAmmoCount.ToString("D2")) + "</color>";
                }

                ammoText.text = ammoCountStr;
            }
#else
            Debug.LogError($"PlayerInputControllerOld.UpdateAmmoUI() - TODO - RIDE Modularization - needs to be refactored");
#endif
        }

        void EnableCharacterRenderer(bool enableFlag)
        {
            //Debug.LogFormat("{0} EnableCharacterRenderer: {1}", name, enableFlag);
            foreach (MeshRenderer meshRenderer in agentTransform.GetComponentsInChildren<MeshRenderer>())
            {
#if false
                if (ammoText != null && meshRenderer.gameObject != ammoText.gameObject)
                    meshRenderer.enabled = enableFlag;
#else
                Debug.LogError($"PlayerInputControllerOld.EnableCharacterRenderer() - TODO - RIDE Modularization - needs to be refactored");
#endif
            }

            foreach (SkinnedMeshRenderer meshRenderer in agentTransform.GetComponentsInChildren<SkinnedMeshRenderer>())
                meshRenderer.enabled = enableFlag;
        }

        void SetLayerRecursively(Transform transform, int layer)
        {
            transform.gameObject.layer = layer;
            foreach (Transform child in transform)
                SetLayerRecursively(child, layer);
        }

        T FindParentOfType<T>(Transform child) where T : Component
        {
            if (child != null && child.GetComponent<T>() != null)
                return child.GetComponent<T>();
            else if (child.parent != null)
                return FindParentOfType<T>(child.parent);

            return default;
        }

        private static float ClampAngle(float angle, float min, float max)
        {
            angle %= 360;
            if ((angle >= -360F) && (angle <= 360F))
            {
                if (angle < -360F)
                {
                    angle += 360F;
                }
                if (angle > 360F)
                {
                    angle -= 360F;
                }
            }
            return Mathf.Clamp(angle, min, max);
        }

        public RideID GetControlledActor()
        {
            return m_rideID;
        }

        public PlayerInputControllerParams GetParams()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
