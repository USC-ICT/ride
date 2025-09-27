using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class SaccadeExampleBasedController : FunctionalityController, ISaccade
{
    [Header("Configuration")]
    [Range(0,1)]
    public float m_magnitude = 1f;
    public bool m_saccadeMode;

    #region Internal Variables
    
    [Header("Animator Controller Linking")]
    string m_targetLayer = "SaccadeExampleBasedController";

    private AnimContParameter m_paramPitchUp;
    private AnimContParameter m_paramPitchDown;
    private AnimContParameter m_paramYawLeft;
    private AnimContParameter m_paramYawRight;

    [Range(-1,1)]
    public float m_pitch = 0f;
    [Range(-1,1)]
    public float m_yaw = 0f;

    // Saccade stuff
    float timeBetweenShift = 3f;
    float lastShiftTime = 0;
    public bool m_saccadeTriggered = false;
    //CharacterDefines.SaccadeType m_mode = CharacterDefines.SaccadeType.Default;
    #endregion
    
    // Constructor, set required derived variables
    public SaccadeExampleBasedController()
    {
        Layers = new List<string> {m_targetLayer};
        
        // Test class for parameters
        m_paramPitchUp = new AnimContParameter("SaccadeExampleBasedController/PitchUp", AnimatorControllerParameterType.Float);
        m_parameters.Add(m_paramPitchUp);
        
        m_paramPitchDown = new AnimContParameter("SaccadeExampleBasedController/PitchDown", AnimatorControllerParameterType.Float);
        m_parameters.Add(m_paramPitchDown);
        
        m_paramYawLeft = new AnimContParameter("SaccadeExampleBasedController/YawLeft", AnimatorControllerParameterType.Float);
        m_parameters.Add(m_paramYawLeft);
        
        m_paramYawRight = new AnimContParameter("SaccadeExampleBasedController/YawRight", AnimatorControllerParameterType.Float);
        m_parameters.Add(m_paramYawRight);
        
        
        ControllerDescription = "Simulate saccade behavior; the quick movement of eyes from one position to another.";
        AnimationGuidelines = "Each cardinal direction should have the center of iris at the very edge of the visible opening. "+
                              "\n\n"+
                              "Directions such as yaw left and right are relative to the character. "+
                              "So for example 'yaw left' would have the eye directed left.";
    }
    
    // ---------------------------------------------------------------------------------------------
    #region Properties

    public List<string> TargetParameters
    {
        get
        {
            List<string> m_paramNames = new List<string>();

            foreach (AnimContParameter param in m_parameters)
            {
                m_paramNames.Add(param.name);
            }

            return m_paramNames;  
        }
    }

    public bool IsPerformingSaccade
    {
        get { return m_saccadeTriggered; }
    }

    #endregion

    // ---------------------------------------------------------------------------------------------
    #region Event Functions

    public override void Update()
    {
        base.Update();

        shiftEyes();

        driveAnimatorParameters();
    }

    #endregion

    /// <summary>
    /// Connect results of saccade controller to animator controller parameters.
    /// </summary>
    void driveAnimatorParameters()
    {
        if (m_pitch >= 0)
        {
            m_animator.SetFloat(m_paramPitchUp.name, m_pitch * m_magnitude);
            m_animator.SetFloat(m_paramPitchDown.name, 0f);
        }
        else
        {
            m_animator.SetFloat(m_paramPitchUp.name, 0f);
            m_animator.SetFloat(m_paramPitchDown.name, -1 * m_pitch * m_magnitude);
        }

        if (m_yaw >= 0)
        {
            m_animator.SetFloat(m_paramYawRight.name, m_yaw * m_magnitude);
            m_animator.SetFloat(m_paramYawLeft.name, 0f);
        }
        else
        {
            m_animator.SetFloat(m_paramYawRight.name, 0f);
            m_animator.SetFloat(m_paramYawLeft.name, -1 * m_yaw * m_magnitude);
        }
    }


    /// <summary>
    /// Control the shifting of the eyes.
    /// </summary>
    void shiftEyes()
    {
        if (Time.time - lastShiftTime > timeBetweenShift)
        {
            lastShiftTime = Time.time;
            PerformSaccade();
        }
        else {
            m_saccadeTriggered = false;
        }
    }

    public void PerformSaccade()
    {
        // set that shift was triggered
        m_saccadeTriggered = true;

        // set new random pitch and yaw 
        m_pitch = UnityEngine.Random.Range(-1f, 1f);
        m_yaw = UnityEngine.Random.Range(-1f, 1f);
    }

    public void SetMode(CharacterDefines.SaccadeType mode)
    {
        //m_mode = mode;
    }
}
}
