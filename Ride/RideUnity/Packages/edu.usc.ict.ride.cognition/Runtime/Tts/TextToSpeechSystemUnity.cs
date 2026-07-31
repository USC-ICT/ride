using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Progress of a provider's voice list. Providers that ship a fixed list are
    /// <see cref="Ready"/> from the start; providers that ask a service for one move
    /// <see cref="NotFetched"/> -> <see cref="Fetching"/> -> <see cref="Ready"/> or
    /// <see cref="Unavailable"/>. The distinction matters because
    /// <see cref="Unavailable"/> is retryable: a service that was down at startup may be
    /// running by the time a user selects it.
    /// </summary>
    public enum VoiceListState
    {
        /// <summary>No attempt has been made to retrieve the voice list yet.</summary>
        NotFetched,

        /// <summary>A retrieval attempt is in flight; callers needing a voice should wait.</summary>
        Fetching,

        /// <summary>The provider's own voice list was retrieved successfully.</summary>
        Ready,

        /// <summary>Retrieval failed and a fallback list is in use. Worth trying again.</summary>
        Unavailable
    }

    /// <summary>
    /// Base abstract class for Unity-based text-to-speech systems.
    /// Provides lifecycle, file writing, and voice handling infrastructure.
    /// </summary>
    public abstract class TextToSpeechSystemUnity : RideSystemMonoBehaviour, ITextToSpeechSystem
    {
        [field: SerializeField] public float timeout { get; protected set; } = 10.0f;

        [SerializeField, Min(0f), Tooltip(
            "Additional seconds allowed per thousand characters of text.\n" +
            "Synthesis time scales with utterance length, so the wait budget is 'timeout' plus this " +
            "allowance rather than a fixed value; a constant timeout fails long utterances.")]
        private float m_secondsPerThousandCharacters = 15f;

        [field: Header("Text To Speech Status")]
        [field: SerializeField] public string generatedAudioFilePath { get; private set; }
        [field: SerializeField] public float generatedAudioLength { get; private set; }
        [field: SerializeField] public bool textToSpeechProcessing { get; private set; } = false;


        /// <inheritdoc/>
        public abstract string[] GetAvailableVoices();


        /// <summary>
        /// The longest text this provider accepts in a single synthesis request. Services differ
        /// widely, and exceeding the limit is rejected rather than truncated, so callers should
        /// shorten or split text to fit.
        /// </summary>
        /// <remarks>
        /// The default is the ceiling this pipeline is built to speak - roughly six minutes of
        /// audio - rather than "unlimited": a provider that declares nothing is then bounded by
        /// something sane instead of by whatever an application happens to send. Providers that
        /// accept more raise it, and providers that accept less lower it.
        /// </remarks>
        public virtual int MaxRequestCharacters => DefaultMaxRequestCharacters;


        /// <summary>
        /// Longest utterance the speech pipeline is designed to handle in one turn, about six
        /// minutes of audio. Applications may impose a smaller limit of their own.
        /// </summary>
        public const int DefaultMaxRequestCharacters = 5000;


        /// <summary>
        /// How far this provider has got in retrieving its voice list. Defaults to
        /// <see cref="VoiceListState.Ready"/> for providers whose voices are known without asking a
        /// service; those that fetch a list report progress through <see cref="RefreshVoices"/>.
        /// </summary>
        public VoiceListState VoiceListStatus { get; protected set; } = VoiceListState.Ready;


        /// <summary>
        /// Whether the voice list has settled, whether or not the provider's own list was obtained.
        /// A settled-but-<see cref="VoiceListState.Unavailable"/> provider still has a usable
        /// fallback voice, so speech can proceed.
        /// </summary>
        public bool VoicesResolved =>
            VoiceListStatus == VoiceListState.Ready || VoiceListStatus == VoiceListState.Unavailable;


        /// <summary>
        /// Starts (or restarts) retrieval of the provider's voice list. Safe to call repeatedly; a
        /// request already in flight is left to finish.
        /// </summary>
        public void RefreshVoices()
        {
            if (VoiceListStatus == VoiceListState.Fetching)
                return;

            VoiceListStatus = VoiceListState.Fetching;
            StartCoroutine(RunVoiceFetch());
        }


        /// <summary>
        /// Retries voice retrieval only if the previous attempt failed. Call this when a provider
        /// becomes the active one: a local service that was not running at startup may be running
        /// now, and without this the fallback voice would be the only choice for the rest of the
        /// session.
        /// </summary>
        public void RefreshVoicesIfUnavailable()
        {
            if (VoiceListStatus == VoiceListState.Unavailable)
                RefreshVoices();
        }


        /// <summary>
        /// Retrieves the provider's voice list, calling <see cref="CompleteVoiceFetch"/> before it
        /// ends. Providers with a fixed list do not implement this.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        protected virtual IEnumerator FetchAvailableVoices()
        {
            yield break;
        }


        /// <summary>
        /// Reports the outcome of a voice-list retrieval. Implementations must call this on every
        /// exit path, including failures, since callers wait for the list to settle before speaking.
        /// </summary>
        /// <param name="retrieved">True if the provider's own list was obtained; false if a fallback is in use.</param>
        protected void CompleteVoiceFetch(bool retrieved)
            => VoiceListStatus = retrieved ? VoiceListState.Ready : VoiceListState.Unavailable;


        /// <summary>
        /// Waits until the voice list has settled, starting a first retrieval if none has run.
        /// Never waits on a failed attempt, so a provider whose service is down still speaks with
        /// its fallback voice instead of stalling.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        protected IEnumerator WaitForVoices()
        {
            if (VoiceListStatus == VoiceListState.NotFetched)
                RefreshVoices();

            while (VoiceListStatus == VoiceListState.Fetching)
                yield return null;
        }


        IEnumerator RunVoiceFetch()
        {
            yield return FetchAvailableVoices();

            // A provider that returned without reporting would otherwise leave callers waiting.
            if (VoiceListStatus == VoiceListState.Fetching)
            {
                Debug.LogWarning($"[{GetType().Name}] Voice retrieval ended without reporting an outcome.");
                VoiceListStatus = VoiceListState.Unavailable;
            }
        }


        /// <summary>
        /// The time to allow for one synthesis request, scaled by how much text was submitted.
        /// Generating several minutes of speech legitimately takes far longer than a short reply, so
        /// a fixed budget would abandon long utterances that were about to succeed.
        /// </summary>
        /// <param name="text">The text being synthesized.</param>
        /// <returns>Seconds to wait before treating the request as failed.</returns>
        public float GetGenerationTimeoutSeconds(string text)
        {
            int length = text != null ? text.Length : 0;
            return timeout + length / 1000f * m_secondsPerThousandCharacters;
        }


        /// <summary>
        /// Scales a provider's own request timeout by the amount of text submitted, using the same
        /// per-length allowance as <see cref="GetGenerationTimeoutSeconds"/>. Providers that set a
        /// transport timeout should use this so a long utterance is not cut off in flight.
        /// </summary>
        /// <param name="baseSeconds">The provider's timeout for a short request.</param>
        /// <param name="text">The text being synthesized.</param>
        /// <returns>Seconds to allow for the request.</returns>
        protected int GetRequestTimeoutSeconds(int baseSeconds, string text)
        {
            int length = text != null ? text.Length : 0;
            return baseSeconds + Mathf.CeilToInt(length / 1000f * m_secondsPerThousandCharacters);
        }


        /// <inheritdoc/>
        public virtual bool ContainsVoice(string voice)
        {
            return Array.Exists(GetAvailableVoices(), val => val == voice);
        }


        /// <inheritdoc/>
        public void CreateTextToSpeech(string voice, string text, TextToSpeechResult resultCallback)
        {
            if (!ContainsVoice(voice))
            {
                Debug.Log("Voice not found!");
                return;
            }

            if (textToSpeechProcessing)
            {
                Debug.Log("TTS already in progress!");
                return;
            }

            InitalizeTTSVariables();


            StartTextToSpeechGeneration(voice, text);
            StartCoroutine(WaitForTextToSpeechCreation(resultCallback, GetGenerationTimeoutSeconds(text)));
        }


        /// <inheritdoc/>
        public int GetVoiceIndex(string voice)
        {
            if (GetAvailableVoices().Length <= 0) { return -1; }
            if (ContainsVoice(voice) == false) { return -1; }

            return Array.FindIndex(GetAvailableVoices(), x => x == voice);
        }


        /// <summary>
        /// Initializes base TTS variables and resets lipsync-related state.
        /// </summary>
        protected virtual void InitalizeTTSVariables()
        {
            generatedAudioFilePath = "";
            generatedAudioLength = 0;
            textToSpeechProcessing = true;
        }

       
        /// <summary>
        /// Coroutine to wait for TTS generation to finish or timeout.
        /// Invokes callback with the path to the generated audio.
        /// </summary>
        /// <param name="resultCallback">The callback to invoke on completion.<see cref = "TextToSpeechResult"/></param>
        /// <param name="timeoutSeconds">How long to wait, from <see cref="GetGenerationTimeoutSeconds"/>.</param>
        IEnumerator WaitForTextToSpeechCreation(TextToSpeechResult resultCallback, float timeoutSeconds)
        {
            float timer = 0;
            while (textToSpeechProcessing && timer < timeoutSeconds)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            resultCallback?.Invoke(generatedAudioFilePath);
        }


        /// <summary>
        /// Should be called by child classes when audio generation completes.
        /// </summary>
        /// <param name="generatedAudioFilePath">File path of the generated audio.</param>
        /// <param name="generatedAudioLength">Length of the audio in seconds.</param>
        protected void CompleteTextToSpeechGeneration(string generatedAudioFilePath, float generatedAudioLength = 0)
        {
            this.generatedAudioLength = generatedAudioLength;
            this.generatedAudioFilePath = generatedAudioFilePath;
            textToSpeechProcessing = false;
        }


        /// <summary>
        /// Starts the platform-specific TTS generation process.
        /// Must be implemented by derived classes.
        /// </summary>
        /// <param name="voice">The voice to use.</param>
        /// <param name="text">The input text to speak.</param>
        protected abstract void StartTextToSpeechGeneration(string voice, string text);


        /// <summary>
        /// Writes the AudioStream returned from the call to
        /// SynthesizeSpeechAsync to a file in MP3 format.
        /// </summary>
        /// <param name="audioStream">The AudioStream returned from the
        /// call to the SynthesizeSpeechAsync method.</param>
        /// <param name="outputFileName">The full path to the file in which to
        /// save the audio stream.</param>
        protected void WriteStreamToFile(Stream audioStream, string outputFileName)
        {

            using (var stream = new FileStream(outputFileName, FileMode.Create))
            {
                byte[] buffer = new byte[8 * 1024];
                int readBytes;

                while ((readBytes = audioStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, readBytes);
                }
            }
            Debug.Log($"Saved {outputFileName} to disk.");
        }

        /// <summary>
        /// Writes the audio stream to disk on a worker thread (non-blocking for main thread).
        /// Logic matches WriteStreamToFile (sync) but executed via Task.Run.
        /// </summary>
        /// <param name="audioStream">The AudioStream returned from the
        /// call to the SynthesizeSpeechAsync method.</param>
        /// <param name="outputFileName">The full path to the file in which to
        /// save the audio stream.</param>
        protected Task WriteStreamToFileAsync(Stream audioStream, string outputFileName)
        {
            return Task.Run(() =>
            {
                if (audioStream == null)
                    throw new ArgumentNullException(nameof(audioStream));
                if (string.IsNullOrEmpty(outputFileName))
                    throw new ArgumentNullException(nameof(outputFileName));

                var dir = Path.GetDirectoryName(outputFileName);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                byte[] buffer = new byte[8 * 1024];
                int readBytes;

                using (var fs = new FileStream(outputFileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    // If stream was read earlier, rewind if possible
                    if (audioStream.CanSeek && audioStream.Position != 0)
                        audioStream.Position = 0;

                    while ((readBytes = audioStream.Read(buffer, 0, buffer.Length)) > 0)
                        fs.Write(buffer, 0, readBytes);

                    fs.Flush();
                }
            });
        }
    }
}
