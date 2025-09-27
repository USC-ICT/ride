using Ride.UI;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RideScrollView))]
public class AutoScrollingVerticalText : MonoBehaviour
{
    public RideTextTMPro m_text;
    private string m_previousText;
    private float m_scrollTime = 0.5f;
    private RideScrollView m_scrollView;
    private Coroutine m_scrollCoroutine;

    private void Start()
    {
        m_scrollView = GetComponent<RideScrollView>();
        if (m_text != null)
            m_previousText = m_text.text;
    }

    private void Update()
    {
        if(m_text != null && m_previousText != m_text.text)
        {
            StartScrolling();
            m_previousText = m_text.text;
        }
    }

    public void StartScrolling()
    {
        m_scrollCoroutine = StartCoroutine(ScrollToBottom());
    }

    public void StopScrolling()
    {
        if(m_scrollCoroutine != null)
            StopCoroutine(m_scrollCoroutine);
    }

    IEnumerator ScrollToBottom()
    {
        float startValue = m_scrollView.verticalValue;
        float endValue = 0f;
        float curTime = 0;
        while (curTime < m_scrollTime)
        {
            m_scrollView.verticalValue = Mathf.Lerp(startValue, endValue, curTime / m_scrollTime);
            curTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        m_scrollView.verticalValue = endValue;
    }
}
