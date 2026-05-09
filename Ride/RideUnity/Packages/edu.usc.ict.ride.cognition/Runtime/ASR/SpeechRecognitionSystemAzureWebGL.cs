using System;
using UnityEngine;

namespace Ride.SpeechRecognition
{
    /// <summary>
    /// RIDE automatic speech recognition (ASR) system that uses the Azure Speech SDK JavaScript
    /// implementation via a WebGL bridge.
    ///
    /// This system is intended for Unity WebGL builds, where native Azure Speech SDK usage is not
    /// available. It communicates with JavaScript through a small .jslib plugin and receives events
    /// via Unity SendMessage callbacks routed through <see cref="AzureAsrBridge"/>.
    ///
    /// Design notes:
    /// - This system owns the <see cref="AzureAsrBridge"/> GameObject and does not require scene setup.
    /// - Recognition results are forwarded into the common <see cref="SpeechRecognitionSystemUnity"/>
    ///   event pipeline via <c>OnPartialSpeechRecognized</c> and <c>OnSpeechRecognized</c>.
    /// - Microphone selection is not supported in WebGL through Unity's Microphone API; the browser
    ///   will select the active input device based on user settings.
    /// - WebGL microphone access requires an explicit user gesture (click/tap) and HTTPS in typical deployments.
    /// </summary>
    public class SpeechRecognitionSystemAzureWebGL : SpeechRecognitionSystemUnity
    {
        const string DEFAULT_MICROHONE_NAME = "AzureDefault";

        [Tooltip(
            "WebGL bridge component for Azure Speech Recognition.\n\n" +
            "This MonoBehaviour receives callbacks from the Azure Speech SDK JavaScript runtime " +
            "via a .jslib plugin and Unity SendMessage, and forwards them into C# events.\n\n" +
            "Setup requirements (WebGL only):\n" +
            "- The Azure ASR WebGL setup step must be run once per project.\n" +
            "- Required JavaScript files must exist in StreamingAssets.\n" +
            "- See: RIDE > Cognition > ASR > WebGL Setup (StreamingAssets).\n\n" +
            "Notes:\n" +
            "- This component must exist in the scene for WebGL builds.\n" +
            "- The browser controls microphone permissions and device selection.\n" +
            "- Inspector API keys are for debugging only; do NOT ship real keys."
        )]
        public AzureAsrBridge m_azureAsrBridge;


        public override bool IsSupported => RideUtils.IsWebGL();
        public override bool SupportsContinuousRecognition => true;


        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            if (m_azureAsrBridge == null)
            {
                Debug.LogWarning("[AzureWebGL ASR] Missing AzureAsrBridge; cannot use this system.");
                return;
            }

            m_azureAsrBridge.PartialReceived += OnBridgePartialReceived;
            m_azureAsrBridge.FinalReceived += OnBridgeFinalReceived;
            m_azureAsrBridge.ErrorReceived += OnBridgeErrorReceived;
            m_azureAsrBridge.InfoReceived += OnBridgeInfoReceived;
            m_azureAsrBridge.SessionChanged += OnBridgeSessionChanged;
            m_azureAsrBridge.MicLevelReceived += OnBridgeMicLevelReceived;
        }

        /// <inheritdoc/>
        public override void SetMicrophone(string deviceName)
        {
            // WebGL does not expose Unity's Microphone.devices in a meaningful way. The browser controls this.
            // We still track the value for UI/display consistency.
            base.SetMicrophone(deviceName);

            if (RideUtils.IsWebGL())
            {
                if (string.IsNullOrEmpty(deviceName))
                {
                    SelectedMicrophone = DEFAULT_MICROHONE_NAME;

                    Debug.Log("[AzureWebGL ASR] SetMicrophone called with empty deviceName; using default microphone.");
                }
                else
                {
                    Debug.Log($"[AzureWebGL ASR] SetMicrophone requested '{deviceName}', but WebGL uses the browser-selected input device.");
                }
            }
        }

        /// <inheritdoc/>
        public override void OnRecognizingStarted()
        {
            base.OnRecognizingStarted();

            if (!IsSupported)
            {
                Debug.LogWarning("[AzureWebGL ASR] Start requested but system is not supported on this platform.");
                return;
            }

            // Pull key/region/language from config.
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            var apiKey = configSystem.config.azureSpeech.apiKey;
            var region = configSystem.config.azureSpeech.region;

            // Language handling: use whatever the base/system currently uses, or a config-driven default.
            // If you already have a standard place for this (e.g., configSystem.Config.azureSpeech.language),
            // we will wire it up in the next pass.
            string language = "en-US";

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(region))
            {
                Debug.LogError("[AzureWebGL ASR] Azure Speech key/region not configured (Config.azureSpeech.apiKey / region).");
                return;
            }

            // Start WebGL recognition. AzureAsrBridge internally guards against double-starts.
            m_azureAsrBridge.SetDefaults(apiKey, region, language);
            m_azureAsrBridge.StartRecognition(apiKey, region, language);

            Debug.Log("[AzureWebGL ASR] Recognition started via AzureAsrBridge.");
        }

        /// <inheritdoc/>
        public override void OnRecognizingStopped()
        {
            base.OnRecognizingStopped();

            if (!RideUtils.IsWebGL())
                return;

            if (m_azureAsrBridge != null)
            {
                m_azureAsrBridge.StopRecognition();
                Debug.Log("[AzureWebGL ASR] Recognition stopped via AzureAsrBridge.");
            }
        }

        private void OnBridgePartialReceived(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            OnPartialSpeechRecognized(text, Confidence);
        }

        private void OnBridgeFinalReceived(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            OnSpeechRecognized(text, Confidence);
        }

        private void OnBridgeErrorReceived(string error)
        {
            if (string.IsNullOrEmpty(error))
                return;

            Debug.LogError($"[AzureWebGL ASR] JS error: {error}");
        }

        private void OnBridgeInfoReceived(string info)
        {
            if (string.IsNullOrEmpty(info))
                return;

            Debug.Log($"[AzureWebGL ASR] Info: {info}");
        }

        private void OnBridgeSessionChanged(string session)
        {
            if (string.IsNullOrEmpty(session))
                return;

            Debug.Log($"[AzureWebGL ASR] Session: {session}");
        }

        private void OnBridgeMicLevelReceived(float level)
        {
            // Optional: expose mic level in a future property / debug UI.
            // For now, do nothing.
        }
    }
}
