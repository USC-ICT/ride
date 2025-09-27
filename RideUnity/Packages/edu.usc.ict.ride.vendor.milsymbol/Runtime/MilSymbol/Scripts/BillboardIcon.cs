using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardIcon : MonoBehaviour {

    private Camera m_MainCam;
    
    readonly string TAG = "MainCamera";

    [SerializeField]
    private Transform m_ScaleRoot = null;

    [SerializeField]
    private Transform m_PivotRoot = null;

    [SerializeField]
    private GameObject m_Highlight = null;

    private bool m_IsHighlighted = false;

    private float m_ScreenScale = .05f;
    private float m_VRScale = .1f; 

    public bool IsHighlighted
    {
        get { return m_IsHighlighted; }
        set
        {
            m_IsHighlighted = value;
            if (m_Highlight)
            {
                m_Highlight.SetActive(value);
            }
        }
    }

    void Start()
    {
        GameObject withTag = GameObject.FindGameObjectWithTag(TAG);
        m_MainCam = withTag.GetComponent<Camera>();
        //MOVE SYMBOL FORWARD FOR ORDINANCE WHICH IS PLACE ON BUILDINGS
        //TODO FOR ANY ORDINANCE IF INTERSECT WITH BUILDING?
        if (gameObject.GetComponent<BillboardSymbol>() && gameObject.GetComponent<BillboardSymbol>().SIDC == "10031000001623000000")
        {
              Vector3 zForward = gameObject.transform.localPosition;
              zForward.z = -2.5f;
              gameObject.transform.localPosition = zForward;
        }
    }

    void Update()
    {
        if (!m_PivotRoot || !m_ScaleRoot)
        {
            Debug.LogError("Billboard not set up correctly!", this);
            enabled = false;
            return;
        }
        // Look parallel to the camera forward vector, not directly at the 
        // camera. This makes things look less weird when symbols are at the
        // edges of the screen.
        if (xrDisplayIsRunning())  //if (UnityEngine.XR.XRDevice.isPresent)
        {
            m_PivotRoot.LookAt(m_MainCam.transform.position);
        }   
        else
            m_PivotRoot.LookAt(m_PivotRoot.transform.position - m_MainCam.transform.forward);
	


        float distance = (transform.position - m_MainCam.transform.position).magnitude;
        if (xrDisplayIsRunning())  //if(UnityEngine.XR.XRDevice.isPresent)
            m_ScaleRoot.localScale = Vector3.one * (m_VRScale * distance);
        else
            m_ScaleRoot.localScale = Vector3.one * (m_ScreenScale * distance);
    }

    static bool xrDisplayIsRunning()
    {
        // https://docs.unity3d.com/ScriptReference/XR.XRDevice-isPresent.html
        var xrDisplaySubsystems = new List<UnityEngine.XR.XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(xrDisplaySubsystems);
        foreach (var xrDisplay in xrDisplaySubsystems)
        {
            if (xrDisplay.running)
                return true;
        }
        return false;
    }
}
