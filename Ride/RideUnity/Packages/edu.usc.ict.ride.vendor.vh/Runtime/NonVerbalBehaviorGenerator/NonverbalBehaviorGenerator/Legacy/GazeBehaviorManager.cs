#nullable disable

using Microsoft.Extensions.Logging;
using NonverbalBehaviorGenerator.LegacyInterop;
using NonverbalBehaviorGenerator.Models;
using System;
using System.Threading.Tasks;
using System.Xml;

namespace NonverbalBehaviorGenerator.Legacy
{
    internal sealed class GazeBehaviorManager
    {
        private readonly ILogger _logger;

        public GazeBehaviorManager(ILogger logger)
        {
            _logger = logger;
        }

        public async Task ProcessGazeMessageAsync(XmlDocument _inputDoc, IContext context, LegacyRequest _currentMessage)
        {
            try
            {
                XmlNode gazeTag = _inputDoc.GetElementsByTagName("gaze")[0];
                string reason = gazeTag.InnerText;
                //string type = gazeTag.Attributes["type"].Value;
                string target = gazeTag.Attributes["target"].Value;

                await context.GazeInfo.SetGazeTargetAsync(target);
                //var ruleXml = await context.GetBehaviorRuleXmlAsync();
                //var ruleDocument = new XmlDocument();
                //ruleDocument.LoadXml(ruleXml);
                //XmlNodeList ruleNodes = ruleDocument.GetElementsByTagName("gazereason");
                XmlNode newGazeTag = _inputDoc.CreateElement("gazereason");
                XMLHelperMethods.AttachAttributeToNode(_inputDoc, newGazeTag, "participant", await context.AgentInfo.GetCharacterIdAsync());
                XMLHelperMethods.AttachAttributeToNode(_inputDoc, newGazeTag, "type", reason);
                XMLHelperMethods.AttachAttributeToNode(_inputDoc, newGazeTag, "priority", "4");
                XMLHelperMethods.AttachAttributeToNode(_inputDoc, newGazeTag, "prev_target", await context.GazeInfo.GetPreviousTargetAsync());
                XMLHelperMethods.AttachAttributeToNode(_inputDoc, newGazeTag, "target", target);
                _inputDoc.GetElementsByTagName("bml")[0].AppendChild(newGazeTag);
                await context.GazeInfo.SetPreviousTargetAsync(target);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error while processing gaze message");
            }
        }

    }
}
