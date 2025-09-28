using System.Threading.Tasks;
using System.Xml;

namespace NonverbalBehaviorGenerator.Models
{
    public static class ContextExtensions {

        ///<remarks>Refactor of NVBGCharacter.m_saliencyMapInitialized</remarks>
        public static async Task<bool> GetHasSaliencyMapXmlAsync(this IContext context) {
            var saliencyMapXml = await context.GetSaliencyMapXmlAsync();
            var result = saliencyMapXml is not null;
            return result;
        }

        public static async Task<bool> GetHasFacialExpressionXmlAsync(this IContext context) { 
            var facialExpressionXml = await context.GetFacialExpressionXmlAsync();
            var result = facialExpressionXml is not null;
            return result;
        }

        public static async Task<XmlDocument> GetBehaviorXmlDocumentAsync(this IContext context) { 
            var ruleXml = await context.GetBehaviorRuleXmlAsync();
            var ruleDocument = new XmlDocument();
            ruleDocument.LoadXml(ruleXml);
            return ruleDocument;
        }
    }
}
