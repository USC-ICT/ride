using System.Collections;
using UnityEngine;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Abstract base class for TTS systems that support lipsync data output.
    /// </summary>
    public abstract class TextToSpeechSystemLipsynced : TextToSpeechSystemUnity, ILipsyncedTextToSpeechSystem
    {
        [field: Header("Lipsync Status")]
        [field: SerializeField] public string lipsyncSchedule { get; private set; }
        [field: SerializeField] public bool lipsyncProcessing { get; private set; } = false;

        /// <inheritdoc/>
        public void CreateTextToSpeech(string voice, string text, LipsyncedTextToSpeechResult resultCallback)
        {
            if (!ContainsVoice(voice))
            {
                Debug.Log("Voice not found!");
                return;
            }

            if (textToSpeechProcessing || lipsyncProcessing)
            {
                Debug.Log("TTS already in progress!");
                return;
            }

            InitalizeTTSVariables();

            StartTextToSpeechGeneration(voice, text);
            StartLipsyncGeneration(voice, text);

            StartCoroutine(WaitForLipsyncedTextToSpeech(resultCallback));
        }

        /// <inheritdoc/>
        protected override void InitalizeTTSVariables()
        {
            base.InitalizeTTSVariables();
            lipsyncSchedule = "";
            lipsyncProcessing = true;
        }

        /// <summary>
        /// Signals that lipsync data generation is complete.
        /// </summary>
        /// <param name="lipsyncSchedule">The generated lipsync data (e.g. XML string).</param>
        protected void CompleteLipsyncGeneration(string lipsyncSchedule)
        {
            this.lipsyncSchedule = lipsyncSchedule;
            lipsyncProcessing = false;
        }

        /// <summary>
        /// Starts the lipsync data generation for the provided voice and text.
        /// Must be implemented by child classes.
        /// </summary>
        /// <param name="voice">The selected voice.</param>
        /// <param name="text">The input text to convert to speech.</param>
        protected abstract void StartLipsyncGeneration(string voice, string text);


        /// <summary>
        /// Coroutine that waits for both TTS and lipsync to complete before invoking the result callback.
        /// </summary>
        /// <param name="resultCallback">Callback with lipsync XML and audio file path.< see cref = "LipsyncedTextToSpeechResult" /></param>
        IEnumerator WaitForLipsyncedTextToSpeech(LipsyncedTextToSpeechResult resultCallback)
        {
            float timer = 0;
            while ((lipsyncProcessing || textToSpeechProcessing) && timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            resultCallback?.Invoke(lipsyncSchedule, generatedAudioFilePath);
        }
    }
}
