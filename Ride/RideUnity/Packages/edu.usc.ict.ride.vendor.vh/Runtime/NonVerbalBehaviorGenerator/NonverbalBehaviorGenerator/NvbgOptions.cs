#nullable enable

using NonverbalBehaviorGenerator.Models;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;

namespace NonverbalBehaviorGenerator
{
    /// <summary>
    /// Holding the effective options of current NVBG instance
    /// </summary>
    public sealed class NvbgOptions : INvbgOptions, IContextFactory
    {
        /// <summary>
        /// This is the absolute path of default English model.
        /// </summary>
        public static string DefaultParserModelDirectory => Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(NvbgOptions)).Location), 
            "Resources", 
            "ParserModelEN"
        );

        public string CharacterId { get; }
        public string TransformXsl { get; }
        public XmlResolver? TransformXslResolver { get; }
        public string RuleXml { get; }
        public string? FacialExpressionXml { get; }
        public string IdlePostureId { get; }
        public string ParserModelDirectory { get; }
        public Dictionary<string, Stream> Streams { get; }
        public IDictionary<string, string>? ParseTreeCache { get; }
        public string SaliencyMapXml { get; }
        public string StoryPointId { get; }
        public bool AllBehavior { get; }
        public bool SaliencyGlance { get; }
        public bool SaliencyIdleGaze { get; }
        public bool SpeakerGaze { get; }
        public bool SpeakerGesture { get; }
        public bool ListenerGaze { get; }
        public bool PosRules { get; }

        public NvbgOptions(
            string characterId, 
            string transformXsl,
            XmlResolver? transformXslResolver,
            string ruleXml, 
            string? facialExpressionXml, 
            string idlePostureId,
            string parserModelDirectory,
            Dictionary<string, Stream> streams,
            IDictionary<string, string>? parseTreeCache,
            string saliencyMapXml,
            string storyPointId,
            bool allBehavior,
            bool saliencyGlance,
            bool saliencyIdleGaze,
            bool speakerGaze,
            bool speakerGesture,
            bool listenerGaze,
            bool posRules
        )
        {
            CharacterId = characterId;
            TransformXsl = transformXsl;
            TransformXslResolver = transformXslResolver;
            RuleXml = ruleXml;
            FacialExpressionXml = facialExpressionXml;
            IdlePostureId = idlePostureId;
            ParserModelDirectory = parserModelDirectory;
            Streams = streams;
            ParseTreeCache = parseTreeCache;
            SaliencyMapXml = saliencyMapXml;
            StoryPointId = storyPointId;
            AllBehavior = allBehavior;
            SaliencyGlance = saliencyGlance;
            SaliencyIdleGaze = saliencyIdleGaze;
            SpeakerGaze = speakerGaze;
            SpeakerGesture = speakerGesture;
            ListenerGaze = listenerGaze;
            PosRules = posRules;
        }

        public IContextFactory ContextFactory => this;

        #region IContextFactory
        public Task<IContext> CreateContextAsync()
        {
            var result = new InMemoryContext(CharacterId, RuleXml, FacialExpressionXml, IdlePostureId, StoryPointId, ParseTreeCache, AllBehavior, SaliencyGlance, SaliencyIdleGaze, SpeakerGaze, SpeakerGesture, ListenerGaze, PosRules);
            return Task.FromResult<IContext>(result);
        } 
        #endregion
    }
}

