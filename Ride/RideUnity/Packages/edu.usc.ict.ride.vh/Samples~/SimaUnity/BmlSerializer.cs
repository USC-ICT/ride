using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

/// <summary>
/// Provides helper methods for saving and serializing BML XML documents.
/// Supports saving to disk, retrieving XML as a string (with or without declaration),
/// and combining save and retrieval operations.
/// </summary>
public static class BmlSerializer
{
    /// <summary>
    /// Saves the given <see cref="XDocument"/> to a file at the specified path.
    /// </summary>
    /// <param name="bmlDoc">The BML XML document to save.</param>
    /// <param name="filePath">The destination file path where the XML will be written.</param>
    public static void SaveToFile(XDocument bmlDoc, string filePath)
    {
        bmlDoc.Save(filePath); 
    }

    /// <summary>
    /// Returns the XML document as a string.
    /// </summary>
    /// <param name="bmlDoc">The BML XML document to convert to string.</param>
    /// <param name="includeDeclaration">If true, includes the XML declaration at the top of the string.</param>
    /// <returns>
    /// The serialized XML as a string. If <paramref name="includeDeclaration"/> is false,
    /// only the root element and its content are returned without declaration.
    /// </returns>
    public static string GetString(XDocument bmlDoc, bool includeDeclaration = false)
    {
        return includeDeclaration
            ? bmlDoc.ToString()
            : bmlDoc.Root?.ToString(SaveOptions.DisableFormatting) ?? "";
    }

    /// <summary>
    /// Saves the BML XML document to a file and returns its content as a string without the XML declaration.
    /// </summary>
    /// <param name="bmlDoc">The BML XML document to save.</param>
    /// <param name="filePath">The destination file path.</param>
    /// <returns>The serialized XML string without declaration.</returns>
    public static string SaveToFileAndGetInnerXml(XDocument bmlDoc, string filePath)
    {
        SaveToFile(bmlDoc, filePath);
        return GetString(bmlDoc, includeDeclaration: false);
    }

    /// <summary>
    /// Returns the XML string with an explicit declaration (`<?xml version="1.0" encoding="utf-8"?>`).
    /// </summary>
    /// <param name="doc">The XML document to serialize.</param>
    /// <returns>
    /// The serialized XML string, formatted with indentation and including the XML declaration.
    /// </returns>
    public static string GetXmlStringWithDeclaration(XDocument doc)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false // :white_check_mark: this ensures the declaration appears
        };
        using var stringWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(stringWriter, settings);
        doc.Save(xmlWriter);
        return stringWriter.ToString();
    }
}

