using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride;

namespace VHAssets
{
    /// <summary>
    /// Procedurally controls head movement such as nods, shakes, and tilts. Gazing is NOT handled here
    /// </summary>
    public class HeadController : MonoBehaviour
    {
        #region Constants
        const float DefaultSpeed = 0.5f;
        const float DirectionDampTime = 0.25f;
        #endregion

        public enum MovementType
        {
            Nod,
            Shake,
            Tilt,
        }

        class MovementData
        {
            public MovementType m_MovementType;
            public float m_CurrentNeckRot;
            public float m_TimePassed = 0;
            public float m_TimeToComplete = 0;
            public float m_Amplitude;
            public float m_Frequency;
            public float m_NumTimes;
            //public float m_RotOffset;
            public bool m_Reverse; // used for returning back to the original orientation
            //public float m_ReverseRot; // used for storying the current rot when the reverse occured

            public MovementData(MovementType movementType, float amplitude, float frequency, float numTimes, float timeToComplete)
            {
                m_MovementType = movementType;
                m_NumTimes = numTimes;
                m_Amplitude = amplitude;
                m_Frequency = frequency;
                m_TimeToComplete = timeToComplete;
                m_Reverse = false;
            }
        }

        #region Fields
        [SerializeField] private string m_NeckTransformName = "JtSkullA";
        [SerializeField] private bool m_flipTransform = false;
        [SerializeField] private float m_NodAmplifier = 30.0f;
        [SerializeField] private float m_ShakeAmplifier = 45.0f;
        [SerializeField] private float m_TiltAmplifier = 30.0f;

        private Transform m_Neck;
        private List<MovementData> m_CurrentHeadMovements = new List<MovementData>();

        private Quaternion m_initialNeckLocalRotation;
        #endregion

        #region Functions
        void Awake()
        {
            if (!TryGetComponent(out ILoadableAsset loadedAsset))
                InitializeLoadedAsset();
        }

        public void InitializeLoadedAsset()
        {
            var activeTransforms = GetComponentsInChildren<Transform>(false);
            Transform neckTransform = Array.Find(activeTransforms, t => t.name == m_NeckTransformName);
            if (neckTransform != null)
                m_Neck = neckTransform;
            else
                Debug.LogError($"Couldn't find active neck object named {m_NeckTransformName} on character {name}. Gazing, nodding, and shaking won't work");

            if (m_Neck != null)
                m_initialNeckLocalRotation = m_Neck.localRotation;
        }

        public void ResetLoadedAsset()
        {
            // Clear any in-flight movements so LateUpdate becomes a no-op.
            m_CurrentHeadMovements.Clear();

            // Best-effort: restore the neck pose, then drop the reference to avoid holding
            // onto unloaded hierarchy transforms.
            if (m_Neck != null)
                m_Neck.localRotation = m_initialNeckLocalRotation;

            m_Neck = null;
        }

        void LateUpdate()
        {
            if (m_Neck == null)
                return;

            // IMPORTANT:
            // We intentionally apply head movements in list order (0..N-1).
            // If multiple movements affect the same axes in the same frame, later entries
            // are applied after earlier ones and therefore "win" visually.
            //
            // This loop is written to safely remove finished movements without skipping entries.
            for (int i = 0; i < m_CurrentHeadMovements.Count; i++)
            {
                var movement = m_CurrentHeadMovements[i];

                bool finished = DoHeadMovement(movement);
                if (!finished)
                    continue;

                m_CurrentHeadMovements.RemoveAt(i);

                // We removed the element at i; the next element shifts into i.
                // Decrement i so the loop processes the new element at this index.
                i--;
            }
        }

        public void NodHead(float amount, float numTimes, float duration) => CreateHeadMovement(MovementType.Nod, amount, numTimes, duration);

        public void ShakeHead(float amount, float numTimes, float duration) => CreateHeadMovement(MovementType.Shake, amount, numTimes, duration);

        public void TiltHead(float amount, float numTimes, float duration) => CreateHeadMovement(MovementType.Tilt, amount, numTimes, duration);

        void CreateHeadMovement(MovementType type, float amount, float numTimes, float duration)
        {
            duration = Mathf.Abs(duration);
            //int index = m_CurrentHeadMovements.FindIndex(m => m.m_MovementType == type);
            //if (index != -1)
            //{
            //    // a movement of this type is already happening, disregard it
            //    Debug.LogWarningFormat("There is already a head movement of type {0}. Wait for it to finish befure issuing a movement of the same type", type);
            //    return;
            //}

            amount = Mathf.Clamp(amount, -1, 1);
            if (amount == 0)
                amount = DefaultSpeed;

            float amplitude = 1;
            float frequency = 1;

            switch (type)
            {
                case MovementType.Nod:
                    amplitude = amount * m_NodAmplifier;
                    frequency = (Mathf.PI * 2.0f) / (duration);
                    break;

                case MovementType.Shake:
                    amplitude = amount * m_ShakeAmplifier;
                    frequency = (Mathf.PI * 2.0f) / (duration);
                    break;

                case MovementType.Tilt:
                default:
                    amplitude = amount * m_TiltAmplifier;
                    frequency = (Mathf.PI * 2.0f) / (duration);
                    break;
            }

            m_CurrentHeadMovements.Insert(0, new MovementData(type, amplitude, frequency, numTimes, duration));
        }

        bool DoHeadMovement(MovementData movementData)
        {
            bool isMovementFinished = false;
            float t = movementData.m_TimePassed / movementData.m_TimeToComplete;

            if (movementData.m_Reverse)
                movementData.m_CurrentNeckRot = Mathf.SmoothStep(movementData.m_CurrentNeckRot, 0, t);
            else
                movementData.m_CurrentNeckRot = movementData.m_Amplitude * Mathf.Sin((t) * 2.0f * Mathf.PI * movementData.m_NumTimes);
        
            movementData.m_TimePassed += Time.deltaTime;

            m_Neck.transform.Rotate(GetRotationAxis(movementData.m_MovementType), movementData.m_CurrentNeckRot, Space.World);

            if (movementData.m_TimePassed >= movementData.m_TimeToComplete)
                isMovementFinished = true;

            return isMovementFinished;
        }

        /// <summary>
        /// Stops all current head movements and gracefully returns the neck back to its original orientation
        /// </summary>
        public void Stop()
        {
            foreach (var movement in m_CurrentHeadMovements)
            {
                movement.m_Reverse = true;
                movement.m_TimePassed = 0;
                movement.m_TimeToComplete = DirectionDampTime;
            }
        }

        Vector3 GetRotationAxis(MovementType type)
        {
            var axis = m_Neck.forward;
            if (type == MovementType.Nod)
            {
                if (m_flipTransform)
                    axis = m_Neck.right;
                else
                    axis = m_Neck.forward;
            }
            else if (type == MovementType.Shake)
            {
                if (m_flipTransform)
                    axis = m_Neck.up;
                else
                    axis = m_Neck.right;
            }
            else
            {
                axis = m_Neck.up;
            }

            return axis;
        }

#if false
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                NodHead(1, 2, 3);
            if (Input.GetKeyDown(KeyCode.Alpha2))
                NodHead(2, 4, 6);
            if (Input.GetKeyDown(KeyCode.Alpha3))
                Stop();
        }
#endif
        #endregion
    }
}
