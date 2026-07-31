using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

using Debug = UnityEngine.Debug;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Unity <see cref="MonoBehaviour"/> responsible for communicating with the
    /// ElevenLabs Text-to-Speech service and preparing speech timing data used by
    /// the RIDE lipsync pipeline.
    /// </summary>
    /// <remarks>
    /// This component performs all provider-specific work required to generate
    /// speech using the ElevenLabs REST API. It retrieves available voices,
    /// submits synthesis requests, decodes the returned audio, and extracts
    /// timestamp alignment data from the ElevenLabs response.
    ///
    /// <para>
    /// In addition to generating audio, this component converts the alignment
    /// information returned by ElevenLabs into several intermediate timing
    /// representations used by the RIDE speech animation pipeline:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Character-level alignment returned by the ElevenLabs timestamp endpoint.
    /// </item>
    /// <item>
    /// Word-level segments extracted from the alignment stream.
    /// </item>
    /// <item>
    /// IPA pronunciation lookup results obtained from the local IPA dictionary.
    /// </item>
    /// <item>
    /// Tokenized IPA phone segments distributed across the duration of each word.
    /// </item>
    /// </list>
    ///
    /// <para>
    /// These computed results are exposed through fields such as
    /// <c>LastWordSegments</c>, <c>LastWordIpaSegments</c>, and
    /// <c>LastPhoneSegments</c>. The higher-level
    /// <see cref="TextToSpeechSystemElevenLabs"/> system consumes this data to
    /// construct FaceFX viseme schedules and generate RIDE-compatible speech XML.
    /// </para>
    ///
    /// <para>
    /// This class intentionally contains the provider-specific linguistic
    /// processing pipeline (alignment interpretation, IPA tokenization, and
    /// phoneme timing distribution) so that the higher-level TTS system can remain
    /// focused on RIDE speech orchestration and provider-agnostic playback logic.
    /// </para>
    ///
    /// <para>
    /// On WebGL builds the audio generation path may instead be handled by an
    /// external hosted proxy service. In those cases this component may be bypassed for
    /// audio generation and alignment data may not be available.
    /// </para>
    /// </remarks>
    public class ElevenLabsTextToSpeech : MonoBehaviour
    {
        public enum Model
        {
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

#if UNITY_WEBGL
        [DllImport("__Internal")] private static extern IntPtr RideWebGLAudio_CreateAudioBlobUrl(string mimeType, string audioBase64);
#else
        private static IntPtr RideWebGLAudio_CreateAudioBlobUrl(string mimeType, string audioBase64) => IntPtr.Zero;
#endif

#if UNITY_WEBGL
        [Serializable]
        private class WebGlLambdaRequest
        {
            public string text;
            public string voice_id;
            public string model_id;
            public ElevenLabsVoiceSettings voice_settings;
        }

        [Serializable]
        private class WebGlLambdaAudioReply
        {
            public string url;
        }

        [Serializable]
        private class WebGlLambdaGenerateReply
        {
            public string url;
            public string audio_base64;
            public ElevenLabsAlignment alignment;
            public ElevenLabsAlignment normalized_alignment;
        }
#endif

        [Serializable]
        private class ElevenLabsVoiceSettings
        {
            public float stability;
            public float similarity_boost;
        }

        [Serializable]
        private class ElevenLabsSynthesisRequest
        {
            public string text;
            public string model_id;
            public ElevenLabsVoiceSettings voice_settings;
        }

        /// <summary>
        /// Represents the full timestamped response returned by the ElevenLabs text-to-speech
        /// endpoint that includes both audio data and alignment metadata.
        /// ref: Elevenlabs Docs <see cref="https://elevenlabs.io/docs/api-reference/text-to-speech/convert-with-timestamps"/>
        /// </summary>
        /// <remarks>
        /// The response may include both raw <see cref="alignment"/> and normalized
        /// <see cref="normalized_alignment"/> character streams. The normalized alignment
        /// is generally preferred when constructing word and phoneme timing data.
        /// </remarks>
        [Serializable]
        public class ElevenLabsTimestampsResult
        {
            public string audio_base64;
            public ElevenLabsAlignment alignment;
            public ElevenLabsAlignment normalized_alignment;
        }

        /// <summary>
        /// Represents the character-level alignment data returned by the ElevenLabs
        /// timestamp endpoint for either the raw or normalized text stream.
        /// </summary>
        /// <remarks>
        /// Unity's <see cref="JsonUtility"/> does not reliably deserialize JSON arrays
        /// of single-character strings into <see cref="char"/> arrays or lists, so the
        /// character stream is stored as <see cref="string"/> values and converted on demand.
        /// The start and end arrays are expected to align by index with <see cref="characters"/>.
        /// </remarks>
        [Serializable]
        public class ElevenLabsAlignment
        {
            public string[] characters;  // use string array here to avoid issues with JSON converting
            public double[] character_start_times_seconds;
            public double[] character_end_times_seconds;

            /// <summary>
            /// Converts the JSON-friendly string-based character array into a <see cref="char"/> array.
            /// </summary>
            /// <returns>
            /// A character array with the same logical length as <see cref="characters"/>.
            /// Empty or null entries are converted to <c>'\0'</c>.
            /// </returns>
            /// <remarks>
            /// ElevenLabs returns characters as JSON strings. This helper performs a defensive conversion
            /// for downstream timing and tokenization code that expects indexed characters.
            /// </remarks>
            public char[] ToCharArray()
            {
                if (characters == null)
                    return Array.Empty<char>();

                char[] result = new char[characters.Length];

                for (int i = 0; i < characters.Length; i++)
                {
                    string s = characters[i];

                    // Most entries are 1-char strings ("T", "h", " ")
                    // Defensive: if empty/null, use '\0'. If longer, take first char.
                    result[i] = (!string.IsNullOrEmpty(s)) ? s[0] : '\0';
                }

                return result;
            }
        }

        /// <summary>
        /// Represents a word-level time span derived from the character-level ElevenLabs alignment data.
        /// </summary>
        /// <remarks>
        /// A <see cref="WordSegment"/> stores the detected word text, its inclusive character span in the
        /// alignment stream, and the corresponding start and end times in seconds.
        /// </remarks>
        public struct WordSegment
        {
            public string Word;
            public double StartTimeSeconds;
            public double EndTimeSeconds;

            /// <summary>Gets or sets the starting character index, inclusive, within the source alignment.</summary>
            public int StartCharIndex;
            /// <summary>Gets or sets the ending character index, inclusive, within the source alignment.</summary>
            public int EndCharIndex;

            public override string ToString() => $"{Word} [{StartTimeSeconds:0.000} - {EndTimeSeconds:0.000}] (chars {StartCharIndex}-{EndCharIndex})";
        }

        /// <summary>
        /// Represents a word segment together with the IPA pronunciation lookup result
        /// used for later phoneme and viseme generation.
        /// </summary>
        /// <remarks>
        /// This structure preserves the original word timing while attaching the dictionary
        /// pronunciation, if one was found.
        /// </remarks>
        public struct WordIpaSegment
        {
            public WordSegment Word;
            public bool HasIpa;
            public string Ipa;

            public override string ToString() => $"{Word.Word} [{Word.StartTimeSeconds:0.000}-{Word.EndTimeSeconds:0.000}] IPA={(HasIpa ? Ipa : "<missing>")}";
        }

        /// <summary>
        /// Represents a phoneme-like IPA token allocated to a specific time span within a word.
        /// </summary>
        /// <remarks>
        /// These segments are produced by tokenizing a word-level IPA pronunciation and distributing
        /// the word duration across the resulting tokens using simple weighting heuristics.
        /// </remarks>
        public struct PhoneSegment
        {
            public string IpaToken;          // e.g., "ə", "b", "aʊ", "nd"
            public double StartTimeSeconds;
            public double EndTimeSeconds;

            /// <summary>Gets or sets the index of the source word in the word-to-IPA segment list (LastWordIpaSegments).</summary>
            public int WordIndex;
            /// <summary>Gets or sets the original source word for debugging and traceability.</summary>
            public string SourceWord;

            public override string ToString() => $"{IpaToken} [{StartTimeSeconds:0.000}-{EndTimeSeconds:0.000}] ({SourceWord})";
        }

        /// <summary>
        /// Represents a time-stamped FaceFX pose keyframe generated from the IPA timing pipeline.
        /// </summary>
        /// <remarks>
        /// A keyframe stores the time at which a particular FaceFX pose should be applied.
        /// Multiple keyframes may be generated for a single word or pause region.
        /// </remarks>
        public struct FacefxKeyframe
        {
            public double TimeSeconds;
            public string[] FacefxVisemes;
            public float[] Amounts;

            public FacefxKeyframe(double timeSeconds, string[] facefxVisemes, float[] amounts)
            {
                TimeSeconds = timeSeconds;
                FacefxVisemes = facefxVisemes;
                Amounts = amounts;
            }

            public override string ToString()
            {
                if (FacefxVisemes == null || Amounts == null)
                    return $"{TimeSeconds:0.000} <null>";

                int n = Math.Min(FacefxVisemes.Length, Amounts.Length);
                string s = "";
                for (int i = 0; i < n; i++)
                {
                    if (i > 0) s += ", ";
                    s += $"{FacefxVisemes[i]}={Amounts[i]:0.00}";
                }
                return $"{TimeSeconds:0.000} {s}";
            }
        }

        public ElevenLabsTimestampsResult LastTimestampsResult { get; private set; }
        public IReadOnlyList<WordSegment> LastWordSegments { get; private set; } = Array.Empty<WordSegment>();
        public IReadOnlyList<WordIpaSegment> LastWordIpaSegments { get; private set; } = Array.Empty<WordIpaSegment>();
        public IReadOnlyList<PhoneSegment> LastPhoneSegments { get; private set; } = Array.Empty<PhoneSegment>();
        public IReadOnlyList<FacefxKeyframe> LastFacefxKeyframes { get; private set; } = Array.Empty<FacefxKeyframe>();
        public AudioClip LastGeneratedAudioClip { get; private set; }
        public bool DebugOutputEnabled { get; private set; }
        public int ActiveTimingRequestVersion { get; private set; }
        public int CompletedTimingRequestVersion { get; private set; }
        public int FailedTimingRequestVersion { get; private set; }
        public bool IsTimingDataReadyForActiveRequest => ActiveTimingRequestVersion > 0 && CompletedTimingRequestVersion == ActiveTimingRequestVersion;
        public string LastGeneratedAudioPathOrUrl { get; private set; }

        const string BaseUrl = "https://api.elevenlabs.io/v1/";
        const int RescaleFactor = 32767;

        public float clipTime { get; private set; }

        public string savedFilePath;

        public AudioSource audioSource;
        [SerializeField] private int selectedVoiceIndex = 0;

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

        [Range(0f, 1f)] public float stabilityParameter = 0.9f;
        [Range(0f, 1f)] public float similarityParameter = 0.75f;

        [SerializeField] private List<IpaDictionary> ipaDictionaries = new();
        private bool m_ipaDictionariesAudited = false;

        public class IPAtoFacefxMap
        {
            public readonly string facefxPhonemeLabel;
            public readonly string[] ipaTokens;
            public readonly string[] facefxVisemes;
            public readonly float[] amounts;

            public IPAtoFacefxMap(string _facefxPhonemeLabel, string[] _ipaTokens, (string viseme, float amount)[] _poses)
            {
                facefxPhonemeLabel = _facefxPhonemeLabel;
                ipaTokens = _ipaTokens;
                facefxVisemes = new string[_poses.Length];
                amounts = new float[_poses.Length];

                for (int i = 0; i < _poses.Length; i++)
                {
                    facefxVisemes[i] = _poses[i].viseme;
                    amounts[i] = _poses[i].amount;
                }
            }
        }

        private static string[] Tokens(params string[] tokens) => tokens;
        private static (string viseme, float amount)[] Pose(float open, float W, float ShCh, float PBM, float FV, float wide, float tBack, float tRoof, float tTeeth)
            => new[] { ("open", open), ("W", W), ("ShCh", ShCh), ("PBM", PBM), ("FV", FV), ("wide", wide), ("tBack", tBack), ("tRoof", tRoof), ("tTeeth", tTeeth) };

        /// <summary>
        /// Static mapping from IPA phoneme tokens to FaceFX viseme targets.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This table converts phoneme tokens produced by the IPA dictionary into the
        /// corresponding FaceFX mouth and tongue targets used by the default FaceFX
        /// character setup.
        /// </para>
        ///
        /// <para>
        /// The phoneme symbols used here follow the <b>International Phonetic Alphabet (IPA)</b>,
        /// a standardized notation system for representing speech sounds. IPA is widely used
        /// in pronunciation dictionaries and speech processing pipelines.
        /// </para>
        ///
        /// <para>
        /// References:
        /// <list type="bullet">
        /// <item>
        /// IPA overview:
        /// https://en.wikipedia.org/wiki/International_Phonetic_Alphabet
        /// </item>
        /// <item>
        /// Official IPA chart:
        /// https://www.internationalphoneticassociation.org/content/ipa-chart
        /// </item>
        /// <item>
        /// IPA dictionary project used by this system:
        /// https://github.com/open-dict-data/ipa-dict
        /// </item>
        /// <item>
        /// FaceFX default character setup and viseme targets:
        /// https://facefx.github.io/documentation/doc/default-character-setup
        /// </item>
        /// </list>
        /// </para>
        ///
        /// <para>
        /// Each entry is keyed by a FaceFX phoneme label and stores the FaceFX targets and
        /// weights shown by the FaceFX application. IPA tokens produced by the local
        /// dictionary are normalized into these FaceFX phoneme labels before pose lookup.
        /// This keeps the runtime pose table easy to compare against FaceFX authoring data
        /// while preserving compatibility with the Unicode IPA token stream used elsewhere
        /// in this package. For example:
        /// </para>
        /// <list type="bullet">
        /// <item><c>IY</c> – high front vowel used for IPA tokens such as <c>i</c></item>
        /// <item><c>OW</c> – rounded back diphthong used for IPA tokens such as <c>oʊ</c></item>
        /// <item><c>P</c>, <c>B</c>, <c>M</c> – bilabial closure categories</item>
        /// <item><c>TH</c>, <c>DH</c> – dental fricative categories</item>
        /// <item><c>CH</c>, <c>JH</c> – affricate categories</item>
        /// <item>
        /// <c>tRoof</c>, <c>tBack</c>, <c>tTeeth</c> – tongue placement targets used for
        /// alveolar, velar, and dental consonants.
        /// </item>
        /// </list>
        ///
        /// <para>
        /// The IPA alias tokens listed here correspond to the phoneme inventory typically
        /// emitted by the English <c>ipa-dict</c> dataset (for example <c>en_US</c>). Not
        /// every IPA symbol in the full IPA chart is mapped; only the subset commonly used
        /// by the dictionary is required.
        /// </para>
        ///
        /// <para>
        /// The rows are ordered to match the FaceFX application phoneme table captured
        /// during development so the code can be compared directly against that reference.
        /// Some IPA-to-FaceFX label assignments are less certain than others; those rows
        /// include confidence comments to make later review easier.
        /// </para>
        ///
        /// <para>
        /// This mapping is intentionally approximate. The goal is to produce visually
        /// plausible mouth shapes for real-time lip sync rather than a physically exact
        /// articulatory model.
        /// </para>
        /// </remarks>
        private static readonly IPAtoFacefxMap[] m_IPAtoFacefxEntries =
        {
            // Silence / separators
            new("SILENCE", Tokens("silence"), Pose(0, 0, 0, 0, 0, 0, 0, 0, 0)),

            // Rows below are ordered to match the FaceFX application table shown in the
            // reference screenshot so the values can be compared directly.
            //                                 open   W      ShCh   PBM    FV     wide   tBack  tRoof  tTeeth

            // Stops / nasals / related consonant helper rows
            new("P",    Tokens("p"),      Pose(0.00f, 0.00f, 0.00f, 0.90f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("B",    Tokens("b"),      Pose(0.00f, 0.00f, 0.00f, 0.90f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("T",    Tokens("t"),      Pose(0.40f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f)),
            new("D",    Tokens("d"),      Pose(0.40f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f)),
            new("K",    Tokens("k"),      Pose(0.25f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f, 0.00f)),
            new("G",    Tokens("g", "ɡ"), Pose(0.25f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f, 0.00f)),
            new("M",    Tokens("m"),      Pose(0.00f, 0.00f, 0.00f, 0.90f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("N",    Tokens("n"),      Pose(0.40f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f)),
            new("NG",   Tokens("ŋ"),      Pose(0.40f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f, 0.00f)),
            new("RA",   Tokens(),         Pose(0.40f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.50f, 0.00f)), // Low confidence: FaceFX rhotic helper row, no direct ipa-dict token mapping assigned yet.
            new("RU",   Tokens(),         Pose(0.25f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f, 0.00f)), // Low confidence: FaceFX rhotic helper row, no direct ipa-dict token mapping assigned yet.
            new("FLAP", Tokens("ɾ"),      Pose(0.30f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.00f)), // Medium confidence: maps the alveolar flap directly when present in the dictionary.
            new("PH",   Tokens(),         Pose(0.10f, 0.00f, 0.00f, 0.00f, 0.40f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: screenshot row preserved for comparison; no direct ipa-dict alias assigned yet.

            // Fricatives / affricates / related consonant helper rows
            new("F",    Tokens("f"),      Pose(0.00f, 0.00f, 0.00f, 0.00f, 0.75f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("V",    Tokens("v"),      Pose(0.00f, 0.00f, 0.00f, 0.00f, 0.75f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("TH",   Tokens("θ"),      Pose(0.45f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.90f)),
            new("DH",   Tokens("ð"),      Pose(0.45f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.90f)),
            new("S",    Tokens("s"),      Pose(0.15f, 0.00f, 0.00f, 0.00f, 0.00f, 0.50f, 0.00f, 0.40f, 0.00f)),
            new("Z",    Tokens("z"),      Pose(0.15f, 0.00f, 0.00f, 0.00f, 0.00f, 0.50f, 0.00f, 0.40f, 0.00f)),
            new("SH",   Tokens("ʃ"),      Pose(0.00f, 0.00f, 0.85f, 0.00f, 0.00f, 0.00f, 0.00f, 0.40f, 0.00f)),
            new("ZH",   Tokens("ʒ"),      Pose(0.00f, 0.00f, 0.85f, 0.00f, 0.00f, 0.00f, 0.00f, 0.40f, 0.00f)),
            new("CX",   Tokens(),         Pose(0.25f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f, 0.00f)), // Low confidence: screenshot row preserved; exact IPA correspondence is unclear in this pipeline.
            new("X",    Tokens("x"),      Pose(0.25f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f, 0.00f)), // Medium confidence: maps voiceless velar fricative when present.
            new("GH",   Tokens("ɣ"),      Pose(0.25f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f, 0.00f)), // Medium confidence: maps voiced velar fricative when present.
            new("HH",   Tokens("h"),      Pose(0.30f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("R",    Tokens("ɹ"),      Pose(0.10f, 0.00f, 0.70f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("Y",    Tokens("j"),      Pose(0.00f, 0.50f, 0.30f, 0.00f, 0.00f, 0.00f, 0.00f, 0.40f, 0.00f)), // Medium confidence: FaceFX "Y" row likely corresponds to IPA /j/.
            new("L",    Tokens("l", "ɫ"), Pose(0.40f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f)),
            new("W",    Tokens("w"),      Pose(0.00f, 0.85f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("H",    Tokens(),         Pose(0.20f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: retained from the screenshot, but the local IPA tokenizer currently emits /h/ -> HH.
            new("TS",   Tokens("ts"),     Pose(0.40f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f)), // Low confidence: preserved for diagnostics; the tokenizer usually emits /t/ + /s/ separately.
            new("CH",   Tokens("tʃ"),     Pose(0.00f, 0.00f, 0.85f, 0.00f, 0.00f, 0.00f, 0.00f, 0.40f, 0.00f)),
            new("JH",   Tokens("dʒ"),     Pose(0.00f, 0.00f, 0.85f, 0.00f, 0.00f, 0.00f, 0.00f, 0.40f, 0.00f)),

            // Vowels / diphthongs / rhotic vowels / related helper rows
            new("IY",   Tokens("i"),      Pose(0.20f, 0.00f, 0.00f, 0.00f, 0.00f, 0.80f, 0.00f, 0.20f, 0.00f)),
            new("E",    Tokens("e"),      Pose(0.35f, 0.00f, 0.00f, 0.00f, 0.00f, 0.25f, 0.00f, 0.20f, 0.00f)), // Medium confidence: direct IPA /e/ is uncommon in ipa-dict but preserved for completeness.
            new("EN",   Tokens(),         Pose(0.35f, 0.00f, 0.00f, 0.00f, 0.00f, 0.25f, 0.00f, 0.20f, 0.00f)), // Low confidence: likely an authoring helper row rather than a direct ipa-dict token.
            new("EH",   Tokens("ɛ"),      Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f, 0.00f)),
            new("A",    Tokens("a"),      Pose(0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Medium confidence: direct /a/ is uncommon in en_US ipa-dict but preserved from the screenshot.
            new("AA",   Tokens("ɑ"),      Pose(0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("AAN",  Tokens(),         Pose(0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: nasalized authoring helper row; no direct ipa-dict alias assigned yet.
            new("AO",   Tokens("ɔ"),      Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("AON",  Tokens(),         Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: nasalized authoring helper row; no direct ipa-dict alias assigned yet.
            new("O",    Tokens("o"),      Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("ON",   Tokens(),         Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: nasalized authoring helper row; no direct ipa-dict alias assigned yet.
            new("UW",   Tokens("u"),      Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("UY",   Tokens(),         Pose(0.00f, 0.85f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: rounded high-front helper row; no direct ipa-dict alias assigned yet.
            new("EU",   Tokens(),         Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: screenshot row preserved; exact IPA correspondence is unclear in this pipeline.
            new("OE",   Tokens("ø"),      Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Medium confidence: maps front rounded /ø/ if encountered.
            new("OEN",  Tokens(),         Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: nasalized helper row; no direct ipa-dict alias assigned yet.
            new("AH",   Tokens("ʌ"),      Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f, 0.00f)),
            new("IH",   Tokens("ɪ"),      Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f, 0.00f)),
            new("UU",   Tokens(),         Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)), // Low confidence: screenshot row preserved; exact IPA correspondence is unclear in this pipeline.
            new("UH",   Tokens("ʊ"),      Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("AX",   Tokens("ə"),      Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f, 0.00f)),
            new("UX",   Tokens(),         Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f, 0.00f)), // Low confidence: screenshot row preserved; exact IPA correspondence is unclear in this pipeline.
            new("AE",   Tokens("æ"),      Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f, 0.00f)),
            new("ER",   Tokens("ɝ"),      Pose(0.40f, 0.00f, 0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.50f, 0.00f)),
            new("AXR",  Tokens("ɚ"),      Pose(0.40f, 0.00f, 0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.50f, 0.00f)), // Medium confidence: maps unstressed rhotic schwa.
            new("EXR",  Tokens(),         Pose(0.40f, 0.00f, 0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.50f, 0.00f)), // Low confidence: retained from the screenshot; no direct ipa-dict alias assigned yet.
            new("EY",   Tokens("eɪ"),     Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f, 0.00f)),
            new("AW",   Tokens("aʊ"),     Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f)),
            new("AY",   Tokens("aɪ"),     Pose(0.50f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.60f, 0.40f, 0.00f)),
            new("OY",   Tokens("ɔɪ"),     Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("OW",   Tokens("oʊ"),     Pose(0.40f, 0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)),
            new("OPEN", Tokens(),         Pose(0.55f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f)) // Low confidence: helper row retained for parity with the screenshot.
        };

        private static readonly Dictionary<string, string> m_IPAToFacefxPhonemeMap = BuildIPAToFacefxPhonemeMap();
        private static readonly Dictionary<string, IPAtoFacefxMap> m_FacefxPhonemeToPoseMap = BuildFacefxPhonemeToPoseMap();

        private static readonly string[] s_CommonMultiCharTokens =
        {
            // Affricates / common digraph phones
            "tʃ",
            "dʒ",

            // Common diphthongs (English-ish)
            "aɪ",
            "aʊ",
            "ɔɪ",
            "eɪ",
            "oʊ"
        };


        void Awake()
        {
            InitializeAudioSource();
            savedFilePath = Application.persistentDataPath + "/saved_audio.wav";
        }

        private void Start()
        {
            StartCoroutine(GetAvailableVoicesCoroutine());
        }

        /// <summary>
        /// The longest text the selected model accepts in one synthesis request. ElevenLabs rejects
        /// anything longer rather than truncating it, and the ceiling differs sharply between
        /// models, so text must be shortened or split at sentence boundaries to fit the active
        /// choice.
        /// </summary>
        public int MaxRequestCharacters
        {
            get
            {
                switch (currentModel)
                {
                    case Model.FlashV2_5:
                    case Model.TurboV2_5:      return 40000;
                    case Model.MultilingualV2: return 10000;
                    case Model.V3:             return 3000;
                    default:                   return 10000;
                }
            }
        }

        private string GetModelId()
        {
            switch (currentModel)
            {
                case Model.MultilingualV2: return "eleven_multilingual_v2";
                case Model.FlashV2_5: return "eleven_flash_v2_5";
                case Model.TurboV2_5: return "eleven_turbo_v2_5";
                case Model.V3: return "eleven_v3";
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

        private void ClearAvailableVoices()
        {
            AvailableVoices = new List<VoiceData>();
            AvailableVoiceNames = new List<string>();
            AvailableVoiceIDs = new List<string>();
            Voices = new List<string>();
            VoicesReady = true;
        }

        private void SetAvailableVoices(IEnumerable<VoiceData> voices)
        {
            AvailableVoices = voices != null ? new List<VoiceData>(voices) : new List<VoiceData>();
            AvailableVoiceNames = AvailableVoices.Select(voice => voice.name).ToList();
            AvailableVoiceIDs = AvailableVoices.Select(voice => voice.voice_id).ToList();
            Voices = new List<string>(AvailableVoiceNames);
            VoicesReady = true;
        }

        public IEnumerator GetAvailableVoicesCoroutine()
        {
#if UNITY_WEBGL
            string voicesUrl = ConfigurationSystemUnity.GetElevenLabsTtsProxyEndpoint("voices");
            if (string.IsNullOrWhiteSpace(voicesUrl))
            {
                AvailableVoices = new List<VoiceData>();
                AvailableVoiceNames = new List<string>();
                AvailableVoiceIDs = new List<string>();
                Voices = new List<string>();
                VoicesReady = true;
                yield break;
            }

            using (var request = UnityWebRequest.Get(voicesUrl))
            {
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"ElevenLabsTextToSpeech.GetAvailableVoicesCoroutine() - WebGL voices fetch failed: {request.error}");
                    ClearAvailableVoices();
                    yield break;
                }

                VoiceDataContainer reply = null;
                try
                {
                    reply = JsonUtility.FromJson<VoiceDataContainer>(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ElevenLabsTextToSpeech.GetAvailableVoicesCoroutine() - WebGL voices JSON parse failed: {ex.Message}");
                    ClearAvailableVoices();
                    yield break;
                }

                SetAvailableVoices((reply?.voices ?? new List<VoiceData>())
                    .Where(voiceData => voiceData != null && !string.IsNullOrEmpty(voiceData.name) && !string.IsNullOrEmpty(voiceData.voice_id))
                    .GroupBy(voiceData => voiceData.voice_id, StringComparer.Ordinal)
                    .Select(group => group.First())
                    .ToList());
            }
#else
            using (var www = UnityWebRequest.Get(BaseUrl + "voices"))
            {
                var configSystem = Systems.Get<ConfigurationSystemUnity>();
                var apiKey = configSystem.config.elevenLabs.apiKey;

                www.SetRequestHeader("Accept", "application/json");
                www.SetRequestHeader("xi-api-key", apiKey);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(www.error);
                    yield break;
                }

                var voiceDataContainer = JsonUtility.FromJson<VoiceDataContainer>(www.downloadHandler.text);
                SetAvailableVoices(voiceDataContainer.voices);

                //foreach (var voice in AvailableVoices)
                //    Debug.Log($"Voice Name: {voice.name}, Voice ID: {voice.voice_id}");
            }
#endif
        }

        public int BeginTimingRequest()
        {
            ActiveTimingRequestVersion++;
            FailedTimingRequestVersion = 0;
            LastGeneratedAudioPathOrUrl = null;
            return ActiveTimingRequestVersion;
        }

        public void SetDebugOutputEnabled(bool enabled)
        {
            DebugOutputEnabled = enabled;

            if (enabled && !m_ipaDictionariesAudited)
            {
                AuditIpaDictionaries();
                m_ipaDictionariesAudited = true;
            }
        }

        public IEnumerator ConvertTextToSpeechCoroutine(string text)
        {
            int requestVersion = BeginTimingRequest();
            yield return StartCoroutine(ConvertTextToSpeechCoroutine(text, requestVersion));
        }

        public IEnumerator ConvertTextToSpeechCoroutine(string text, int requestVersion)
        {
            if (SelectedVoiceIndex != -1 && SelectedVoiceIndex < AvailableVoices.Count)
            {
                var voiceID = AvailableVoices[SelectedVoiceIndex].voice_id;
                //yield return StartCoroutine(GetAudioClip(text, voiceID));
                yield return StartCoroutine(GetAudioClipWithTimestamps(text, voiceID, requestVersion));
            }
            else
            {
                Debug.LogError("Invalid voice selected.");
                yield break;
            }
        }

        public IEnumerator RequestSpeechGenerationCoroutine(string text, string voice, int requestVersion, Action<bool> resultCallback)
        {
#if UNITY_WEBGL
            yield return StartCoroutine(RequestWebGlGenerateCoroutine(text, voice, requestVersion, resultCallback));
#else
            if (!string.IsNullOrEmpty(voice))
                SelectedVoiceIndex = Voices != null ? Voices.IndexOf(voice) : -1;

            if (SelectedVoiceIndex == -1 || SelectedVoiceIndex >= AvailableVoices.Count)
            {
                Debug.LogError("ElevenLabsTextToSpeech.RequestSpeechGenerationCoroutine() - Invalid voice selected.");
                FailedTimingRequestVersion = requestVersion;
                LastGeneratedAudioPathOrUrl = null;
                resultCallback?.Invoke(false);
                yield break;
            }

            yield return StartCoroutine(ConvertTextToSpeechCoroutine(text, requestVersion));

            bool success = CompletedTimingRequestVersion == requestVersion;
            if (success)
                LastGeneratedAudioPathOrUrl = savedFilePath;
            else
            {
                FailedTimingRequestVersion = requestVersion;
                LastGeneratedAudioPathOrUrl = null;
            }

            resultCallback?.Invoke(success);
#endif
        }

        public static string PreprocessString(string input)
        {
            // Replace newlines and any other non-standard characters
            string processedInput = Regex.Replace(input, @"\t|\n|\r", " ");
            // add additional lines of processing here

            return processedInput;
        }

        private string BuildSynthesisRequestJson(string textToSpeak, bool includeVoiceSettings = true)
        {
            var request = new ElevenLabsSynthesisRequest
            {
                text = textToSpeak,
                model_id = GetModelId(),
                voice_settings = includeVoiceSettings
                    ? new ElevenLabsVoiceSettings { stability = stabilityParameter, similarity_boost = similarityParameter }
                    : null
            };

            return JsonUtility.ToJson(request);
        }

        private static string FormatRequestFailure(UnityWebRequest www, string operation)
        {
            string responseBody = www.downloadHandler?.text;
            if (string.IsNullOrEmpty(responseBody))
                return $"{operation} failed: {www.error}";

            return $"{operation} failed: {www.error}. Response: {responseBody}";
        }

#if UNITY_WEBGL
        private string BuildWebGlLambdaRequestJson(string textToSpeak, string voice)
        {
            string voiceId = GetVoiceIdForRequestedVoice(voice);
            var request = new WebGlLambdaRequest
            {
                text = textToSpeak,
                voice_id = voiceId,
                model_id = GetModelId(),
                voice_settings = new ElevenLabsVoiceSettings
                {
                    stability = stabilityParameter,
                    similarity_boost = similarityParameter
                }
            };

            return JsonUtility.ToJson(request);
        }

        private string GetVoiceIdForRequestedVoice(string voice)
        {
            if (!string.IsNullOrEmpty(voice))
            {
                int voiceIndex = Voices != null ? Voices.IndexOf(voice) : -1;
                if (voiceIndex >= 0 && voiceIndex < AvailableVoiceIDs.Count)
                    return AvailableVoiceIDs[voiceIndex];
            }

            if (SelectedVoiceIndex >= 0 && SelectedVoiceIndex < AvailableVoiceIDs.Count)
                return AvailableVoiceIDs[SelectedVoiceIndex];

            return string.Empty;
        }

        private IEnumerator RequestWebGlGenerateCoroutine(string textToSpeak, string voice, int requestVersion, Action<bool> resultCallback)
        {
            textToSpeak = PreprocessString(textToSpeak);

            string url = ConfigurationSystemUnity.GetElevenLabsTtsProxyEndpoint("generate");
            if (string.IsNullOrWhiteSpace(url))
            {
                FailedTimingRequestVersion = requestVersion;
                LastGeneratedAudioPathOrUrl = null;
                resultCallback?.Invoke(false);
                yield break;
            }

            string body = BuildWebGlLambdaRequestJson(textToSpeak, voice);

            if (string.IsNullOrEmpty(GetVoiceIdForRequestedVoice(voice)))
            {
                Debug.LogError("ElevenLabsTextToSpeech.RequestWebGlGenerateCoroutine() - Missing voice_id for requested WebGL voice.");
                FailedTimingRequestVersion = requestVersion;
                resultCallback?.Invoke(false);
                yield break;
            }

            using (var request = UnityWebRequest.Put(url, body))
            {
                request.method = UnityWebRequest.kHttpVerbPOST;
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError(FormatRequestFailure(request, "ElevenLabs WebGL generate request"));
                    FailedTimingRequestVersion = requestVersion;
                    resultCallback?.Invoke(false);
                    yield break;
                }

                WebGlLambdaGenerateReply reply = null;
                try
                {
                    reply = JsonUtility.FromJson<WebGlLambdaGenerateReply>(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"ElevenLabsTextToSpeech.RequestWebGlGenerateCoroutine() - JSON parse error: {ex.Message}");
                    FailedTimingRequestVersion = requestVersion;
                    resultCallback?.Invoke(false);
                    yield break;
                }

                if (reply == null)
                {
                    Debug.LogError("ElevenLabsTextToSpeech.RequestWebGlGenerateCoroutine() - Missing Lambda reply.");
                    FailedTimingRequestVersion = requestVersion;
                    resultCallback?.Invoke(false);
                    yield break;
                }

                var timestampsResult = new ElevenLabsTimestampsResult
                {
                    audio_base64 = reply.audio_base64,
                    alignment = reply.alignment,
                    normalized_alignment = reply.normalized_alignment
                };

                ApplyTimestampsResult(timestampsResult, requestVersion);

                if (!string.IsNullOrEmpty(reply.audio_base64))
                {
                    LastGeneratedAudioClip = null;
                    clipTime = EstimateAlignmentDurationSeconds(reply.normalized_alignment ?? reply.alignment);
                    LastGeneratedAudioPathOrUrl = CreateInlineAudioReference(reply.audio_base64);
                    if (string.IsNullOrEmpty(LastGeneratedAudioPathOrUrl))
                    {
                        Debug.LogError("ElevenLabsTextToSpeech.RequestWebGlGenerateCoroutine() - Failed to create an inline audio reference.");
                        FailedTimingRequestVersion = requestVersion;
                        resultCallback?.Invoke(false);
                        yield break;
                    }

                    if (DebugOutputEnabled)
                        Debug.Log($"[ElevenLabs WebGL] Audio delivery=inline/blob, estimatedDuration={clipTime:0.###}s");

                    resultCallback?.Invoke(true);
                    yield break;
                }

                if (string.IsNullOrEmpty(reply.url))
                {
                    Debug.LogError("ElevenLabsTextToSpeech.RequestWebGlGenerateCoroutine() - Missing audio payload in Lambda reply.");
                    FailedTimingRequestVersion = requestVersion;
                    LastGeneratedAudioPathOrUrl = null;
                    resultCallback?.Invoke(false);
                    yield break;
                }

                LastGeneratedAudioPathOrUrl = reply.url;
                if (DebugOutputEnabled)
                    Debug.Log($"[ElevenLabs WebGL] Audio delivery=s3/url, estimatedDuration={clipTime:0.###}s, url={reply.url}");

                resultCallback?.Invoke(true);
            }
        }
#endif

        private static float EstimateAlignmentDurationSeconds(ElevenLabsAlignment alignment)
        {
            if (alignment?.character_end_times_seconds == null || alignment.character_end_times_seconds.Length == 0)
                return 0f;

            double maxEndTime = 0d;
            for (int i = 0; i < alignment.character_end_times_seconds.Length; i++)
            {
                if (alignment.character_end_times_seconds[i] > maxEndTime)
                    maxEndTime = alignment.character_end_times_seconds[i];
            }

            return (float)maxEndTime;
        }

        private static string CreateInlineAudioReference(string audioBase64)
        {
            if (string.IsNullOrEmpty(audioBase64))
                return null;

            IntPtr blobUrlPtr = RideWebGLAudio_CreateAudioBlobUrl("audio/mpeg", audioBase64);
            return blobUrlPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(blobUrlPtr) : null;
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

            var postData = BuildSynthesisRequestJson(textToSpeak);
            var ttsUrl = $"{BaseUrl}text-to-speech/{voiceID}";

            using (var www = UnityWebRequestMultimedia.GetAudioClip(ttsUrl, AudioType.MPEG))
            {
                var configSystem = Systems.Get<ConfigurationSystemUnity>();
                var apiKey = configSystem.config.elevenLabs.apiKey;

                www.method = UnityWebRequest.kHttpVerbPOST;
                www.SetRequestHeader("Accept", "audio/mpeg");
                www.SetRequestHeader("xi-api-key", apiKey);
                www.SetRequestHeader("Content-Type", "application/json");

                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(FormatRequestFailure(www, "ElevenLabs audio request"));
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(www);

                clipTime = clip.length;
                LastGeneratedAudioClip = clip;
                SaveAudioClipToWav(clip, savedFilePath);
            }
        }

        private IEnumerator GetAudioClipWithTimestamps(string textToSpeak, string voiceID, int requestVersion)
        {
            textToSpeak = PreprocessString(textToSpeak); // preprocess the text

            var postData = BuildSynthesisRequestJson(textToSpeak);
            var ttsUrl = $"{BaseUrl}text-to-speech/{voiceID}/with-timestamps";

            using (var www = UnityWebRequest.Get(ttsUrl))
            {
                var configSystem = Systems.Get<ConfigurationSystemUnity>();
                var apiKey = configSystem.config.elevenLabs.apiKey;

                www.method = UnityWebRequest.kHttpVerbPOST;
                www.SetRequestHeader("xi-api-key", apiKey);
                www.SetRequestHeader("Content-Type", "application/json");

                var bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(FormatRequestFailure(www, "ElevenLabs timestamp request"));
                    yield break;
                }

                var result = JsonUtility.FromJson<ElevenLabsTimestampsResult>(www.downloadHandler.text);
                ApplyTimestampsResult(result, requestVersion);

                // Convert base64 to audio clip and save
                yield return StartCoroutine(DecodeBase64ToAudioClip(result.audio_base64));
            }
        }

        private void ApplyTimestampsResult(ElevenLabsTimestampsResult result, int requestVersion)
        {
            LastTimestampsResult = result;

            var alignment = result != null ? (result.normalized_alignment ?? result.alignment) : null;
            LastWordSegments = ExtractWordSegments(alignment);
            LastWordIpaSegments = BuildWordIpaSegments(LastWordSegments, ipaDictionaries);
            LastPhoneSegments = BuildPhoneSegments(LastWordIpaSegments);
            LastFacefxKeyframes = BuildFacefxScheduleFromPhones(LastPhoneSegments, gapToNeutralSeconds: 0.05);
            CompletedTimingRequestVersion = requestVersion;

            if (!DebugOutputEnabled)
                return;

            foreach (var word in LastWordSegments)
                Debug.Log($"[ElevenLabs Word] {word}");

            var missingWords = new List<string>();
            var fallbackWordSummaries = new List<string>();
            var unmappedPhoneSummaries = new List<string>();
            var unmappedPhoneTokens = new HashSet<string>(StringComparer.Ordinal);
            foreach (var segment in LastWordIpaSegments)
            {
                if (!segment.HasIpa)
                {
                    missingWords.Add(segment.Word.Word);
                    fallbackWordSummaries.Add($"{segment.Word.Word} => {string.Join(" ", BuildFallbackTokensForWord(segment.Word.Word))}");
                }
            }

            foreach (var phone in LastPhoneSegments)
            {
                if (string.IsNullOrEmpty(phone.IpaToken))
                    continue;

                string normalizedToken = NormalizeIpaTokenForPoseLookup(phone.IpaToken);
                if (TryGetPose(phone.IpaToken, out _))
                    continue;

                unmappedPhoneTokens.Add(normalizedToken);

                if (!string.Equals(phone.IpaToken, normalizedToken, StringComparison.Ordinal))
                    unmappedPhoneSummaries.Add($"{phone.IpaToken}->{normalizedToken} ({phone.SourceWord}) [{phone.StartTimeSeconds:0.000}-{phone.EndTimeSeconds:0.000}]");
                else
                    unmappedPhoneSummaries.Add($"{phone.IpaToken} ({phone.SourceWord}) [{phone.StartTimeSeconds:0.000}-{phone.EndTimeSeconds:0.000}]");
            }

            Debug.Log($"[ElevenLabs IPA] Words={LastWordIpaSegments.Count}, MissingIPA={missingWords.Count} - {string.Join(", ", missingWords)}");
            Debug.Log($"[ElevenLabs Fallback] Words={fallbackWordSummaries.Count} - {string.Join(", ", fallbackWordSummaries)}");
            Debug.Log($"[ElevenLabs Phones] count={LastPhoneSegments.Count} - {string.Join(", ", LastPhoneSegments)}");
            Debug.Log($"[ElevenLabs FaceFX Map] MissingPhoneMappings={unmappedPhoneSummaries.Count}, UniqueTokens={unmappedPhoneTokens.Count} - {string.Join(", ", unmappedPhoneSummaries)}");
            Debug.Log($"[ElevenLabs FaceFX Raw] Keyframes={LastFacefxKeyframes.Count}{Environment.NewLine}{FormatFacefxKeyframesForDebug(LastFacefxKeyframes)}");
        }

        private IEnumerator GetAudioClipPlay(string textToSpeak, string voiceID)
        {
            var postData = BuildSynthesisRequestJson(textToSpeak, includeVoiceSettings: false);
            var ttsUrl = $"{BaseUrl}text-to-speech/{voiceID}";

            using (var www = UnityWebRequestMultimedia.GetAudioClip(ttsUrl, AudioType.MPEG))
            {
                var configSystem = Systems.Get<ConfigurationSystemUnity>();
                var apiKey = configSystem.config.elevenLabs.apiKey;

                www.method = UnityWebRequest.kHttpVerbPOST;
                www.SetRequestHeader("Accept", "audio/mpeg");
                www.SetRequestHeader("xi-api-key", apiKey);
                www.SetRequestHeader("Content-Type", "application/json");

                var bodyRaw = Encoding.UTF8.GetBytes(postData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(FormatRequestFailure(www, "ElevenLabs preview audio request"));
                    yield break;
                }

                var clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.clip = clip;
                audioSource.Play();
            }
        }

        private IEnumerator GetStreamingAudioClip(string textToSpeak, string voiceID)
        {
            var postData = BuildSynthesisRequestJson(textToSpeak, includeVoiceSettings: false);
            var ttsUrl = $"{BaseUrl}text-to-speech/{voiceID}/stream";

            using (var www = new UnityWebRequest(ttsUrl, UnityWebRequest.kHttpVerbPOST))
            {
                var configSystem = Systems.Get<ConfigurationSystemUnity>();
                var apiKey = configSystem.config.elevenLabs.apiKey;

                var downloadHandler = new DownloadHandlerAudioClip(www.url, AudioType.MPEG);
                downloadHandler.streamAudio = true;

                www.downloadHandler = downloadHandler;

                www.SetRequestHeader("Accept", "audio/mpeg");
                www.SetRequestHeader("xi-api-key", apiKey);
                www.SetRequestHeader("Content-Type", "application/json");

                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(postData));

                Debug.Log($"ElevenLabsTextToSpeech.GetStreamingAudioClip() - Sending POST request to URL: {ttsUrl}");
                Debug.Log($"ElevenLabsTextToSpeech.GetStreamingAudioClip() - POST data: {postData}");

                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("ElevenLabsTextToSpeech.GetStreamingAudioClip() - Successfully received response from server.");
                    var clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.clip = clip;
                    audioSource.Play();
                }
                else
                {
                    Debug.Log($"ElevenLabsTextToSpeech.GetStreamingAudioClip() - {FormatRequestFailure(www, "ElevenLabs streaming audio request")}");
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

            string localAudioPath = SaveAudioBytesToPersistentFile(audioData, audioType, "tempAudio");
            if (string.IsNullOrEmpty(localAudioPath))
                yield break;

            string normalizedPath = localAudioPath.Replace("\\", "/");
            string fileUrl = normalizedPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase)
                ? normalizedPath
                : "file://" + normalizedPath;

            // Keeping samples compressed saves memory, but AudioClip.GetData cannot read them,
            // and the clip is re-encoded to a WAV further down. Platforms that take that WAV
            // route therefore need the samples decompressed. WebGL delivers its audio as a blob
            // URL and never reaches this method, so it is listed here for intent only.
            bool keepSamplesCompressed = false;
            if (RideUtils.IsWebGL() && !RideUtils.IsEditor())
                keepSamplesCompressed = true;

            var downloadHandler = new DownloadHandlerAudioClip(fileUrl, audioType)
            {
                compressed = keepSamplesCompressed
            };

            using (var www = new UnityWebRequest(fileUrl, UnityWebRequest.kHttpVerbGET, downloadHandler, null))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);

                    clipTime = audioClip.length;
                    LastGeneratedAudioClip = audioClip;
                    SaveAudioClipToWav(audioClip, savedFilePath);

                    //Debug.Log("Audio clip loaded successfully!");
                }
                else
                {
                    Debug.LogError($"Failed to load audio: {www.error}");
                }
            }
        }

        private string SaveAudioBytesToPersistentFile(byte[] audioData, AudioType audioType, string fileNamePrefix)
        {
            if (audioData == null || audioData.Length == 0)
            {
                Debug.LogError("ElevenLabsTextToSpeech.SaveAudioBytesToPersistentFile() - Audio data is null or empty.");
                return null;
            }

            string extension = GetExtension(audioType);
            if (string.IsNullOrEmpty(extension) || string.Equals(extension, ".audio", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError($"ElevenLabsTextToSpeech.SaveAudioBytesToPersistentFile() - Unsupported audio type: {audioType}");
                return null;
            }

            string filePath = Path.Combine(Application.persistentDataPath, fileNamePrefix + extension);
            File.WriteAllBytes(filePath, audioData);
            return filePath;
        }

        private static AudioType DetectAudioType(byte[] data)
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

        private static string GetExtension(AudioType type)
        {
            switch (type)
            {
                case AudioType.WAV: return ".wav";
                case AudioType.MPEG: return ".mp3";
                case AudioType.OGGVORBIS: return ".ogg";
                default: return ".audio";
            }
        }

        private static Dictionary<string, string> BuildIPAToFacefxPhonemeMap()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in m_IPAtoFacefxEntries)
            {
                foreach (string token in entry.ipaTokens)
                {
                    if (string.IsNullOrWhiteSpace(token))
                        continue;

                    if (map.ContainsKey(token))
                    {
                        Debug.LogWarning($"Duplicate IPA token mapping detected: '{token}'");
                        continue;
                    }

                    map[token] = entry.facefxPhonemeLabel;
                }
            }

            return map;
        }

        private static Dictionary<string, IPAtoFacefxMap> BuildFacefxPhonemeToPoseMap()
        {
            var map = new Dictionary<string, IPAtoFacefxMap>(StringComparer.Ordinal);
            foreach (var entry in m_IPAtoFacefxEntries)
            {
                if (string.IsNullOrWhiteSpace(entry.facefxPhonemeLabel))
                    continue;

                if (map.ContainsKey(entry.facefxPhonemeLabel))
                {
                    Debug.LogWarning($"Duplicate FaceFX phoneme mapping detected: '{entry.facefxPhonemeLabel}'");
                    continue;
                }

                map[entry.facefxPhonemeLabel] = entry;
            }

            return map;
        }

        public static bool TryGetPose(string ipaToken, out IPAtoFacefxMap pose)
        {
            pose = null;

            if (string.IsNullOrWhiteSpace(ipaToken))
                return false;

            string normalizedToken = NormalizeIpaTokenForPoseLookup(ipaToken);
            if (string.IsNullOrWhiteSpace(normalizedToken))
                return false;

            if (!m_IPAToFacefxPhonemeMap.TryGetValue(normalizedToken, out string facefxPhonemeLabel))
                return false;

            return m_FacefxPhonemeToPoseMap.TryGetValue(facefxPhonemeLabel, out pose);
        }

        public static bool TryGetFacefxPhonemeLabel(string ipaToken, out string facefxPhonemeLabel)
        {
            facefxPhonemeLabel = null;

            if (string.IsNullOrWhiteSpace(ipaToken))
                return false;

            string normalizedToken = NormalizeIpaTokenForPoseLookup(ipaToken);
            if (string.IsNullOrWhiteSpace(normalizedToken))
                return false;

            return m_IPAToFacefxPhonemeMap.TryGetValue(normalizedToken, out facefxPhonemeLabel);
        }

        private static string NormalizeIpaTokenForPoseLookup(string ipaToken)
        {
            if (string.IsNullOrEmpty(ipaToken))
                return string.Empty;

            StringBuilder sb = null;

            for (int i = 0; i < ipaToken.Length; i++)
            {
                char c = ipaToken[i];
                if (IsLengthMark(c) || IsPrimaryCombining(c))
                {
                    sb ??= new StringBuilder(ipaToken.Length);
                    if (sb.Length == 0 && i > 0)
                        sb.Append(ipaToken, 0, i);

                    continue;
                }

                sb?.Append(c);
            }

            return sb != null ? sb.ToString() : ipaToken;
        }

        public static string FormatFacefxKeyframesForDebug(IReadOnlyList<FacefxKeyframe> keyframes)
        {
            if (keyframes == null || keyframes.Count == 0)
                return "<none>";

            StringBuilder sb = new();
            for (int i = 0; i < keyframes.Count; i++)
            {
                if (i > 0)
                    sb.AppendLine();

                AppendFacefxPoseDebugLine(sb, keyframes[i].TimeSeconds, keyframes[i].FacefxVisemes, keyframes[i].Amounts);
            }

            return sb.ToString();
        }

        public static void AppendFacefxPoseDebugLine(StringBuilder sb, double timeSeconds, string[] facefxVisemes, float[] amounts)
        {
            sb.Append(timeSeconds.ToString("0.000"));
            sb.Append("  ");

            if (facefxVisemes == null || amounts == null)
            {
                sb.Append("<null>");
                return;
            }

            int count = Math.Min(facefxVisemes.Length, amounts.Length);
            bool wroteAny = false;

            for (int i = 0; i < count; i++)
            {
                float amount = amounts[i];
                if (Mathf.Approximately(amount, 0f))
                    continue;

                if (wroteAny)
                    sb.Append(", ");

                sb.Append(facefxVisemes[i]);
                sb.Append('=');
                sb.Append(amount.ToString("0.00"));
                wroteAny = true;
            }

            if (!wroteAny)
                sb.Append("<zero>");
        }

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c);
        private static bool IsApostrophe(char c) => c == '\'' || c == '’';  // Treat apostrophe as joiner when surrounded by word chars (don't, you're, we'll).

        /// <summary>
        /// Extracts word-level timing spans from an ElevenLabs character alignment stream.
        /// </summary>
        /// <param name="alignment">The character-level alignment returned by ElevenLabs.</param>
        /// <returns>
        /// A list of <see cref="WordSegment"/> values describing each detected word and its timing span.
        /// </returns>
        /// <remarks>
        /// This method walks the aligned character stream, groups contiguous word characters into words,
        /// preserves apostrophes when they are surrounded by word characters, and computes word timing
        /// from the first and last aligned characters in each group. Non-word tokens such as punctuation
        /// are currently skipped.
        /// </remarks>
        private static List<WordSegment> ExtractWordSegments(ElevenLabsAlignment alignment)
        {
            var segments = new List<WordSegment>();

            if (alignment == null ||
                alignment.characters == null ||
                alignment.character_start_times_seconds == null ||
                alignment.character_end_times_seconds == null)
                return segments;

            char[] chars = alignment.ToCharArray();
            double[] starts = alignment.character_start_times_seconds;
            double[] ends = alignment.character_end_times_seconds;

            int n = chars.Length;
            if (starts.Length != n || ends.Length != n)
                n = Math.Min(n, Math.Min(starts.Length, ends.Length));  // Defensive: if lengths mismatch, use the shortest.

            int i = 0;
            while (i < n)
            {
                char c = chars[i];

                if (char.IsWhiteSpace(c)) { i++; continue; }  // Skip whitespace quickly.

                // Start of a word?
                if (IsWordChar(c))
                {
                    int startIndex = i;
                    int endIndex = i;

                    i++;

                    while (i < n)
                    {
                        char cc = chars[i];

                        if (IsWordChar(cc))
                        {
                            endIndex = i;
                            i++;
                            continue;
                        }

                        // Allow apostrophe within a word if surrounded by word chars.
                        if (IsApostrophe(cc))
                        {
                            bool hasPrev = (i - 1) >= startIndex && IsWordChar(chars[i - 1]);
                            bool hasNext = (i + 1) < n && IsWordChar(chars[i + 1]);
                            if (hasPrev && hasNext)
                            {
                                endIndex = i;
                                i++;
                                continue;
                            }
                        }

                        break;
                    }

                    // Build the word string from the char span.
                    string word = new string(chars, startIndex, (endIndex - startIndex) + 1);
                    double startTime = starts[startIndex];
                    double endTime = ends[endIndex];

                    // Defensive clamp: occasionally end < start if alignment is weird.
                    if (endTime < startTime)
                        endTime = startTime;

                    segments.Add(new WordSegment
                    {
                        Word = word,
                        StartTimeSeconds = startTime,
                        EndTimeSeconds = endTime,
                        StartCharIndex = startIndex,
                        EndCharIndex = endIndex
                    });

                    continue;
                }

                i++;  // Non-word token (punctuation, etc.) - skip for now.
            }

            return segments;
        }

        /// <summary>
        /// Builds a list of word-to-IPA segments by applying dictionary lookup to previously
        /// extracted word timing spans.
        /// </summary>
        /// <param name="words">The word segments extracted from the ElevenLabs alignment.</param>
        /// <param name="ipaDictionary">The IPA dictionary used for pronunciation lookup.</param>
        /// <returns>
        /// A list of <see cref="WordIpaSegment"/> values, one for each input word segment.
        /// </returns>
        /// <remarks>
        /// This method preserves the original timing information regardless of whether a pronunciation
        /// is found. Missing IPA entries are represented by <see cref="WordIpaSegment.HasIpa"/> being false.
        /// </remarks>
        private static List<WordIpaSegment> BuildWordIpaSegments(IReadOnlyList<WordSegment> words, IReadOnlyList<IpaDictionary> ipaDictionaries)
        {
            var outList = new List<WordIpaSegment>(words != null ? words.Count : 0);
            if (words == null || words.Count == 0)
                return outList;

            foreach (var word in words)
            {
                bool hasIpa = TryGetIpa(ipaDictionaries, word.Word, out var ipa);
                outList.Add(new WordIpaSegment { Word = word, HasIpa = hasIpa, Ipa = ipa });
            }

            return outList;
        }

        private static bool TryGetIpa(IReadOnlyList<IpaDictionary> ipaDictionaries, string word, out string ipa)
        {
            if (ipaDictionaries != null)
            {
                foreach (var dict in ipaDictionaries)
                {
                    if (dict == null || !dict.IsLoaded)
                        continue;

                    if (dict.TryGetIpa(word, out ipa))
                        return true;
                }
            }

            ipa = null;
            return false;
        }

        private static bool IsStressMark(char c) => c == 'ˈ' || c == 'ˌ';
        private static bool IsLengthMark(char c) => c == 'ː';
        private static bool IsPrimaryCombining(char c) => c == '̃' || c == '̩' || c == '̯';  // Add more as you encounter them in your dict.
        private static bool IsWhitespaceOrSeparator(char c) => char.IsWhiteSpace(c) || c == '.' || c == '-';
        private static bool IsFallbackLatinLetter(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        private static bool IsFallbackVowelLetter(char c)
        {
            switch (char.ToLowerInvariant(c))
            {
                case 'a':
                case 'e':
                case 'i':
                case 'o':
                case 'u':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Tokenizes an IPA pronunciation string into phoneme-like units suitable for timing allocation
        /// and FaceFX pose lookup.
        /// </summary>
        /// <param name="ipa">The IPA pronunciation string to tokenize.</param>
        /// <returns>
        /// A list of IPA tokens in spoken order.
        /// </returns>
        /// <remarks>
        /// This tokenizer performs a practical, English-oriented split rather than a full phonological parse.
        /// It removes surrounding slashes when present, ignores stress markers, prefers known multi-character
        /// tokens such as affricates and common diphthongs, and attaches recognized length or combining marks
        /// to the token they modify.
        /// </remarks>
        private static List<string> TokenizeIpa(string ipa)
        {
            var tokens = new List<string>(16);

            if (string.IsNullOrEmpty(ipa))
                return tokens;

            // Some sources include surrounding slashes. If your loader already strips them, this is a no-op.
            string s = ipa.Trim();
            if (s.Length >= 2 && s[0] == '/' && s[s.Length - 1] == '/')
                s = s.Substring(1, s.Length - 2);

            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];

                if (IsWhitespaceOrSeparator(c)) { i++; continue; }
                if (IsStressMark(c)) { i++; continue; }  // ignore stress for now.

                // Try multi-char tokens first.
                bool matched = false;
                for (int t = 0; t < s_CommonMultiCharTokens.Length; t++)
                {
                    string mt = s_CommonMultiCharTokens[t];
                    if (i + mt.Length <= s.Length && string.CompareOrdinal(s, i, mt, 0, mt.Length) == 0)
                    {
                        tokens.Add(mt);
                        i += mt.Length;
                        matched = true;
                        break;
                    }
                }

                if (matched)
                    continue;

                // Single char token.
                // Also absorb a following length mark or combining mark into the same token.
                int start = i;
                i++;

                while (i < s.Length)
                {
                    char cc = s[i];
                    if (IsLengthMark(cc) || IsPrimaryCombining(cc))
                    {
                        i++;
                        continue;
                    }
                    break;
                }

                tokens.Add(s.Substring(start, i - start));
            }

            return tokens;
        }

        private static bool IsVowelLikeToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            // approximate vowel list. Expand as needed.
            // We treat diphthongs like "aɪ" as vowel-like too.
            for (int i = 0; i < token.Length; i++)
            {
                char c = token[i];
                switch (c)
                {
                    case 'a':
                    case 'e':
                    case 'i':
                    case 'o':
                    case 'u':
                    case 'ɑ':
                    case 'æ':
                    case 'ə':
                    case 'ɛ':
                    case 'ɪ':
                    case 'ʊ':
                    case 'ɔ':
                    case 'ɜ':
                    case 'ʌ':
                    case 'ɒ':
                    case 'ø':
                    case 'y':
                        return true;
                }
            }

            return false;
        }

        private static double GetTokenWeight(string token)
        {
            if (string.IsNullOrEmpty(token))
                return 1.0;

            double w = IsVowelLikeToken(token) ? 1.5 : 1.0;

            // Length mark increases weight.
            if (token.IndexOf('ː') >= 0)
                w *= 1.5;

            return w;
        }

        private static List<string> BuildFallbackTokensForWord(string word)
        {
            var tokens = new List<string>(8);
            if (string.IsNullOrWhiteSpace(word))
                return tokens;

            string s = word.ToLowerInvariant();
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (!IsFallbackLatinLetter(c))
                {
                    i++;
                    continue;
                }

                if ((i + 1) < s.Length)
                {
                    char c1 = s[i + 1];
                    switch (c)
                    {
                        case 's' when c1 == 'h':
                            tokens.Add("ʃ");
                            i += 2;
                            continue;
                        case 'c' when c1 == 'h':
                            tokens.Add("tʃ");
                            i += 2;
                            continue;
                        case 't' when c1 == 'h':
                            tokens.Add("θ");
                            i += 2;
                            continue;
                        case 'p' when c1 == 'h':
                            tokens.Add("f");
                            i += 2;
                            continue;
                        case 'n' when c1 == 'g':
                            tokens.Add("ŋ");
                            i += 2;
                            continue;
                        case 'w' when c1 == 'h':
                            tokens.Add("w");
                            i += 2;
                            continue;
                        case 'c' when c1 == 'k':
                            tokens.Add("k");
                            i += 2;
                            continue;
                        case 'q' when c1 == 'u':
                            tokens.Add("k");
                            tokens.Add("w");
                            i += 2;
                            continue;
                    }
                }

                if (IsFallbackVowelLetter(c))
                {
                    int start = i;
                    i++;
                    while (i < s.Length && IsFallbackVowelLetter(s[i]))
                        i++;

                    tokens.Add(GetFallbackVowelToken(s, start, i - start));
                    continue;
                }

                switch (c)
                {
                    case 'm':
                    case 'b':
                    case 'p':
                    case 'f':
                    case 'v':
                    case 'w':
                    case 's':
                    case 'z':
                    case 't':
                    case 'd':
                    case 'n':
                    case 'h':
                        tokens.Add(c.ToString());
                        break;
                    case 'r':
                        tokens.Add("ɹ");
                        break;
                    case 'l':
                        tokens.Add("l");
                        break;
                    case 'j':
                        tokens.Add("dʒ");
                        break;
                    case 'y':
                        tokens.Add("j");
                        break;
                    case 'k':
                    case 'c':
                    case 'q':
                        tokens.Add("k");
                        break;
                    case 'g':
                        tokens.Add("g");
                        break;
                    case 'x':
                        tokens.Add("k");
                        break;
                }

                i++;
            }

            if (tokens.Count == 0)
                tokens.Add("ə");
            else if (!tokens.Any(IsVowelLikeToken))
                tokens.Insert(Math.Min(1, tokens.Count), "ə");

            return tokens;
        }

        private static string GetFallbackVowelToken(string word, int startIndex, int length)
        {
            if (string.IsNullOrEmpty(word) || length <= 0)
                return "ə";

            string run = word.Substring(startIndex, length);
            char first = run[0];

            if (run.IndexOf('o') >= 0)
                return "oʊ";
            if (run.IndexOf('u') >= 0)
                return "u";

            switch (first)
            {
                case 'a': return "æ";
                case 'e': return "ɛ";
                case 'i': return "ɪ";
                default: return "ə";
            }
        }

        private static void AppendWeightedPhoneSegments(List<PhoneSegment> outList, List<string> tokens, double wordStart, double wordEnd, int wordIndex, string sourceWord)
        {
            if (outList == null || tokens == null || tokens.Count == 0)
                return;

            double duration = wordEnd - wordStart;
            if (duration <= 0.000001)
                return;

            double totalWeight = 0.0;
            double[] weights = new double[tokens.Count];
            for (int i = 0; i < tokens.Count; i++)
            {
                double wt = GetTokenWeight(tokens[i]);
                weights[i] = wt;
                totalWeight += wt;
            }

            if (totalWeight <= 0.000001)
                return;

            double t0 = wordStart;
            for (int i = 0; i < tokens.Count; i++)
            {
                double frac = weights[i] / totalWeight;
                double t1 = (i == tokens.Count - 1) ? wordEnd : (t0 + (duration * frac));

                if (t1 < t0)
                    t1 = t0;

                outList.Add(new PhoneSegment
                {
                    IpaToken = tokens[i],
                    StartTimeSeconds = t0,
                    EndTimeSeconds = t1,
                    WordIndex = wordIndex,
                    SourceWord = sourceWord
                });

                t0 = t1;
            }
        }

        /// <summary>
        /// Builds time-stamped IPA token segments from word-level IPA pronunciations.
        /// </summary>
        /// <param name="words">The word-level IPA segments to convert.</param>
        /// <returns>
        /// A list of <see cref="PhoneSegment"/> values representing tokenized IPA units allocated
        /// across the time span of each source word.
        /// </returns>
        /// <remarks>
        /// Each word duration is divided proportionally according to token weights generated by
        /// <see cref="GetTokenWeight(string)"/>. Words without IPA data fall back to a small
        /// English-leaning consonant-anchor plus generic-vowel heuristic so the generated schedule
        /// still produces approximate mouth movement instead of remaining neutral.
        /// </remarks>
        private static List<PhoneSegment> BuildPhoneSegments(IReadOnlyList<WordIpaSegment> words)
        {
            var outList = new List<PhoneSegment>(capacity: 256);

            if (words == null || words.Count == 0)
                return outList;

            for (int wi = 0; wi < words.Count; wi++)
            {
                WordIpaSegment w = words[wi];

                double wordStart = w.Word.StartTimeSeconds;
                double wordEnd = w.Word.EndTimeSeconds;

                // Defensive: skip zero/negative duration.
                double duration = wordEnd - wordStart;
                if (duration <= 0.000001)
                    continue;

                List<string> tokens = (w.HasIpa && !string.IsNullOrEmpty(w.Ipa))
                    ? TokenizeIpa(w.Ipa)
                    : BuildFallbackTokensForWord(w.Word.Word);

                AppendWeightedPhoneSegments(outList, tokens, wordStart, wordEnd, wi, w.Word.Word);
            }

            return outList;
        }

        /// <summary>
        /// Gets the neutral or silence FaceFX pose used when no specific IPA token mapping is available
        /// or when the mouth should return to rest during pauses.
        /// </summary>
        /// <returns>The neutral FaceFX pose.</returns>
        /// <remarks>
        /// This method prefers the dedicated silence entry from the IPA-to-FaceFX map.
        /// </remarks>
        public static IPAtoFacefxMap GetNeutralPose() => m_FacefxPhonemeToPoseMap["SILENCE"];

        /// <summary>
        /// Builds an approximate FaceFX keyframe schedule from externally supplied word timings.
        /// </summary>
        /// <param name="words">Word timings to convert into phoneme-weighted viseme keyframes.</param>
        /// <param name="ipaDictionaries">Optional IPA dictionaries used for pronunciation lookup.</param>
        /// <param name="gapToNeutralSeconds">Minimum pause duration before inserting a neutral keyframe.</param>
        /// <returns>A FaceFX-style keyframe sequence suitable for proxy lipsync generation.</returns>
        public static List<FacefxKeyframe> BuildApproximateFacefxSchedule(
            IReadOnlyList<WordSegment> words,
            IReadOnlyList<IpaDictionary> ipaDictionaries,
            double gapToNeutralSeconds = 0.05)
        {
            var wordIpaSegments = BuildWordIpaSegments(words, ipaDictionaries);
            var phoneSegments = BuildPhoneSegments(wordIpaSegments);
            return BuildFacefxScheduleFromPhones(phoneSegments, gapToNeutralSeconds);
        }

        /// <summary>
        /// Builds a FaceFX keyframe schedule from a sequence of time-stamped IPA token segments.
        /// </summary>
        /// <param name="phones">The IPA token segments to convert.</param>
        /// <param name="gapToNeutralSeconds">
        /// The minimum pause duration required before a neutral keyframe is inserted between tokens.
        /// </param>
        /// <returns>
        /// A list of <see cref="FacefxKeyframe"/> values suitable for driving a FaceFX-style lipsync sequence.
        /// </returns>
        /// <remarks>
        /// Each phone contributes a pose key at its start time. If a sufficiently large gap exists between
        /// consecutive phones, a neutral pose is inserted at the end of the preceding phone to close or relax
        /// the mouth during the pause. The generated sequence is also forced back to neutral at the end.
        /// </remarks>
        private static List<FacefxKeyframe> BuildFacefxScheduleFromPhones(IReadOnlyList<PhoneSegment> phones, double gapToNeutralSeconds)
        {
            var outList = new List<FacefxKeyframe>(phones != null ? (phones.Count * 2) : 0);

            if (phones == null || phones.Count == 0)
                return outList;

            var neutral = GetNeutralPose();

            double lastEnd = -1.0;

            for (int i = 0; i < phones.Count; i++)
            {
                PhoneSegment p = phones[i];

                if (lastEnd >= 0.0)
                {
                    double gap = p.StartTimeSeconds - lastEnd;
                    if (gap > gapToNeutralSeconds)
                    {
                        // Insert neutral at the end of the last phone to close mouth during pause.
                        outList.Add(new FacefxKeyframe(lastEnd, neutral.facefxVisemes, neutral.amounts));
                    }
                }

                if (!TryGetPose(p.IpaToken, out var pose) || pose == null)
                    pose = neutral;  // Token not mapped -> neutral for now (future work: smarter fallback).

                outList.Add(new FacefxKeyframe(p.StartTimeSeconds, pose.facefxVisemes, pose.amounts));

                lastEnd = p.EndTimeSeconds;
            }

            // Ensure we end neutral.
            if (lastEnd >= 0.0)
                outList.Add(new FacefxKeyframe(lastEnd, neutral.facefxVisemes, neutral.amounts));

            return outList;
        }

        /// <summary>
        /// Audits the IPA dictionaries assigned to this component and reports mapping coverage
        /// against the IPA-to-FaceFX viseme table.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method scans every pronunciation entry in the configured <see cref="IpaDictionary"/>
        /// instances, tokenizes the IPA strings using the same tokenizer used during lip-sync
        /// generation, and checks whether each IPA token has a corresponding entry in
        /// <c>m_IPAToFacefxPhonemeMap</c>.
        /// </para>
        ///
        /// <para>
        /// This is primarily a diagnostic tool used during development to verify that the
        /// IPA dictionary phoneme inventory is fully covered by the viseme mapping table.
        /// </para>
        /// </remarks>
        private void AuditIpaDictionaries()
        {
            var mappedTokens = new HashSet<string>(m_IPAToFacefxPhonemeMap.Keys, StringComparer.Ordinal);
            var tokenCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var unmappedTokenCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            int dictionaryCount = 0;
            int loadedDictionaryCount = 0;
            int totalEntries = 0;
            int totalEntriesWithIpa = 0;
            int totalEntriesFullyMapped = 0;
            int totalEntriesPartiallyMapped = 0;
            int totalEntriesFullyUnmapped = 0;
            int totalTokens = 0;
            int mappedTokenTotal = 0;
            int unmappedTokenTotal = 0;

            if (ipaDictionaries == null || ipaDictionaries.Count == 0)
            {
                Debug.Log("[IPA Audit] No IPA dictionaries assigned.");
                return;
            }

            for (int dictIndex = 0; dictIndex < ipaDictionaries.Count; dictIndex++)
            {
                IpaDictionary dict = ipaDictionaries[dictIndex];
                dictionaryCount++;

                if (dict == null)
                {
                    Debug.Log($"[IPA Audit] Dictionary slot {dictIndex} is null.");
                    continue;
                }

                if (!dict.IsLoaded)
                {
                    Debug.Log($"[IPA Audit] Dictionary slot {dictIndex} is not loaded.");
                    continue;
                }

                loadedDictionaryCount++;

                int dictEntries = 0;
                int dictFullyMapped = 0;
                int dictPartiallyMapped = 0;
                int dictFullyUnmapped = 0;
                int dictTokenTotal = 0;
                int dictMappedTokenTotal = 0;
                int dictUnmappedTokenTotal = 0;

                foreach (var entry in dict.EnumerateEntries())
                {
                    dictEntries++;
                    totalEntries++;
                    totalEntriesWithIpa++;

                    var tokens = TokenizeIpa(entry.Value);
                    if (tokens == null || tokens.Count == 0)
                        continue;

                    int entryMapped = 0;
                    int entryUnmapped = 0;

                    foreach (var token in tokens)
                    {
                        if (string.IsNullOrEmpty(token))
                            continue;

                        totalTokens++;
                        dictTokenTotal++;

                        if (tokenCounts.TryGetValue(token, out int tokenCount))
                            tokenCounts[token] = tokenCount + 1;
                        else
                            tokenCounts[token] = 1;

                        if (mappedTokens.Contains(token))
                        {
                            mappedTokenTotal++;
                            dictMappedTokenTotal++;
                            entryMapped++;
                        }
                        else
                        {
                            unmappedTokenTotal++;
                            dictUnmappedTokenTotal++;
                            entryUnmapped++;

                            if (unmappedTokenCounts.TryGetValue(token, out int unmappedCount))
                                unmappedTokenCounts[token] = unmappedCount + 1;
                            else
                                unmappedTokenCounts[token] = 1;
                        }
                    }

                    if (entryMapped > 0 && entryUnmapped == 0)
                    {
                        totalEntriesFullyMapped++;
                        dictFullyMapped++;
                    }
                    else if (entryMapped > 0 && entryUnmapped > 0)
                    {
                        totalEntriesPartiallyMapped++;
                        dictPartiallyMapped++;
                    }
                    else if (entryMapped == 0 && entryUnmapped > 0)
                    {
                        totalEntriesFullyUnmapped++;
                        dictFullyUnmapped++;
                    }
                }

                float dictCoverage = dictTokenTotal > 0 ? (100f * dictMappedTokenTotal / dictTokenTotal) : 0f;

                Debug.Log(
                    $"[IPA Audit] Dict {dictIndex}: entries={dictEntries}, " +
                    $"tokens={dictTokenTotal}, mapped={dictMappedTokenTotal}, unmapped={dictUnmappedTokenTotal}, " +
                    $"coverage={dictCoverage:0.0}%, fullyMappedEntries={dictFullyMapped}, " +
                    $"partiallyMappedEntries={dictPartiallyMapped}, fullyUnmappedEntries={dictFullyUnmapped}");
            }

            float totalCoverage = totalTokens > 0 ? (100f * mappedTokenTotal / totalTokens) : 0f;

            var topUnmapped = unmappedTokenCounts.ToList();
            topUnmapped.Sort((a, b) => b.Value.CompareTo(a.Value));

            var topAll = tokenCounts.ToList();
            topAll.Sort((a, b) => b.Value.CompareTo(a.Value));

            Debug.Log(
                $"[IPA Audit Summary] dictionaries={dictionaryCount}, loaded={loadedDictionaryCount}, " +
                $"entries={totalEntries}, tokens={totalTokens}, mapped={mappedTokenTotal}, unmapped={unmappedTokenTotal}, " +
                $"coverage={totalCoverage:0.0}%, fullyMappedEntries={totalEntriesFullyMapped}, " +
                $"partiallyMappedEntries={totalEntriesPartiallyMapped}, fullyUnmappedEntries={totalEntriesFullyUnmapped}");

            int maxToPrint = 25;

            if (topUnmapped.Count == 0)
            {
                Debug.Log("[IPA Audit] No unmapped IPA tokens found.");
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("[IPA Audit] Top unmapped IPA tokens:");
                for (int i = 0; i < topUnmapped.Count && i < maxToPrint; i++)
                    sb.AppendLine($"  {topUnmapped[i].Key} : {topUnmapped[i].Value}");
                Debug.Log(sb.ToString());
            }

            if (topAll.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("[IPA Audit] Top IPA tokens overall:");
                for (int i = 0; i < topAll.Count && i < maxToPrint; i++)
                {
                    string token = topAll[i].Key;
                    bool isMapped = mappedTokens.Contains(token);
                    sb.AppendLine($"  {token} : {topAll[i].Value} {(isMapped ? "[mapped]" : "[UNMAPPED]")}");
                }
                Debug.Log(sb.ToString());
            }
        }
    }
}
