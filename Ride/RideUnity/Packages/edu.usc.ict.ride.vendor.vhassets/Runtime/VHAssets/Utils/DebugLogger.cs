using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VHAssets
{
public abstract class DebugLogger : MonoBehaviour
{
    #region Constants
    protected const int MaxTextLength = 15000;

    protected class TextLine
    {
        public string text;
        public Color color;
        public LogType logType;
        public float lifeTimeLeft;

        public TextLine(string _text, Color _color)
        {
            text = _text;
            color = _color;
        }

        public TextLine(string _text, Color _color, LogType _type, float _lifeTimeLeft)
        {
            text = _text;
            color = _color;
            logType = _type;
            lifeTimeLeft = _lifeTimeLeft;
        }
    }
    #endregion

    #region Variables
    public int m_MaxLogCapacity = 200;
    public int m_CanvasSortingOrder = 100;
    //public float m_FontScaler = 1f;

    protected List<TextLine> m_CachedText = new List<TextLine>();
    protected Text[] m_LoggedText;
    protected int m_TextCacheIndex;
    protected Text m_HolderText;

    int m_CachedScreenWidth = 1;
    int m_CachedScreenHeight = 1;
    #endregion

    #region Functions
    public virtual void Start()
    {
    }

    protected void CreateUIText(Transform textParent)
    {
        m_LoggedText = new Text[m_MaxLogCapacity];
        for (int i = 0; i < m_LoggedText.Length; i++)
        {
            m_LoggedText[i] = VHUI.CreateText("TextLine", textParent, string.Empty, Color.white);
            LayoutElement ele = m_LoggedText[i].gameObject.AddComponent<LayoutElement>();
            ele.flexibleWidth = 1;
            m_LoggedText[i].gameObject.SetActive(false);
            m_LoggedText[i].alignment = TextAnchor.MiddleLeft;
            m_LoggedText[i].resizeTextForBestFit = true;
        }

        m_HolderText = VHUI.CreateText("Holder", textParent, string.Empty, Color.white);
        m_HolderText.gameObject.AddComponent<LayoutElement>();
        m_HolderText.gameObject.SetActive(false);
        m_HolderText.alignment = TextAnchor.MiddleLeft;
        m_HolderText.resizeTextForBestFit = true;
    }


    public virtual void Update()
    {
        CheckResolutionResize();
    }

    public abstract void AddText(string text, Color c, LogType logType);

    void CheckResolutionResize()
    {
        if (m_CachedScreenWidth != Screen.width || m_CachedScreenHeight != Screen.height)
        {
            m_CachedScreenWidth = Screen.width;
            m_CachedScreenHeight = Screen.height;
            RebuildDisplay();
        }
    }

    protected virtual void RebuildDisplay()
    {
        //yield return null;
    }

    protected Text FindInactiveText()
    {
        return Array.Find<Text>(m_LoggedText, t => !t.gameObject.activeSelf);
    }
    #endregion
}
}
