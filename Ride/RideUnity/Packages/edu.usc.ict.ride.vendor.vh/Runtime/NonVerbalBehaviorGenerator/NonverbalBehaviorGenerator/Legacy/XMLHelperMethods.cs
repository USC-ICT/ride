#nullable disable
using System.Xml;

namespace NonverbalBehaviorGenerator.Legacy
{
    internal static class XMLHelperMethods
    {
        public static void AttachAttributeToNode(XmlDocument _inputDoc, XmlNode _node, string _attributeName, string _attributeValue)
        {
            _node.Attributes.RemoveNamedItem(_attributeName);
            XmlAttribute attribute = _inputDoc.CreateAttribute(_attributeName);
            attribute.Value = _attributeValue;
            _node.Attributes.Append(attribute);
        }
    }
}
