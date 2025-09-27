using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride
{
    public class WebProvider : MilSymbolProvider
    {
        public override void RequestTextureForSIDC(string sidc, Action<Texture2D> callback)
        {
            StartCoroutine(GetTexture(sidc, callback));
        }

        private IEnumerator GetTexture(string sidc, Action<Texture2D> callback)
        {
            string url = string.Format("http://localhost:3000/symbol/get/{0}.png", sidc);

            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.Log(req.error);
                }
                else
                {
                    // Get downloaded asset bundle
                    var texture = DownloadHandlerTexture.GetContent(req);
                    callback(texture);
                }
            }
        }
    }

}
