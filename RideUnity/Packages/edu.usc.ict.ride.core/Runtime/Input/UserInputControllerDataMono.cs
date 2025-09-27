using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Ride.IO
{
    public enum InputActionMapType
    {
        Agent,
        Vehicle
    }

    [Serializable]
    public struct UserInputControllerData
    {
        public InputActionMapType inputControllerType;
#if ENABLE_INPUT_SYSTEM
        public UnityEngine.InputSystem.InputActionAsset actionAsset;
#endif
        public float lookSensitivity;

        [HideInInspector]
        public string actionMap;
    }

    public class UserInputControllerDataMono : RideDataUnityBootstrap
    {
        public UserInputControllerData data;

        public override object GetData()
        {
            switch (data.inputControllerType)
            {
                case InputActionMapType.Agent:
                    data.actionMap = "Gameplay";
                    break;
                case InputActionMapType.Vehicle:
                    data.actionMap = "Vehicle";
                    break;
                default:
                    break;
            }

            return data;
        }

        private void OnDisable()
        {
#if ENABLE_INPUT_SYSTEM
            if (GetComponent<PlayerInput>() != null)
                GetComponent<PlayerInput>().actions = null;
#endif
        }
    }
}
