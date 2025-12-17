using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class GazeController_LookAt : GazeController
{
    #region Constants
    [Serializable]
    public class GazeObject
    {
        public enum Axis
        {
            X,
            Y,
            Z
        }

        public string m_Name = "";
        public string m_JointTransformName = "";
        [Range(0, 1)] public float m_Weight = 1;
        public Axis m_LookAxis = Axis.Z;

        [HideInInspector] public Transform m_Gazer;
        [HideInInspector] public GameObject m_AnimationRef;
        [HideInInspector] public GameObject m_GazeRef;
        [HideInInspector] public float m_CurrentWeight;

        public GazeObject(string name, string jointTransformName, float weight, Axis lookAxis)
        {
            m_Name = name;
            m_JointTransformName = jointTransformName;
            m_Weight = weight;
            m_LookAxis = lookAxis;
        }

        public Vector3 GetLookVector()
        {
            Vector3 look;
            switch (m_LookAxis)
            {
                case Axis.X: look = m_Gazer.transform.right; break;
                case Axis.Y: look = m_Gazer.transform.up; break;
                case Axis.Z: default: look = m_Gazer.transform.forward; break;
            }
            return look;
        }

        public void SetLookVector(Vector3 look)
        {
            switch (m_LookAxis)
            {
                case Axis.X: m_Gazer.transform.right = look; break;
                case Axis.Y: m_Gazer.transform.up = look; break;
                case Axis.Z: default: m_Gazer.transform.forward = look; break;
            }
        }
    }
    #endregion

    #region Variables
    public bool showDebugGizmos = false;

    [SerializeField] GazeObject m_LeftEye = new GazeObject("Left Eye", "JtEyeLf", 1, GazeObject.Axis.Z);
    [SerializeField] GazeObject m_RightEye = new GazeObject("Right Eye", "JtEyeRt", 1, GazeObject.Axis.Z);
    [SerializeField] GazeObject m_Head = new GazeObject("Head", "JtSkullA", 1, GazeObject.Axis.Y);
    [SerializeField] GazeObject m_Body = new GazeObject("Body", "JtSpineC", 1, GazeObject.Axis.Y);

    [SerializeField]
    GazeObject[] m_AdditionalGazers = null;

    List<GazeObject> m_Gazers = new List<GazeObject>();
    #endregion

    #region Properties
    public override float HeadGazeWeight
    {
        get { return m_Head.m_Weight; }
        set { m_Head.m_Weight = value; }
    }
    public override float EyeGazeWeight
    {
        get { return m_LeftEye.m_Weight; }
        set
        {
            m_LeftEye.m_Weight = value;
            m_RightEye.m_Weight = value;
        }
    }
    public override float BodyGazeWeight
    {
        get { return m_Body.m_Weight; }
        set { m_Body.m_Weight = value; }
    }
    public override float CurrentHeadGazeWeight
    {
        get { return m_Body.m_CurrentWeight; }
        set { m_Body.m_CurrentWeight = value; }
    }
    public override float CurrentEyeGazeWeight
    {
        get { return m_LeftEye.m_CurrentWeight; }
        set
        {
            m_LeftEye.m_CurrentWeight = value;
            m_RightEye.m_CurrentWeight = value;
        }
    }
    public override float CurrentBodyGazeWeight
    {
        get { return m_Body.m_CurrentWeight; }
        set { m_Body.m_CurrentWeight = value; }
    }
    #endregion


    // ---------------------------------------------------------------------------------------------
    void Start()
    {
        m_Gazers.Add(m_Body);
        m_Gazers.Add(m_Head);
        m_Gazers.Add(m_LeftEye);
        m_Gazers.Add(m_RightEye);

        if (m_AdditionalGazers != null)
        {
            m_Gazers.AddRange(m_AdditionalGazers);
        }

        foreach (GazeObject go in m_Gazers)
        {
           GameObject jointGO = VHUtils.FindChildRecursive(gameObject, go.m_JointTransformName);
            if (jointGO != null)
            {
                go.m_Gazer = jointGO.transform;
                go.m_AnimationRef = new GameObject(go.m_JointTransformName + "AnimationRef");
                go.m_GazeRef = new GameObject(go.m_JointTransformName + "GazeRef");
            }
            else
            {
                Debug.LogError("Can't find gaze joint " + go.m_JointTransformName + " in gameobject hierarchy " + name);
            }
        }
    }


    // ---------------------------------------------------------------------------------------------
    void LateUpdate()
    {
        foreach (GazeObject go in m_Gazers)
        {
            if (go.m_Weight == 0)
            {
                continue;
            }

            // Must go here as animation system updates between Update and LateUpdate
            ParentConstraint(go.m_AnimationRef.transform, go.m_Gazer);

            // Position & orient gaze reference objects
            go.m_GazeRef.transform.position = go.m_Gazer.transform.position;
            go.m_GazeRef.transform.LookAt(m_GazeTarget.transform);

            // Assign final vector for eye joints
            //go.m_Gazer.transform.rotation = Quaternion.Slerp(go.m_AnimationRef.transform.rotation, go.m_GazeRef.transform.rotation, go.m_Weight);
            go.SetLookVector(Vector3.Slerp(go.m_AnimationRef.transform.forward, go.m_GazeRef.transform.forward, go.m_Weight));
        }

    }

    // ---------------------------------------------------------------------------------------------
    void OnDrawGizmos()
    {
        if (showDebugGizmos == true && Application.isPlaying)
        {
            foreach (GazeObject go in m_Gazers)
            {
                if (go.m_Weight == 0)
                {
                    continue;
                }

                // Animation
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(go.m_AnimationRef.transform.position, go.m_AnimationRef.transform.forward);

                // Gaze
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(go.m_Gazer.position, go.m_GazeRef.transform.forward);

                Gizmos.DrawWireSphere(m_GazeTarget.transform.position, .025f);

                // Reference
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(go.m_Gazer.position, .005f);

                // Final Result
                Gizmos.color = Color.red;
                Gizmos.DrawRay(go.m_Gazer.position, go.m_Gazer.forward);
            }
        }
    }

    // intentionally empty, actual implementation is in LateUpdate()
    public override void UpdateGaze() { }

    void ParentConstraint(Transform Constrained, Transform Constrainer)
    {
        Constrained.SetPositionAndRotation(Constrainer.position, Constrainer.rotation);
    }
}
}
