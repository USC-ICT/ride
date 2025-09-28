using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class SplashScreens : MonoBehaviour
{
    #region Variables

    [Serializable]
    public class SplashInfo
    {
        public Texture2D texture; // can be null if you just want a blank screen for an interval
        public float fadeinTime;  // all in seconds
        public float displayTime;
        public float fadeoutTime;
        public ScaleMode scaleMode;
    }

    public List<SplashInfo> m_splashScreens = new List<SplashInfo>();
    public string m_sceneToLoad;

    int m_currentSplash = 0;
    float m_currentSplashStartTime;
    float m_currentSplashAlpha = 1.0f;

    #endregion


    void Start()
    {
        m_currentSplashStartTime = Time.time;
    }


    void Update()
    {
        if (m_currentSplash < m_splashScreens.Count)
        {
            m_currentSplashAlpha = GetAlphaValue();

            //Debug.Log(string.Format("{0} - {1} - {2} - {3}", Time.time, m_currentSplashStartTime, m_splashScreens[m_currentSplash].timeout, m_currentSplashAlpha));
        }

        if (m_currentSplash >= m_splashScreens.Count)
        {
            LoadNextScene();
        }


        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadNextScene();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            m_currentSplash++;
            m_currentSplashStartTime = Time.time;
        }
    }


    void OnGUI()
    {
        if (m_currentSplash < m_splashScreens.Count)
        {
            if (m_splashScreens[m_currentSplash].texture)
            {
                GUI.color = new Color(1, 1, 1, m_currentSplashAlpha);
                Rect r = new Rect(0, 0, Screen.width, Screen.height);
                GUI.DrawTexture(r, m_splashScreens[m_currentSplash].texture, m_splashScreens[m_currentSplash].scaleMode, true);
                GUI.color = Color.white;
            }
        }
    }


    void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(m_sceneToLoad))
        {
            VHUtils.SceneManagerLoadScene(m_sceneToLoad);
        }
    }


    float GetAlphaValue()
    {
        float currentTime = Time.time - m_currentSplashStartTime;
        SplashInfo currentSplash = m_splashScreens[m_currentSplash];
        float alpha;

        if (currentTime <= currentSplash.fadeinTime)
        {
            alpha = currentTime / currentSplash.fadeinTime;
        }
        else if (currentTime < currentSplash.fadeinTime + currentSplash.displayTime)
        {
            alpha = 1.0f;
        }
        else if (currentTime < currentSplash.fadeinTime + currentSplash.displayTime + currentSplash.fadeoutTime)
        {
            float timeLeft = currentTime - currentSplash.displayTime - currentSplash.fadeinTime;
            alpha = 1.0f - (timeLeft / currentSplash.fadeoutTime);
        }
        else
        {
            alpha = 0;
            m_currentSplash++;
            m_currentSplashStartTime = Time.time;
        }

        return alpha;
    }
}
}
