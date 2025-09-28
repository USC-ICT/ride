using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.IO
{
    public interface IInputSystem : IRideSystem
    {
        /// <summary>
        /// Gets the position of the mouse on the screen
        /// </summary>
        /// <returns>TSSVector2 with X and Y position of the mouse cursor on the screen</returns>
        RideVector2 mousePosition { get; }

        /// <summary>
        /// Gets the general IInputController interface for any actor
        /// </summary>
        /// <param name="id">The actor id</param>
        /// <returns>Actor's IInputController interface. Null if no interface exists</returns>
        IInputController GetInputController(RideID id);

        IInputControllerNew GetInputControllerNew(RideID id);

        IInputControllable GetInputControllable(RideID id, InputControlType controllableType);

        bool AttachControllerToControllable(RideID controllerId, RideID controllableId);

        void DetachController(RideID controllerId);

        void DetachControllable(RideID controllableId);

        bool HasController(RideID id);

        bool HasControllable(RideID id);

        bool HasControllable(RideID id, InputControlType controllableType);

        /// <summary>
        /// Gets the IPlayerInputController interface for the agent
        /// </summary>
        /// <param name="agentId">The agent id</param>
        /// <returns>Agent's IPlayerInputController interface. Null if no interface exists</returns>
        IPlayerInputController GetPlayerInputController(RideID agentId);

        /// <summary>
        /// Checks if the agent has an active IPlayerInputController
        /// </summary>
        /// <param name="agentId">The agent</param>
        /// <returns>True if agent has an active IPlayerInputController</returns>
        bool HasActivePlayerInputController(RideID agentId);

        /// <summary>
        /// Checks if the agent has an existing IPlayerInputController
        /// </summary>
        /// <param name="agentId">The agent</param>
        /// <returns>True if agent has an existing IPlayerInputController</returns>
        bool HasExistingPlayerInputController(RideID agentId);

        /// <summary>
        /// Links a IPlayerInputController to an agent
        /// </summary>
        /// <param name="agentId">The agent</param>
        /// <param name="inputController">The IPlayerInputController object</param>
        /// <returns>True if attachment is successful</returns>
        bool AttachPlayerInputController(RideID agentId, IPlayerInputController inputController);

        /// <summary>
        /// Adds a IPlayerInputController to an agent
        /// </summary>
        /// <param name="agentId">The agent</param>
        /// <returns>True if adding a new IPlayerInputController is successful</returns>
        /// *** CURRENTLY NOT IMPLEMENTED YET ***
        bool AddPlayerInputController(RideID agentId);

        /// <summary>
        /// Toggles on/off the IPlayerInputController for an agent
        /// </summary>
        /// <param name="agentId">The agent</param>
        /// <param name="enable">The toggle on or off (true/false)</param>
        void TogglePlayerInputController(RideID agentId, bool enable);

        /// <summary>
        /// Gets the input controllables of the object
        /// </summary>
        /// <param name="id">object ID</param>
        /// <returns>The input controllables of the object</returns>
        IEnumerable<IInputControllable> GetControllables(RideID id);

        /// <summary>
        /// Checks the position of a specific axis, i.e. how left or right the left joystick is.
        /// </summary>
        /// <param name="axisName">The axis</param>
        /// <returns>position of the axis.</returns>
        float GetAxis(string axisName);

        /// <summary>
        /// Checks the position of a specific axis, and if a specific input layer is active.
        /// </summary>
        /// <param name="axisName">The axis</param>
        /// <param name="layer">The layer being checked is active</param>
        /// <returns>position of the axis. Should return 0 is layer is inactive.</returns>
        float GetAxis(string axisName, RideInputLayer layer);

        /// <summary>
        /// Checks if a specific key is pressed
        /// </summary>
        /// <param name="keyCode">The key being checked is pressed</param>
        /// <returns>True if key is being pressed</returns>
        bool GetKey(RideKeyCode keyCode);

        /// <summary>
        /// Checks if a specific key is pressed, and if a specific input layer is active.
        /// </summary>
        /// <param name="keyCode">The key being checked is pressed</param>
        /// <param name="layer">The layer being checked is active</param>
        /// <returns>True if key is being pressed and layer is on.</returns>
        bool GetKey(RideKeyCode keyCode, RideInputLayer layer);

        /// <summary>
        /// Checks if a specific key is pressed down
        /// </summary>
        /// <param name="keyCode">The key being checked is pressed down</param>
        /// <returns>True if key is being pressed down</returns>
        bool GetKeyDown(RideKeyCode keyCode);

        /// <summary>
        /// Checks if a specific key is pressed down, and if a specific input layer is active.
        /// </summary>
        /// <param name="keyCode">The key being checked is pressed down</param>
        /// <param name="layer">The layer being checked is active</param>
        /// <returns>True if key is being pressed down and layer is on.</returns>
        bool GetKeyDown(RideKeyCode keyCode, RideInputLayer layer);

        /// <summary>
        /// Checks if a specific key is pressed up
        /// </summary>
        /// <param name="keyCode">The key being checked is pressed up</param>
        /// <returns>True if key is being pressed up</returns>
        bool GetKeyUp(RideKeyCode keyCode);

        bool GetKeyUp(RideKeyCode keyCode, RideInputLayer layer);

        /// <summary>
        /// Checks if a specific mouse button is pressed
        /// </summary>
        /// <param name="mouseButton">The mouse button being checked is pressed</param>
        /// <returns>True if mouse button is being pressed</returns>
        bool GetMouseButton(int mouseButton);
        bool GetMouseButton(int mouseButton, RideInputLayer layer);

        /// <summary>
        /// Checks if a specific mouse button is pressed down
        /// </summary>
        /// <param name="mouseButton">The mouse button being checked is pressed down</param>
        /// <returns>True if mouse button is being pressed down</returns>
        bool GetMouseButtonDown(int mouseButton);
        bool GetMouseButtonDown(int mouseButton, RideInputLayer layer);

        /// <summary>
        /// Checks if a specific mouse button is pressed up
        /// </summary>
        /// <param name="mouseButton">The mouse button being checked is pressed up</param>
        /// <returns>True if mouse button is being pressed up</returns>
        bool GetMouseButtonUp(int mouseButton);
        bool GetMouseButtonUp(int mouseButton, RideInputLayer layer);

        /// <summary>
        /// Checks if a series of keys is pressed
        /// </summary>
        /// <param name="keyCodes">The keys being checked are pressed</param>
        /// <returns>True if keys are being pressed</returns>
        bool GetKeys(RideKeyCode[] keyCodes);
        bool GetKeys(RideKeyCode[] keyCodes, RideInputLayer layer);

        /// <summary>
        /// Checks if a series of keys is pressed down
        /// </summary>
        /// <param name="keyCodes">The keys being checked are pressed down</param>
        /// <returns>True if keys are being pressed down</returns>
        bool GetKeysDown(RideKeyCode[] keyCodes);
        bool GetKeysDown(RideKeyCode[] keyCodes, RideInputLayer layer);

        /// <summary>
        /// Checks if a series of keys is pressed up
        /// </summary>
        /// <param name="keyCodes">The keys being checked are pressed up</param>
        /// <returns>True if keys are being pressed up</returns>
        bool GetKeysUp(RideKeyCode[] keyCodes);
        bool GetKeysUp(RideKeyCode[] keyCodes, RideInputLayer layer);

        /// <summary>
        /// Checks if a series of mouse buttons is pressed
        /// </summary>
        /// <param name="keyCodes">The mosue buttons being checked are pressed</param>
        /// <returns>True if mouse buttons are being pressed</returns>
        bool GetMouseButtons(int[] mouseButtons);
        bool GetMouseButtons(int[] mouseButtons, RideInputLayer layer);

        /// <summary>
        /// Checks if a series of mouse buttons is pressed down
        /// </summary>
        /// <param name="keyCodes">The mosue buttons being checked are pressed down</param>
        /// <returns>True if mouse buttons are being pressed down</returns>
        bool GetMouseButtonsDown(int[] mouseButtons);
        bool GetMouseButtonsDown(int[] mouseButtons, RideInputLayer layer);

        /// <summary>
        /// Checks if a series of mouse buttons is pressed up
        /// </summary>
        /// <param name="keyCodes">The mosue buttons being checked are pressed up</param>
        /// <returns>True if mouse buttons are being pressed up</returns>
        bool GetMouseButtonsUp(int[] mouseButtons);
        bool GetMouseButtonsUp(int[] mouseButtons, RideInputLayer layer);

        /// <summary>
        /// Gets an array of keys that are being pressed
        /// </summary>
        /// <returns>Array of keys that are being pressed</returns>
        RideKeyCode[] GetKeysPressed();

        /// <summary>
        /// Gets an array of keys that are being pressed down
        /// </summary>
        /// <returns>Array of keys that are being pressed down</returns>
        RideKeyCode[] GetKeysPressedDown();

        /// <summary>
        /// Gets an array of keys that are being pressed up
        /// </summary>
        /// <returns>Array of keys that are being pressed up</returns>
        RideKeyCode[] GetKeysPressedUp();

        /// <summary>
        /// Sets bitmask for ignoring input of a certain context.
        /// </summary>
        /// <param name="layer">The layer to be affected.</param>
        /// <param name="isOn">True: layer is active. False: layer is inactive, input of this context will be ignored.</param>
        void SetInputLayer(RideInputLayer layer, bool isOn);

        /// <summary>
        /// Returns false if input to the given layer is being ignored.
        /// </summary>
        /// <param name="layer"></param>
        /// <returns></returns>
        bool GetInputLayerActive(RideInputLayer layer);

        /// <summary>
        /// Returns true if it's been a while since the mouse button was pressed down. Use this to see if a mouse up represents a click or a hold.
        /// </summary>
        /// <param name="mouseButton">Mouse button.</param>
        /// <returns></returns>
        bool IsMouseButtonLongDown(int mouseButton);


        /// **********************************
        /// VEHICLE INPUT CONTROLLER FUNCTIONS
        /// **********************************

        ILocomotionInputController GetLocomotionController(RideID locomotionID);
    }

    [Serializable]
    public struct InputData
    {
    }
}
