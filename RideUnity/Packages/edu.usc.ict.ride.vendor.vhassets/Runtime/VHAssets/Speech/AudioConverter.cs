using System.IO;
using UnityEngine;
using SavWavWebsterNoble;

namespace VHAssets
{
public static class AudioConverter
{
    #region Functions
    static string FlacPath
    {
        get { return string.Format("{0}Flac/Flac.exe", VHFile.GetStreamingAssetsPath()); }
    }
    public static byte [] ConvertClipToFlac(AudioClip clip, string filename)
    {
        return ConvertClipToFlac(clip, filename, FlacPath);
    }

    public static byte [] ConvertClipToFlac(AudioClip clip, string filename, string flacExePath)
    {
#if !UNITY_WSA
        // here we convert a unity audio clip to a wav then convert the wav to a flac
        // using the flac command line tool https://xiph.org/flac/download.html
        ConvertClipToWav(clip, filename);


        // use flac
        System.Diagnostics.Process flacProcess = new System.Diagnostics.Process();
        flacProcess.StartInfo.FileName = flacExePath;
        flacProcess.StartInfo.Arguments = string.Format("-f -8 {0}", filename);
#if !UNITY_WEBGL
        flacProcess.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
#endif
        flacProcess.Start();
        flacProcess.WaitForExit();
#endif

        return VHFile.FileWrapper.ReadAllBytes(Path.ChangeExtension(filename, ".flac"));
    }

    public static byte [] ConvertClipToWav(AudioClip clip, string filename)
    {
        if (!SavWav.Save(filename, clip))
        {
            Debug.LogError("ConvertClipToWav FAILED: " + filename);
            return null;
        }

        return VHFile.FileWrapper.ReadAllBytes(filename);
    }
    #endregion
}
}
