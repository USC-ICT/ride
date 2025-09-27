using System.Xml;

namespace NonverbalBehaviorGenerator
{
    internal static class XmlExtensions
    {

        public static bool ContainsAnyElementNamedAs(this XmlDocument document, string elementName)
        {
            using var list = document.GetElementsByTagName(elementName);
            return list.Count > 0;
        }

        public static void AttachAttribute(this XmlNode node, string attributeName, string attributeValue)
        {
            _ = node.Attributes.RemoveNamedItem(attributeName);
            var attribute = node.OwnerDocument.CreateAttribute(attributeName);
            attribute.Value = attributeValue;
            node.Attributes.Append(attribute);
        }
    }
}
