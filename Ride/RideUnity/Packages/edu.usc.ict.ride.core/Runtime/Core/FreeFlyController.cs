// Part of the Ride API
// Copyright - USC Institute for Creative Technologies (https://ict.usc.edu)

using UnityEngine;

namespace Ride
{
    /// <summary>
    /// A lightweight, flythrough controller for navigating freely in 3D space using WASD + mouse look.
    /// Attach this component to a camera or camera parent to allow manual movement and orientation similar to a first-person
    /// flycam. Mouse look can be toggled at runtime, and movement speed can be shifted for faster/slower travel.
    ///
    /// This controller is intended for simulation or scene authoring workflows. It respects user input focus
    /// to avoid movement during UI interaction. 
    ///
    /// Features:
    /// - WASD + QE movement Toggle mouse look with 'J'. Hold shift for alternate speed.
    /// - Mouse look with clamped rotation (toggleable)
    /// - Adjustable movement and look sensitivity
    /// - Optional integration with terrain-aware speed controllers
    ///
    /// Usage: Add this component to your main camera or a camera rig GameObject. Assign movement keys and configure
    /// sensitivity and bounds as needed.
    /// </summary>
    public class FreeFlyController : RideMonoBehaviour
    {
        #region Variables
        public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }

        [Header("Rotation Settings")]
        public RotationAxes axes = RotationAxes.MouseXAndY;
        public float sensitivityX = 15;
        public float sensitivityY = 15;
        public bool m_CameraRotationOn = false;

        [Header("Movement Settings")]
        public float movementSpeed = 5;
        public float secondaryMovementSpeed = 2.5f;

        [Header("Rotation Limits")]
        public float minimumX = -360;
        public float maximumX = 360;
        public float minimumY = -60;
        public float maximumY = 60;

        [Header("Key Bindings")]
        public KeyCode[] m_MoveForwardKeys = { KeyCode.W, KeyCode.UpArrow };
        public KeyCode[] m_MoveBackwardKeys = { KeyCode.S, KeyCode.DownArrow };
        public KeyCode[] m_MoveLeftKeys = { KeyCode.A, KeyCode.LeftArrow };
        public KeyCode[] m_MoveRightKeys = { KeyCode.D, KeyCode.RightArrow };
        public KeyCode[] m_MoveUpKeys = { KeyCode.E };
        public KeyCode[] m_MoveDownKeys = { KeyCode.Q };
        public KeyCode[] m_ToggleMouseLookKeys = { KeyCode.J };

        public delegate void MovementCallback();

        protected float rotationX = 0;
        protected float rotationY = 0;
        #endregion

        #region Functions
        /// <summary>
        /// Initializes the controller by freezing Rigidbody rotation (if present) and capturing the initial local Euler angles.
        /// This helps ensure smooth mouse look transitions by avoiding sudden rotational jumps when toggling rotation on.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            var rigidBody = GetComponent<Rigidbody>();
            if (rigidBody)
                rigidBody.freezeRotation = true;

            rotationX = transform.localRotation.eulerAngles.y;
            rotationY = transform.localRotation.eulerAngles.x;
        }

        /// <summary>
        /// Handles input polling each frame for both mouse-based rotation (if enabled) and directional movement.
        /// Rotation and translation are clamped and applied in local space. Movement is suppressed when any UI element has input focus.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if (RideUtils.DoesInputHaveFocus())
                return;

            if (m_CameraRotationOn)
            {
                if (axes == RotationAxes.MouseXAndY)
                {
                    rotationY += Input.GetAxis("Mouse Y") * -sensitivityY;
                    rotationX += Input.GetAxis("Mouse X") * sensitivityX;

                    rotationY = ClampAngle(rotationY, minimumY, maximumY);
                    rotationX = ClampAngle(rotationX, minimumX, maximumX);

                    Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.right);
                    Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);

                    transform.localRotation = xQuaternion * yQuaternion;
                }
                else if (axes == RotationAxes.MouseX)
                {
                    rotationX += Input.GetAxis("Mouse X") * sensitivityX;
                    rotationX = ClampAngle(rotationX, minimumX, maximumX);

                    Quaternion xQuaternion = Quaternion.AngleAxis(rotationX, Vector3.up);
                    transform.localRotation = xQuaternion;
                }
                else
                {
                    rotationY += Input.GetAxis("Mouse Y") * -sensitivityY;

                    rotationY = ClampAngle(rotationY, minimumY, maximumY);

                    Quaternion yQuaternion = Quaternion.AngleAxis(rotationY, Vector3.right);
                    transform.localRotation = yQuaternion;
                }
            }

            CheckKeyPress(m_MoveForwardKeys, MoveForward);
            CheckKeyPress(m_MoveBackwardKeys, MoveBackward);
            CheckKeyPress(m_MoveLeftKeys, MoveLeft);
            CheckKeyPress(m_MoveRightKeys, MoveRight);
            CheckKeyPress(m_MoveUpKeys, MoveUp);
            CheckKeyPress(m_MoveDownKeys, MoveDown);
            CheckKeyDown(m_ToggleMouseLookKeys, ToggleMouseLook);
        }

        /// <summary>
        /// Returns the current movement speed, using the secondary speed if shift is held.
        /// </summary>
        /// <returns>Movement speed in units per second.</returns>
        protected float GetMovementSpeed() => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? secondaryMovementSpeed : movementSpeed;

        public virtual void MoveForward() => transform.localPosition += GetMovementSpeed() * Time.unscaledDeltaTime * transform.forward;
        public virtual void MoveBackward() => transform.localPosition += GetMovementSpeed() * Time.unscaledDeltaTime * -transform.forward;
        public virtual void MoveLeft() => transform.localPosition -= GetMovementSpeed() * Time.unscaledDeltaTime * transform.right;
        public virtual void MoveRight() => transform.localPosition += GetMovementSpeed() * Time.unscaledDeltaTime * transform.right;
        public virtual void MoveUp() => transform.localPosition += GetMovementSpeed() * Time.unscaledDeltaTime * transform.up;
        public virtual void MoveDown() => transform.localPosition -= GetMovementSpeed() * Time.unscaledDeltaTime * transform.up;

        /// <summary>
        /// Toggles mouse look mode on or off. When enabled, initializes the internal rotation state from the current transform.
        /// Euler angles are converted to signed representation where needed to avoid wrap-around issues when clamping.
        /// </summary>
        public void ToggleMouseLook()
        {
            m_CameraRotationOn = !m_CameraRotationOn;

            if (m_CameraRotationOn)
            {
                rotationX = transform.localRotation.eulerAngles.y;
                if (rotationX > maximumX)
                    rotationX = -(360 - rotationX);

                rotationY = transform.localRotation.eulerAngles.x;
                if (rotationY > maximumY)
                    rotationY = -(360 - rotationY);
            }
        }

        /// <summary>
        /// Invokes the provided movement callback if any key in the set is currently pressed.
        /// </summary>
        /// <param name="movementKeys">Set of keys to check.</param>
        /// <param name="cb">Callback to invoke when a key is pressed.</param>
        public void CheckKeyPress(KeyCode[] movementKeys, MovementCallback cb)
        {
            if (movementKeys == null)
                return;

            foreach (var key in movementKeys)
            {
                if (Input.GetKey(key))
                {
                    cb();
                    break;
                }
            }
        }

        /// <summary>
        /// Invokes the provided movement callback if any key in the set was pressed down this frame.
        /// </summary>
        /// <param name="movementKeys">Set of keys to check.</param>
        /// <param name="cb">Callback to invoke when a key is pressed down.</param>
        protected void CheckKeyDown(KeyCode[] movementKeys, MovementCallback cb)
        {
            if (movementKeys == null)
                return;

            foreach (var key in movementKeys)
            {
                if (Input.GetKeyDown(key))
                {
                    cb();
                    break;
                }
            }
        }

        /// <summary>
        /// Safely clamps an input angle between a minimum and maximum value, after wrapping into a 360-degree cycle.
        /// This method was introduced to work around Unity's legacy behavior where negative Euler angles were discarded or
        /// auto-wrapped, which could cause erratic movement when toggling rotation modes.
        /// 
        /// Only call this when working directly with user-controlled Euler angle state.
        /// </summary>
        /// <param name="angle">Raw angle input in degrees</param>
        /// <param name="min">Minimum allowed angle (typically negative)</param>
        /// <param name="max">Maximum allowed angle</param>
        /// <returns>The clamped angle with corrected wrapping</returns>
        public static float ClampAngle(float angle, float min, float max)
        {
            angle %= 360;
            if ((angle >= -360F) && (angle <= 360F))
            {
                if (angle < -360F)
                    angle += 360F;
                if (angle > 360F)
                    angle -= 360F;
            }

            return ClampRot(angle, min, max);
        }

        /// <summary>
        /// Provides custom clamping logic tailored to Unity's non-negative rotation normalization.
        /// Used internally by <see cref="ClampAngle"/> to ensure consistency when Unity remaps angles behind the scenes.
        /// </summary>
        /// <param name="rot">Input rotation value</param>
        /// <param name="min">Minimum allowed rotation</param>
        /// <param name="max">Maximum allowed rotation</param>
        /// <returns>Clamped rotation value</returns>
        public static float ClampRot(float rot, float min, float max)
        {
            // unity isn't doing negative rotations anymore, so the angles have to clamped in a different way
            if (rot < -max)
                rot = -max;
            else if (rot > -min)
                rot = -min;

            return rot;
        }

        #endregion
    }
}
