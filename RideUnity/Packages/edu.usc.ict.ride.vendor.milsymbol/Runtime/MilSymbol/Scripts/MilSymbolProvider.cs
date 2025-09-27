using UnityEngine;
using System.Collections;

public abstract class MilSymbolProvider : MonoBehaviour{

    public abstract void RequestTextureForSIDC(string sidc, System.Action<Texture2D> callback);

}
