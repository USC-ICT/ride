using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace VHAssets
{
public class MapLocator : MonoBehaviour
{
    #region Constants
    const string MapsUrl = "https://maps.googleapis.com/maps/api/staticmap?center={0}&zoom=13&size={1}x{2}&maptype=roadmap&markers=color:blue|label:S|40.702147,-74.015794&markers=color:green|label:G|40.711614,-74.012318&markers=color:red|label:C|40.718217,-73.998284";
    public delegate void OnReceivedMapImage(Texture2D map);
    #endregion

    #region Variables
    public int m_ImageWidth = 600;
    public int m_ImageHeight = 300;
    #endregion

    #region Functions
    void Start()
    {
    }

    void Update()
    {
    }

    public void Locate(string location, OnReceivedMapImage onReceivedMapImage)
    {
        StartCoroutine(LocateCoroutine(location, onReceivedMapImage));
    }

    IEnumerator LocateCoroutine(string location, OnReceivedMapImage onReceivedMapImage)
    {
        string url = string.Format(MapsUrl, FormatLocation(location), m_ImageWidth, m_ImageHeight);
        var www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError("Map Locator Error: " + www.error);
        }

        onReceivedMapImage?.Invoke(DownloadHandlerTexture.GetContent(www));
    }

    string FormatLocation(string location)
    {
        return location.Replace(' ', '+');
    }
    #endregion
}
}
