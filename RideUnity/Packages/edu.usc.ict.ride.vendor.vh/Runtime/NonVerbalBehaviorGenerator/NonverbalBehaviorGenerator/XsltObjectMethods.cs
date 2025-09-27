#nullable enable

using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Xml;
using System.Linq;
using NonverbalBehaviorGenerator.Models;

namespace NonverbalBehaviorGenerator
{
    /// <summary>
    /// Original Comment: XSL extension Object that gets invoked from the XSL sheet
    /// </summary>
    /// <remarks>Refactor of XsltObjectMethods</remarks>
    internal sealed class XsltObjectMethods
    {
        private readonly ILogger? _logger;
        private readonly Random _random = new Random();
        private readonly string _characterId;
        private readonly XmlDocument _behaviorFile;

        public XsltObjectMethods(ILogger? logger, string characterId, XmlDocument behaviorFile)
        {
            _logger = logger;
            _characterId = characterId;
            _behaviorFile = behaviorFile;
        }

        /// <summary>
        /// Original Comment: method is invoked form the .xsl file in data folder. This selects an animation from the xml rule file and inserts it into the final BML
        /// </summary>
        public string xslGetAnimation(string _keyWord, string _posture, string _participant)
        {
            Trace.Assert(_participant == _characterId, "NVBG refactor simplification assumption broken");
            try
            {
                using var rules = _behaviorFile.GetElementsByTagName("rule");
                var rule = rules
                    .Cast<XmlNode>()
                    .FirstOrDefault(r => r.Attributes["keyword"].Value == _keyWord);
                if (rule != null)
                {
                    if (_keyWord == "idle_gaze")
                    {
                        using var patterns = rule.ChildNodes;
                        if (patterns.Count > 0)
                        {
                            int idx = _random.Next(0, patterns.Count);
                            string gazeOffset = patterns[idx].InnerText;
                            return gazeOffset;
                        }
                    }
                    using var postures = ((XmlElement)rule).GetElementsByTagName("posture");
                    var posture = postures
                        .Cast<XmlNode>()
                        .FirstOrDefault(p => p.Attributes["name"].Value == _posture);
                    if (posture != null)
                    {
                        using var clips = posture.ChildNodes;
                        if (clips.Count > 0)
                        {
                            int idx = _random.Next(0, clips.Count);
                            string animationName = clips[idx].InnerText;
                            return animationName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "XSLT object method error");
            }
            return "none";
        }
    }
}
