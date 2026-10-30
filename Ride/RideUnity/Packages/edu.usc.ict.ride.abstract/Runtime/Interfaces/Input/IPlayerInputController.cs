using System;

namespace Ride.IO
{
    /// <summary>Identifies the camera perspective used by a player input controller.</summary>
    public enum PlayerView
    {
        /// <summary>A first-person view from the controlled actor.</summary>
        FirstPerson = 1,

        /// <summary>A third-person view positioned over the actor's shoulder.</summary>
        ThirdPersonOverTheShoulder = 2,

        /// <summary>A third-person view locked to the actor.</summary>
        ThirdPersonLocked = 4,

        /// <summary>A freely rotating third-person view.</summary>
        ThirdPersonFree = 8
    }

    /// <summary>
    /// Carries a vector, view, or scalar value for player input events.
    /// Consumers read the field associated with the event they subscribed to.
    /// </summary>
    public class PlayerInputControllerEventArgs : EventArgs
    {
        /// <summary>The vector value associated with movement, look, rotation, or zoom input.</summary>
        public RideVector2 vec2Val = RideVector2.zero;

        /// <summary>The view value associated with a camera-view change.</summary>
        public PlayerView viewVal = PlayerView.FirstPerson;

        /// <summary>The scalar value associated with the input event.</summary>
        public float floatVal = 0.0f;

        /// <summary>Initializes event data with default values.</summary>
        public PlayerInputControllerEventArgs()
        {
        }

        /// <summary>
        /// Initializes event data with a vector value.
        /// </summary>
        /// <param name="vec2">The vector value carried by the event.</param>
        public PlayerInputControllerEventArgs(RideVector2 vec2)
        {
            vec2Val = vec2;
        }

        /// <summary>
        /// Initializes event data with a player-view value.
        /// </summary>
        /// <param name="view">The player view carried by the event.</param>
        public PlayerInputControllerEventArgs(PlayerView view)
        {
            viewVal = view;
        }

        /// <summary>
        /// Initializes event data with a scalar value.
        /// </summary>
        /// <param name="val">The scalar value carried by the event.</param>
        public PlayerInputControllerEventArgs(float val)
        {
            floatVal = val;
        }
    }

    /// <summary>Contains the latest sampled movement and action state from a player input controller.</summary>
    public struct PlayerInputControllerParams
    {
        /// <summary>The current movement input.</summary>
        public RideVector2 movement;

        /// <summary>The current look or rotation input.</summary>
        public RideVector2 rotation;

        /// <summary>Whether jump input has been triggered.</summary>
        public bool jumpTrigger;

        /// <summary>Whether fire input is currently pressed or has been triggered.</summary>
        public bool fireTrigger;

        /// <summary>Whether fire input has been released.</summary>
        public bool fireRelease;

        /// <summary>Whether a firing-mode change has been triggered.</summary>
        public bool fireModeTrigger;

        /// <summary>Whether reload input has been triggered.</summary>
        public bool reloadTrigger;

        /// <summary>Whether sprint input is currently pressed or has been triggered.</summary>
        public bool sprintTrigger;

        /// <summary>Whether sprint input has been released.</summary>
        public bool sprintRelease;
    }

    /// <summary>
    /// Defines player input events, camera state, and sampled action state for a controlled actor.
    /// Character movement, animation, inventory, camera, and booster components subscribe to these events.
    /// </summary>
    public interface IPlayerInputController : IInputController
    {
        /// <summary>Occurs when movement input changes.</summary>
        event EventHandler onMoveCall;

        /// <summary>Occurs when look input changes.</summary>
        event EventHandler onLookCall;

        /// <summary>Occurs when the controller produces an actor-rotation input.</summary>
        event EventHandler onRotateCall;

        /// <summary>Occurs when fire input is pressed.</summary>
        event EventHandler onFirePressCall;

        /// <summary>Occurs when fire input is released.</summary>
        event EventHandler onFireReleaseCall;

        /// <summary>Occurs when aim input is pressed.</summary>
        event EventHandler onFireAimCall;

        /// <summary>Occurs when aim input is released.</summary>
        event EventHandler onFireAimReleaseCall;

        /// <summary>Occurs when crouch input requests a posture toggle.</summary>
        event EventHandler onCrouchToggleCall;

        /// <summary>Occurs when prone input requests a posture toggle.</summary>
        event EventHandler onProneToggleCall;

        /// <summary>Occurs when the options-menu input is triggered.</summary>
        event EventHandler onMenuOptionsCall;

        /// <summary>Occurs when jump input is triggered.</summary>
        event EventHandler onJumpInputCall;

        /// <summary>Occurs when sprint input is pressed.</summary>
        event EventHandler onSprintCall;

        /// <summary>Occurs when sprint input is released.</summary>
        event EventHandler onSprintReleaseCall;

        /// <summary>Occurs when reload input is triggered.</summary>
        event EventHandler onReloadCall;

        /// <summary>Occurs when firing-mode input is triggered.</summary>
        event EventHandler onToggleFiringModeCall;

        /// <summary>Occurs when throw input is triggered.</summary>
        event EventHandler onThrowCall;

        /// <summary>Gets the active player camera view.</summary>
        PlayerView playerView { get; }

        /// <summary>Gets or sets whether the controller processes player input.</summary>
        bool Active { get; set; }

        /// <summary>Gets or sets whether the player's crosshair is visible.</summary>
        bool CrosshairActive { get; set; }

        /// <summary>Gets the identifier of the controlled actor.</summary>
        RideID m_rideID { get; }

        /// <summary>Gets or sets the rendering depth of the player camera.</summary>
        float cameraDepth { get; set; }

        /// <summary>Gets a ray projected through the center of the player camera.</summary>
        RideRay cameraRay { get; }

        /// <summary>Gets or sets whether the controller suppresses actor-control actions for observer use.</summary>
        bool ObserverMode { get; set; }

        /// <summary>Stops capture by the player camera.</summary>
        void StopCamera();

        /// <summary>
        /// Gets the controller's latest sampled movement and action state.
        /// </summary>
        /// <returns>The current player input parameters.</returns>
        PlayerInputControllerParams GetParams();
    }
}
