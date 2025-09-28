using System;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;

namespace VHAssets
{
public static class SpeechUtils
{
    public static string GetRemoteSpeechCommand(string charName, string voice, string text, string outputPath)
    {
        return $@"RemoteSpeechCmd speak {charName} 1 {voice} {outputPath} " +
               $@"   <?xml version=""1.0"" encoding=""UTF-8""?>" +
               $@"      <speech id=""sp1"" ref=""{Path.GetFileNameWithoutExtension(outputPath)}"" type=""application/ssml+xml"">{CreateMarks(text)}</speech>";
    }

    public static string CreateMarks(string text)
    {
        string[] split = text.Split(' ');

        if (split == null || split.Length == 0)
        {
            return text;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append(string.Format("<mark name=\"T0\" />{0}", split[0]));
        int markCounter = 1;
        for (int i = 1; i < split.Length; i++)
        {
            builder.Append(string.Format("<mark name=\"T{0}\" /><mark name=\"T{1}\" />{2}", markCounter, markCounter + 1, split[i]));
            markCounter += 2;
        }

        builder.Append(string.Format("<mark name=\"T{0}\" />", markCounter/*split.Length % 2 == 0 ? split.Length + 1 : split.Length*/));
        return builder.ToString();
    }

    public static string RemoveSpecialCharacters(string str)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in str)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c == ' '))
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    public static string [] GetvrSpeechSequence(string turnId, string speakerId, string text)
    {
        return new string []
        {
            $"vrSpeech start {turnId} {speakerId}",
            $"vrSpeech finished-speaking {turnId}",
            $"vrSpeech interp {turnId} 1 1.0 normal {text}",
            $"vrSpeech emotion {turnId} 1 1.0 normal neutral",
            $"vrSpeech tone {turnId} 1 1.0 normal flat",
            $"vrSpeech asr-complete {turnId}",
        };
    }

    public static void ParsevrExpress(string vrExpress, out string refid, out string text)
    {
        StringReader xml = null;
        XmlTextReader reader = null;
        refid = "";
        text = "";

#if !UNITY_WSA
        try
        {
            xml = new StringReader(vrExpress);
            reader = new XmlTextReader(xml);
            ParsevrExpress(reader, out refid, out text);
        }
        catch (Exception e)
        {
            //succeeded = false;
            Debug.LogError(string.Format("Failed when loading. Error: {0} {1}. couldn't load string {2}", e.Message, e.InnerException, vrExpress));
        }
        finally
        {
            xml?.Close();
            reader?.Close();
        }
#else
        Debug.LogErrorFormat("TtsReader.ReadTtsXml() - not implemented on this platform.");
#endif

        if (string.IsNullOrEmpty(refid))
        {
            Debug.LogError("Failed to parse ref from vrExpress");
        }
        else if (string.IsNullOrEmpty(text))
        {
            Debug.LogError("Failed to parse text from vrExpress");
        }
    }

    static void ParsevrExpress(XmlTextReader reader, out string refId, out string text)
    {
        refId = "";
        text = "";
        while (reader.Read())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    if (reader.Name == "speech")
                    {
                        refId = reader["ref"];
                        text = reader.ReadInnerXml();
                        return;
                    }
                    break;
            }
        }
    }
}
}
