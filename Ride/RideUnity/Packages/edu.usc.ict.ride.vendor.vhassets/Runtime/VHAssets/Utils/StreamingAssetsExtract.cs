using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VHAssets
{
public static class StreamingAssetsExtract
{
    static bool m_streamingAssetsExtracted = false;
    static List<string> m_fileList = new List<string>();

    public static bool StreamingAssetsExtracted { get { return m_streamingAssetsExtracted; } }

    public static bool ExtractStreamingAssets()
    {
        // this function will extract all the files in the StreamingAssets folder and write
        // them to disk to the Application.persistantdatapath folder
        // this is required for Android when working with Native libraries that need to do
        // traditional file i/o for loading files off of disk.
        // this requires an updated file in StreamingAssets named fileList.txt
        // fileList.txt can be generated with fileListBuilder.bat

        if (!m_streamingAssetsExtracted)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                // add the files in fileList to m_fileList
                m_fileList = new List<string>();

                var wwwFileList = VHFile.LoadStreamingAssets("fileList.txt");

                if (wwwFileList != null)
                {
                    string wwwtext = wwwFileList.downloadHandler.text;
                    string[] split = wwwtext.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in split)
                    {
                        string sTrim = s.Trim();

                        // ignore .meta files in the text file
                        if (sTrim.EndsWith(".meta"))
                            continue;

                        m_fileList.Add(sTrim);

                        //Debug.Log("ExtractStreamingAssets() - '" + sTrim + "'");
                    }
                }


                // extract the files in fileListExtract to the Android persistant path
                var wwwList = VHFile.LoadStreamingAssets("fileListExtract.txt");

                if (wwwList != null)
                {
                    string wwwtext = wwwFileList.downloadHandler.text;
                    string [] split = wwwtext.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    foreach (var s in split)
                    {
                        string sTrim = s.Trim();

                        // ignore .meta files in the text file
                        if (sTrim.EndsWith(".meta"))
                            continue;

                        string targetPath = Application.persistentDataPath + "/" + sTrim;

                        Directory.CreateDirectory(Path.GetDirectoryName(targetPath));

                        var www = VHFile.LoadStreamingAssets(sTrim);
                        if (www != null)
                        {
                            m_fileList.Add(sTrim);

                            using (FileStream fs = new FileStream(targetPath, FileMode.Create))
                            {
                                using (BinaryWriter w = new BinaryWriter(fs))
                                {
                                    w.Write(www.downloadHandler.data);

                                    //Debug.Log("ExtractStreamingAssets() - wrote - '" + targetPath + "'");
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("ExtractStreamingAssets() - LoadDynamic() failed - '" + sTrim + "'");
                        }
                    }
                }
            }

            m_streamingAssetsExtracted = true;
        }

        return true;
    }

    public static string [] GetFiles()
    {
        ExtractStreamingAssets();

        return m_fileList.ToArray();
    }

    public static string [] GetFiles(string path)
    {
        ExtractStreamingAssets();

        List<string> list = new List<string>();

        foreach (var file in m_fileList)
        {
            if (file.StartsWith(path))
                list.Add(file);
        }

        return list.ToArray();
    }

    public static string [] GetFiles(string path, string extension)
    {
        // extension includes the dot, eg  ".wav"

        ExtractStreamingAssets();

        List<string> list = new List<string>();

        foreach (var file in m_fileList)
        {
            if (file.StartsWith(path) && Path.GetExtension(file) == extension)
                list.Add(file);
        }

        return list.ToArray();
    }
}
}
