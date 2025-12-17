using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            public TextAsset textAsset;
            public override string ToString() => fileName;

            public StreamInfo(string _filename) { fileName = _filename; textAsset = null; }
        }

        public List<StreamInfo> m_streams = new()
        {
            new("NVBG_behavior_description.xsl"),
            new("NVBG_rules.xsl"),
            new("NVBG_transform.xsl"),
            new("rule_input_ChrKevin.xml"),
            new("saliency_map_init_kevin.xml"),
            // ParserModelEN folder
            new("endings.txt"),
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
            new("h.g"),
            new("h.lambdas"),
            new("headInfo.txt"),
            new("l.g"),
            new("l.lambdas"),
            new("lm.g"),
            new("lm.lambdas"),
            new("m.g"),
            new("m.lambdas"),
            new("nttCounts.txt"),
            new("pSgT.txt"),
            new("pUgT.txt"),
            new("r.g"),
            new("r.lambdas"),
            new("rm.g"),
            new("rm.lambdas"),
            new("ru.g"),
            new("ru.lambdas"),
            new("terms.txt"),
            new("tt.g"),
            new("tt.lambdas"),
            new("u.g"),
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
                if (!string.IsNullOrEmpty(stream.fileName) && stream.textAsset != null)
                    streams.Add($"{parserModelDirectory}/{stream.fileName}", new MemoryStream(Encoding.UTF8.GetBytes(stream.textAsset.text)));
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
    }
}
