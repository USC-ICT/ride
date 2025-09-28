using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

namespace VHAssets
{
public class SpeechRecognizer_GoogleASR : SpeechRecognizer
{
    #region Constants
    const string AsrUrl = "https://www.google.com/speech-api/v2/recognize";
    #endregion

    #region Variables
    //public string m_ClientName = "chromium";
    public string m_Language = "en-US";
    public int m_MaxResults = 3;
    string key = "0123456789abcdefghijklmnopqrstuvwxyzABC";
    #endregion

    #region Functions
    protected override void PerformRecognition(AudioClip clip)
    {
        base.PerformRecognition(clip);

        StartCoroutine(MakeWebRequest(clip));
    }

    IEnumerator MakeWebRequest(AudioClip clip)
    {
        List<RecognizerResult> recognizerResults = new List<RecognizerResult>();

        // for information on google url get and post params, go here: https://github.com/gillesdemey/google-speech-v2
        string url = string.Format("{0}?lang={1}&pfilter=0&maxresults={2}&key={3}&output=json&client=chromium&pfilter=2", AsrUrl, m_Language, m_MaxResults, key);

        Debug.Log(url);
        // Google ASR requires the audio data to be in flac format
        byte[] flacData = AudioConverter.ConvertClipToFlac(clip, VHFile.GetStreamingAssetsPath() + "Flac/testwav.wav");

        WWWForm form = new WWWForm();
        form.AddBinaryData("body", flacData);

        // make the request and wait
        UnityWebRequest www = UnityWebRequest.Post(url, form);
        www.SetRequestHeader("Content-Type", "audio/x-flac; rate=" + clip.frequency);
        www.SetRequestHeader("charset", "utf-8");
        yield return www.SendWebRequest();

        var wwwtext = www.downloadHandler.text;

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError("GoogleASR webrequest error: " + www.error);
            DispatchResults(recognizerResults);
            yield break;
        }
        else if (string.IsNullOrEmpty(wwwtext))
        {
            //Debug.LogError("GoogleASR webrequest didn't return anything");
            DispatchResults(recognizerResults);
            yield break;
        }

        // parse the json results
        //Debug.Log(www.text);

        JSONNode node = JSON.Parse(wwwtext);

        JSONArray results = node["result"].AsArray;
        for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
        {
            JSONArray alternativeArray = results[resultIndex]["alternative"].AsArray;

            for (int alternativeIndex = 0; alternativeIndex < alternativeArray.Count; alternativeIndex++)
            {
                float confidence = 0;
                JSONNode currNode = alternativeArray[alternativeIndex];
                if (currNode["confidence"] != null)
                {
                    confidence = currNode["confidence"].AsFloat;
                }
                //Debug.Log(currNode["utterance"] + " " + confidence);
                recognizerResults.Add(new RecognizerResult(currNode["transcript"], confidence));
            }
        }

        DispatchResults(recognizerResults);
    }
    #endregion
}
}
