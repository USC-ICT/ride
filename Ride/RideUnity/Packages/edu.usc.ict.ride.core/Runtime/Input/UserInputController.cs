using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Ride.IO
{
    public class UserInputController : RideMonoBehaviour, IInputControllerNew
    {
#if ENABLE_INPUT_SYSTEM
        public PlayerInput playerInput = null;
#endif
        public List<RideID> controlledActors = new List<RideID>();
        public float lookSensitivity = 10.0f;
        public RideID controllerId => id;

        public virtual InputControlType controllerType => InputControlType.None;

        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM
            playerInput = gameObject.AddComponent<PlayerInput>();
#endif
        }

        public virtual void AddControlledActorID(RideID id)
        {
            if (!controlledActors.Contains(id))
                controlledActors.Add(id);
        }

        public virtual void RemoveControlledActorID(RideID id)
        {
            if (controlledActors.Contains(id))
                controlledActors.Remove(id);
        }

        public RideID[] GetControlledActors()
        {
            return controlledActors.ToArray();
        }

        protected virtual bool UpdateInput()
        {
            if (controlledActors.Count < 1)
                return false;

            return true;
        }
    }
}
