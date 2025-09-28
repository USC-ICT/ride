using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class EyeController : ManagerController, IBlink, ISaccade
{
    #region Variables
    public IBlink m_blinkController;
    public ISaccade m_saccadeController;
    #endregion


    #region Functions
    public override void Start()
    {
        base.Start();
        m_blinkController = gameObject.GetComponent<BlinkExampleBasedController>();
        m_saccadeController = gameObject.GetComponent<SaccadeExampleBasedController>();
    }

    public override void Update()
    {
        saccadeTriggerBlinks();
    }

    void saccadeTriggerBlinks()
    {
        // If 'saccade triggered'
        if (m_saccadeController.IsPerformingSaccade == false)
        {
            return;
        }
        Debug.Log("Saccade Triggered!");


        // if not blinking

        // do blink
        if (Random.Range(1, 3) > 1)
        {
            m_blinkController.Blink();
        }
    }

    public bool IsBlinking { get { return m_blinkController.IsBlinking; } }

    public bool IsPerformingSaccade { get { return m_saccadeController.IsPerformingSaccade; } }

    public void Blink()
    {
        m_blinkController.Blink();
    }

    public void PerformSaccade()
    {
        m_saccadeController.PerformSaccade();
    }

    public void SetMode(CharacterDefines.SaccadeType mode)
    {
        m_saccadeController.SetMode(mode);
    }
    #endregion
}
}
