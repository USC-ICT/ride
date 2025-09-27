using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Base abstract class for Unity-based text-to-speech systems.
    /// Provides lifecycle, file writing, and voice handling infrastructure.
    /// </summary>
    public abstract class TextToSpeechSystemUnity : RideSystemMonoBehaviour, ITextToSpeechSystem
    {
        [field: SerializeField] public float timeout { get; private set; } = 10.0f;
        [field: Header("Text To Speech Status")]
        [field: SerializeField] public string generatedAudioFilePath { get; private set; }
        [field: SerializeField] public float generatedAudioLength { get; private set; }
        [field: SerializeField] public bool textToSpeechProcessing { get; private set; } = false;


        /// <inheritdoc/>
        public abstract string[] GetAvailableVoices();


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
            StartCoroutine(WaitForTextToSpeechCreation(resultCallback));
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
        IEnumerator WaitForTextToSpeechCreation(TextToSpeechResult resultCallback)
        {
            float timer = 0;
            while (textToSpeechProcessing && timer < timeout)
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
