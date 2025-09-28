using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class BlinkExampleBasedController : FunctionalityController, IBlink
{
    #region Animator Controller Layers & Parameters

    [Header("Animator Controller Linking")]
    public string m_targetLayer = "BlinkExampleBasedController";
    // public string m_paramDoBlink = "BlinkExampleBasedController/DoBlink";
    private AnimContParameter m_paramDoBlink;

    #endregion


    #region Blink
    float m_lastShiftTime = 0f;
    float m_timeBetweenShift = 4f;
    //bool m_isBlinking = false; // TODO: get the blink animation length and when a blink occurs set this to true for the length in seconds of the blink animation
    #endregion

    BlinkExampleBasedController()
    {
        Layers = new List<string> {m_targetLayer};
        
        m_paramDoBlink = new AnimContParameter("BlinkExampleBasedController/DoBlink", AnimatorControllerParameterType.Trigger);
        m_parameters.Add(m_paramDoBlink);
    }

    public bool IsBlinking { get { return false; } }

    
    #region Event Functions

//    public override void Start()
//    {
//
//        // m_layers.Add(m_targetLayer);
//        // m_parameters.Add(m_paramDoBlink);
//
//        base.Start();
//    }


    public override void Update()
    {
        base.Update();
        randomBlinking();
    }

    #endregion


    /// <summary>
    /// Control the random triggering of the blink.
    /// </summary>
    void randomBlinking()
    {
        if (Time.time - m_lastShiftTime > m_timeBetweenShift)
        {
            m_lastShiftTime = Time.time;

            m_animator.SetTrigger(m_paramDoBlink.name);
        }
    }


    /// <summary>
    /// Trigger a blink.
    /// </summary>
    public void Blink()
    {
        m_animator.SetTrigger(m_paramDoBlink.name);
        
        m_lastShiftTime = Time.time;
        Debug.Log("Blink Trigged");
    }
}
}
