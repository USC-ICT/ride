using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FileProvider : MilSymbolProvider {

    private static readonly string Template = "2525D/{0}";

    private static readonly string UnknownSymbol = "10031500002000000000";
    

    public override void RequestTextureForSIDC(string sidc, Action<Texture2D> callback)
    {
        Texture2D tex = Resources.Load<Texture2D>(string.Format(Template, sidc));
        if (!tex)
        {
            Debug.Log("Symbol ID: " + sidc + " Not Found!");
            tex = Resources.Load<Texture2D>(string.Format(Template, UnknownSymbol));
        }
        callback(tex);
    }
}
