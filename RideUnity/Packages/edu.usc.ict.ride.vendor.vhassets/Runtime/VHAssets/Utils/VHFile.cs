using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace VHAssets
{
public static class VHFile
{
    public delegate void OnExceptionThrown(Exception e);


    public static void CopyDirectory(string sourceDirectory, string destinationDirectory, bool copySubdirectories, bool overwrite, string directoryExcludeString = "")
    {
        // usage: CopyDirectory("Assets/Scripts, "Assets/ScriptsNew", true, true, ".svn");

        // taken from "How to: Copy Directories" http://msdn.microsoft.com/en-us/library/bb762914.aspx
        // modified to include overwrite flag

        DirectoryInfo dir = new DirectoryInfo(sourceDirectory);
        DirectoryInfo[] dirs3 = VHFile.DirectoryInfoWrapper.GetDirectories(dir);

        // If the source directory does not exist, throw an exception.
        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirectory);
        }

        if (!string.IsNullOrEmpty(directoryExcludeString))
        {
            if (dir.FullName.Contains(directoryExcludeString))
            {
                return;
            }
        }

        // If the destination directory does not exist, create it.
        if (!Directory.Exists(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }


        // Get the file contents of the directory to copy.
        FileInfo[] files = VHFile.DirectoryInfoWrapper.GetFiles(dir);

        foreach (FileInfo file in files)
        {
            // Create the path to the new copy of the file.
            string temppath = Path.Combine(destinationDirectory, file.Name);

            // Copy the file.  If destination exists, clear any read-only flag that might be present
            if (File.Exists(temppath))  File.SetAttributes(temppath, FileAttributes.Normal);
            VHFile.FileInfoWrapper.CopyTo(file, temppath, overwrite);
        }

        // If copySubDirs is true, copy the subdirectories.
        if (copySubdirectories)
        {
            foreach (DirectoryInfo subdir in dirs3)
            {
                // Create the subdirectory.
                string temppath = Path.Combine(destinationDirectory, subdir.Name);

                // Copy the subdirectories.
                CopyDirectory(subdir.FullName, temppath, copySubdirectories, overwrite, directoryExcludeString);
            }
        }
    }

    public static void CopyFiles(string[] sourceFiles, string destinationDirectory)
    {
        for (int i = 0; i < sourceFiles.Length; i++)
        {
            if (File.Exists(sourceFiles[i]))
            {
                File.Copy(sourceFiles[i], destinationDirectory + Path.GetFileName(sourceFiles[i]), true);
            }
        }
    }

    public static void CopyFiles(string sourceDirectory, string destinationDirectory, string searchPattern, SearchOption searchOption)
    {
        // usage: CopyFiles("Assets/Scripts", "Assets/NewScripts", "*.cs", SearchOption.AllDirectories);

        string[] files = Directory.GetFiles(sourceDirectory, searchPattern, searchOption);
        CopyFiles(files, destinationDirectory);
    }

    public static void MoveFiles(string sourceDirectory, string destinationDirectory, string searchPattern, SearchOption searchOption)
    {
        string[] files = Directory.GetFiles(sourceDirectory, searchPattern, searchOption);

        for (int i = 0; i < files.Length; i++)
        {
            string destinationPath = destinationDirectory + Path.GetFileName(files[i]);
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(files[i], destinationPath);
        }
    }


    public static string [] GetStreamingAssetsFiles(string path, string extension)
    {
        // this function works on all platforms, special cased for Android.  See StreamingAssetsExtract class.

        // path is relative to StreamingAssets (see LoadStreamingAssets())
        // extension includes the dot, eg  ".wav"
        // wildcards not accepted in this function

        if (Application.platform == RuntimePlatform.Android)
        {
            return StreamingAssetsExtract.GetFiles(path, extension);
        }
        else
        {
            string [] files = VHFile.DirectoryWrapper.GetFiles(VHFile.GetStreamingAssetsPath() + path, "*" + extension, SearchOption.AllDirectories);
            List<string> fileList = new List<string>(files);
            for (int i = 0; i < fileList.Count; i++)
            {
                fileList[i] = fileList[i].Replace(VHFile.GetStreamingAssetsPath(), "");
            }
            return fileList.ToArray();
        }
    }

    public static string GetStreamingAssetsPath()
    {
        string path;

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.LinuxPlayer ||
            Application.platform == RuntimePlatform.LinuxEditor)
        {
            path = Application.streamingAssetsPath + "/";
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            path = "";
            Debug.Log("GetStreamingAssetsPath() - This function is invalid on Android.  Cannot access streaming assets path directly.  Need to use WWW url format (see GetStreamingAssetsURL())");
        }
        else
        {
            path = "";
            Debug.Log("GetStreamingAssetsPath() error - Platform not supported");
        }

        return path;
    }

    public static string GetStreamingAssetsURL()
    {
        string path;

        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer)
        {
            path = "file://" + Application.streamingAssetsPath + "/";
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            path = "file://" + Application.streamingAssetsPath + "/";
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            path = "jar:file://" + Application.dataPath + "!/assets/";
        }
        else
        {
            path = "";
            Debug.Log("GetStreamingAssetsURL() error - Platform not supported");
        }

        return path;
    }

    public static string GetExternalAssetsPath()
    {
        // this is the same for all platforms except for Android, where persistantDataPath is used

        string path;

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.OSXPlayer)
        {
            path = Application.streamingAssetsPath + "/";
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            path = Application.persistentDataPath + "/";
        }
        else
        {
            path = "";
            Debug.Log("GetExternalAssetsPath() error - Platform not supported");
        }

        return path;
    }

    public static UnityWebRequest LoadStreamingAssets(string filename)
    {
        // This function will load data from the StreamingAssets folder in a cross-platform manner.
        // filename is relative to the StreamingAssets folder.
        // ie:  \Assets\StreamingAssets\data\file.dat
        // filename should be:  data\file.dat

        // data can be accessed through the WWW accessors:  .bytes, .texture, .audioClip, etc.

        // this function doesn't return until the data has been completely loaded.
        // see LoadStreamingAssetsAsync() for an asynchronous version.

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            Debug.LogWarning("LoadStreamingAssets() - doesn't work on iOS since it needs to wait a frame before it will start loading, and will get stuck in an endless loop.  Use LoadStreamingAssetsBytes() instead.  (See unity support #3167)");
            return null;
        }

        string fullPath = GetStreamingAssetsURL() + filename;

        //Debug.Log("LoadStreamingAssets() - fullPath = " + fullPath);

        var www = UnityWebRequest.Get(fullPath);
        www.SendWebRequest();
        while (!www.isDone) { }

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.Log("LoadStreamingAssets() error - " + www.error);
            return null;
        }

        //Debug.Log("length: " + www.bytes.Length);

        return www;
    }

    public static UnityWebRequest LoadStreamingAssetsAsync(string filename)
    {
        // This function will load data from the StreamingAssets folder in a cross-platform manner.
        // filename is relative to the StreamingAssets folder.
        // ie:  \Assets\StreamingAssets\data\file.dat
        // filename should be:  data\file.dat

        // data can be accessed through the WWW accessors:  .bytes, .texture, .audioClip, etc.

        // returned WWW needs to wait until it is finished loading, via isDone or yield
        // do not while loop on isDone, because on certain platforms, this will cause an endless loop, see unity support #3167

        string fullPath = GetStreamingAssetsURL() + filename;

        Debug.Log("fullPath: " + fullPath);
        //Debug.Log("LoadStreamingAssetsAsync() - fullPath = " + fullPath);

        var www = UnityWebRequest.Get(fullPath);
        www.SendWebRequest();
        return www;
    }

    public static byte [] LoadStreamingAssetsBytes(string filename)
    {
        // This function will load data from the StreamingAssets folder in a cross-platform manner.
        // filename is relative to the StreamingAssets folder.
        // ie:  \Assets\StreamingAssets\data\file.dat
        // filename should be:  data\file.dat

        // this function doesn't return until the data has been completely loaded.

        //Debug.Log("LoadStreamingAssetsBytes() - " + filename);

        if (Application.platform == RuntimePlatform.Android)
        {
            // Android needs to use the WWW class
            return LoadStreamingAssets(filename).downloadHandler.data;
        }
        else
        {
            string fullPath = GetStreamingAssetsPath() + filename;
            byte [] bytes = VHFile.FileWrapper.ReadAllBytes(fullPath);
            return bytes;
        }
    }

    public static string RemovePathUpTo(string upToHere, string path)
    {
        int index = path.LastIndexOf(upToHere);
        if (index > -1)
        {
            return path.Substring(index);
        }

        return path;
    }

    public static void ClearAttributesRecursive(string currentDir)
    {
        // recursively clear the attributes on all files under the given folder

        if (Directory.Exists(currentDir))
        {
            string [] subDirs = Directory.GetDirectories(currentDir);
            foreach (string dir in subDirs)
            {
                ClearAttributesRecursive(dir);
            }

            string [] files = Directory.GetFiles(currentDir);
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
            }
        }
    }


    static public void WriteXML<T>(string filename, T data)
    {
        WriteXML<T>(filename, data, null);
    }

    static public void WriteXML<T>(string filename, T data, OnExceptionThrown cb)
    {
        XmlSerializer serializer = null;
        TextWriter writer = null;
#if !UNITY_WSA
        try
        {
            serializer = new XmlSerializer(typeof(T));
            writer = new StreamWriter(filename);
            serializer.Serialize(writer, data);
            writer.WriteLine();
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to write xml file " + filename + " Message: " + e.Message + ". Inner Exception: " + e.InnerException);
            cb?.Invoke(e);
        }
        finally
        {
            writer?.Close();
        }
#else
        Debug.LogErrorFormat("VHFile.WriteXML() - not implemented on this platform.");
#endif
    }

    static public T ReadXML<T>(string filename)
    {
        T retval = default;
        XmlSerializer serializer = null;
        FileStream fs = null;

#if !UNITY_WSA
        try
        {
            serializer = new XmlSerializer(typeof(T));
            fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            retval = (T)serializer.Deserialize(fs);
        }
        catch (Exception e)
        {
            Debug.LogError("Failed to read xml file " + filename + " Message: " + e.Message + ". Inner Exception: " + e.InnerException);
        }
        finally
        {
            fs?.Close();
        }
#else
        Debug.LogErrorFormat("VHFile.ReadXML() - not implemented on this platform.");
#endif

        return retval;
    }


    static public string FindPathToFolder(string folderName)
    {
        string folderLocation = string.Empty;
        string [] dirs = VHFile.DirectoryWrapper.GetDirectories(Application.dataPath, folderName, SearchOption.AllDirectories);
        for (int i = 0; i < dirs.Length; i++)
        {
            if (dirs[i].Contains(folderName))
            {
                folderLocation = dirs[i];
                folderLocation = folderLocation.Replace('\\', '/');

                int index = folderLocation.IndexOf("/Assets");
                if (index != -1)
                {
                    folderLocation = folderLocation.Remove(0, index + 1);
                }
                folderLocation += '/';
                break;
            }
        }

        return folderLocation;
    }



    public static class DirectoryWrapper
    {
        public static void Delete(string path, bool recursive)
        {
#if UNITY_WEBGL
            Debug.LogError("Directory.Delete() isn't defined on this platform");
#else
            Directory.Delete(path, recursive);
#endif
        }

        public static string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
        {
#if UNITY_WEBGL
            Debug.LogError("Directory.GetDirectories() isn't defined on this platform");
            return null;
#else
            return Directory.GetDirectories(path, searchPattern, searchOption);
#endif
        }

        public static string [] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
#if UNITY_WEBGL
            Debug.LogError("Directory.GetFiles() isn't defined on this platform");
            return null;
#else
            return Directory.GetFiles(path, searchPattern, searchOption);
#endif
        }
    }


    public static class DirectoryInfoWrapper
    {
        public static DirectoryInfo [] GetDirectories(DirectoryInfo source)
        {
#if UNITY_WEBGL
            Debug.LogError("DirectoryInfo.GetDirectories() isn't defined on this platform");
            return null;
#else
            return source.GetDirectories();
#endif
        }

        public static FileInfo[] GetFiles(DirectoryInfo source)
        {
#if UNITY_WEBGL
            Debug.LogError("DirectoryInfo.GetFiles() isn't defined on this platform");
            return null;
#else
            return source.GetFiles();
#endif
        }
    }


    public static class FileWrapper
    {
        public static void AppendAllText(string path, string contents)
        {
#if UNITY_WEBGL
            Debug.LogError("File.AppendAllText() isn't defined on this platform");
#else
            File.AppendAllText(path, contents);
#endif
        }

        public static void Move(string sourceFileName, string destFileName)
        {
#if UNITY_WEBGL
            Debug.LogError("File.Move() isn't defined on this platform");
#else
            File.Move(sourceFileName, destFileName);
#endif
        }

        public static FileStream Open(string path, FileMode mode, FileAccess access)
        {
#if UNITY_WEBGL
            Debug.LogError("File.Open() isn't defined on this platform");
            return null;
#else
            return File.Open(path, mode, access);
#endif
        }

        public static byte [] ReadAllBytes(string path)
        {
#if UNITY_WEBGL
            Debug.LogError("File.FileReadAllBytes() isn't defined on this platform");
            return null;
#else
            return File.ReadAllBytes(path);
#endif
        }

        public static string ReadAllText(string path)
        {
#if UNITY_WEBGL
            Debug.LogError("File.ReadAllText() isn't defined on this platform");
            return null;
#else
            return File.ReadAllText(path);
#endif
        }
    }


    public static class FileInfoWrapper
    {
        public static FileInfo CopyTo(FileInfo source, string destFileName, bool overwrite)
        {
#if UNITY_WEBGL
            Debug.LogError("FileInfo.CopyTo() isn't defined on this platform");
            return null;
#else
            return source.CopyTo(destFileName, overwrite);
#endif
        }
    }
}
}
