using System;

namespace Ride.IO
{
    public enum PlayerView
    {
        FirstPerson = 1,
        ThirdPersonOverTheShoulder = 2,
        ThirdPersonLocked = 4,
        ThirdPersonFree = 8
    }

    public class PlayerInputControllerEventArgs : EventArgs
    {
        public RideVector2 vec2Val = RideVector2.zero;
        public PlayerView viewVal = PlayerView.FirstPerson;
        public float floatVal = 0.0f;

        public PlayerInputControllerEventArgs()
        {
        }

        public PlayerInputControllerEventArgs(RideVector2 vec2)
        {
            vec2Val = vec2;
        }

        public PlayerInputControllerEventArgs(PlayerView view)
        {
            viewVal = view;
        }

        public PlayerInputControllerEventArgs(float val)
        {
            floatVal = val;
        }
    }

    public struct PlayerInputControllerParams
    {
        public RideVector2 movement;
        public RideVector2 rotation;
        public bool jumpTrigger;
        public bool fireTrigger;
        public bool fireRelease;
        public bool fireModeTrigger;
        public bool reloadTrigger;
        public bool sprintTrigger;
        public bool sprintRelease;
    }

    public interface IPlayerInputController : IInputController
    {
        event EventHandler onMoveCall;
        event EventHandler onLookCall;
        event EventHandler onRotateCall;
        event EventHandler onFirePressCall;
        event EventHandler onFireReleaseCall;
        event EventHandler onFireAimCall;
        event EventHandler onFireAimReleaseCall;
        event EventHandler onCrouchToggleCall;
        event EventHandler onProneToggleCall;
        event EventHandler onMenuOptionsCall;
        event EventHandler onJumpInputCall;
        event EventHandler onSprintCall;
        event EventHandler onSprintReleaseCall;
        event EventHandler onReloadCall;
        event EventHandler onToggleFiringModeCall;
        event EventHandler onThrowCall;

        PlayerView playerView { get; }
        bool Active { get; set; }
        bool CrosshairActive { get; set; }
        RideID m_rideID { get; }
        float cameraDepth { get; set; }
        RideRay cameraRay { get; }
        bool ObserverMode { get; set; }

        void StopCamera();

        PlayerInputControllerParams GetParams();
    }
}
