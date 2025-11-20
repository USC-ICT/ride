using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

using Debug = UnityEngine.Debug;

namespace Ride.TextToSpeech
{
    /*
     * ElevenLabsTextToSpeech (Unity MonoBehaviour)
     * --------------------------------------------
     * Purpose
     *   Thin Unity wrapper around the ElevenLabs REST API. Fetches the voice list and
     *   synthesizes speech to an AudioClip, then writes a WAV file for downstream use.
     *
     * How it is used in RIDE
     *   - The higher-level system (TextToSpeechSystemElevenLabs) selects a voice by name,
     *     waits for voices to be available, and invokes ConvertTextToSpeechCoroutine().
     *   - For WebGL builds, audio is typically generated via a Lambda proxy (to S3) and this
     *     MB is bypassed; for Desktop/Editor this MB talks to ElevenLabs directly.
     *
     * Key members
     *   - IEnumerator GetAvailableVoicesCoroutine()
     *       Calls GET /v1/voices; fills AvailableVoices, AvailableVoiceNames/IDs, Voices, then sets VoicesReady.
     *   - IEnumerator ConvertTextToSpeechCoroutine(string text)
     *       POST /v1/text-to-speech/{voice_id} (Accept: audio/mpeg), decodes MP3 to AudioClip,
     *       then writes a 16-bit PCM WAV at Application.persistentDataPath (savedFilePath).
     *   - IEnumerator WaitForVoices()
     *       Yields until VoicesReady is true.
     *
     * Configuration
     *   - Requires ConfigurationSystemUnity with config.elevenLabs.apiKey (xi-api-key header).
     *   - Model selection is controlled by 'currentModel' -> GetModelId().
     *
     * Platforms
     *   - Desktop/Editor: used directly to contact ElevenLabs, decode MP3, and save WAV.
     *   - WebGL: usually NOT used for synthesis; audio comes from an S3 URL returned by a Lambda proxy.
     *
     * Notes / Gotchas
     *   - JSON body is currently constructed via string interpolation. If text contains quotes or control
     *     chars, escape or switch to JsonUtility.ToJson() to avoid malformed JSON.
     *   - Consider setting UnityWebRequest.timeouts and trimming verbose logs for production.
     *   - WAV files are larger than MP3; this is intentional for compatibility with downstream systems.
     */
    public class ElevenLabsTextToSpeech : MonoBehaviour
    {
        public enum Model
        {
            EnglishV1,
            MultilingualV1,
            MultilingualV2,
            FlashV2_5,
            TurboV2_5,
            V3
        }

        [Serializable]
        public class VoiceData
        {
            public string name;
            public string voice_id;
        }

        [Serializable]
        public class VoiceDataContainer
        {
            public List<VoiceData> voices;
        }

        [Serializable]
        public class ElevenLabsTimestampsResult
        {
            public string audio_base64;
            public ElevenLabsAlignment alignment;
            public ElevenLabsAlignment normalized_alignment;
        }

        public class ElevenLabsAlignment
        {
            public char[] characters;
            public double[] character_start_times_seconds;
            public double[] character_end_times_seconds;
        }

        const string BaseUrl = "https://api.elevenlabs.io/v1/";
        const int RescaleFactor = 32767;

        string m_apiKey;

        public float clipTime { get; private set; }

        public string savedFilePath;

        public AudioSource audioSource;
        [SerializeField]
        private int selectedVoiceIndex = 0;

        public int SelectedVoiceIndex
        {
            get { return selectedVoiceIndex; }
            set
            {
                if (value >= 0 && value < AvailableVoices.Count)
                    selectedVoiceIndex = value;
                else
                    Debug.LogError("Invalid voice index selected.");
            }
        }

        public List<VoiceData> AvailableVoices { get; private set; } = new List<VoiceData>();
        public List<string> AvailableVoiceNames { get; private set; } = new List<string>();
        public List<string> AvailableVoiceIDs { get; private set; } = new List<string>();

        public List<string> Voices;
        public bool VoicesReady;

        public Model currentModel;

        void Awake()
        {
            InitializeAudioSource();
            savedFilePath = Application.persistentDataPath + "/saved_audio.wav";
        }

        private void Start()
        {
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            m_apiKey = configSystem.config.elevenLabs.apiKey;

            StartCoroutine(GetAvailableVoicesCoroutine());
        }

        private string GetModelId()
        {
            switch (currentModel)
            {
                case Model.EnglishV1: return "eleven_monolingual_v1";
                case Model.MultilingualV1: return "eleven_multilingual_v1";
                case Model.MultilingualV2: return "eleven_multilingual_v2";
                default: throw new NotImplementedException();
            }
        }

        private void InitializeAudioSource()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        public IEnumerator GetAvailableVoicesCoroutine()
        {
            using (var www = UnityWebRequest.Get(BaseUrl + "voices"))
            {
                www.SetRequestHeader("Accept", "application/json");
                www.SetRequestHeader("xi-api-key", m_apiKey);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                    yield break;
                }

                var voiceDataContainer = JsonUtility.FromJson<VoiceDataContainer>(www.downloadHandler.text);
                AvailableVoices = voiceDataContainer.voices;
                AvailableVoiceNames = AvailableVoices.Select(voice => voice.name).ToList();
                AvailableVoiceIDs = AvailableVoices.Select(voice => voice.voice_id).ToList();

                // populate Voices list with voice names
                Voices = AvailableVoices.Select(voice => voice.name).ToList();
                VoicesReady = true;

                //foreach (var voice in AvailableVoices)
                //    Debug.Log($"Voice Name: {voice.name}, Voice ID: {voice.voice_id}");
            }
        }

        public IEnumerator ConvertTextToSpeechCoroutine(string text)
        {
            if (SelectedVoiceIndex != -1 && SelectedVoiceIndex < AvailableVoices.Count)
            {
                var voiceID = AvailableVoices[SelectedVoiceIndex].voice_id;
                //yield return StartCoroutine(GetAudioClip(text, voiceID));
                yield return StartCoroutine(GetAudioClipWithTimestamps(text, voiceID));
            }
            else
            {
                Debug.LogError("Invalid voice selected.");
                yield break;
            }
        }

        public static string PreprocessString(string input)
        {
            // Replace newlines and any other non-standard characters
            string processedInput = Regex.Replace(input, @"\t|\n|\r", " ");
            // add additional lines of processing here

            return processedInput;
        }

        public void ConvertTextToSpeechClip(string text)
        {
            text = PreprocessString(text); // preprocess the text

            if (SelectedVoiceIndex != -1 && SelectedVoiceIndex < AvailableVoices.Count)
            {
                var voiceID = AvailableVoices[SelectedVoiceIndex].voice_id;
                StartCoroutine(GetAudioClipPlay(text, voiceID));
            }
            else
            {
                Debug.LogError("Invalid voice selected.");
            }
        }

        public void ConvertTextToSpeechStream(string text)
        {
            text = PreprocessString(text); // preprocess the text

            if (SelectedVoiceIndex != -1 && SelectedVoiceIndex < AvailableVoices.Count)
            {
                var voiceID = AvailableVoices[SelectedVoiceIndex].voice_id;
                StartCoroutine(GetStreamingAudioClip(text, voiceID));
            }
            else
            {
                Debug.LogError("Invalid voice selected.");
            }
        }            

        private IEnumerator GetAudioClip(string textToSpeak, string voiceID)
        {
            textToSpeak = PreprocessString(textToSpeak); // preprocess the text

            var postData = $"{{ \"text\": \"{textToSpeak}\", \"model_id\": \"{GetModelId()}\", \"voice_settings\": {{ \"stability\": 0.5, \"similarity_boost\": 0.5 }} }}";
            var ttsUrl = BaseUrl + "text-to-speech/" + voiceID;

            using (var www = UnityWebRequestMultimedia.GetAudioClip(ttsUrl, AudioType.MPEG))
            {
                www.method = UnityWebRequest.kHttpVerbPOST;
                www.SetRequestHeader("Accept", "audio/mpeg");
                www.SetRequestHeader("xi-api-key", m_apiKey);
                www.SetRequestHeader("Content-Type", "application/json");

                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                    yield break;
                }                

                var clip = DownloadHandlerAudioClip.GetContent(www);

                clipTime = clip.length;
                SaveAudioClipToWav(clip, savedFilePath);
            }
        }

        private IEnumerator GetAudioClipWithTimestamps(string textToSpeak, string voiceID)
        {
            textToSpeak = PreprocessString(textToSpeak); // preprocess the text

            var postData = $"{{ \"text\": \"{textToSpeak}\", \"model_id\": \"{GetModelId()}\", \"voice_settings\": {{ \"stability\": 0.5, \"similarity_boost\": 0.5 }} }}";
            var ttsUrl = BaseUrl + "text-to-speech/" + voiceID + "/with-timestamps";

            using (var www = UnityWebRequest.Get(ttsUrl))
            {
                www.method = UnityWebRequest.kHttpVerbPOST;
                www.SetRequestHeader("xi-api-key", m_apiKey);
                www.SetRequestHeader("Content-Type", "application/json");

                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                    yield break;
                }

                ElevenLabsTimestampsResult result = JsonUtility.FromJson<ElevenLabsTimestampsResult>(www.downloadHandler.text);

                // Convert base64 to audio clip and save
                yield return StartCoroutine(DecodeBase64ToAudioClip(result.audio_base64));
            }
        }

        private IEnumerator GetAudioClipPlay(string textToSpeak, string voiceID)
        {
            var postData = $"{{ \"text\": \"{textToSpeak}\", \"model_id\": \"{GetModelId()}\"}}";
            var ttsUrl = BaseUrl + "text-to-speech/" + voiceID;

            using (var www = UnityWebRequestMultimedia.GetAudioClip(ttsUrl, AudioType.MPEG))
            {
                www.method = UnityWebRequest.kHttpVerbPOST;
                www.SetRequestHeader("Accept", "audio/mpeg");
                www.SetRequestHeader("xi-api-key", m_apiKey);
                www.SetRequestHeader("Content-Type", "application/json");

                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
            }
        }

        private IEnumerator GetStreamingAudioClip(string textToSpeak, string voiceID)
        {
            var postData = $"{{\"text\": \"{textToSpeak}\", \"model_id\": \"{GetModelId()}\"}}";
            var ttsUrl = BaseUrl + "text-to-speech/" + voiceID + "/stream";

            using (var www = new UnityWebRequest(ttsUrl, UnityWebRequest.kHttpVerbPOST))
            {
                www.downloadHandler = new DownloadHandlerAudioClip(www.url, AudioType.MPEG);
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;

                www.SetRequestHeader("Accept", "audio/mpeg");
                www.SetRequestHeader("xi-api-key", m_apiKey);
                www.SetRequestHeader("Content-Type", "application/json");

                www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(postData));

                // Log the request details
                Debug.Log("Sending POST request to URL: " + ttsUrl);
                Debug.Log("POST data: " + postData);

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("Successfully received response from server.");
                    var clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    audioSource.Play();
                }
                else
                {
                    // Log error details
                    Debug.Log($"Request failed with error: {www.error}");
                    Debug.Log($"Response code: {www.responseCode}");
                }
            }
        }

        public IEnumerator WaitForVoices()
        {
            while (!VoicesReady)
                yield return null;
        }

        private static void SaveAudioClipToWav(AudioClip clip, string filePath)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            var bytes = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                var byteArr = BitConverter.GetBytes((short)(samples[i] * RescaleFactor));
                byteArr.CopyTo(bytes, i * 2);
            }

            WriteWav(filePath, bytes, clip.channels, clip.frequency);
        }

        private static void WriteWav(string filename, byte[] bytes, int channels, int sampleRate)
        {
            using (var stream = new FileStream(filename, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + bytes.Length);
                writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }

        private IEnumerator DecodeBase64ToAudioClip(string base64String)
        {
            // Decode base64 string to byte array
            byte[] audioData = Convert.FromBase64String(base64String);

            // Determine audio type based on header bytes
            AudioType audioType = DetectAudioType(audioData);

            // Create a temporary file path
            string tempPath = Application.temporaryCachePath + "/tempAudio" + GetExtension(audioType);

            // Write bytes to temporary file
            System.IO.File.WriteAllBytes(tempPath, audioData);

            // Load audio clip using UnityWebRequest
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, audioType))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    AudioClip audioClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);

                    clipTime = audioClip.length;
                    SaveAudioClipToWav(audioClip, savedFilePath);

                    //Debug.Log("Audio clip loaded successfully!");
                }
                else
                {
                    Debug.LogError("Failed to load audio: " + www.error);
                }
            }

            // Clean up temporary file
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
        }

        private AudioType DetectAudioType(byte[] data)
        {
            // Check file signature (magic numbers)
            if (data.Length < 4) return AudioType.UNKNOWN;

            // WAV file signature
            if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46)
                return AudioType.WAV;

            // MP3 file signature
            if (data[0] == 0xFF && (data[1] & 0xE0) == 0xE0)
                return AudioType.MPEG;
            if (data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
                return AudioType.MPEG;

            // OGG file signature
            if (data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67 && data[3] == 0x53)
                return AudioType.OGGVORBIS;

            return AudioType.UNKNOWN;
        }

        private string GetExtension(AudioType type)
        {
            switch (type)
            {
                case AudioType.WAV: return ".wav";
                case AudioType.MPEG: return ".mp3";
                case AudioType.OGGVORBIS: return ".ogg";
                default: return ".audio";
            }
        }
    }
}
