using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Ride.WorldState;
using Ride.Entities;

namespace Ride.IO
{
    public class InputSystemMono : RideSystemMonoBehaviour, IInputSystem
    {
        protected Dictionary<RideID, IInputController> inputControllers = new Dictionary<RideID, IInputController>();
        protected Dictionary<RideID, IInputControllerNew> inputControllersNew = new Dictionary<RideID, IInputControllerNew>();
        protected Dictionary<RideID, List<IInputControllable>> inputControllablesMap = new Dictionary<RideID, List<IInputControllable>>();
        int activeInputLayers = int.MaxValue;
        float[] mouseDownPressTime = new float[3]; // Each element represents the time at which a mouse button of the same element index was pressed down.
        float holdTimeThreshold = 0.2f; // How long does a button have to be held down for it to be considered a long press?


        public override void SystemAwake()
        {
            base.SystemAwake();

            // TODO - Input System refactor, this system should not be concerned with entities
            if (Globals.api.worldStateSystem != null)
            {
                Globals.api.worldStateSystem.AddListener<EntityCreatedEvent>(WorldEvent.entityDataCreated, onEntityCreated);
                Globals.api.worldStateSystem.AddListener<EntitySetupEvent>(WorldEvent.entityDataSetup, onEntitySetup);
                Globals.api.worldStateSystem.AddListener<EntityEvent>(WorldEvent.entityDisabled, onEntityDisabled);
                Globals.api.worldStateSystem.AddListener<EntityEvent>(WorldEvent.entityDataDestroyed, onEntityDestroyed);
            }
        }

        public RideVector2 mousePosition
        {
            get
            {
                return new RideVector2(Input.mousePosition.x, Input.mousePosition.y);
            }
        }

        public override void SystemUpdate(float dt)
        {
            base.SystemUpdate(dt);
            for (int i = 0; i < 3; i++)
            {
                if (GetMouseButtonDown(i))
                {
                    mouseDownPressTime[i] = Time.time;
                }
            }
        }

        public IInputController GetInputController(RideID id)
        {
            if (inputControllers.ContainsKey(id))
                return inputControllers[id];

            return null;
        }

        public IInputControllerNew GetInputControllerNew(RideID id)
        {
            if (inputControllersNew.ContainsKey(id))
                return inputControllersNew[id];

            return null;
        }

        public IInputControllable GetInputControllable(RideID id, InputControlType controllableType)
        {
            if (inputControllablesMap.ContainsKey(id))
                return inputControllablesMap[id].Find(i => i.controllableType == controllableType);

            return null;
        }

        IInputControllerNew GetController(RideID controllerId)
        {
            if (inputControllersNew.ContainsKey(controllerId))
                return inputControllersNew[controllerId];

            return null;
        }

        IInputControllable GetControllable(RideID controllableId, InputControlType controllableType)
        {
            if (inputControllablesMap.ContainsKey(controllableId))
                return inputControllablesMap[controllableId].Find(i => i.controllableType == controllableType);

            return null;
        }

        public bool AttachControllerToControllable(RideID controllerId, RideID controllableId)
        {
            if (inputControllersNew.ContainsKey(controllerId) && inputControllablesMap.ContainsKey(controllableId))
            {
                IInputControllerNew controller = inputControllersNew[controllerId];

                foreach (IInputControllable controllable in inputControllablesMap[controllableId])
                {
                    if (controller != null && controllable != null && controllable.controllableType != InputControlType.None)
                    {
                        if (controllable.GetController() == RideID.Null)
                        {
                            controller.AddControlledActorID(controllableId);
                            controllable.SetupInputController(controller);
                        }
                        else
                        {
                            // Re-add control of agents that have already been set up, but were released at some point prior.
                            controller.AddControlledActorID(controllableId);
                        }
                    }
                }
            }

            return true;
        }

        public void DetachController(RideID controllerId)
        {
            if (inputControllersNew.ContainsKey(controllerId))
            {
                foreach(var controllableId in inputControllersNew[controllerId].GetControlledActors())
                {
                    DetachControllable(controllableId);
                }
            }
        }

        public void DetachControllable(RideID controllableId)
        {
            if (inputControllablesMap.ContainsKey(controllableId))
            {
                foreach (IInputControllable controllable in inputControllablesMap[controllableId])
                {
                    if (controllable.controllableType != InputControlType.None)
                    {
                        inputControllersNew[controllable.GetController()].RemoveControlledActorID(controllableId);
                    }
                }
            }
        }

        public bool HasController(RideID id)
        {
            return inputControllersNew.ContainsKey(id);
        }

        public bool HasControllable(RideID id)
        {
            return inputControllablesMap.ContainsKey(id);
        }

        public bool HasControllable(RideID id, InputControlType controllableType)
        {
            return inputControllablesMap.ContainsKey(id) ? inputControllablesMap[id].Find(i => i.controllableType == controllableType) != null : false;
        }

        public IPlayerInputController GetPlayerInputController(RideID agentId)
        {
            if (inputControllers.ContainsKey(agentId) && inputControllers[agentId] is IPlayerInputController playerInputController)
                return playerInputController;

            return null;
        }

        public bool HasActivePlayerInputController(RideID agentId)
        {
            IPlayerInputController playerController = GetPlayerInputController(agentId);

            if (playerController != null)
                return playerController.Active;

            return false;
        }

        public bool HasExistingPlayerInputController(RideID agentId)
        {
            IPlayerInputController playerController = GetPlayerInputController(agentId);

            return (playerController != null);
        }

        public bool AttachPlayerInputController(RideID agentId, IPlayerInputController inputController)
        {
            if (inputController == null)
                return false;

            foreach (RideID otherAgent in inputControllers.Keys)
            {
                if (inputControllers[otherAgent] == inputController)
                {
                    inputControllers[otherAgent] = null;
                    break;
                }
            }

            if (!inputControllers.ContainsKey(agentId))
                inputControllers.Add(agentId, inputController);
            else
                inputControllers[agentId] = inputController;

            return true;
        }

        // TODO: IMPLEMENT
        public bool AddPlayerInputController(RideID agentId)
        {
            return false;
        }

        public void TogglePlayerInputController(RideID agentId, bool enable)
        {
            if (inputControllers.ContainsKey(agentId) && inputControllers[agentId] is IPlayerInputController playerInputController)
            {
                if (playerInputController is PlayerInputControllerOld controllerOld)
                {
                    controllerOld.enabled = enable;
#if ENABLE_INPUT_SYSTEM
                    PlayerInput playerInput = controllerOld.GetComponent<PlayerInput>();
                    if (playerInput != null)
                        playerInput.enabled = enable;
#endif
                }
                else if (playerInputController is PlayerInputController controller)
                {
                    controller.enabled = enable;
#if ENABLE_INPUT_SYSTEM
                    PlayerInput playerInput = controller.GetComponent<PlayerInput>();
                    if (playerInput != null)
                        playerInput.enabled = enable;
#endif
                }
            }
        }

        public IEnumerable<IInputControllable> GetControllables(RideID id)
        {
            if (inputControllablesMap.ContainsKey(id))
                return inputControllablesMap[id];

            return null;
        }

        public bool GetKey(RideKeyCode keyCode)
        {
            return Input.GetKey(keyCode);
        }

        public bool GetKeyDown(RideKeyCode keyCode)
        {
            return Input.GetKeyDown(keyCode);
        }

        public bool GetKeyUp(RideKeyCode keyCode)
        {
            return Input.GetKeyUp(keyCode);
        }

        public bool GetMouseButton(int mouseButton)
        {
            return Input.GetMouseButton(mouseButton);
        }

        public bool GetMouseButtonDown(int mouseButton)
        {
            return Input.GetMouseButtonDown(mouseButton);
        }

        public bool GetMouseButtonUp(int mouseButton)
        {
            return Input.GetMouseButtonUp(mouseButton);
        }

        public bool GetKeys(RideKeyCode[] keyCodes)
        {
            foreach (RideKeyCode code in keyCodes)
            {
                if (!GetKey(code))
                    return false;
            }

            return true;
        }

        public bool GetKeysDown(RideKeyCode[] keyCodes)
        {
            foreach (RideKeyCode code in keyCodes)
            {
                if (!GetKeyDown(code))
                    return false;
            }

            return true;
        }

        public bool GetKeysUp(RideKeyCode[] keyCodes)
        {
            foreach (RideKeyCode code in keyCodes)
            {
                if (!GetKeyUp(code))
                    return false;
            }

            return true;
        }

        public bool GetMouseButtons(int[] mouseButtons)
        {
            foreach (int button in mouseButtons)
            {
                if (!GetMouseButton(button))
                    return false;
            }

            return true;
        }

        public bool GetMouseButtonsDown(int[] mouseButtons)
        {
            foreach (int button in mouseButtons)
            {
                if (!GetMouseButtonDown(button))
                    return false;
            }

            return true;
        }

        public bool GetMouseButtonsUp(int[] mouseButtons)
        {
            foreach (int button in mouseButtons)
            {
                if (!GetMouseButtonUp(button))
                    return false;
            }

            return true;
        }

        public RideKeyCode[] GetKeysPressed()
        {
            var keyCodes = new List<RideKeyCode>();
            for (int i = 0; i < RideKeyCode.MaxDefinedKeyCode; i++)
            {
                KeyCode key = (KeyCode)i;
                if (Input.GetKey(key))
                    keyCodes.Add(new RideKeyCode(key));
            }

            return keyCodes.ToArray();
        }

        public RideKeyCode[] GetKeysPressedDown()
        {
            var keyCodes = new List<RideKeyCode>();
            for (int i = 0; i < RideKeyCode.MaxDefinedKeyCode; i++)
            {
                KeyCode key = (KeyCode)i;
                if (Input.GetKeyDown(key))
                    keyCodes.Add(new RideKeyCode(key));
            }

            return keyCodes.ToArray();
        }

        public RideKeyCode[] GetKeysPressedUp()
        {
            var keyCodes = new List<RideKeyCode>();
            for (int i = 0; i < RideKeyCode.MaxDefinedKeyCode; i++)
            {
                KeyCode key = (KeyCode)i;
                if (Input.GetKeyUp(key))
                    keyCodes.Add(new RideKeyCode(key));
            }

            return keyCodes.ToArray();
        }

        public void SetInputLayer(RideInputLayer layer, bool isOn)
        {
            if (isOn)
            {
                activeInputLayers |= (int)layer;
                InputLayerModifiedEvent e = new InputLayerModifiedEvent(layer, isOn);
                Globals.api.worldStateSystem.DispatchEvent(WorldEvent.inputLayersModified, e);
            }
            else
            {
                activeInputLayers &= ~(int)layer;
                InputLayerModifiedEvent e = new InputLayerModifiedEvent(layer, isOn);
                Globals.api.worldStateSystem.DispatchEvent(WorldEvent.inputLayersModified, e);
            }
        }

        public bool GetInputLayerActive(RideInputLayer layer)
        {
            if ((activeInputLayers & (int)layer) == 0)
            {
                return false;
            }

            return true;
        }

        public bool GetKey(RideKeyCode keyCode, RideInputLayer layer)
        {
            return GetKey(keyCode) && GetInputLayerActive(layer);
        }

        public bool GetKeyDown(RideKeyCode keyCode, RideInputLayer layer)
        {
            return GetKeyDown(keyCode) && GetInputLayerActive(layer);
        }

        public bool GetKeyUp(RideKeyCode keyCode, RideInputLayer layer)
        {
            return GetKeyUp(keyCode) && GetInputLayerActive(layer);
        }

        public bool GetMouseButton(int mouseButton, RideInputLayer layer)
        {
            return GetMouseButton(mouseButton) && GetInputLayerActive(layer);
        }

        public bool GetMouseButtonDown(int mouseButton, RideInputLayer layer)
        {
            return GetMouseButtonDown(mouseButton) && GetInputLayerActive(layer);
        }

        public bool GetMouseButtonUp(int mouseButton, RideInputLayer layer)
        {
            return GetMouseButtonUp(mouseButton) && GetInputLayerActive(layer);
        }

        public bool GetKeys(RideKeyCode[] keyCodes, RideInputLayer layer)
        {
            return GetKeys(keyCodes) && GetInputLayerActive(layer);
        }

        public bool GetKeysDown(RideKeyCode[] keyCodes, RideInputLayer layer)
        {
            return GetKeysDown(keyCodes) && GetInputLayerActive(layer);
        }

        public bool GetKeysUp(RideKeyCode[] keyCodes, RideInputLayer layer)
        {
            return GetKeysUp(keyCodes) && GetInputLayerActive(layer);
        }

        public bool GetMouseButtons(int[] mouseButtons, RideInputLayer layer)
        {
            return GetMouseButtons(mouseButtons) && GetInputLayerActive(layer);
        }

        public bool GetMouseButtonsDown(int[] mouseButtons, RideInputLayer layer)
        {
            return GetMouseButtonsDown(mouseButtons) && GetInputLayerActive(layer);
        }

        public bool GetMouseButtonsUp(int[] mouseButtons, RideInputLayer layer)
        {
            return GetMouseButtonsUp(mouseButtons) && GetInputLayerActive(layer);
        }

        public float GetAxis(string axisName)
        {
            return Input.GetAxis(axisName);
        }

        public float GetAxis(string axisName, RideInputLayer layer)
        {
            if (!GetInputLayerActive(layer))
            {
                return 0f;
            }

            return GetAxis(axisName);
        }

        public bool IsMouseButtonLongDown(int mouseButton)
        {
            if (Time.time - mouseDownPressTime[mouseButton] > holdTimeThreshold)
            {
                return true;
            }
            return false;
        }


        /// **********************************
        /// INPUT CONTROLLER MAPPING FUNCTIONS
        /// **********************************
        protected virtual void onEntityCreated(WorldEventMarker marker, EntityCreatedEvent entityCreatedEvent)
        {
            // Old controller handling
            if (entityCreatedEvent.entityObjData is IInputControllerData data)
            {
                setupInputController(data, entityCreatedEvent.entityID);
                return;
            }

            // New controller handling
            if (entityCreatedEvent.entityObjData is UserInputControllerData userInputData)
            {
                IEntityInputController entityInputController = null;
#if false
                switch (userInputData.inputControllerType)
                {
                    case InputActionMapType.Vehicle:
                        entityInputController = Globals.api.componentSystem.AddComponent<VehicleUserInputController>(entityCreatedEvent.entityID);
                        break;
                    default:
                        //RideLogSystem.LogWarning("Input controller type not handled: " + userInputData.inputControllerType.ToString());
                        break;
                }
#else
                Debug.LogError($"InputSystemMono.onEntityCreated() - TODO - RIDE Modularization - needs to be refactored");
#endif
                if (entityInputController != null)
                    SetupUserInputController(entityInputController, userInputData, entityCreatedEvent.entityID);
            }

            // New controllable handling
            if (entityCreatedEvent.entityObjData is InputControllableData inputControllableData)
            {
                IInputControllable inputControllable = null;
#if false
                switch (inputControllableData.controllableType)
                {
                    case InputControlType.Vehicle:
                        inputControllable = compSystem.AddComponent<VehicleInputControllable>(entityCreatedEvent.entityID);
                        break;
                    case InputControlType.Camera:
                        inputControllable = compSystem.AddComponent<CameraInputControllable>(entityCreatedEvent.entityID);
                        break;
                    case InputControlType.MountedWeapon:
                        inputControllable = compSystem.AddComponent<MountedWeaponInputControllable>(entityCreatedEvent.entityID);
                        break;
                    default:
                        RideLogSystem.LogWarning("Input controllable type not handled: " + inputControllableData.controllableType.ToString());
                        break;
                }
#else
                Debug.LogError($"InputSystemMono.onEntityCreated() - TODO - RIDE Modularization - needs to be refactored");
#endif
                if (inputControllable != null)
                    SetupInputControllable(inputControllable, inputControllableData, entityCreatedEvent.entityID);
            }
        }

        void SetupUserInputController(IEntityInputController entityInputController, UserInputControllerData userInputData, RideID id)
        {
            entityInputController.onEntityInput += HandleUserInput;

            if (entityInputController is UserInputController userInputController)
            {
                userInputController.id = id;
                userInputController.lookSensitivity = userInputData.lookSensitivity;
#if ENABLE_INPUT_SYSTEM
                userInputController.playerInput.actions = userInputData.actionAsset;
                userInputController.playerInput.SwitchCurrentActionMap(userInputData.actionMap);
#endif
            }

            if (!inputControllersNew.ContainsKey(id))
                inputControllersNew.Add(id, entityInputController);

            Globals.api.worldStateSystem.DispatchEvent(WorldEvent.entityDataSetup, new EntitySetupEvent(id, new RideMonoBehaviour[] { (RideMonoBehaviour)entityInputController }));
        }

        void SetupInputControllable(IInputControllable inputControllable, InputControllableData inputControllableData, RideID id)
        {
            if (!inputControllablesMap.ContainsKey(id))
                inputControllablesMap.Add(id, new List<IInputControllable>());

            if (!inputControllablesMap[id].Contains(inputControllable))
                inputControllablesMap[id].Add(inputControllable);

            if (inputControllable is EntityInputControllable entityControllable)
            {
                entityControllable.id = id;
                entityControllable.SetControllableProperties(inputControllableData.controllableProperties);
                Globals.api.worldStateSystem.DispatchEvent(WorldEvent.entityDataSetup, new EntitySetupEvent(id, new RideMonoBehaviour[] { entityControllable }));
            }
        }

        protected virtual void onEntitySetup(WorldEventMarker marker, EntitySetupEvent entitySetupEvent)
        {
            if (entitySetupEvent.entityComponents != null)
            {
                foreach (RideMonoBehaviour component in entitySetupEvent.entityComponents)
                {
                    if (component is IInputControllable controllable)
                    {
                        if (!inputControllablesMap.ContainsKey(entitySetupEvent.entityID))
                            inputControllablesMap.Add(entitySetupEvent.entityID, new List<IInputControllable>());

                        if (!inputControllablesMap[entitySetupEvent.entityID].Contains(controllable))
                            inputControllablesMap[entitySetupEvent.entityID].Add(controllable);
                    }

                    if (component is IInputController controller)
                    {
                        if (!inputControllers.ContainsKey(entitySetupEvent.entityID))
                            inputControllers.Add(entitySetupEvent.entityID, controller);
                    }
                }
            }

            CheckForControllerAttachment(entitySetupEvent.entityID);
        }

        protected virtual void onEntityDisabled(WorldEventMarker marker, EntityEvent entityDisabled)
        {
            if (inputControllablesMap.ContainsKey(entityDisabled.entityID))
            {
                foreach (IInputControllable controllable in inputControllablesMap[entityDisabled.entityID])
                    controllable.Enable = false;
            }
        }

        protected virtual void onEntityDestroyed(WorldEventMarker marker, EntityEvent entityDestroyed)
        {
            if (inputControllablesMap.ContainsKey(entityDestroyed.entityID))
            {
                inputControllablesMap.Remove(entityDestroyed.entityID);
            }

            if (inputControllers.ContainsKey(entityDestroyed.entityID))
            {
                inputControllers.Remove(entityDestroyed.entityID);
            }
        }

        void CheckForControllerAttachment(RideID id)
        {
            if (inputControllablesMap.ContainsKey(id) && inputControllers.ContainsKey(id))
            {
                foreach (IInputControllable controllable in inputControllablesMap[id])
                {
                    if (controllable.inputController == null)
                        controllable.SetupInputController(inputControllers[id]);
                }
            }
        }

        protected bool setupInputController(IInputControllerData data, RideID id)
        {
            IInputController controller = data.InstallControllerComponent(id);
            controller.SetupData(id, data);
            if (controller is RideMonoBehaviour rideObj)
                Globals.api.worldStateSystem.DispatchEvent<EntitySetupEvent>(WorldEvent.entityDataSetup, new EntitySetupEvent(id, new RideMonoBehaviour[] { rideObj }));
            return true;
        }

        public ILocomotionInputController GetLocomotionController(RideID locomotionID)
        {
            if (inputControllers.ContainsKey(locomotionID) && inputControllers[locomotionID] is ILocomotionInputController locomotionInputController)
                return locomotionInputController;

            return null;
        }

        #region Input handling
        void HandleUserInput(object sender, EventArgs e)
        {
#if false
            if (e is VehicleInputControllerEventArgs vehicleArgs)
            {
                IInputControllerNew controller = Globals.api.inputSystem.GetInputControllerNew(vehicleArgs.controllerId);
                if (controller == null)
                    return;

                foreach (RideID controllableId in controller.GetControlledActors())
                {
                    foreach (IInputControllable controllable in Globals.api.inputSystem.GetControllables(controllableId))
                    {
                        if (!controllable.Enable)
                            continue;

                        if (controllable is IVehicleInputControllable vehicleControllable)
                        {
                            vehicleControllable.Drive(vehicleArgs.forwardMovement);
                            vehicleControllable.TurnWheel(vehicleArgs.turningMovement);

                            if (vehicleArgs.brake)
                                vehicleControllable.Brake();

                            vehicleControllable.Boost(vehicleArgs.boost);
                        }
                        else if (controllable is ICameraInputControllable cameraControllable)
                        {
                            foreach (RideID cameraId in vehicleArgs.controllableIds)
                                Globals.api.cameraSystem.RotateCamera(cameraId, vehicleArgs.lookInput);
                        }
                        else if (controllable is IMountedWeaponInputControllable mountedWeaponControllable)
                        {
                            if (vehicleArgs.aiming)
                            {
                                // Get weapon
                                IWeapon weapon = null;
                                foreach (RideID weaponId in vehicleArgs.controllableIds)
                                {
                                    weapon = Globals.api.equipmentSystem.GetWeapon(weaponId);
                                    if (weapon != null)
                                        break;
                                }

                                if (weapon == null)
                                    return;

                                // Get owner
                                RideID weaponOwner = weapon.owner;
                                if (weaponOwner == RideID.Null)
                                    return;

                                // Get camera from owner and find target location through raycast
                                RideRay screenPointRay = Globals.api.cameraSystem.ScreenPointToRay(weaponOwner, new RideVector3(Screen.width * 0.5f, Screen.height * 0.5f, 0.0f));
                                RideRaycastHit hitInfo;

                                // Try to find a target to hit
                                if (RideUtils.Raycast(screenPointRay, out hitInfo))
                                {
                                    // Get aim location of target
                                    RideVector3 aimLocation = new RideVector3(hitInfo.hitInfo.point);

                                    // Aim weapon at target location
                                    Globals.api.attackSystem.AimAtTarget(weaponOwner, aimLocation, weapon.id);
                                }
                            }

                            if (vehicleArgs.firing)
                            {
                                foreach (RideID weaponId in vehicleArgs.controllableIds)
                                    Globals.api.equipmentSystem.Fire(weaponId);
                            }
                        }
                    }
                }
            }
#else
            Debug.LogError($"InputSystemMono.HandleUserInput() - TODO - RIDE Modularization - needs to be refactored");
#endif
        }
        #endregion

        /// **********************************
        /// PLAYER INPUT CONTROLLER FUNCTIONS
        /// **********************************
        public PlayerInputControllerParams GetPlayerInputControllerParams(RideID id)
        {
            IInputController inputController = inputControllers[id];
            return ((IPlayerInputController)inputController).GetParams();
        }
    }

    public class EntityInputControllerEventArgs : EventArgs
    {
        public RideID controllerId;
        public List<RideID> controllableIds = new List<RideID>();
        public float lookSensitivity;

        public EntityInputControllerEventArgs(RideID controllerId, RideID controllableId)
        {
            this.controllerId = controllerId;
            controllableIds.Add(controllableId);
            lookSensitivity = 10.0f;
        }

        public void AddNewControllableId(RideID controllableId)
        {
            if (!controllableIds.Contains(controllableId))
                controllableIds.Add(controllableId);
        }

        public void RemoveControllableId(RideID controllableId)
        {
            if (controllableIds.Contains(controllableId))
                controllableIds.Remove(controllableId);
        }
    }
}
