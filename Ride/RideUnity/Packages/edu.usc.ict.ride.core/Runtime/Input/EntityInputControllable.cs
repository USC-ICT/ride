using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.IO
{
    public class EntityInputControllable : RideMonoBehaviour, IEntityInputControllable
    {
        public string GetControllableType() { return "EntityInputControllable"; }
        public float moveSpeed { get; set; } = 0.0f;

        private RideID controllerId = RideID.Null;

        public bool Enable { get => enabled; set => enabled = value; }

        public IInputController inputController { get; private set; }

        public IInputControllerNew inputControllerNew { get; private set; }

        public virtual InputControlType controllableType => InputControlType.None;

        public RideID GetController()
        {
            return controllerId;
        }

        public virtual void SetControllableProperties(InputControllableProperties properties)
        {
        }

        public virtual void SetupInputController(IInputController inputController)
        {
            this.inputController = inputController;
        }

        public virtual void SetupInputController(IInputControllerNew inputController)
        {
            inputControllerNew = inputController;
            controllerId = (inputController != null) ? inputController.controllerId : RideID.Null;
        }
    }
}
