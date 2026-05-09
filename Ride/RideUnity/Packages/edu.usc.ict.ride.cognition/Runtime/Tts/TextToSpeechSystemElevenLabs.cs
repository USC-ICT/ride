using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Provides an ElevenLabs-backed implementation of <see cref="TextToSpeechSystemLipsynced"/>
    /// that generates synthesized speech audio and provider-compatible lipsync data for the
    /// RIDE speech playback pipeline.
    /// </summary>
    /// <remarks>
    /// This system acts as the RIDE-facing adapter for ElevenLabs text-to-speech generation.
    /// Provider-specific communication and timestamp extraction are performed by the associated
    /// <see cref="ElevenLabsTextToSpeech"/> MonoBehaviour, while this class coordinates the
    /// higher-level speech workflow expected by <see cref="TextToSpeechSystemLipsynced"/>.
    ///
    /// <para>
    /// For non-WebGL platforms, this implementation generates audio through ElevenLabs and
    /// builds lipsync XML by converting precomputed word and phone timing data into the
    /// existing RIDE speech format. The resulting XML intentionally mirrors the structure
    /// used by the Azure implementation so that downstream playback and animation systems
    /// can remain provider-agnostic.
    /// </para>
    ///
    /// <para>
    /// For WebGL, this implementation uses the ElevenLabs Lambda proxy for voice
    /// enumeration, audio generation, and timestamp alignment retrieval so it can
    /// produce the same word/phone-driven lipsync data as the native path.
    /// </para>
    ///
    /// <para>
    /// This class depends on <see cref="ElevenLabsTextToSpeech"/> for provider operations
    /// such as voice enumeration, request submission, audio decoding, and extraction of
    /// timestamp-derived word and phone segments.
    /// </para>
    /// </remarks>
    public class TextToSpeechSystemElevenLabs : TextToSpeechSystemLipsynced  //TextToSpeechSystemProxyLipsynced
    {
        [SerializeField] private ElevenLabsTextToSpeech textToSpeech;

        public int SelectedVoiceIndex
        {
            get => textToSpeech.SelectedVoiceIndex;
            set => textToSpeech.SelectedVoiceIndex = value;
        }

        public List<string> Voices => textToSpeech.Voices;

        /// <summary>Indicates whether the list of available voices has been loaded.</summary>
        public bool VoicesReady => textToSpeech.VoicesReady;

        /// <inheritdoc/>
        public override string[] GetAvailableVoices() => textToSpeech.Voices.ToArray();


        /// <inheritdoc/>
        protected override void StartTextToSpeechGeneration(string voice, string text)
        {
            textToSpeech.SetDebugOutputEnabled(lipsyncDebugOutput);
            int requestVersion = textToSpeech.BeginTimingRequest();
            StartCoroutine(StartTextToSpeechGenerationCoroutine(voice, text, requestVersion));
        }

        /// <inheritdoc/>
        protected override void StartLipsyncGeneration(string voice, string text)
        {
            textToSpeech.SetDebugOutputEnabled(lipsyncDebugOutput);
            StartCoroutine(StartLipsyncGenerationCoroutine(voice, text));
        }

        /// <summary>
        /// Coroutine that waits for ElevenLabs TTS generation and completes processing.
        /// </summary>
        /// <param name="text">The input text for speech generation.</param>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator StartTextToSpeechGenerationCoroutine(string voice, string text, int requestVersion)
        {
            yield return StartCoroutine(textToSpeech.WaitForVoices());
            voice = ResolveVoiceOrDefault(voice);

            bool success = false;
            yield return StartCoroutine(textToSpeech.RequestSpeechGenerationCoroutine(text, voice, requestVersion, requestSucceeded => success = requestSucceeded));

            if (!success || string.IsNullOrEmpty(textToSpeech.LastGeneratedAudioPathOrUrl))
            {
                CompleteTextToSpeechGeneration(null);
                yield break;
            }

            CompleteTextToSpeechGeneration(textToSpeech.LastGeneratedAudioPathOrUrl, textToSpeech.clipTime);
        }

        /// <summary>
        /// Begins asynchronous generation of ElevenLabs speech audio and corresponding
        /// lipsync metadata.
        /// </summary>
        /// <param name="text">
        /// The text that should be synthesized by the ElevenLabs TTS service.
        /// </param>
        /// <param name="generatedAudioFilePath">
        /// The local path where the decoded audio file will be written after generation.
        /// </param>
        /// <param name="requestId">
        /// An identifier associated with this request, used by the base lipsync system
        /// to correlate generation completion with the originating call.
        /// </param>
        /// <returns>
        /// A coroutine enumerator that performs the asynchronous HTTP request and
        /// completes the lipsync generation once the response has been processed.
        /// </returns>
        /// <remarks>
        /// This coroutine performs the following steps:
        /// <list type="number">
        /// <item>
        /// Sends a POST request to the ElevenLabs TTS endpoint requesting both audio
        /// and character-level timestamp alignment.
        /// </item>
        /// <item>
        /// Parses the returned JSON into <see cref="ElevenLabsTextToSpeech.ElevenLabsTimestampsResult"/>.
        /// </item>
        /// <item>
        /// Decodes the <c>audio_base64</c> payload and writes the resulting audio file
        /// to <paramref name="generatedAudioFilePath"/>.
        /// </item>
        /// <item>
        /// Uses the precomputed timing data stored on the
        /// <see cref="ElevenLabsTextToSpeech"/> component
        /// (<c>LastWordSegments</c>, <c>LastPhoneSegments</c>, etc.) to construct
        /// an Azure-compatible lipsync XML payload.
        /// </item>
        /// <item>
        /// Signals completion to the base lipsync system by invoking the appropriate
        /// completion callback.
        /// </item>
        /// </list>
        /// <para>
        /// This coroutine is intentionally structured to match the execution model used
        /// by other <see cref="TextToSpeechSystemLipsynced"/> implementations so that
        /// ElevenLabs can be used interchangeably with providers such as Azure.
        /// </para>
        /// </remarks>
        private IEnumerator StartLipsyncGenerationCoroutine(string voice, string text)
        {
            yield return StartCoroutine(textToSpeech.WaitForVoices());
            voice = ResolveVoiceOrDefault(voice);

            int expectedRequestVersion;
            if (textToSpeech.ActiveTimingRequestVersion > textToSpeech.CompletedTimingRequestVersion &&
                textToSpeech.ActiveTimingRequestVersion > textToSpeech.FailedTimingRequestVersion)
            {
                expectedRequestVersion = textToSpeech.ActiveTimingRequestVersion;
            }
            else
            {
                expectedRequestVersion = textToSpeech.BeginTimingRequest();

                bool success = false;
                yield return StartCoroutine(textToSpeech.RequestSpeechGenerationCoroutine(text, voice, expectedRequestVersion, requestSucceeded => success = requestSucceeded));
                if (!success)
                {
                    CompleteLipsyncGeneration(string.Empty);
                    yield break;
                }
            }

            yield return StartCoroutine(WaitForTimingRequestVersion(expectedRequestVersion));

            if (textToSpeech.FailedTimingRequestVersion == expectedRequestVersion ||
                textToSpeech.CompletedTimingRequestVersion != expectedRequestVersion)
            {
                CompleteLipsyncGeneration(string.Empty);
                yield break;
            }

            CompleteLipsyncFromLatestTimings(textToSpeech.LastGeneratedAudioPathOrUrl);
        }

        private string ResolveVoiceOrDefault(string voice)
        {
            if (!string.IsNullOrEmpty(voice))
                return voice;

            string[] availableVoices = GetAvailableVoices();
            return availableVoices.Length > 0 ? availableVoices[0] : "Aria";
        }

        private IEnumerator WaitForTimingRequestVersion(int expectedRequestVersion)
        {
            float timer = 0f;
            while (textToSpeech.CompletedTimingRequestVersion != expectedRequestVersion &&
                   textToSpeech.FailedTimingRequestVersion != expectedRequestVersion &&
                   timer < timeout)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        private void CompleteLipsyncFromLatestTimings(string soundFilePathOrUrl)
        {
            AudioSpeechMap map = BuildElevenLabsAudioSpeechMap(textToSpeech.LastWordSegments, textToSpeech.LastPhoneSegments, soundFilePathOrUrl);
            string xml = map != null ? TextToSpeechXMLBuilder.BuildSpeechXML(map) : string.Empty;
            if (lipsyncDebugOutput)
                LogSpeechXmlDebug(map, xml, "ElevenLabs");
            CompleteLipsyncGeneration(xml);
        }

        /// <summary>
        /// Converts precomputed ElevenLabs word and IPA token timing data into a
        /// RIDE-compatible speech XML document containing viseme keyframes and word markers.
        /// </summary>
        /// <param name="words">
        /// Word-level timing segments extracted from the ElevenLabs alignment stream.
        /// Each entry provides the spoken word and its start/end time within the audio.
        /// </param>
        /// <param name="phones">
        /// Time-stamped IPA token segments derived from dictionary pronunciation lookup
        /// and IPA tokenization.
        /// </param>
        /// <param name="soundFilePathOrUrl">
        /// The path or URL of the generated audio file that should be referenced by the
        /// resulting speech XML.
        /// </param>
        /// <returns>
        /// An XML string describing the speech playback event, including viseme animation
        /// data, word timing markers, and the associated audio file.
        /// </returns>
        /// <remarks>
        /// This method acts as the adapter between the ElevenLabs IPA timing pipeline and
        /// the existing RIDE speech playback system.
        ///
        /// All linguistic processing (word extraction, IPA lookup, tokenization, and token
        /// timing distribution) is performed earlier by <see cref="ElevenLabsTextToSpeech"/>.
        /// This method only converts the prepared segments into the FaceFX-style viseme
        /// schedule expected by the RIDE runtime.
        ///
        /// The conversion process performs the following steps:
        ///
        /// <list type="number">
        /// <item>
        /// Generates start/end markers for each word using <paramref name="words"/>,
        /// populating the <see cref="AudioSpeechMap.MarkList"/> and
        /// <see cref="AudioSpeechMap.WordTimingList"/>.
        /// </item>
        /// <item>
        /// Converts each IPA token in <paramref name="phones"/> into a FaceFX pose using
        /// <see cref="ElevenLabsTextToSpeech.TryGetPose(string, out ElevenLabsTextToSpeech.IPAtoFacefxMap)"/>.
        /// </item>
        /// <item>
        /// Emits viseme keyframes at the start time of each phone segment.
        /// </item>
        /// <item>
        /// Inserts a neutral pose when a sufficiently large gap exists between adjacent
        /// phone segments so the mouth returns to a resting state during pauses.
        /// </item>
        /// <item>
        /// Ensures the final pose returns to neutral at the end of the speech.
        /// </item>
        /// <item>
        /// Packages the generated viseme list and word timing data into an
        /// <see cref="AudioSpeechMap"/>, performs minor timing adjustments, and then
        /// converts the structure into the final speech XML format using
        /// <see cref="TextToSpeechXMLBuilder.BuildSpeechXML(AudioSpeechMap)"/>.
        /// </item>
        /// </list>
        ///
        /// The generated XML format intentionally mirrors the output produced by the
        /// Azure TTS implementation so that downstream speech playback and animation
        /// systems remain provider-agnostic.
        /// </remarks>
        private AudioSpeechMap BuildElevenLabsAudioSpeechMap(
            IReadOnlyList<ElevenLabsTextToSpeech.WordSegment> words,
            IReadOnlyList<ElevenLabsTextToSpeech.PhoneSegment> phones,
            string soundFilePathOrUrl)
        {
            if (words == null || words.Count == 0)
                return null;

            var markList = new List<KeyValuePairS<string, double>>(words.Count * 2);
            var wordTimingList = new List<WordTimingData>(words.Count);

            int markIndex = 0;
            foreach (var word in words)
            {
                markList.Add(new KeyValuePairS<string, double>($"T{markIndex++}", word.StartTimeSeconds));
                wordTimingList.Add(new WordTimingData(word.Word, word.StartTimeSeconds, word.EndTimeSeconds));
                markList.Add(new KeyValuePairS<string, double>($"T{markIndex++}", word.EndTimeSeconds));
            }

            var visemeList = new List<GenerateAudioReplyViseme>((phones != null ? phones.Count : 0) * 2);

            var neutral = ElevenLabsTextToSpeech.GetNeutralPose();
            const double gapToNeutralSeconds = 0.05;

            double lastEnd = -1.0;

            if (phones != null)
            {
                for (int i = 0; i < phones.Count; i++)
                {
                    var phone = phones[i];

                    double startTime = phone.StartTimeSeconds;
                    double endTime = phone.EndTimeSeconds;

                    if (endTime < startTime)
                        endTime = startTime;

                    if (lastEnd >= 0.0 && (startTime - lastEnd) > gapToNeutralSeconds)
                    {
                        for (int j = 0; j < neutral.facefxVisemes.Length; j++)
                            visemeList.Add(new GenerateAudioReplyViseme(neutral.facefxVisemes[j], lastEnd, neutral.amounts[j]));
                    }

                    if (!ElevenLabsTextToSpeech.TryGetPose(phone.IpaToken, out var pose) || pose == null)
                        pose = neutral;

                    for (int j = 0; j < pose.facefxVisemes.Length; j++)
                        visemeList.Add(new GenerateAudioReplyViseme(pose.facefxVisemes[j], startTime, pose.amounts[j]));

                    lastEnd = endTime;
                }
            }

            if (lastEnd >= 0.0)
            {
                for (int j = 0; j < neutral.facefxVisemes.Length; j++)
                    visemeList.Add(new GenerateAudioReplyViseme(neutral.facefxVisemes[j], lastEnd, neutral.amounts[j]));
            }

            AudioSpeechMap map = new()
            {
                soundFile = soundFilePathOrUrl ?? string.Empty,
                VisemeList = visemeList,
                MarkList = markList,
                WordTimingList = wordTimingList
            };

            ApplyConfiguredVisemeTrimming(map, "ElevenLabs");

            map.AdjustWordTimings();
            map.AdjustEndMarkTimings();
            return map;
        }
    }
}
