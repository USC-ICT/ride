using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// WebGL Azure Automatic Speech Recognition (ASR) bridge.
    ///
    /// This MonoBehaviour acts as the Unity-side endpoint for the Azure Speech SDK
    /// JavaScript implementation when running in WebGL. It exposes a small C# API
    /// for starting and stopping recognition and receives recognition events from
    /// JavaScript via Unity SendMessage callbacks.
    ///
    /// Architecture overview:
    /// - Unity C# calls into a .jslib plugin (AzureAsr.jslib).
    /// - The .jslib dynamically loads the Azure Speech SDK and helper scripts.
    /// - JavaScript sends recognition events back to this component via SendMessage.
    /// - Events are surfaced to C# through strongly-typed C# events.
    ///
    /// WebGL setup requirements:
    /// - A one-time editor setup step must be run to copy required JavaScript files
    ///   into StreamingAssets.
    /// - Open: RIDE > Cognition > ASR > WebGL Setup (StreamingAssets)
    /// - This copies the following files:
    ///     - speech-sdk.bundle.js
    ///     - AzureAsrBridge.js
    ///     - MicLevel.js
    ///
    /// Platform notes:
    /// - This bridge is only functional on WebGL builds.
    /// - All WebGL interop is guarded by UNITY_WEBGL && !UNITY_EDITOR.
    /// - Microphone access and device selection are controlled by the browser.
    /// - A user gesture (click/tap) is required to start microphone capture in WebGL.
    ///
    /// Security notes:
    /// - Inspector Azure API key fields are for development and debugging only.
    /// - Do NOT ship real Azure subscription keys in client-side WebGL builds.
    /// - Production deployments should use a secure token / proxy approach.
    ///
    /// JS -> C# callback contract (must match exactly):
    /// - OnAzureAsrPartial(string)
    /// - OnAzureAsrFinal(string)
    /// - OnAzureAsrError(string)
    /// - OnAzureAsrSession(string)
    /// - OnAzureAsrInfo(string)
    /// - OnError(string)        (generic error path)
    /// - OnMicLevel(string)
    /// </summary>
    public class AzureAsrBridge : RideMonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void AzureAsr_Start(string goName, string key, string region, string lang);
        [DllImport("__Internal")] private static extern void AzureAsr_Stop();
        [DllImport("__Internal")] private static extern void AzureAsr_Ping(string goName);
        [DllImport("__Internal")] private static extern void MicLevel_Start(string goName);
        [DllImport("__Internal")] private static extern void MicLevel_Stop();
#else
        private static void AzureAsr_Start(string goName, string key, string region, string lang) { }
        private static void AzureAsr_Stop() { }
        private static void AzureAsr_Ping(string goName) { }
        private static void MicLevel_Start(string goName) { }
        private static void MicLevel_Stop() { }
#endif

        [Header("Optional Inspector Defaults (DO NOT SHIP REAL KEYS)")]
        [SerializeField] private string m_azureSpeechKey = "YOUR_AZURE_SPEECH_KEY";
        [SerializeField] private string m_azureSpeechRegion = "westus";
        [SerializeField] private string m_language = "en-US";

        public bool IsRunning { get; private set; }
        public string PartialText { get; private set; } = "";
        public string FinalText { get; private set; } = "";
        public string LastInfo { get; private set; } = "";
        public float MicLevel { get; private set; } = 0f; // 0..1-ish

        public event Action<string> PartialReceived;
        public event Action<string> FinalReceived;
        public event Action<string> ErrorReceived;
        public event Action<string> SessionChanged;
        public event Action<string> InfoReceived;
        public event Action<float> MicLevelReceived;


        private void Awake()
        {
            // JS plugin expects this name for SendMessage targets.
            gameObject.name = "AzureAsrBridge";
        }

        protected override void Start()
        {
            base.Start();

            AzureAsr_Ping(gameObject.name);
        }

        /// <summary>Start recognition using inspector defaults.</summary>
        public void StartRecognition()
        {
            StartRecognition(m_azureSpeechKey, m_azureSpeechRegion, m_language);
        }

        /// <summary>Start recognition using explicit config.</summary>
        public void StartRecognition(string key, string region, string language)
        {
            if (IsRunning)
                return;

            IsRunning = true;

            PartialText = "";
            FinalText = "";
            LastInfo = "";

            AzureAsr_Start(gameObject.name, key, region, language);
            MicLevel_Start(gameObject.name);
        }

        public void StopRecognition()
        {
            if (!IsRunning)
                return;

            IsRunning = false;

            AzureAsr_Stop();
            MicLevel_Stop();
        }

        /// <summary>Update inspector defaults (useful for debug UI or runtime config).</summary>
        public void SetDefaults(string key, string region, string language)
        {
            m_azureSpeechKey = key ?? "";
            m_azureSpeechRegion = region ?? "";
            m_language = language ?? "";
        }

        public void GetDefaults(out string key, out string region, out string language)
        {
            key = m_azureSpeechKey;
            region = m_azureSpeechRegion;
            language = m_language;
        }


        // ---------------------------------------------------------------------
        // JS -> C# callbacks (names must match JS SendMessage targets)
        // ---------------------------------------------------------------------

        public void OnAzureAsrPartial(string text)
        {
            PartialText = text ?? "";
            PartialReceived?.Invoke(PartialText);
        }

        public void OnAzureAsrFinal(string text)
        {
            FinalText = text ?? "";
            FinalReceived?.Invoke(FinalText);
        }

        public void OnError(string msg) => OnAzureAsrError(msg);

        public void OnAzureAsrError(string msg)
        {
            string safe = msg ?? "(unknown error)";
            LastInfo = $"ERR: {safe}";
            IsRunning = false;

            Debug.LogError($"[ASR] {safe}");
            ErrorReceived?.Invoke(safe);
        }

        public void OnAzureAsrSession(string state)
        {
            LastInfo = $"session {state ?? ""}";
            SessionChanged?.Invoke(state);
        }

        public void OnAzureAsrInfo(string info)
        {
            LastInfo = info ?? "";
            InfoReceived?.Invoke(LastInfo);
        }

        public void OnMicLevel(string levelStr)
        {
            if (float.TryParse(levelStr, out float v))
            {
                MicLevel = Mathf.Clamp01(v);
                MicLevelReceived?.Invoke(MicLevel);
            }
        }
    }
}
