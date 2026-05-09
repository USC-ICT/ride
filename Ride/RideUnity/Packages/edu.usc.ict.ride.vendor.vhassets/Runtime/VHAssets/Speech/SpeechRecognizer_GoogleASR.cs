using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace VHAssets
{
public class SpeechRecognizer_GoogleASR : SpeechRecognizer
{
    [Serializable]
    class GoogleAsrAlternative
    {
        public string transcript;
        public float confidence;
    }

    [Serializable]
    class GoogleAsrResult
    {
        public GoogleAsrAlternative[] alternative;
        public bool final;
    }

    [Serializable]
    class GoogleAsrResponse
    {
        public GoogleAsrResult[] result;
        public int result_index;
    }

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

        AppendRecognitionResults(wwwtext, recognizerResults);

        DispatchResults(recognizerResults);
    }

    /// <summary>
    /// Parses Google ASR response text and appends any recognized alternatives to <paramref name="recognizerResults"/>.
    /// </summary>
    /// <remarks>
    /// This endpoint needs special handling because the legacy Google Speech API v2 response is not always returned as a
    /// single JSON document. In practice, it may return multiple JSON objects separated by newlines, commonly including
    /// an initial line with an empty <c>result</c> array followed by a later line containing the actual recognition data.
    ///
    /// Unity's <see cref="JsonUtility"/> can only deserialize one JSON object at a time, so this method reads the
    /// response line-by-line and deserializes each non-empty line independently.
    ///
    /// This code previously used SimpleJSON, whose parser was tolerant of this multiline payload format. The parser was
    /// recently refactored to remove that dependency, so this method preserves the same behavior explicitly while using
    /// Unity's built-in JSON support.
    ///
    /// Only the subset of fields used by this recognizer is modeled: <c>result</c>, <c>alternative</c>,
    /// <c>transcript</c>, and optional <c>confidence</c>. Extra fields are ignored.
    /// </remarks>
    /// <param name="responseText">Raw response body returned by the Google ASR request.</param>
    /// <param name="recognizerResults">Destination list that receives parsed recognition alternatives.</param>
    static void AppendRecognitionResults(string responseText, List<RecognizerResult> recognizerResults)
    {
        // Google Speech API v2 responses can contain multiple JSON documents separated by newlines.
        using StringReader reader = new StringReader(responseText);
        string line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line))
                continue;

            GoogleAsrResponse response;
            try
            {
                response = JsonUtility.FromJson<GoogleAsrResponse>(line);
            }
            catch (ArgumentException ex)
            {
                Debug.LogWarning($"GoogleASR failed to parse response line: {ex.Message}");
                continue;
            }

            if (response?.result == null)
                continue;

            foreach (GoogleAsrResult result in response.result)
            {
                if (result?.alternative == null)
                    continue;

                foreach (GoogleAsrAlternative alternative in result.alternative)
                {
                    if (alternative == null || string.IsNullOrEmpty(alternative.transcript))
                        continue;

                    //Debug.Log(alternative.transcript + " " + alternative.confidence);

                    recognizerResults.Add(new RecognizerResult(alternative.transcript, alternative.confidence));
                }
            }
        }
    }
    #endregion
}
}
