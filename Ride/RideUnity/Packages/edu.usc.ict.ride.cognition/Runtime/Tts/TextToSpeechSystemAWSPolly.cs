using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// AWS Polly text-to-speech system implementation for the RIDE framework.
    /// </summary>
    /// <remarks>
    /// This class provides full text-to-speech functionality using Amazon Polly, including optional
    /// viseme-based lip synchronization and word/sentence timing data.
    ///
    /// It supports asynchronous generation of audio and speech mark data using coroutines,
    /// making it compatible with Unity WebGL builds (via proxy) and non-WebGL platforms (via the Polly SDK).
    ///
    /// During initialization, <see cref="SystemInit"/> authenticates with AWS using credentials
    /// from the RIDE configuration system and retrieves the list of supported voices from the Polly service.
    /// 
    /// Voice-dependent methods such as <see cref="GetAvailableVoices"/>, <see cref="GenerateAudioSpeechMap"/>,
    /// and <see cref="StartTextToSpeechGeneration"/> will automatically block internally until the voice list
    /// is available. Alternatively, clients may poll <see cref="TextToSpeechSystemUnity.VoicesResolved"/>.
    ///
    /// For non-WebGL builds, this class uses the native AWS SDK for Polly. For WebGL, it uses the configured
    /// server-side proxy endpoint for voice lookup, viseme generation, and audio generation.
    ///
    /// Implements:
    /// - <see cref="ILipsyncedTextToSpeechSystem"/> for audio + lipsync XML output
    /// - <see cref="ILipsyncMapper"/> for converting IPA phonemes to FaceFX visemes
    /// </remarks>
    public class TextToSpeechSystemAWSPolly : TextToSpeechSystemLipsynced, ILipsyncMapper
    {
        [Serializable]
        class SpeechMark
        {
            public double time = 0; // the timestamp in milliseconds from the beginning of the corresponding audio stream
            public string type = ""; // the type of speech mark (sentence, word, viseme, or ssml).
            public int start = 0; // the offset in bytes of the start of the object in the input text (not including viseme marks)
            public int end = 0; // the offset in bytes of the object's end in the input text (not including viseme marks)
            public string value = ""; // this varies depending on the type of speech mark- SSML: <mark> SSML tag | viseme: the viseme name | word or sentence: a substring of the input text, as delimited by the start and end fields
        }

        class IPAtoFacefxMap
        {
            public readonly string ipaPhoneme = "";
            public readonly string[] facefxVisemes;
            public readonly float[] amounts;

            public IPAtoFacefxMap(string _ipaPhoneme, string _facefxViseme, float _amount)
            {
                ipaPhoneme = _ipaPhoneme;
                facefxVisemes = new string[] { _facefxViseme };
                amounts = new float[] { _amount };
            }

            public IPAtoFacefxMap(string _ipaPhoneme, string[] _facefxVisemes, float[] _amounts)
            {
                ipaPhoneme = _ipaPhoneme;
                facefxVisemes = _facefxVisemes;
                amounts = _amounts;
            }
        }

        [Serializable]
        private struct PollyRawWordMark
        {
            public string Value;
            public double TimeSeconds;

            public PollyRawWordMark(string value, double timeSeconds)
            {
                Value = value;
                TimeSeconds = timeSeconds;
            }

            public override string ToString() => $"[{TimeSeconds:0.000}] '{Value}'";
        }

        [Serializable]
        private struct PollyRawVisemeMark
        {
            public string Value;
            public double TimeSeconds;

            public PollyRawVisemeMark(string value, double timeSeconds)
            {
                Value = value;
                TimeSeconds = timeSeconds;
            }

            public override string ToString() => $"[{TimeSeconds:0.000}] {Value}";
        }

#if UNITY_WEBGL
        [Serializable]
        class VoicesReply
        {
            public string[] voices;
        }

        [Serializable]
        private class TTSRequest
        {
            public string text;
            public string voice;
        }

        [Serializable]
        private class TTSReplyAudio
        {
            public string url;
        }

        [Serializable]
        private class TTSReplyVisemes
        {
            public SpeechMark[] visemes;
        }
#endif

        Dictionary<string, IPAtoFacefxMap> m_IPAtoFacefxMap = new Dictionary<string, IPAtoFacefxMap>
        {
            { "p",  new IPAtoFacefxMap("p",  new[] { "BMP" }, new[] { 1f }) },
            { "t",  new IPAtoFacefxMap("t",  new[] { "open", "tRoof" }, new[] { 0.6f, 1f }) },
            { "S",  new IPAtoFacefxMap("S",  new[] { "ShCh" }, new[] { 1f }) },
            { "T",  new IPAtoFacefxMap("T",  new[] { "open", "tTeeth" }, new[] { 0.4f, 0.9f }) },
            { "f",  new IPAtoFacefxMap("f",  new[] { "FV" }, new[] { 1f }) },
            { "k",  new IPAtoFacefxMap("k",  new[] { "open", "tBack" }, new[] { 0.3f, 1f }) },
            { "i",  new IPAtoFacefxMap("i",  new[] { "open", "wide" }, new[] { 0.4f, 0.1f }) },
            { "r",  new IPAtoFacefxMap("r",  new[] { "ShCh", "W" }, new[] { 0.2f, 0.1f }) },
            { "s",  new IPAtoFacefxMap("s",  new[] { "open", "tRoof" }, new[] { 0.15f, 0.8f }) },
            { "u",  new IPAtoFacefxMap("u",  new[] { "W" }, new[] { 0.9f }) },
            { "@",  new IPAtoFacefxMap("@",  new[] { "open", "wide" }, new[] { 0.35f, 0.1f }) },
            { "a",  new IPAtoFacefxMap("a",  new[] { "open", "wide" }, new[] { 0.7f, 0.15f }) },
            { "e",  new IPAtoFacefxMap("e",  new[] { "open", "wide" }, new[] { 0.4f, 0.25f }) },
            { "E",  new IPAtoFacefxMap("E",  new[] { "ShCh", "BMP" }, new[] { 0.3f, 0.1f }) },
            { "O",  new IPAtoFacefxMap("O",  new[] { "open" }, new[] { 0.7f }) },
            { "o",  new IPAtoFacefxMap("o",  new[] { "open", "W" }, new[] { 0.25f, 0.45f }) },
            { "sil", new IPAtoFacefxMap("sil", new[] { "open" }, new[] { 0.0f }) },
        };

        string[] m_voices = new string[] { "Loading..." };

#if !UNITY_WEBGL
        Amazon.Polly.AmazonPollyClient m_pollyClient;
        bool ConnectionActive => m_pollyClient != null;
#endif

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

#if !UNITY_WEBGL
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            string m_awsAccessKey = configSystem.config.awsPolly.accessKey;
            string m_awsSecretKey = configSystem.config.awsPolly.secretKey;
            m_pollyClient = new Amazon.Polly.AmazonPollyClient(m_awsAccessKey, m_awsSecretKey, Amazon.RegionEndpoint.USWest2);
#endif

            VoiceListStatus = VoiceListState.NotFetched;
            RefreshVoices();
        }

        /// <inheritdoc/>
        public override string[] GetAvailableVoices() => m_voices;

        /// <inheritdoc/>
        /// <remarks>
        /// Amazon Polly's synchronous speech synthesis accepts 3000 billed characters per request
        /// and rejects anything longer, so callers must shorten or split text before submitting it.
        /// </remarks>
        public override int MaxRequestCharacters => 3000;


        /// <inheritdoc/>
        public void GenerateAudioSpeechMap(string voice, string text, Action<AudioSpeechMap> resultCallback)
        {
            if (voice == string.Empty) voice = GetAvailableVoices()[0];

            StartCoroutine(GenerateSpeechMarksCoroutine(voice, text, resultCallback));
        }

        private IEnumerator GenerateSpeechMarksCoroutine(string voice, string text, Action<AudioSpeechMap> resultCallback)
        {
            yield return WaitForVoices();

            if (string.IsNullOrEmpty(voice))
                voice = GetAvailableVoices().Length > 0 ? GetAvailableVoices()[0] : "Joanna";

#if !UNITY_WEBGL
            var markRequest = new Amazon.Polly.Model.SynthesizeSpeechRequest
            {
                Engine = "neural",
                OutputFormat = Amazon.Polly.OutputFormat.Json,
                Text = text,
                VoiceId = voice,
                SpeechMarkTypes = new List<string> { "sentence", "viseme", "word" }
            };

            var task = m_pollyClient.SynthesizeSpeechAsync(markRequest);
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted || task.Result == null)
            {
                Debug.LogError($"GenerateSpeechMarksCoroutine() - SynthesizeSpeechAsync failed.");
                yield break;
            }

            var markResponse = task.Result;
            var markStream = markResponse.AudioStream;

            var marks = new List<SpeechMark>();
            using (StreamReader reader = new StreamReader(markStream))
            {
                // read the marks, line by line from the stream we received
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    marks.Add(JsonUtility.FromJson<SpeechMark>(line));
                }
            }

            var map = new AudioSpeechMap
            {
                soundFile = $"{Application.persistentDataPath}/pollyTTS.mp3",
                WordTimingList = new List<WordTimingData>(),
                MarkList = new List<KeyValuePairS<string, double>>(),
                VisemeList = new List<GenerateAudioReplyViseme>(),
            };

            ParseMarks(marks, ref map);
            LogPollyProviderDebug(marks);
            resultCallback?.Invoke(map);
#else
            yield return WaitForVoices();

            string url = ConfigurationSystemUnity.GetPollyTtsProxyEndpoint("visemes");
            if (string.IsNullOrWhiteSpace(url))
            {
                resultCallback?.Invoke(null);
                yield break;
            }

            string jsonBody = JsonUtility.ToJson(new TTSRequest() { text = text, voice = voice });

            using (var request = UnityWebRequest.Put(url, jsonBody))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"GenerateSpeechMarksCoroutine() - Error: {request.error}");
                    resultCallback?.Invoke(null);
                    yield break;
                }

                TTSReplyVisemes reply;
                try
                {
                    reply = JsonUtility.FromJson<TTSReplyVisemes>(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"GenerateSpeechMarksCoroutine() - JSON parse error: {ex.Message}");
                    resultCallback?.Invoke(null);
                    yield break;
                }

                var marks = reply.visemes;

                var map = new AudioSpeechMap
                {
                    soundFile = "",
                    WordTimingList = new List<WordTimingData>(),
                    MarkList = new List<KeyValuePairS<string, double>>(),
                    VisemeList = new List<GenerateAudioReplyViseme>(),
                };

                ParseMarks(marks, ref map);
                LogPollyProviderDebug(marks);
                resultCallback?.Invoke(map);
            }
#endif
        }

        private IEnumerator StartTextToSpeechGenerationCoroutine(string voice, string text)
        {
            yield return WaitForVoices();

            if (string.IsNullOrEmpty(voice))
                voice = GetAvailableVoices().Length > 0 ? GetAvailableVoices()[0] : "Joanna";

#if !UNITY_WEBGL
            var audioRequest = new Amazon.Polly.Model.SynthesizeSpeechRequest
            {
                Engine = "neural",
                OutputFormat = Amazon.Polly.OutputFormat.Mp3,
                Text = text,
                VoiceId = voice,
            };

            Task<Amazon.Polly.Model.SynthesizeSpeechResponse> task = null;

            try
            {
                task = m_pollyClient.SynthesizeSpeechAsync(audioRequest);
            }
            catch (Exception e)
            {
                Debug.LogError($"StartTextToSpeechGenerationCoroutine() - Polly SynthesizeSpeechAsync threw immediately: {e.Message}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted || task.Result == null)
            {
                Debug.LogError($"StartTextToSpeechGenerationCoroutine() - Polly SynthesizeSpeechAsync failed: {task.Exception?.Flatten().InnerException?.Message}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            var audioResponse = task.Result;

            string fileName = $"{Application.persistentDataPath}/pollyTTS.mp3";

            var writeTask = WriteStreamToFileAsync(audioResponse.AudioStream, fileName);
            while (!writeTask.IsCompleted)
                yield return null;

            if (writeTask.IsFaulted)
            {
                Debug.LogError($"WriteStreamToFileAsync failed: {writeTask.Exception?.Flatten().InnerException?.Message}");
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            CompleteTextToSpeechGeneration(fileName);
#else
            string url = ConfigurationSystemUnity.GetPollyTtsProxyEndpoint("audio");
            if (string.IsNullOrWhiteSpace(url))
            {
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            string jsonBody = JsonUtility.ToJson(new TTSRequest() { text = text, voice = voice });

            using (var request = UnityWebRequest.Put(url, jsonBody))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"StartTextToSpeechGenerationCoroutine() - Error: {request.error}");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                TTSReplyAudio reply;
                try
                {
                    reply = JsonUtility.FromJson<TTSReplyAudio>(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"StartTextToSpeechGenerationCoroutine() - JSON parse error: {ex.Message}");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                if (reply == null || string.IsNullOrEmpty(reply.url))
                {
                    Debug.LogError($"StartTextToSpeechGenerationCoroutine() - No URL returned from Lambda.");
                    CompleteTextToSpeechGeneration(null);
                    yield break;
                }

                Debug.Log($"[WebGL] MP3 available at: {reply.url}");

                CompleteTextToSpeechGeneration(reply.url);
            }
#endif
        }

        /// <inheritdoc/>
        protected override void StartTextToSpeechGeneration(string voice, string text)
        {
            StartCoroutine(StartTextToSpeechGenerationCoroutine(voice, text));
        }

        /// <inheritdoc/>
        protected override void StartLipsyncGeneration(string voice, string text)
        {
            GenerateAudioSpeechMap(voice, text, OnAudioSpeechGeneration);
        }

        /// <summary>
        /// Called when lipsync map has been built and ready for schedule construction.
        /// <see cref="AudioSpeechMap"/>
        /// </summary>
        private void OnAudioSpeechGeneration(AudioSpeechMap audioSpeechMap)
        {
            string xml = audioSpeechMap != null ? TextToSpeechXMLBuilder.BuildSpeechXML(audioSpeechMap) : string.Empty;
            LogSpeechXmlDebug(audioSpeechMap, xml, "Polly");
            CompleteLipsyncGeneration(xml);
        }

        /// <summary>
        /// Convert the aws speech marks into proper TtsRelay output data
        /// </summary>
        /// <param name="marks"></param>
        /// <param name="generateAudioReplyReturn"><see cref="AudioSpeechMap"/></param>
        private void ParseMarks(IEnumerable<SpeechMark> marks, ref AudioSpeechMap generateAudioReplyReturn)
        {
            const double MILLISECONDS_IN_SECOND = 1000;
            int wordIndex = 0;
            int markIndex = 0;
            double lastTime = 0;
            foreach (var mark in marks)
            {
                lastTime = mark.time / MILLISECONDS_IN_SECOND;

                if (mark.type == "viseme")
                {
                    if (m_IPAtoFacefxMap.ContainsKey(mark.value))
                    {
                        var map = m_IPAtoFacefxMap[mark.value];
                        for (int i = 0; i < map.facefxVisemes.Length; i++)
                            generateAudioReplyReturn.VisemeList.Add(new GenerateAudioReplyViseme(map.facefxVisemes[i], mark.time / MILLISECONDS_IN_SECOND, map.amounts[i]));
                    }
                    else
                    {
                        Debug.Log($"Failed to map IPA {mark.value} to facefx. Disgarding");
                    }
                }
                else if (mark.type == "word")
                {
                    double time = mark.time / MILLISECONDS_IN_SECOND;
                    generateAudioReplyReturn.MarkList.Add(new KeyValuePairS<string, double>("T" + markIndex, time));
                    generateAudioReplyReturn.MarkList.Add(new KeyValuePairS<string, double>("T" + (markIndex + 1), 0));

                    generateAudioReplyReturn.WordTimingList.Add(new WordTimingData(mark.value, time, 0));

                    int prevWorldIndex = wordIndex - 1;
                    if (prevWorldIndex >= 0)
                    {
                        ChangeEndWordTiming(generateAudioReplyReturn.WordTimingList, prevWorldIndex, time);
                        ChangeEndMarkTiming(generateAudioReplyReturn.MarkList, markIndex - 1, time);
                    }

                    wordIndex += 1;
                    markIndex += 2;
                }
            }

            if (generateAudioReplyReturn.WordTimingList.Count > 0)
            {
                ChangeEndMarkTiming(generateAudioReplyReturn.MarkList, generateAudioReplyReturn.MarkList.Count - 1, lastTime);
                ChangeEndWordTiming(generateAudioReplyReturn.WordTimingList, generateAudioReplyReturn.WordTimingList.Count - 1, lastTime);
            }
        }

        private void LogPollyProviderDebug(IEnumerable<SpeechMark> marks)
        {
            if (!lipsyncDebugOutput || marks == null)
                return;

            const double millisecondsInSecond = 1000.0;

            int totalMarks = 0;
            int wordCount = 0;
            int visemeCount = 0;
            int sentenceCount = 0;
            var words = new List<PollyRawWordMark>();
            var visemes = new List<PollyRawVisemeMark>();
            var unmappedTokens = new HashSet<string>(StringComparer.Ordinal);

            foreach (var mark in marks)
            {
                if (mark == null)
                    continue;

                totalMarks++;
                double timeSeconds = mark.time / millisecondsInSecond;

                if (mark.type == "word")
                {
                    wordCount++;
                    words.Add(new PollyRawWordMark(mark.value ?? string.Empty, timeSeconds));
                    // Debug.Log($"[Polly Word Raw] {timeSeconds:0.000} '{mark.value}'");
                }
                else if (mark.type == "viseme")
                {
                    visemeCount++;
                    visemes.Add(new PollyRawVisemeMark(mark.value ?? string.Empty, timeSeconds));
                    // Debug.Log($"[Polly Viseme Raw] {timeSeconds:0.000} {mark.value}");

                    if (string.IsNullOrEmpty(mark.value) || !m_IPAtoFacefxMap.ContainsKey(mark.value))
                        unmappedTokens.Add(mark.value ?? "<null>");
                }
                else if (mark.type == "sentence")
                {
                    sentenceCount++;
                }
            }

            Debug.Log($"[Polly Raw] TotalMarks={totalMarks}, Words={wordCount}, Visemes={visemeCount}, Sentences={sentenceCount}, MissingPhoneMappings={unmappedTokens.Count}");
            Debug.Log($"[Polly Words] {(words.Count == 0 ? "<none>" : string.Join(", ", words))}");
            Debug.Log($"[Polly Visemes] {(visemes.Count == 0 ? "<none>" : string.Join(", ", visemes))}");
            Debug.Log($"[Polly FaceFX Map] MissingPhoneMappings={(unmappedTokens.Count == 0 ? "<none>" : string.Join(", ", unmappedTokens))}");
        }

        /// <summary>
        /// Updates the end time of a word in the WordTimingList.
        /// </summary>
        /// <param name="wordTimings">The list of word timings.</param>
        /// <param name="index">The index of the word to update.</param>
        /// <param name="endTime">The end time to set.</param>
        private static void ChangeEndWordTiming(List<WordTimingData> wordTimings, int index, double endTime)
        {
            var wordTiming = wordTimings[index];
            wordTiming.End = endTime;
            wordTimings[index] = wordTiming;
        }

        /// <summary>
        /// Updates the end time of a mark in the MarkList.
        /// </summary>
        /// <param name="wordTimings">The list of marks.</param>
        /// <param name="index">The index of the mark to update.</param>
        /// <param name="endTime">The end time to set.</param>
        private static void ChangeEndMarkTiming(List<KeyValuePairS<string, double>> wordTimings, int index, double endTime)
        {
            var pair = wordTimings[index];
            pair.Value = endTime;
            wordTimings[index] = pair;
        }

        /// <summary>
        /// Asynchronously queries AWS Polly for the list of available TTS voices.
        /// </summary>
        /// <inheritdoc/>
        protected override IEnumerator FetchAvailableVoices()
        {
#if !UNITY_WEBGL
            if (!ConnectionActive)
            {
                Debug.LogWarning("AWS Polly not initialized.");
                m_voices = new string[] { };
                CompleteVoiceFetch(false);
                yield break;
            }

            var voices = new List<string>();

            var voiceRequest = new Amazon.Polly.Model.DescribeVoicesRequest()
            {
                Engine = "neural",
                LanguageCode = "en-US",
            };

            string nextToken = null;

            do
            {
                voiceRequest.NextToken = nextToken;

                Task<Amazon.Polly.Model.DescribeVoicesResponse> task = null;

                try
                {
                    task = m_pollyClient.DescribeVoicesAsync(voiceRequest);
                }
                catch (Exception e)
                {
                    Debug.LogError("FetchAvailableVoices() - Exception in DescribeVoicesAsync: " + e.Message);
                    m_voices = new string[] { };
                    CompleteVoiceFetch(false);
                    yield break;
                }

                while (!task.IsCompleted)
                    yield return null;

                if (task.IsFaulted || task.Result == null)
                {
                    Debug.LogError("FetchAvailableVoices() - DescribeVoicesAsync failed.");
                    m_voices = new string[] { };
                    CompleteVoiceFetch(false);
                    yield break;
                }

                var response = task.Result;
                foreach (var voice in response.Voices)
                    voices.Add(voice.Name);

                nextToken = response.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));

            m_voices = voices.ToArray();
            CompleteVoiceFetch(true);
#else
            string lambdaUrl = ConfigurationSystemUnity.GetPollyTtsProxyEndpoint("voices");
            if (string.IsNullOrWhiteSpace(lambdaUrl))
            {
                m_voices = new string[0];
                CompleteVoiceFetch(false);
                yield break;
            }

            using (var request = UnityWebRequest.Get(lambdaUrl))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"FetchAvailableVoices() - Failed to get voices: {request.error}");
                    m_voices = new string[] { };
                    CompleteVoiceFetch(false);
                    yield break;
                }

                try
                {
                    var reply = JsonUtility.FromJson<VoicesReply>(request.downloadHandler.text);
                    m_voices = reply.voices;
                    CompleteVoiceFetch(true);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"FetchAvailableVoices() - JSON parse error: {ex.Message}");
                    m_voices = new string[] { };
                    CompleteVoiceFetch(false);
                }
            }
#endif
        }
    }
}
