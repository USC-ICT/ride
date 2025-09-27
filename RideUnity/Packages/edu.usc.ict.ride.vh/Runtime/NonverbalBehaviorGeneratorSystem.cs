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
    public sealed class NonverbalBehaviorGeneratorSystem : RideSystemMonoBehaviour, INonverbalGeneratorSystem
    {
        private const string StoryPointId = "toolkitsession";

        /// <summary>
        /// Struct to represent an input stream for NVBG, containing the filename and the associated TextAsset.
        /// </summary>
        [Serializable] public class StreamInfo { public string fileName; public TextAsset textAsset; }
        public List<StreamInfo> m_streams = new()
        {
            new StreamInfo() { fileName = "NVBG_behavior_description.xsl", textAsset = null },
            new StreamInfo() { fileName = "NVBG_rules.xsl", textAsset = null },
            new StreamInfo() { fileName = "NVBG_transform.xsl", textAsset = null },
            new StreamInfo() { fileName = "rule_input_ChrKevin.xml", textAsset = null },
            new StreamInfo() { fileName = "saliency_map_init_kevin.xml", textAsset = null },
            // ParserModelEN folder
            new StreamInfo() { fileName = "endings.txt", textAsset = null },
            new StreamInfo() { fileName = "featInfo.h", textAsset = null },
            new StreamInfo() { fileName = "featInfo.l", textAsset = null },
            new StreamInfo() { fileName = "featInfo.lm", textAsset = null },
            new StreamInfo() { fileName = "featInfo.m", textAsset = null },
            new StreamInfo() { fileName = "featInfo.r", textAsset = null },
            new StreamInfo() { fileName = "featInfo.rm", textAsset = null },
            new StreamInfo() { fileName = "featInfo.ru", textAsset = null },
            new StreamInfo() { fileName = "featInfo.s", textAsset = null },
            new StreamInfo() { fileName = "featInfo.t", textAsset = null },
            new StreamInfo() { fileName = "featInfo.tt", textAsset = null },
            new StreamInfo() { fileName = "featInfo.u", textAsset = null },
            new StreamInfo() { fileName = "h.g", textAsset = null },
            new StreamInfo() { fileName = "h.lambdas", textAsset = null },
            new StreamInfo() { fileName = "headInfo.txt", textAsset = null },
            new StreamInfo() { fileName = "l.g", textAsset = null },
            new StreamInfo() { fileName = "l.lambdas", textAsset = null },
            new StreamInfo() { fileName = "lm.g", textAsset = null },
            new StreamInfo() { fileName = "lm.lambdas", textAsset = null },
            new StreamInfo() { fileName = "m.g", textAsset = null },
            new StreamInfo() { fileName = "m.lambdas", textAsset = null },
            new StreamInfo() { fileName = "nttCounts.txt", textAsset = null },
            new StreamInfo() { fileName = "pSgT.txt", textAsset = null },
            new StreamInfo() { fileName = "pUgT.txt", textAsset = null },
            new StreamInfo() { fileName = "r.g", textAsset = null },
            new StreamInfo() { fileName = "r.lambdas", textAsset = null },
            new StreamInfo() { fileName = "rm.g", textAsset = null },
            new StreamInfo() { fileName = "rm.lambdas", textAsset = null },
            new StreamInfo() { fileName = "ru.g", textAsset = null },
            new StreamInfo() { fileName = "ru.lambdas", textAsset = null },
            new StreamInfo() { fileName = "terms.txt", textAsset = null },
            new StreamInfo() { fileName = "tt.g", textAsset = null },
            new StreamInfo() { fileName = "tt.lambdas", textAsset = null },
            new StreamInfo() { fileName = "u.g", textAsset = null },
            new StreamInfo() { fileName = "u.lambdas", textAsset = null },
            new StreamInfo() { fileName = "unitRules.txt", textAsset = null },
        };

        public string CharacterId = "Kevin";
        public string IdlePostureId = "ChrGenericMleAdult@IdleStandingUpright01";
        public bool m_launchOnStartup = true;

        #region IRideSystem

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();
            if (m_launchOnStartup)
            {
                StartProcess();
            }
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
        public void StartProcess()
        {
            StartProcess(CharacterId);
        }

        /// <summary>
        /// Starts the NVBG process for a specific character.
        /// </summary>
        /// <param name="characterName">Name of the character to generate in NVBG.</param>
        public void StartProcess(string characterName)
        {
            if (characters.ContainsKey(characterName)) { return; }
            CreateCharacter(characterName);

            //Timer not needed since Saliency Idle Gaze is not enabled

            ProcessLoaded = true;
        }

        /// <summary>
        /// Creates a new character with configured options.
        /// </summary>
        /// <param name="characterName">The name of the character to create.</param>
        void CreateCharacter(string characterName)
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

            var options = new NvbgOptions(
                characterId: characterName,//CharacterId,
                transformXsl: m_streams.Find(s => { return s.fileName == $"NVBG_transform.xsl"; }).textAsset.text,  //transformXsl: File.ReadAllText(transformXslFilename),
                transformXslResolver: new TransformXslResolver(parserModelDirectory, streams),  //transformXslResolver: new TransformXslResolver(transformXslFilename),
                ruleXml: m_streams.Find(s => { return s.fileName == $"rule_input_ChrKevin.xml"; }).textAsset.text,  //ruleXml: File.ReadAllText(ruleXmlFilename),
                facialExpressionXml: null,
                idlePostureId: IdlePostureId,
                parserModelDirectory: parserModelDirectory,
                streams: streams,
                parseTreeCache: null,
                saliencyMapXml: m_streams.Find(s => { return s.fileName == $"saliency_map_init_kevin.xml"; }).textAsset.text,  //saliencyMapXml: File.ReadAllText(saliencyMapXmlFilename),
                storyPointId: StoryPointId,
                allBehavior: true,
                saliencyGlance: false,//not implemented at the time of writing
                saliencyIdleGaze: false,//same as ChrKevin.ini
                speakerGaze: true,
                speakerGesture: true,
                listenerGaze: true,
                posRules: true
            );
            var character = new Nvbg(options, logger: null);
            characters[characterName/*CharacterId*/] = character;
        }

        /// <summary>
        /// Stops the NVBG process and disposes all character instances.
        /// </summary>
        public void StopProcess()
        {
            ProcessLoaded = false;
            foreach (var character in characters.Values)
            {
                character.Dispose();
            }
            characters.Clear();
        }
        #endregion

        #region INonverbalGenerator

        private Dictionary<string, Nvbg> characters = new Dictionary<string, Nvbg>();

        /// <inheritdoc/>
        public void GetNonverbalBehavior(string characterName, string text, INonverbalGeneratorSystem.NonverbalBehaviorResult resultCallback)
        {
            text = text.Replace("&", " and ");
            StartCoroutine(Coroutine(characterName, text, resultCallback));
        }

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
            var character = characters[characterName];
            return await character.GetPostureIdAsync();
        }

        /// <summary>
        /// Coroutine that sends a request to the NVBG system and returns the XML result via callback.
        /// </summary>
        private IEnumerator Coroutine(string characterName, string text, INonverbalGeneratorSystem.NonverbalBehaviorResult resultCallback)
        {
            Debug.Assert(characters.ContainsKey(characterName));
            var vrExpressXml = CreateVRExpressXml(characterName, text);
            Debug.LogFormat("NVBG Request - text: {0}", text);
            var request = new NvbgRequest(
                kind: NvbgRequestKind.None,
                messageId: "1488584035542-92-1",//from ExternalProcessNVBG.cs
                sourceId: characterName,//CharacterId,
                targetId: "all",
                xml: vrExpressXml
            );
            var character = characters[characterName];
            var responseTask = character.ProcessAsync(request);

            float startTime = Time.time;
            float timeOut = 18; // in seconds
            while (!responseTask.IsCompleted && Time.time - startTime < timeOut)
            {
                yield return new WaitForEndOfFrame();
            }
            var response = responseTask.Result;
            var xmlText = response.BehaviorMarkupLanguage.InnerXml;
            xmlText = Regex.Replace(xmlText, @"<\?.*?\?>", "");
            xmlText = xmlText.Replace("\r\n", "");
            xmlText = xmlText.Replace("\n", "");
            xmlText = xmlText.Replace("'", "&apos;");
            Debug.LogFormat("NVBG Response - xmlText (Truncated): {0}", xmlText[..Mathf.Min(500, xmlText.Length)]);
            //Debug.LogFormat("NVBG Response - xmlText: {0}", xmlText);
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
