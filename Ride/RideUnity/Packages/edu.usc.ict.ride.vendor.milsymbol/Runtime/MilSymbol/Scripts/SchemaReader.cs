using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using JointMilitarySymbologyLibrary;
using Integration.JMSL;

public class SchemaReader : MonoBehaviour {

    [SerializeField]
    private string[] m_Schemas =
    {
        "jmsml_D_Control_Measure",
         "jmsml_D_Land_Equipment",
         "jmsml_D_Land_Unit"
    };

    /// <summary>
    /// Stores the state of the load-in. Set to true after reading is complete.
    /// </summary>
    private bool m_ReadComplete = false;

    /// <summary>
    /// Stores the MIL-2525D library.
    /// </summary>
    private Librarian m_Librarian;

    /// <summary>
    /// Subscribe to this event to be notified when loading is complete.
    /// </summary>
    //public event LoadCompleteArgs OnLoadComplete;

    public delegate void LoadCompleteArgs(object src);
    public Librarian Librarian { get { return m_Librarian; } }
        
    public bool IsReadComplete { get { return m_ReadComplete; } }

    // Use this for initialization
    void Awake()
    {
        LoadSchema();
    }

    private void LoadSchema()
    {
        string baseSchema = "jmsml_D_Base";

        Library library = LoadFromAsset<Library>(baseSchema);

        SymbolSet[] symbolSets = new SymbolSet[m_Schemas.Length];

        for (int i = 0; i < m_Schemas.Length; i++)
        {
            symbolSets[i] = LoadFromAsset<SymbolSet>(m_Schemas[i]);
        }

        m_Librarian = new Librarian(library, symbolSets);
        
        //m_ReadComplete = true;
        //var tmp = OnLoadComplete;
        //if (null != tmp)
        //{
        //    tmp(this);
        //}
    }

    private T LoadFromAsset<T>(string name)
    {
        XmlSerializer deserializer = new XmlSerializer(typeof(T));
        string assetPath = string.Format("instance/{0}", name);
        TextAsset asset = Resources.Load<TextAsset>(assetPath);
        var reader = new System.IO.StringReader(asset.text);
        T output = (T)deserializer.Deserialize(reader);
        return output;
    }
}
