using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VHAssets
{
public class DebugOnScreenLog : DebugLogger
{
    #region Variables
    public Rect m_displayPosition = new Rect(0.0f, 0.4f, 1.0f, 0.6f);

    public float m_lineScreenPctHeight = 0.03f;
    public float m_normalDisplayTime  = 1.0f;
    public float m_warningDisplayTime = 3.0f;
    public float m_errorDisplayTime   = 8.0f;

    VerticalLayoutGroup m_layout;
    
    #endregion


    public int VisibleLines
    {
        get { return m_layout.transform.childCount; }
    }

    public bool IsShowing {  get { return m_layout.gameObject.activeSelf;  } }

    void Awake()
    {
        transform.position = Vector3.zero;
        Canvas canvas = VHUI.CreateCanvas("DebugOnScreenLogCanvas", this.gameObject, m_CanvasSortingOrder);

        m_layout = VHUI.CreateLayoutGroup<VerticalLayoutGroup>("OnScreenLogLayout", canvas.transform) as VerticalLayoutGroup;
        m_layout.GetComponent<RectTransform>().pivot = new Vector2 (0, 0);
        m_layout.childAlignment = TextAnchor.LowerLeft;
        m_layout.padding = new RectOffset(5,5,5,5);

        canvas.GetComponent<CanvasGroup>().interactable = false;
        canvas.GetComponent<CanvasGroup>().blocksRaycasts = false;

        VHUI.StretchToParent(m_layout.GetComponent<RectTransform>(), m_displayPosition.x * Screen.width * -1, 0,
            (1 - m_displayPosition.width) * Screen.width * -1, (1 - m_displayPosition.height) * Screen.height * -1);

        m_layout.childForceExpandWidth = m_layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = m_layout.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LogCallbackHandler handler = GameObject.FindAnyObjectByType<LogCallbackHandler>();
        if (handler)
            handler.AddCallback(LogCallback);
        else
            Debug.LogWarning("DebugOnScreenLog: LogCallbackHandler component not found in the scene.  Add one if you wish to display Unity Log() messages");

        CreateUIText(m_layout.transform);
    }

    public override void Update()
    {
        base.Update();

        for (int i = 0; i < m_CachedText.Count; i++)
        {
            m_CachedText[i].lifeTimeLeft -= Time.deltaTime;
            if (m_CachedText[i].lifeTimeLeft <= 0)
            {
                m_CachedText.RemoveAt(i--);
            }
        }
    }

    public override void AddText(string text, Color c, LogType logType)
    {
        if (!IsShowing)
        {
            return;
        }

        m_CachedText.Add(new TextLine(text, c, logType, GetDisplayTime(logType)));

        // truncate the text to prevent these errors.  These errors occur on 5.3.6f1.
        //      String too long for TextMeshGenerator. Cutting off characters.
        //      ArgumentException: Mesh can not have more than 65000 vertices
        string textCopy = text.Substring(0, Mathf.Min(text.Length, MaxTextLength));

        //m_HolderText = uGuiUtils.CreateText("Text", m_Layout.transform, textCopy, c);
        float preferredSize = (float)Screen.height * m_lineScreenPctHeight;
        //m_HolderText.fontSize = (int)(preferredSize * m_FontScaler);

        text = VHUI.InsertBreaks(textCopy, m_HolderText.font, m_HolderText.fontSize, (int)((float)Screen.width * 0.95f));
        m_HolderText.gameObject.SetActive(false);

        string[] newLineSeperatedLines = text.Split('\n');
        for (int i = 0; i < newLineSeperatedLines.Length; i++)
        {
            //Text loggedText = uGuiUtils.CreateText("Text", m_Layout.transform, newLineSeperatedLines[i], c);

            Text loggedText = FindInactiveText();
            if (loggedText == null)
            {
                // too much text is already in use
                return;
            }

            loggedText.color = c;
            loggedText.text = newLineSeperatedLines[i];
            loggedText.gameObject.SetActive(true);

            m_TextCacheIndex += 1;
            m_TextCacheIndex %= m_MaxLogCapacity;

            LayoutElement layoutEle = loggedText.gameObject.GetComponent<LayoutElement>();
            layoutEle.preferredHeight = preferredSize;

            //int numLines = loggedText.CalculateNumTextLines();
            //layoutEle.preferredHeight *= numLines;

            float displayTime = GetDisplayTime(logType);

            // fade it out over it's lifetime
            loggedText.FadeAlphaInOut(displayTime, 0, displayTime * 0.2f);

            // so it shows up at the bottom of the list
            loggedText.transform.SetAsLastSibling();

            // get rid of the object based on what type of log type it is
            if (loggedText.gameObject.activeSelf && loggedText.gameObject.activeInHierarchy)
            {
                loggedText.StartCoroutine(WaitAndHide(loggedText, displayTime));
            }
        }
    }

    public void ShowLog(bool show)
    {
        if (show)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    void Show()
    {
        m_layout.gameObject.SetActive(true);
        
    }

    void Hide()
    {
        m_CachedText.Clear();

        for (int i = 0; i < m_layout.transform.childCount; i++)
        {
            Transform child = m_layout.transform.GetChild(i);
            Text t = child.GetComponent<Text>();
            if (t != null && t.gameObject.activeSelf)
            {
                t.StopAllCoroutines();
                t.gameObject.SetActive(false);
            }
        }

        m_layout.gameObject.SetActive(false);
    }

    IEnumerator WaitAndHide(Text loggedText, float displayTime)
    {
        yield return new WaitForSeconds(displayTime);
        loggedText.gameObject.SetActive(false);
    }

    protected override void RebuildDisplay()
    {
        for (int i = 0; i < m_layout.transform.childCount; i++)
        {
            m_layout.transform.GetChild(i).gameObject.SetActive(false);
        }

        List<TextLine> temp = new List<TextLine>(m_CachedText);
        m_CachedText.Clear();

        foreach (TextLine ct in temp)
        {
            AddText(ct.text.Trim(), ct.color, ct.logType);
        }
    }

    float GetDisplayTime(LogType type)
    {
        float displayTime = m_normalDisplayTime;
        if (type == LogType.Error || type == LogType.Exception)
        {
            displayTime = m_errorDisplayTime;
        }
        else if (type == LogType.Warning)
        {
            displayTime = m_warningDisplayTime;
        }
        else
        {
            displayTime = m_normalDisplayTime;
        }
        return displayTime;
    }

    Color GetDisplayColor(LogType type)
    {
        Color color = Color.white;
        if (type == LogType.Error || type == LogType.Exception)
        {
            color = Color.red;
        }
        else if (type == LogType.Warning)
        {
            color = Color.yellow;
        }
        else
        {
            color = Color.white;
        }
        return color;
    }

    LogType GetLogType(Color color)
    {
        LogType type = LogType.Log;
        if (color == Color.red)
        {
            type = LogType.Error;
        }
        else if (color == Color.yellow)
        {
            type = LogType.Warning;
        }
        return type;
    }

#if false
    void OnGUI()
    {
        // gui butttons for testing Debug.Log().  Set to 'true' to enable
        {
            Rect r = new Rect(0.0f, 0.0f, 0.5f, 0.6f);
            GUILayout.BeginArea(VHIMGUI.ScaleToRes(ref r));
            GUILayout.BeginVertical();

            if (GUILayout.Button("Log"))
            {
                Debug.Log("Testing Log Error Message");
            }

            if (GUILayout.Button("Log Multiple"))
            {
                for (int i = 0; i < 100; i++)
                {
                    Debug.LogFormat("Testing a bunch of Log Error Messages - {0}", i + 1);
                }
            }

            if (GUILayout.Button("Log Long"))
            {
                string longError;
                longError = "124jh23k123l4h51kl3j5h1kl51 1jkl51lk4j5 h1lkj45h 1lk435h 1lk35h1lkj5h1lkj5h1lk5j 1klj45 1kl 51lkj54h 1l4k51klj45h1lkj54h1 l4k5 1kl4j5 1lk4j5 1lk45j 1l4kj51l k5 1lk4j5 1l45j1lkj51kl54jh1lkj51lk50998f890fa-0sd9fs0f90sdf 0s f0s df0sdf s90f8 0s9d8f s09df s09f s09 dfs90d fs09f8 09wer2 309523 l2k4j23l4 23l4j 2l34 2l3j4 2l4 2l34j 2l34 2l34l 2l4 2l";
                Debug.LogError(longError);
            }

            if (GUILayout.Button("Very Long"))
            {
                const int stringLength = 17000;
                const string testString = "abcdefhijk";  // length = 10
                int loop = stringLength / testString.Length;

                string longError = "";
                for (int i = 0; i < loop; i++)
                    longError += testString;
                Debug.LogError(longError);
            }

            if (GUILayout.Button("Warning"))
            {
                Debug.LogWarningFormat("Testing Log Warning Message - {0}", System.DateTime.Now);
            }

            if (GUILayout.Button("Error"))
            {
                Debug.LogErrorFormat("Testing Log Error Message - {0}", System.DateTime.Now);
            }

            if (GUILayout.Button("Test"))
            {
                Debug.LogFormat("Testing a regular Log Message - {0}", System.DateTime.Now);
            }

            if (GUILayout.Button("Test2"))
            {
                Debug.Log("at UnityEngine.GUISkin.GetStyle (System.String styleName) [0x00010] in C:\\buildslave\\unity\\build\\Runtime\\IMGUI\\Managed\\GUISkin.cs:319 \n  at UnityEngine.GUIStyle.op_Implicit (System.String str) [0x00020] in C:\\buildslave\\unity\\build\\Runtime\\IMGUI\\Managed\\GUIStyle.cs:586 \n  at UnityEditor.ProjectBrowser.InitSearchMenu () [0x00014] in C:\\buildslave\\unity\\build\\Editor\\Mono\\ProjectBrowser.cs:483 \n  at UnityEditor.ProjectBrowser.AssetStoreSearchEndedCallback () [0x00000] in C:\\buildslave\\unity\\build\\Editor\\Mono\\ProjectBrowser.cs:511 \n  at UnityEditor.ObjectListArea+<QueryAssetStore>c__AnonStorey40.<>m__60 (UnityEditor.AssetStoreSearchResults results) [0x00356] in C:\\buildslave\\unity\\build\\Editor\\Mono\\ObjectListArea.cs:406 \n  at UnityEditor.AssetStoreResultBase`1[Derived].Parse (UnityEditor.AssetStoreResponse response) [0x000fc] in C:\\buildslave\\unity\\build\\Editor\\Mono\\AssetStore\\AssetStoreClient.cs:89 \n  at UnityEditor.AssetStoreClient+<SearchAssets>c__AnonStorey61.<>m__B8 (UnityEditor.AssetStoreResponse ar) [0x00000] in C:\\buildslave\\unity\\build\\Editor\\Mono\\AssetStore\\AssetStoreClient.cs:751 \n  at UnityEditor.AssetStoreClient+<WrapJsonCallback>c__AnonStorey60.<>m__B6 (UnityEditor.AsyncHTTPClient job) [0x00012] in C:\\buildslave\\unity\\build\\Editor\\Mono\\AssetStore\\AssetStoreClient.cs:624 \nUnityEditor.AsyncHTTPClient:Done(State, Int32)\n");
            }

            if (GUILayout.Button("Test3"))
            {
                Debug.Log("Application.streamingAssetsPath - 'E:/svn_vhtoolkit/core/VHAssets/Assets/StreamingAssets'");
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
#endif


    void LogCallback(string logString, string stackTrace, LogType type)
    {
        // sanity check
        if (m_layout == null || VisibleLines > 999)
            return;

        string logStringStripped = logString.Replace("<color=red>", "").Replace("</color>", "");

        AddText(logStringStripped, GetDisplayColor(type), type);
    }
}
}
