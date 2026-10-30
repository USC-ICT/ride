using System;
using System.Collections;
using System.Collections.Generic;

namespace Ride.IO
{
    /// <summary>
    /// Provides controller registration, controller-to-entity attachment, input-layer filtering, and raw device queries.
    /// RIDE UI, camera, player, and vehicle components use this system as the shared input access point.
    /// </summary>
    /// <remarks>
    /// Raw axis, key, and mouse queries follow Unity's
    /// <see href="https://docs.unity3d.com/ScriptReference/Input.html">legacy Input API</see> semantics.
    /// Overloads accepting a <see cref="RideInputLayer"/> suppress input while that layer is inactive.
    /// </remarks>
    public interface IInputSystem : IRideSystem
    {
        /// <summary>Gets the mouse cursor position in screen coordinates.</summary>
        RideVector2 mousePosition { get; }

        /// <summary>
        /// Gets the single-actor input controller registered for an actor.
        /// </summary>
        /// <param name="id">The actor identifier.</param>
        /// <returns>The registered controller, or <c>null</c> when none exists.</returns>
        IInputController GetInputController(RideID id);

        /// <summary>
        /// Gets the multi-actor input controller registered with an identifier.
        /// </summary>
        /// <param name="id">The controller identifier.</param>
        /// <returns>The registered controller, or <c>null</c> when none exists.</returns>
        IInputControllerNew GetInputControllerNew(RideID id);

        /// <summary>
        /// Gets a specific controllable capability registered for an entity.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="controllableType">The controllable category to retrieve.</param>
        /// <returns>The matching controllable, or <c>null</c> when none exists.</returns>
        IInputControllable GetInputControllable(RideID id, InputControlType controllableType);

        /// <summary>
        /// Attaches a registered multi-actor controller to the controllable capabilities of an entity.
        /// </summary>
        /// <param name="controllerId">The controller identifier.</param>
        /// <param name="controllableId">The controllable entity identifier.</param>
        /// <returns><c>true</c> when the attachment request is accepted; otherwise, <c>false</c>.</returns>
        bool AttachControllerToControllable(RideID controllerId, RideID controllableId);

        /// <summary>
        /// Detaches a controller from all entities assigned to it.
        /// </summary>
        /// <param name="controllerId">The controller identifier.</param>
        void DetachController(RideID controllerId);

        /// <summary>
        /// Detaches an entity's controllable capabilities from their controller.
        /// </summary>
        /// <param name="controllableId">The controllable entity identifier.</param>
        void DetachControllable(RideID controllableId);

        /// <summary>
        /// Determines whether a multi-actor controller is registered.
        /// </summary>
        /// <param name="id">The controller identifier.</param>
        /// <returns><c>true</c> when the controller is registered; otherwise, <c>false</c>.</returns>
        bool HasController(RideID id);

        /// <summary>
        /// Determines whether an entity has any registered controllable capabilities.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns><c>true</c> when at least one controllable is registered; otherwise, <c>false</c>.</returns>
        bool HasControllable(RideID id);

        /// <summary>
        /// Determines whether an entity has a registered controllable of a specific category.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="controllableType">The controllable category to find.</param>
        /// <returns><c>true</c> when the matching controllable is registered; otherwise, <c>false</c>.</returns>
        bool HasControllable(RideID id, InputControlType controllableType);

        /// <summary>
        /// Gets the player input controller registered for an agent.
        /// </summary>
        /// <param name="agentId">The agent identifier.</param>
        /// <returns>The player controller, or <c>null</c> when none exists.</returns>
        IPlayerInputController GetPlayerInputController(RideID agentId);

        /// <summary>
        /// Determines whether an agent has an active player input controller.
        /// </summary>
        /// <param name="agentId">The agent identifier.</param>
        /// <returns><c>true</c> when a player controller exists and is active; otherwise, <c>false</c>.</returns>
        bool HasActivePlayerInputController(RideID agentId);

        /// <summary>
        /// Determines whether an agent has a registered player input controller.
        /// </summary>
        /// <param name="agentId">The agent identifier.</param>
        /// <returns><c>true</c> when a player controller exists; otherwise, <c>false</c>.</returns>
        bool HasExistingPlayerInputController(RideID agentId);

        /// <summary>
        /// Associates an existing player input controller with an agent.
        /// </summary>
        /// <param name="agentId">The agent identifier.</param>
        /// <param name="inputController">The player controller to associate.</param>
        /// <returns><c>true</c> when the controller is associated; otherwise, <c>false</c>.</returns>
        bool AttachPlayerInputController(RideID agentId, IPlayerInputController inputController);

        /// <summary>
        /// Creates and associates a player input controller for an agent when supported by the implementation.
        /// </summary>
        /// <param name="agentId">The agent identifier.</param>
        /// <returns><c>true</c> when a controller is created and associated; otherwise, <c>false</c>.</returns>
        bool AddPlayerInputController(RideID agentId);

        /// <summary>
        /// Enables or disables an agent's player input controller.
        /// </summary>
        /// <param name="agentId">The agent identifier.</param>
        /// <param name="enable"><c>true</c> to enable the controller; <c>false</c> to disable it.</param>
        void TogglePlayerInputController(RideID agentId, bool enable);

        /// <summary>
        /// Gets all controllable capabilities registered for an entity.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns>The registered controllables, or <c>null</c> when the entity has none.</returns>
        IEnumerable<IInputControllable> GetControllables(RideID id);

        /// <summary>
        /// Gets the current value of a configured input axis.
        /// </summary>
        /// <param name="axisName">The configured axis name.</param>
        /// <returns>The current axis value.</returns>
        float GetAxis(string axisName);

        /// <summary>
        /// Gets the current value of a configured input axis when an input layer is active.
        /// </summary>
        /// <param name="axisName">The configured axis name.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns>The current axis value, or <c>0</c> when the layer is inactive.</returns>
        float GetAxis(string axisName, RideInputLayer layer);

        /// <summary>
        /// Determines whether a key is currently held.
        /// </summary>
        /// <param name="keyCode">The key to query.</param>
        /// <returns><c>true</c> while the key is held; otherwise, <c>false</c>.</returns>
        bool GetKey(RideKeyCode keyCode);

        /// <summary>
        /// Determines whether a key is currently held and an input layer is active.
        /// </summary>
        /// <param name="keyCode">The key to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> while the key is held and the layer is active; otherwise, <c>false</c>.</returns>
        bool GetKey(RideKeyCode keyCode, RideInputLayer layer);

        /// <summary>
        /// Determines whether a key was pressed during the current frame.
        /// </summary>
        /// <param name="keyCode">The key to query.</param>
        /// <returns><c>true</c> during the frame the key is pressed; otherwise, <c>false</c>.</returns>
        bool GetKeyDown(RideKeyCode keyCode);

        /// <summary>
        /// Determines whether a key was pressed during the current frame and an input layer is active.
        /// </summary>
        /// <param name="keyCode">The key to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> during the press frame when the layer is active; otherwise, <c>false</c>.</returns>
        bool GetKeyDown(RideKeyCode keyCode, RideInputLayer layer);

        /// <summary>
        /// Determines whether a key was released during the current frame.
        /// </summary>
        /// <param name="keyCode">The key to query.</param>
        /// <returns><c>true</c> during the frame the key is released; otherwise, <c>false</c>.</returns>
        bool GetKeyUp(RideKeyCode keyCode);

        /// <summary>
        /// Determines whether a key was released during the current frame and an input layer is active.
        /// </summary>
        /// <param name="keyCode">The key to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> during the release frame when the layer is active; otherwise, <c>false</c>.</returns>
        bool GetKeyUp(RideKeyCode keyCode, RideInputLayer layer);

        /// <summary>
        /// Determines whether a mouse button is currently held.
        /// </summary>
        /// <param name="mouseButton">The zero-based mouse button index.</param>
        /// <returns><c>true</c> while the button is held; otherwise, <c>false</c>.</returns>
        bool GetMouseButton(int mouseButton);

        /// <summary>
        /// Determines whether a mouse button is currently held and an input layer is active.
        /// </summary>
        /// <param name="mouseButton">The zero-based mouse button index.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> while the button is held and the layer is active; otherwise, <c>false</c>.</returns>
        bool GetMouseButton(int mouseButton, RideInputLayer layer);

        /// <summary>
        /// Determines whether a mouse button was pressed during the current frame.
        /// </summary>
        /// <param name="mouseButton">The zero-based mouse button index.</param>
        /// <returns><c>true</c> during the frame the button is pressed; otherwise, <c>false</c>.</returns>
        bool GetMouseButtonDown(int mouseButton);

        /// <summary>
        /// Determines whether a mouse button was pressed during the current frame and an input layer is active.
        /// </summary>
        /// <param name="mouseButton">The zero-based mouse button index.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> during the press frame when the layer is active; otherwise, <c>false</c>.</returns>
        bool GetMouseButtonDown(int mouseButton, RideInputLayer layer);

        /// <summary>
        /// Determines whether a mouse button was released during the current frame.
        /// </summary>
        /// <param name="mouseButton">The zero-based mouse button index.</param>
        /// <returns><c>true</c> during the frame the button is released; otherwise, <c>false</c>.</returns>
        bool GetMouseButtonUp(int mouseButton);

        /// <summary>
        /// Determines whether a mouse button was released during the current frame and an input layer is active.
        /// </summary>
        /// <param name="mouseButton">The zero-based mouse button index.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> during the release frame when the layer is active; otherwise, <c>false</c>.</returns>
        bool GetMouseButtonUp(int mouseButton, RideInputLayer layer);

        /// <summary>
        /// Determines whether all specified keys are currently held.
        /// </summary>
        /// <param name="keyCodes">The keys to query.</param>
        /// <returns><c>true</c> when every key is held; otherwise, <c>false</c>.</returns>
        bool GetKeys(RideKeyCode[] keyCodes);

        /// <summary>
        /// Determines whether all specified keys are held and an input layer is active.
        /// </summary>
        /// <param name="keyCodes">The keys to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> when every key is held and the layer is active; otherwise, <c>false</c>.</returns>
        bool GetKeys(RideKeyCode[] keyCodes, RideInputLayer layer);

        /// <summary>
        /// Determines whether all specified keys were pressed during the current frame.
        /// </summary>
        /// <param name="keyCodes">The keys to query.</param>
        /// <returns><c>true</c> when every key was pressed this frame; otherwise, <c>false</c>.</returns>
        bool GetKeysDown(RideKeyCode[] keyCodes);

        /// <summary>
        /// Determines whether all specified keys were pressed this frame and an input layer is active.
        /// </summary>
        /// <param name="keyCodes">The keys to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> when every key was pressed this frame and the layer is active; otherwise, <c>false</c>.</returns>
        bool GetKeysDown(RideKeyCode[] keyCodes, RideInputLayer layer);

        /// <summary>
        /// Determines whether all specified keys were released during the current frame.
        /// </summary>
        /// <param name="keyCodes">The keys to query.</param>
        /// <returns><c>true</c> when every key was released this frame; otherwise, <c>false</c>.</returns>
        bool GetKeysUp(RideKeyCode[] keyCodes);

        /// <summary>
        /// Determines whether all specified keys were released this frame and an input layer is active.
        /// </summary>
        /// <param name="keyCodes">The keys to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> when every key was released this frame and the layer is active; otherwise, <c>false</c>.</returns>
        bool GetKeysUp(RideKeyCode[] keyCodes, RideInputLayer layer);

        /// <summary>
        /// Determines whether all specified mouse buttons are currently held.
        /// </summary>
        /// <param name="mouseButtons">The zero-based mouse button indices to query.</param>
        /// <returns><c>true</c> when every button is held; otherwise, <c>false</c>.</returns>
        bool GetMouseButtons(int[] mouseButtons);

        /// <summary>
        /// Determines whether all specified mouse buttons are held and an input layer is active.
        /// </summary>
        /// <param name="mouseButtons">The zero-based mouse button indices to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> when every button is held and the layer is active; otherwise, <c>false</c>.</returns>
        bool GetMouseButtons(int[] mouseButtons, RideInputLayer layer);

        /// <summary>
        /// Determines whether all specified mouse buttons were pressed during the current frame.
        /// </summary>
        /// <param name="mouseButtons">The zero-based mouse button indices to query.</param>
        /// <returns><c>true</c> when every button was pressed this frame; otherwise, <c>false</c>.</returns>
        bool GetMouseButtonsDown(int[] mouseButtons);

        /// <summary>
        /// Determines whether all specified mouse buttons were pressed this frame and an input layer is active.
        /// </summary>
        /// <param name="mouseButtons">The zero-based mouse button indices to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> when every button was pressed this frame and the layer is active; otherwise, <c>false</c>.</returns>
        bool GetMouseButtonsDown(int[] mouseButtons, RideInputLayer layer);

        /// <summary>
        /// Determines whether all specified mouse buttons were released during the current frame.
        /// </summary>
        /// <param name="mouseButtons">The zero-based mouse button indices to query.</param>
        /// <returns><c>true</c> when every button was released this frame; otherwise, <c>false</c>.</returns>
        bool GetMouseButtonsUp(int[] mouseButtons);

        /// <summary>
        /// Determines whether all specified mouse buttons were released this frame and an input layer is active.
        /// </summary>
        /// <param name="mouseButtons">The zero-based mouse button indices to query.</param>
        /// <param name="layer">The input layer that must be active.</param>
        /// <returns><c>true</c> when every button was released this frame and the layer is active; otherwise, <c>false</c>.</returns>
        bool GetMouseButtonsUp(int[] mouseButtons, RideInputLayer layer);

        /// <summary>
        /// Gets all keys that are currently held.
        /// </summary>
        /// <returns>The currently held keys.</returns>
        RideKeyCode[] GetKeysPressed();

        /// <summary>
        /// Gets all keys pressed during the current frame.
        /// </summary>
        /// <returns>The keys pressed during the current frame.</returns>
        RideKeyCode[] GetKeysPressedDown();

        /// <summary>
        /// Gets all keys released during the current frame.
        /// </summary>
        /// <returns>The keys released during the current frame.</returns>
        RideKeyCode[] GetKeysPressedUp();

        /// <summary>
        /// Enables or disables input for a contextual input layer.
        /// </summary>
        /// <param name="layer">The input layer to modify.</param>
        /// <param name="isOn"><c>true</c> to accept layer input; <c>false</c> to suppress it.</param>
        void SetInputLayer(RideInputLayer layer, bool isOn);

        /// <summary>
        /// Determines whether a contextual input layer is active.
        /// </summary>
        /// <param name="layer">The input layer to query.</param>
        /// <returns><c>true</c> when the layer accepts input; otherwise, <c>false</c>.</returns>
        bool GetInputLayerActive(RideInputLayer layer);

        /// <summary>
        /// Determines whether a mouse button has been held longer than the implementation's long-press threshold.
        /// </summary>
        /// <param name="mouseButton">The zero-based mouse button index.</param>
        /// <returns><c>true</c> when the button has exceeded the hold threshold; otherwise, <c>false</c>.</returns>
        bool IsMouseButtonLongDown(int mouseButton);

        /// <summary>
        /// Gets the locomotion input controller registered for an entity.
        /// </summary>
        /// <param name="locomotionID">The locomotion entity identifier.</param>
        /// <returns>The locomotion controller, or <c>null</c> when none exists.</returns>
        ILocomotionInputController GetLocomotionController(RideID locomotionID);
    }

    /// <summary>Represents an empty input-system data payload.</summary>
    [Serializable]
    public struct InputData
    {
    }
}
