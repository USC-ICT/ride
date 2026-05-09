using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using NonverbalBehaviorGenerator;

namespace Ride
{
    /// <summary>
    /// System responsible for generating nonverbal behavior using the NVBG engine within a Unity environment.
    /// Loads required XSL/XML/text resources from TextAssets and manages character generation and behavior requests.
    /// </summary>
    public class NonverbalBehaviorGeneratorSystem : RideSystemMonoBehaviour, INonverbalGeneratorSystem
    {
        private const string StoryPointId = "toolkitsession";

        private class NvbgInitData
        {
            public string TransformXsl;
            public string RuleXml;
            public string SaliencyMapXml;
            public Dictionary<string, Stream> Streams;
        }

        /// <summary>
        /// Struct to represent an input stream for NVBG, containing the filename and the associated TextAsset.
        /// </summary>
        [Serializable] public class StreamInfo
        {
            [HideInInspector][SerializeField] public string fileName;
            [Tooltip("Assign either the raw file (as a TextAsset) or a .zip (also imported as a TextAsset).")]
            public TextAsset textAsset;
            [Tooltip("If true, TextAsset.bytes is treated as a .zip, and ZipEntryName is opened from it.")]
            public bool isZip;

            public override string ToString() => fileName;

            public StreamInfo(string _filename, bool _isZip = false) { fileName = _filename; textAsset = null; isZip = _isZip; }
        }

        public List<StreamInfo> m_streams = new()
        {
            new("NVBG_behavior_description.xsl"),
            new("NVBG_rules.xsl"),
            new("NVBG_transform.xsl"),
            new("rule_input_ChrKevin.xml"),
            new("saliency_map_init_kevin.xml"),
            // ParserModelEN folder
            new("endings.txt", _isZip : true),
            new("featInfo.h"),
            new("featInfo.l"),
            new("featInfo.lm"),
            new("featInfo.m"),
            new("featInfo.r"),
            new("featInfo.rm"),
            new("featInfo.ru"),
            new("featInfo.s"),
            new("featInfo.t"),
            new("featInfo.tt"),
            new("featInfo.u"),
            new("h.g", _isZip: true),
            new("h.lambdas"),
            new("headInfo.txt"),
            new("l.g", _isZip: true),
            new("l.lambdas"),
            new("lm.g", _isZip : true),
            new("lm.lambdas"),
            new("m.g", _isZip : true),
            new("m.lambdas"),
            new("nttCounts.txt"),
            new("pSgT.txt", _isZip : true),
            new("pUgT.txt"),
            new("r.g", _isZip : true),
            new("r.lambdas"),
            new("rm.g", _isZip : true),
            new("rm.lambdas"),
            new("ru.g", _isZip : true),
            new("ru.lambdas"),
            new("terms.txt"),
            new("tt.g", _isZip : true),
            new("tt.lambdas"),
            new("u.g", _isZip : true),
            new("u.lambdas"),
            new("unitRules.txt"),
        };

        public string CharacterId = "Kevin";
        public string IdlePostureId = "ChrGenericMleAdult@IdleStandingUpright01";
        public bool m_launchOnStartup = true;

        private Dictionary<string, Nvbg> m_characters = new Dictionary<string, Nvbg>();
        private Dictionary<string, Task<Nvbg>> m_characterInitTasks = new Dictionary<string, Task<Nvbg>>();


        #region IRideSystem

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

            if (m_launchOnStartup)
                StartProcess();
        }

        /// <inheritdoc/>
        public override void SystemShutdown()
        {
            base.SystemShutdown();
            StopProcess();
        }


        #endregion

        #region IExternalProcess
        /// <summary>
        /// Indicates whether the NVBG process has been initialized and loaded.
        /// </summary>
        public bool ProcessLoaded { get; private set; }

        /// <summary>
        /// Starts the NVBG process using the current CharacterId.
        /// </summary>
        public void StartProcess() => StartProcess(CharacterId);

        /// <summary>
        /// Starts the NVBG process for a specific character.
        /// </summary>
        /// <param name="characterName">Name of the character to generate in NVBG.</param>
        public void StartProcess(string characterName)
        {
            if (m_characters.ContainsKey(characterName))
                return;

            CreateCharacter(characterName);

            //Timer not needed since Saliency Idle Gaze is not enabled

            ProcessLoaded = true;
        }

        /// <summary>
        /// Creates a new character with configured options.
        /// </summary>
        /// <param name="characterName">The name of the character to create.</param>
        private void CreateCharacter(string characterName)
        {
            if (m_characters.ContainsKey(characterName))
                return; // already created

            if (m_characterInitTasks.ContainsKey(characterName))
                return; // already initializing

            var initData = BuildInitData(); // main thread: safe for TextAsset.text access

#if UNITY_WEBGL
            // WebGL fallback: no real threading → do it synchronously (same behavior as today)
            var character = CreateCharacterInternal(characterName, IdlePostureId, initData);
            m_characters[characterName] = character;
            ProcessLoaded = true;
#else
            // All other platforms: spin up a background task
            var initTask = Task.Run(() => CreateCharacterInternal(characterName, IdlePostureId, initData));
            m_characterInitTasks[characterName] = initTask;

            // start a coroutine that observes this task and flips ProcessLoaded when done
            StartCoroutine(WaitForCharacterInit(characterName, initTask));
#endif
        }

        /// <summary>
        /// Stops the NVBG process and disposes all character instances.
        /// </summary>
        public void StopProcess()
        {
            ProcessLoaded = false;
            foreach (var character in m_characters.Values)
                character.Dispose();

            m_characters.Clear();
        }
        #endregion

        #region INonverbalGenerator

        /// <inheritdoc/>
        public void GetNonverbalBehavior(string characterName, string text, INonverbalGeneratorSystem.NonverbalBehaviorResult resultCallback) =>
            StartCoroutine(Coroutine(characterName, text, resultCallback));

        //public async Task SetPosture(string characterName, string posture)

        /// <summary>
        /// Updates the posture used for NVBG character generation by recreating the character with a new idle posture.
        /// </summary>
        /// <param name="characterName">Name of the character to update.</param>
        /// <param name="posture">New posture ID to set.</param>
        public void SetPosture(string characterName, string posture)
        {
            // EDF - There's a serious bug in SetPostureIdAsync() where the value doesn't get saved.
            // this is because CreateContextAsync() is called which calls NvbgOptions.CreateContextAsync()
            // which resets all data to the original values.
            // To bypass this, create the Nvbg from scratch everytime we change postures
            IdlePostureId = posture;
            CreateCharacter(characterName);

            // original code
            //var character = characters[characterName];
            //await character.SetPostureIdAsync(posture);
        }

        /// <summary>
        /// Asynchronously gets the current posture ID for a character.
        /// </summary>
        /// <param name="characterName">The character to query.</param>
        /// <returns>Current posture ID string.</returns>
        public async Task<string> GetPosture(string characterName)
        {
            var character = m_characters[characterName];
            return await character.GetPostureIdAsync();
        }

        // main-thread helper to build the streams
        private NvbgInitData BuildInitData()
        {
            //var transformXslFilename = $"{Application.streamingAssetsPath}/nvbg/NVBG_transform.xsl";
            //var ruleXmlFilename = $"{Application.streamingAssetsPath}/nvbg/rule_input_ChrKevin.xml";
            //var saliencyMapXmlFilename = $"{Application.streamingAssetsPath}/nvbg/saliency_map_init_kevin.xml";
            string parserModelDirectory = "unused";  //string parserModelDirectory = $"{Application.streamingAssetsPath}/nvbg/ParserModelEN";

            var streams = new Dictionary<string, Stream>();
            foreach (var stream in m_streams)
            {
                if (string.IsNullOrEmpty(stream.fileName) || stream.textAsset == null)
                    continue;

                Stream s = OpenStream(stream);
                if (s == Stream.Null)
                {
                    Debug.LogError($"NVBG/BLLIP: missing or unreadable asset for '{stream.fileName}'.");
                    continue;
                }

                streams.Add($"{parserModelDirectory}/{stream.fileName}", s);
            }

            string transformXsl = m_streams.Find(s => s.fileName == "NVBG_transform.xsl").textAsset.text;
            string ruleXml      = m_streams.Find(s => s.fileName == "rule_input_ChrKevin.xml").textAsset.text;
            string saliencyXml  = m_streams.Find(s => s.fileName == "saliency_map_init_kevin.xml").textAsset.text;

            return new NvbgInitData
            {
                TransformXsl    = transformXsl,
                RuleXml         = ruleXml,
                SaliencyMapXml  = saliencyXml,
                Streams         = streams
            };
        }

        // internal helper that constructs Nvbg (safe to run on worker thread)
        private Nvbg CreateCharacterInternal(string characterName, string idlePostureId, NvbgInitData initData)
        {
            string parserModelDirectory = "unused";  //string parserModelDirectory = $"{Application.streamingAssetsPath}/nvbg/ParserModelEN";

            var options = new NvbgOptions(
                characterId: characterName,
                transformXsl: initData.TransformXsl,
                transformXslResolver: new TransformXslResolver(parserModelDirectory, initData.Streams),
                ruleXml: initData.RuleXml,
                facialExpressionXml: null,
                idlePostureId: idlePostureId,
                parserModelDirectory: parserModelDirectory,
                streams: initData.Streams,
                parseTreeCache: null,
                saliencyMapXml: initData.SaliencyMapXml,
                storyPointId: StoryPointId,
                allBehavior: true,
                saliencyGlance: false,
                saliencyIdleGaze: false,
                speakerGaze: true,
                speakerGesture: true,
                listenerGaze: true,
                posRules: true
            );

            var character = new Nvbg(options, logger: null);
            return character;
        }

        private IEnumerator WaitForCharacterInit(string characterName, Task<Nvbg> initTask)
        {
            while (!initTask.IsCompleted)
                yield return null;

            m_characterInitTasks.Remove(characterName);

            if (initTask.IsFaulted)
            {
                Debug.LogError($"NVBG initialization failed for {characterName}: {initTask.Exception}");
                yield break;
            }

            m_characters[characterName] = initTask.Result;
            ProcessLoaded = true;
        }

        /// <summary>
        /// Coroutine that sends a request to the NVBG system and returns the XML result via callback.
        /// </summary>
        private IEnumerator Coroutine(string characterName, string text, INonverbalGeneratorSystem.NonverbalBehaviorResult resultCallback)
        {
             text = text.Replace("&", " and ");

            // Ensure character exists / is initialized
            if (!m_characters.TryGetValue(characterName, out Nvbg character))
            {
                // If we haven't started init yet, start it now
                if (!m_characterInitTasks.TryGetValue(characterName, out var initTask))
                {
                    CreateCharacter(characterName);
                    m_characterInitTasks.TryGetValue(characterName, out initTask);
                }

                // If still no task (e.g., WebGL synchronous path), re-check characters
                if (initTask == null)
                {
                    if (!m_characters.TryGetValue(characterName, out character))
                    {
                        Debug.LogError($"NVBG character '{characterName}' not created correctly.");
                        yield break;
                    }
                }
                else
                {
                    // Wait (non-blocking) until init is done
                    while (!initTask.IsCompleted)
                        yield return null;

                    if (initTask.IsFaulted)
                    {
                        Debug.LogError($"NVBG initialization failed for {characterName}: {initTask.Exception}");
                        yield break;
                    }

                    character = initTask.Result;
                    m_characters[characterName] = character;
                }
            }

            // character is ready
            var vrExpressXml = CreateVRExpressXml(characterName, text);

            Debug.Log($"NVBG Request - text: {text}");

            var request = new NvbgRequest(
                kind: NvbgRequestKind.None,
                messageId: "1488584035542-92-1",  //from ExternalProcessNVBG.cs
                sourceId: characterName,  //CharacterId,
                targetId: "all",
                xml: vrExpressXml
            );
            var responseTask = character.ProcessAsync(request);

            float startTime = Time.time;
            float timeOut = 18; // in seconds
            while (!responseTask.IsCompleted && Time.time - startTime < timeOut)
                yield return new WaitForEndOfFrame();

            if (!responseTask.IsCompleted)
            {
                Debug.LogError("NVBG response timed out.");
                yield break;
            }

            var response = responseTask.Result;
            var xmlText = response.BehaviorMarkupLanguage.InnerXml;
            xmlText = Regex.Replace(xmlText, @"<\?.*?\?>", "");
            xmlText = xmlText.Replace("\r\n", "");
            xmlText = xmlText.Replace("\n", "");
            xmlText = xmlText.Replace("'", "&apos;");

            Debug.Log($"NVBG Response - xmlText (Truncated): {xmlText[..Mathf.Min(500, xmlText.Length)]}");
            Debug.Log(BuildNvbgUtteranceTimeline(xmlText));
            Debug.Log(BuildNvbgScheduleSummary(xmlText));

            resultCallback(xmlText);
        }

        /// <summary>
        /// Creates a VR Express XML message to send to NVBG for nonverbal behavior generation.
        /// </summary>
        /// <param name="characterName">Character ID to assign to the message.</param>
        /// <param name="text">Speech text to embed in the message.</param>
        /// <returns>Formatted XML string.</returns>
        private static string CreateVRExpressXml(string characterName, string text)
        {
            const string refId = "unused";

            const string messageSkeleton =
                @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no"" ?>" +
                @"<act>" +
                    @"<participant id=""{0}"" role=""actor"" />" +
                    @"<fml>" +
                        @"<turn start=""take"" end=""give"" />" +
                        @"<affect type=""neutral"" target=""addressee""></affect>" +
                        @"<culture type=""neutral""></culture>" +
                        @"<personality type=""neutral""></personality>" +
                    @"</fml>" +
                    @"<bml>" +
                        @"<speech id=""sp1"" ref=""{1}"" type=""application/ssml+xml"">{2}</speech>" +
                    @"</bml>" +
                @"</act>";
            string message = string.Format(messageSkeleton, characterName, refId, text);

            return message;
        }
        #endregion

        #region XmlResolver
        /// <summary>
        /// Resolves XSL transformation references from in-memory TextAssets instead of disk I/O.
        /// </summary>
        private sealed class TransformXslResolver : XmlResolver
        {
            private static readonly string CurrentDirectory = Directory.GetCurrentDirectory();
            private readonly string _baseDirectory;
            Dictionary<string, Stream> _streams;

            //public TransformXslResolver(string transfromXslFilename)

            /// <summary>
            /// Initializes the resolver with a base path and dictionary of filename-stream mappings.
            /// </summary>
            public TransformXslResolver(string baseDirectory, Dictionary<string, Stream> streams)
            {
                _baseDirectory = baseDirectory;  //_baseDirectory = Path.GetDirectoryName(transfromXslFilename) ?? throw new ArgumentException("Invalid transfrom XSL filename", nameof(transfromXslFilename));
                _streams = streams;
            }

            /// <inheritdoc/>
            public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
            {
                //var filename = Path.GetRelativePath(CurrentDirectory, absoluteUri.AbsolutePath);
                //var bytes = File.ReadAllBytes(Path.Combine(_baseDirectory, filename));
                //var result = new MemoryStream(bytes, writable: false);
                //return result;
                string filename = Path.GetFileName(absoluteUri.LocalPath);
                string streamName = $"{_baseDirectory}/{filename}";
                return _streams[streamName];
            }
        }
        #endregion

        private sealed class ZipEntryOwnedStream : Stream
        {
            private readonly Stream m_inner;
            private readonly ZipArchive m_archive;

            public ZipEntryOwnedStream(Stream inner, ZipArchive archive)
            {
                m_inner = inner ?? throw new ArgumentNullException(nameof(inner));
                m_archive = archive ?? throw new ArgumentNullException(nameof(archive));
            }

            public override bool CanRead => m_inner.CanRead;
            public override bool CanSeek => m_inner.CanSeek;
            public override bool CanWrite => false;
            public override long Length => m_inner.Length;

            public override long Position
            {
                get => m_inner.Position;
                set => m_inner.Position = value;
            }

            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => m_inner.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => m_inner.Seek(offset, origin);
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try { m_inner.Dispose(); } catch { }
                    try { m_archive.Dispose(); } catch { }
                }
                base.Dispose(disposing);
            }
        }

        private static Stream OpenStream(StreamInfo info)
        {
            if (info == null || info.textAsset == null)
                return Stream.Null;

            byte[] bytes = info.textAsset.bytes;
            if (bytes == null || bytes.Length == 0)
                return Stream.Null;

            if (!info.isZip)
            {
                // IMPORTANT: Do not go through textAsset.text; it forces a giant string allocation.
                return new MemoryStream(bytes, writable: false);
            }

            string entryName = info.fileName;

            // ZipArchive needs an owning stream. We keep it alive via a wrapper stream.
            var zipBytesStream = new MemoryStream(bytes, writable: false);
            ZipArchive archive = null;
            Stream entryStream = null;

            try
            {
                archive = new ZipArchive(zipBytesStream, ZipArchiveMode.Read, leaveOpen: false);
                ZipArchiveEntry entry = archive.GetEntry(entryName);

                if (entry == null)
                {
                    // Some zips store entries with folder prefixes; you can loosen this later if needed.
                    Debug.LogError($"NVBG/BLLIP: zip for '{info.fileName}' does not contain entry '{entryName}'.");
                    archive.Dispose();
                    return Stream.Null;
                }

                entryStream = entry.Open();
                return new ZipEntryOwnedStream(entryStream, archive);
            }
            catch (Exception e)
            {
                entryStream?.Dispose();
                archive?.Dispose();
                Debug.LogError($"NVBG/BLLIP: failed to open zip stream for '{info.fileName}'. {e}");
                return Stream.Null;
            }
        }

        private static string BuildNvbgUtteranceTimeline(string xmlText, string speechId = "sp1")
        {
            if (string.IsNullOrEmpty(xmlText))
                return "NVBG Utterance Timeline: <empty xmlText>";

            // xmlText is InnerXml, so wrap it.
            string wrapped = $"<root>{xmlText}</root>";

            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(wrapped);
            }
            catch (Exception e)
            {
                return $"NVBG Utterance Timeline: failed to parse xmlText. {e.GetType().Name}: {e.Message}";
            }

            // Prefer the requested speech id, otherwise fall back to first <speech>
            XmlElement speech =
                doc.SelectSingleNode($"//speech[@id='{speechId}']") as XmlElement ??
                doc.SelectSingleNode("//speech") as XmlElement;

            if (speech == null)
                return "NVBG Utterance Timeline: no <speech> node found.";

            var sb = new StringBuilder(1024);
            sb.Append("NVBG Utterance Timeline (speech marks):\n");

            foreach (XmlNode child in speech.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Element)
                {
                    XmlElement e = (XmlElement)child;

                    // In your sample: <mark name="T0" />
                    if (e.Name == "mark")
                    {
                        string name = e.GetAttribute("name"); // e.g. "T0"
                        if (!string.IsNullOrEmpty(name))
                            sb.Append($"[{name}] ");
                    }

                    continue;
                }

                if (child.NodeType == XmlNodeType.Text || child.NodeType == XmlNodeType.CDATA)
                {
                    // XmlDocument already decodes &apos; etc for us in Value.
                    string text = child.Value;

                    if (!string.IsNullOrEmpty(text))
                    {
                        // Keep it readable but don't destroy punctuation.
                        // Collapse newlines/tabs; preserve ordinary spaces.
                        text = text.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

                        sb.Append(text);
                    }
                }
            }

            // Normalize any crazy spacing a bit (optional, but tends to help)
            string result = sb.ToString();
            result = NormalizeSpaces(result);

            return result.TrimEnd();
        }

        private static string NormalizeSpaces(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            // Collapse repeated spaces, but keep single spaces.
            var sb = new StringBuilder(s.Length);
            bool lastWasSpace = false;

            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool isSpace = (c == ' ');

                if (isSpace)
                {
                    if (!lastWasSpace)
                        sb.Append(c);

                    lastWasSpace = true;
                }
                else
                {
                    sb.Append(c);
                    lastWasSpace = false;
                }
            }

            return sb.ToString();
        }

        private static string BuildNvbgScheduleSummary(string xmlText, int maxBehaviorsToPrint = 64)
        {
            if (string.IsNullOrEmpty(xmlText))
                return "NVBG Schedule: <empty xmlText>";

            // xmlText is InnerXml, so it may contain multiple top-level nodes.
            // Wrap it so XmlDocument can load it reliably.
            string wrapped = "<root>" + xmlText + "</root>";

            var doc = new XmlDocument();
            try
            {
                doc.LoadXml(wrapped);
            }
            catch (Exception e)
            {
                return $"NVBG Schedule: failed to parse xmlText. {e.GetType().Name}: {e.Message}";
            }

            var root = doc.DocumentElement;
            if (root == null)
                return "NVBG Schedule: <no root>";

            // Flatten behaviors: consider direct children under <bml> if present, otherwise under <root>.
            var container = root.SelectSingleNode("//bml") ?? root;

            var lines = new List<string>(64);
            int behaviorCount = 0;

            foreach (XmlNode node in container.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element)
                    continue;

                string name = node.Name;

                // Skip common non-behavior / noise.
                if (name == "speech" || name == "mark" || name == "text" || name == "sbm:event" || name == "event")
                    continue;

                string line = SummarizeBehaviorNode((XmlElement)node);
                if (!string.IsNullOrEmpty(line))
                {
                    lines.Add($"  {line}");
                    behaviorCount++;

                    if (behaviorCount >= maxBehaviorsToPrint)
                    {
                        lines.Add($"  ... (truncated after {maxBehaviorsToPrint} behaviors)");
                        break;
                    }
                }
            }

            if (behaviorCount == 0)
                return "NVBG Schedule: no behaviors found (after filtering speech/events).";

            // Optional: stable ordering can be helpful. Many BML streams are already in time-ish order,
            // but if yours isn't, you can sort by ready/start token; I left it as-is for minimal changes.

            var sb = new StringBuilder(2048);
            sb.AppendLine($"NVBG Schedule - behaviors={behaviorCount}");
            for (int i = 0; i < lines.Count; i++)
                sb.AppendLine(lines[i]);

            return sb.ToString();
        }

        private static string SummarizeBehaviorNode(XmlElement e)
        {
            // Common timing attribute names (not all tags use these).
            string start    = Attr(e, "start");
            string ready    = Attr(e, "ready");
            string stroke0  = Attr(e, "strokeStart");
            string stroke   = Attr(e, "stroke");
            string emphasis = Attr(e, "emphasis");
            string relax    = Attr(e, "relax");
            string end      = Attr(e, "end");

            string timing = BuildTiming(start, ready, stroke0, stroke, emphasis, relax, end);

            // Common meta
            string priority = Attr(e, "priority");

            switch (e.Name)
            {
                case "gaze":
                {
                    string participant = Attr(e, "participant");
                    string target      = Attr(e, "target");
                    string direction   = Attr(e, "direction");
                    string angle       = Attr(e, "angle");
                    string jointRange  = Attr(e, "joint-range");
                    return $"[gaze] {Kvp("participant", participant)}{Kvp("target", target)}{Kvp("direction", direction)}{Kvp("angle", angle)}{Kvp("jointRange", jointRange)}{Kvp("priority", priority)}{timing}".TrimEnd();
                }

                case "head":
                {
                    string type    = Attr(e, "type");
                    string amount  = Attr(e, "amount");
                    string repeats = Attr(e, "repeats");
                    return $"[head] {Kvp("type", type)}{Kvp("amount", amount)}{Kvp("repeats", repeats)}{Kvp("priority", priority)}{timing}".TrimEnd();
                }

                case "animation":
                {
                    string name  = Attr(e, "name");
                    string layer = Attr(e, "layer");
                    string speed = Attr(e, "speed");
                    return $"[animation] {Kvp("name", name)}{Kvp("layer", layer)}{Kvp("speed", speed)}{Kvp("priority", priority)}{timing}".TrimEnd();
                }

                default:
                {
                    // Generic fallback
                    string id     = Attr(e, "id");
                    string type   = Attr(e, "type");
                    string target = Attr(e, "target");
                    return $"[{e.Name}] {Kvp("id", id)}{Kvp("type", type)}{Kvp("target", target)}{Kvp("priority", priority)}{timing}".TrimEnd();
                }
            }
        }

        private static string BuildTiming(string start, string ready, string strokeStart, string stroke, string emphasis, string relax, string end)
        {
            var sb = new StringBuilder(128);

            // Use leading space so callers can just append.
            AppendTime(sb, "start", start);
            AppendTime(sb, "ready", ready);
            AppendTime(sb, "strokeStart", strokeStart);
            AppendTime(sb, "stroke", stroke);
            AppendTime(sb, "emphasis", emphasis);
            AppendTime(sb, "relax", relax);
            AppendTime(sb, "end", end);

            return sb.Length > 0 ? sb.ToString() : "";
        }

        private static void AppendTime(StringBuilder sb, string name, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (sb.Length == 0)
                sb.Append(" ");

            sb.Append(name);
            sb.Append("=");
            sb.Append(value);
        }

        private static string Attr(XmlElement e, string name) => e.HasAttribute(name) ? e.GetAttribute(name) : "";
        private static string Kvp(string key, string value) => string.IsNullOrEmpty(value) ? "" : $"{key}={value} ";
    }
}
