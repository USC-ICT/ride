using UnityEngine;
using System.Collections;

public class BillboardSymbol : MonoBehaviour {


    [SerializeField]
    private string m_SIDC = "10009800001000000000";
    
    [SerializeField]
    private Renderer m_SymbolQuad = null;

    [SerializeField]
    private float m_Alpha = 0.75f;
    

    private MilSymbolProvider m_Provider;
    
    public string SIDC {
        get { return m_SIDC; }
        set
        {
            m_SIDC = value;
            RequestTexture();
        }
    }

    public float Alpha
    {
        get { return m_Alpha; }
        set
        {
            m_Alpha = value;
            UpdateAlpha();
        }
    }


    // Use this for initialization
    void Start ()
    {
        m_Provider = FindFirstObjectByType<MilSymbolProvider>();
        RequestTexture();
        UpdateAlpha();
    }

    private void RequestTexture()
    {
        if (m_Provider != null)
        {
            m_Provider.RequestTextureForSIDC(m_SIDC, UpdateSymbol);
        }
    }
    
    private void UpdateSymbol(Texture2D symbol)
    {
        m_SymbolQuad.material.mainTexture = symbol;
    }


    private void UpdateAlpha()
    {
        Color temp = m_SymbolQuad.material.color;
        temp.a = m_Alpha;
        m_SymbolQuad.material.color = temp;
    }
}
