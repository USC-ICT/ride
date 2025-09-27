using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace VHAssets
{
public static class VHUI
{
    #region Constants
    public const float ButtonWidth = 200;
    public const float ButtonHeight = 40;
    #endregion

    #region Functions
    public static Canvas CreateCanvas(string canvasName, GameObject parent, int sortingOrder)
    {
        return CreateCanvas(canvasName, parent, sortingOrder, RenderMode.ScreenSpaceOverlay);
    }

    public static Canvas CreateCanvas(string canvasName, GameObject parent, int sortingOrder, RenderMode renderMode)
    {
        GameObject canvasGO = new GameObject("Canvas", new System.Type[] { typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup) } );
        canvasGO.name = canvasName;
        if (parent != null)
            canvasGO.transform.SetParent(parent.transform);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        // need an event system too if there isn't one
        if (GameObject.FindFirstObjectByType<EventSystem>() == null)
        {
            EventSystem eventSystem = GameObject.Instantiate(Resources.Load<EventSystem>("vhAssetsEventSystem"));
            eventSystem.name = eventSystem.name.Replace("(Clone)", "");
        }

        return canvas;
    }

    public static Image CreateImage(string goName, Transform parent)
    {
        return CreateImage(goName, parent, null);
    }

    public static Image CreateImage(string goName, Transform parent, Sprite sprite)
    {
        Image image = GameObject.Instantiate(Resources.Load<Image>("vhAssetsImage"));
        image.transform.SetParent(parent);
        image.name = goName;
        if (sprite != null)
        {
            image.sprite = sprite;
        }
        return image;
    }

    public static Text CreateText(string goName, Transform parent, string text, Color textColor)
    {
        Text t = GameObject.Instantiate<Text>((Resources.Load<Text>("vhAssetsText")));
        t.name = goName;
        //t.fontSize = 14;
        t.transform.SetParent(parent);
        t.color = textColor;
        t.text = text;
        return t;
    }

    public static Text CreateLayoutText(string goName, Transform parent, string text, Color textColor, float screenPctWidth, float screenPctHeight)
    {
        Text t = CreateText(goName, parent, text, textColor);
        LayoutElement layout = t.gameObject.AddComponent<LayoutElement>();
        SetPreferredLayout(layout, screenPctWidth, screenPctHeight);
        return t;
    }

    public static void SetPreferredLayout(LayoutElement layout, float screenPctWidth, float screenPctHeight)
    {
        layout.preferredWidth = screenPctWidth * Screen.width;
        layout.preferredHeight = screenPctHeight * Screen.height;
    }

    public static InputField CreateInputField(string goName, Transform parent, UnityEngine.Events.UnityAction<string> onEndEditCallback)
    {
        InputField field = GameObject.Instantiate(Resources.Load<InputField>("vhAssetsInputField"));
        field.name = goName;
        field.transform.SetParent(parent);
        if (onEndEditCallback != null)
        {
            field.onEndEdit.AddListener(onEndEditCallback);
        }
        return field;
    }

    public static ScrollRect CreateScrollRect(string goName, Transform parent, bool useContentSizeFitter)
    {
        ScrollRect scrollRect = GameObject.Instantiate(Resources.Load<ScrollRect>("vhAssetsScrollView"));
        scrollRect.name = goName;
        scrollRect.transform.SetParent(parent);
        if (useContentSizeFitter)
        {
            GameObject contentGO = VHUtils.FindChildRecursive(scrollRect.gameObject, "Content");
            contentGO.AddComponent<ContentSizeFitter>();
        }
        return scrollRect;
    }

    public static T CreateLayoutGroup<T>(string goName, Transform parent) where T : LayoutGroup
    {
        return CreateLayoutGroup<T>(goName, parent, new RectOffset(), TextAnchor.UpperLeft);
    }

    public static T CreateLayoutGroup<T>(string goName, Transform parent, RectOffset padding, TextAnchor childAlignment) where T : LayoutGroup
    {
        GameObject go = new GameObject(goName, new System.Type[] { typeof(RectTransform), typeof(T) } );
        go.transform.SetParent(parent);
        T group = go.GetComponent<T>();
        group.padding = padding;
        group.childAlignment = childAlignment;
        return group;
    }

    /// <summary>
    /// Clones a new widget from widgetTemplate and sets it as a child of the layout
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="layout">The parent gameobject of the newly created widget</param>
    /// <param name="widgetTemplate">The object that gets cloned. This object is not modified</param>
    /// <returns></returns>
    public static T AddToLayout<T>(LayoutGroup layout, T widgetTemplate) where T : MonoBehaviour
    {
        if (layout == null)
        {
            Debug.LogError("NO LAYOUT");
        }
        T instance = GameObject.Instantiate<T>(widgetTemplate, layout.transform);
        //instance.transform.SetParent(layout.transform);
        return instance;
    }

    public static void StretchToParent(RectTransform rectTransform)
    {
        StretchToParent(rectTransform, 0, 0, 0, 0);
    }

    public static void StretchToParent(RectTransform rectTransform, float leftInset, float bottomInset, float rightInset, float topInset)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        SetInset(rectTransform, leftInset, bottomInset, rightInset, topInset);
    }

    public static void SetInset(RectTransform rectTransform, float leftInset, float bottomInset, float rightInset, float topInset)
    {
        rectTransform.offsetMin = new Vector2(leftInset, bottomInset);
        rectTransform.offsetMax = new Vector2(rightInset, topInset);
    }

    public static void FadeAlpha(this Text text, float fadeTime, float startAlpha, float targetAlpha)
    {
        text.StartCoroutine(FadeAlphaInternal(text, fadeTime, startAlpha, targetAlpha));
    }

    static IEnumerator FadeAlphaInternal(Text text, float fadeTime, float startAlpha, float targetAlpha)
    {
        float t = 0;
        Color holder = text.color;

        while (t < fadeTime)
        {
            holder.a = Mathf.SmoothStep(startAlpha, targetAlpha, t / fadeTime);
            text.color = holder;
            yield return new WaitForEndOfFrame();
            t += Time.deltaTime;
            //Debug.Log(holder.a);
        }
    }

    public static void FadeAlphaInOut(this Text text, float totalFadeTime, float startRampTime, float endRampTime)
    {
        // fades text in for startRampTime, fades text out for endRampTime, over a total of totalFadeTime
        if (text.gameObject.activeSelf && text.gameObject.activeInHierarchy)
        {
            text.StartCoroutine(FadeAlphaInOutInternal(text, totalFadeTime, startRampTime, endRampTime));
        }
    }

    static IEnumerator FadeAlphaInOutInternal(Text text, float totalFadeTime, float startRampTime, float endRampTime)
    {
        const float startAlpha = 0;
        const float midAlpha = 1;
        const float endAlpha = 0;

        float startTime = Time.time;
        float startEndTime = (startTime + totalFadeTime) - endRampTime;  // time we need to start fading out
        float curTime = Time.time;
        Color color = text.color;

        while (curTime < startTime + totalFadeTime)
        {
            if (curTime < startTime + startRampTime)  // fade in
                color.a = Mathf.Lerp(startAlpha, midAlpha, (curTime - startTime) / startRampTime);
            else if (curTime > startEndTime)  // fade out
                color.a = Mathf.Lerp(midAlpha, endAlpha, (curTime - startEndTime) / endRampTime);
            else
                color.a = midAlpha;

            text.color = color;

            //Debug.Log(string.Format("{0} {1} {2} {3} {4} {5}", curTime, startTime, startTime + fadeTime, startTime + startRampTime, startEndTime, color.a));

            yield return new WaitForEndOfFrame();
            curTime = Time.time;
        }

        text.color = new Color(text.color.r, text.color.g, text.color.b, 0);
    }

    public static string InsertBreaks(string text, Font font, int fontSize, int breakIntervalPixels)
    {
        int length = 0;
        Font myFont = font;
        CharacterInfo characterInfo = new CharacterInfo();
        text = text.Replace("\r\n", "\n");

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\n')
            {
                length = 0;
                continue;
            }


            myFont.GetCharacterInfo(c, out characterInfo, fontSize);
            length += characterInfo.advance;

            if (length > breakIntervalPixels)
            {
                length = 0;
                text = text.Insert(i, "\n");
                i++;
            }
        }
        return text;
    }

    static Toggle.ToggleEvent emptyToggleEvent = new Toggle.ToggleEvent();

    /// <summary>
    /// Sets the value of the Toggle without invoking onValueChanged
    /// </summary>
    /// <param name="instance"></param>
    /// <param name="value"></param>
    public static void SetValue(this Toggle instance, bool value)
    {
        var originalEvent = instance.onValueChanged;
        instance.onValueChanged = emptyToggleEvent;
        instance.isOn = value;
        instance.onValueChanged = originalEvent;
    }
    #endregion
}
}
