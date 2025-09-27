// Part of the Ride API
// Copyright - USC Institute for Creative Technologies (https://ict.usc.edu)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;

namespace VHAssets
{
public static class VHUtils
{
    /// <summary>
    /// Helper functions for Unity's FindObjectOfType. If it isn't found, an error is logged, stating the the searcher couldn't find the component 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="searcherName">The gameobject name or identier for who is looking for this component</param>
    /// <returns></returns>
    public static T FindObjectOfType<T>(string searcherName) where T : Component
    {
        T component = GameObject.FindFirstObjectByType<T>();
        if (component == null)
        {
            Debug.LogErrorFormat("{0} couldn't find object in scene with component {1}", searcherName, component.GetType().ToString());
        }
        return component;
    }

    public static T FindGameObjectAndGetComponent<T>(string gameObjectName, string searcherName) where T : Component
    {
        GameObject go = GameObject.Find(gameObjectName);
        T component = null;
        if (go != null)
        {
            component = go.GetComponent<T>();
            if (component == null)
            {
                Debug.LogErrorFormat("{0} failed to find animator on gameobject {1}", searcherName, gameObjectName);
            }
        }
        else
        {
            Debug.LogErrorFormat("{0} failed to find gameobject with name {1}", searcherName, gameObjectName);
        }
        return component;
    }

    /// <summary>
    /// This will find all components of the given type that exist in the scene, regardless of whether or not their gameobject is active
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> FindObjectsOfTypeAll<T>()
    {
        List<T> results = new List<T>();
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().ToList().ForEach(g => results.AddRange(g.GetComponentsInChildren<T>(true)));
        return results;
    }

    /// <summary>
    /// This will the first component of the given type that exist in the scene, regardless of whether or not their gameobject is active
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static T FindObjectOfTypeAll<T>()
    {
        T result = default;
        List<GameObject> rootGos = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().ToList();
        foreach (GameObject root in rootGos)
        {
            result = root.GetComponentInChildren<T>(true);
            if (result != null)
            {
                return result;
            }
        }
        return default;
    }

    public static GameObject FindChild(GameObject root, string name)
    {
        // this function will search all active and inactive objects.  only searches one layer deep in the hierarchy.
        // you can however specify a 'path' to search in 'name'.  eg, FindChild(gun, "magazine/ammo");

        Transform child = root.transform.Find(name);
        return child != null ? child.gameObject : null;
    }

    public static GameObject FindChildRecursive(GameObject root, string name)
    {
        // this function will search all active and inactive objects.  does a recursive search through all child objects and their children
        // you cannot specify a 'path' in 'name'.  'name' must match the name of the object

        if (root.name == name)
            return root;

        for (int i = 0; i < root.transform.childCount; i++)
        {
            GameObject found = FindChildRecursive(root.transform.GetChild(i).gameObject, name);
            if (found != null)
                return found;
        }

        return null;
    }


    public static GameObject[] FindAllChildren(GameObject root)
    {
        // this function will return all active and inactive objects.  only returns one layer deep in the hierarchy

        List<GameObject> objects = new List<GameObject>();

        for (int i = 0; i < root.transform.childCount; i++)
        {
            objects.Add(root.transform.GetChild(i).gameObject);
        }

        return objects.ToArray();
    }

    public static GameObject[] FindAllChildrenRecursive(GameObject root)
    {
        // this function will return all active and inactive objects.  searches through all child objects and their children

        Stack<Transform> stack = new Stack<Transform>();
        List<GameObject> objects = new List<GameObject>();

        stack.Push(root.transform);

        while (stack.Count > 0)
        {
            Transform trans = stack.Pop();

            for (int i = 0; i < trans.childCount; i++)
            {
                Transform child = trans.GetChild(i);
                stack.Push(child);
                objects.Add(child.gameObject);
            }
        }

        return objects.ToArray();
    }

    public static T FindChildOfType<T>(GameObject root) where T : Component
    {
        // this function will search all active and inactive objects and active and inactive components.
        // This is a recursive search through all children.
        // it will return the first child it finds that matches the type

        GameObject[] children = FindAllChildrenRecursive(root);

        foreach (var child in children)
        {
            T[] components = child.GetComponents<T>();
            if (components.Length > 0)
                return components[0];
        }

        return default;
    }

    public static T[] FindAllChildrenOfType<T>(GameObject root) where T : Component
    {
        // this function will search all active and inactive objects and active and inactive components.
        // This is a recursive search through all children.
        // it will return all children it finds that matches the type.  Note that it returns script components. So if the same components is attached twice to a gameobject, it will return both of them.

        GameObject[] children = FindAllChildrenRecursive(root);
        List<T> childrenOfType = new List<T>();

        foreach (var child in children)
        {
            T[] components = child.GetComponents<T>();
            foreach (var component in components)
                childrenOfType.Add(component);
        }

        return childrenOfType.ToArray();
    }

    public static T FindParentOfType<T>(GameObject child) where T : Component
    {
        // this function will walk up the hierarchy until it finds a gameobject with a parent of the given type.
        // it will return that gameobject or null if not found

        Transform curr = child.transform;
        if (curr == null)
            return null;

        while (curr.parent != null)
        {
            var component = curr.parent.GetComponent<T>();
            if (component != null)
                return component;

            curr = curr.parent;
        }

        return null;
    }

    public static ICharacter FindCharacter(string gameObjectName, string eventName)
    {
        if (string.IsNullOrEmpty(gameObjectName))
        {
            return null;
        }

        ICharacter[] chrs = GameObject.FindObjectsByType<ICharacter>(FindObjectsSortMode.None);
        foreach (ICharacter chr in chrs)
        {
            if (chr.CharacterName == gameObjectName || chr.gameObject.name == gameObjectName)
            {
                return chr;
            }
        }

        Debug.LogWarning(string.Format("Couldn't find Character {0} in the scene. Event {1} needs to be looked at", gameObjectName, eventName));
        return null;
    }

    public static GameObject GetRootGameObject(GameObject child)
    {
        // this function will walk up the hierarchy until it finds a gameobject with a parent of 'null'.
        // it will return that gameobject

        Transform curr = child.transform;
        if (curr == null)
            return null;

        while (curr.parent != null)
        {
            curr = curr.parent;
        }

        return curr.gameObject;
    }

    public static string GetGameObjectPath(GameObject o)
    {
        // this function returns the 'path' in the scene hierarchy, returned as a string, separated by a slash
        // most likely very inefficient, but concise

        // https://answers.unity.com/questions/8500/how-can-i-get-the-full-path-to-a-gameobject.html
        return string.Join("/", o.GetComponentsInParent<Transform>().Select(t => t.name).Reverse().ToArray());
    }

    public static void DestroyChildren(Transform parent)
    {
        for (int i = 0; parent != null && i < parent.childCount; i++)
        {
            GameObject.Destroy(parent.GetChild(i).gameObject);
        }
    }

    public static void DrawTransformLines(Transform t, float length)
    {
        Debug.DrawRay(t.position, t.right * length, Color.red);
        Debug.DrawRay(t.position, t.up * length, Color.green);
        Debug.DrawRay(t.position, t.forward * length, Color.blue);
    }

    public delegate void OnAudioFinishedPlaying(AudioClip clip);

    public static void PlayWWWSound(MonoBehaviour behaviour, UnityWebRequest www, AudioSource source, bool loop)
    {
        behaviour.StartCoroutine(PlayWWWSoundInternal(behaviour, www.url, source, loop, AudioType.WAV, null));
    }

    public static void PlayWWWSound(MonoBehaviour behaviour, UnityWebRequest www, AudioSource source, bool loop, AudioType audioType)
    {
        behaviour.StartCoroutine(PlayWWWSoundInternal(behaviour, www.url, source, loop, audioType, null));
    }

    public static void PlayWWWSound(MonoBehaviour behaviour, string url, AudioSource source, bool loop, AudioType audioType)
    {
        behaviour.StartCoroutine(PlayWWWSoundInternal(behaviour, url, source, loop, audioType, null));
    }

    public static void PlayWWWSound(MonoBehaviour behaviour, string url, AudioSource source, bool loop, AudioType audioType, OnAudioFinishedPlaying onFinishedPlaying)
    {
        behaviour.StartCoroutine(PlayWWWSoundInternal(behaviour, url, source, loop, audioType, onFinishedPlaying));
    }

    public static IEnumerator PlayWWWSoundInternal(MonoBehaviour behaviour, string url, AudioSource source, bool loop, AudioType audioType, OnAudioFinishedPlaying onFinishedPlaying)
    {
        using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(url, audioType))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(request.error);
            }
            else
            {
                AudioClip myClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(request);
                source.clip = myClip;

                while (myClip.loadState == AudioDataLoadState.Unloaded ||
                       myClip.loadState == AudioDataLoadState.Loading)
                {
                    yield return new WaitForEndOfFrame();
                }


                source.clip.name = url;
                source.loop = loop;
                source.Play();

                if (onFinishedPlaying != null)
                {
                    behaviour.StartCoroutine(WaitForClipToFinish(source.clip, onFinishedPlaying));
                }
            }
        }
    }

    static IEnumerator WaitForClipToFinish(AudioClip clip, OnAudioFinishedPlaying onFinishedPlaying)
    {
        yield return new WaitForSeconds(clip.length);

        onFinishedPlaying?.Invoke(clip);
    }


    public static void CreateAxisLines()
    {
        // this is a one-time function for generating the axis lines.  You can run this and copy-paste the objects into the scene
        float width = 0.01f;
        CreateCylinder(new Vector3(-10, 0, 0), new Vector3(0, 0, 0), width, Color.red - new Color(0.5f, 0, 0));
        CreateCylinder(new Vector3(0, 0, 0), new Vector3(10, 0, 0), width, Color.red);
        CreateCylinder(new Vector3(0, -10, 0), new Vector3(0, 0, 0), width, Color.green - new Color(0, 0.5f, 0));
        CreateCylinder(new Vector3(0, 0, 0), new Vector3(0, 10, 0), width, Color.green);
        CreateCylinder(new Vector3(0, 0, -10), new Vector3(0, 0, 0), width, Color.blue - new Color(0, 0, 0.5f));
        CreateCylinder(new Vector3(0, 0, 0), new Vector3(0, 0, 10), width, Color.blue);
    }

    public static void CreateCylinder(Vector3 start, Vector3 end, float width, Color color)
    {
        Vector3 offset = end - start;
        Vector3 scale = new Vector3(width, offset.magnitude / 2.0f, width);
        Vector3 position = start + (offset / 2.0f);

        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);  //)Instantiate(cylinderPrefab, position, Quaternion.identity);
        cylinder.transform.SetPositionAndRotation(position, Quaternion.identity);
        cylinder.transform.up = offset;
        cylinder.transform.localScale = scale;
        cylinder.GetComponent<Renderer>().material.color = color;
    }

    public static string GetCommonAspectText(float aspectRatio)
    {
        // http://en.wikipedia.org/wiki/List_of_common_resolutions
        const float check = 0.04f;
        if (Math.Abs(aspectRatio - 1.0000f) < check) return "1:1";
        else if (Math.Abs(aspectRatio - 1.2500f) < check) return "5:4";
        else if (Math.Abs(aspectRatio - 1.3333f) < check) return "4:3";
        else if (Math.Abs(aspectRatio - 1.5000f) < check) return "3:2";
        else if (Math.Abs(aspectRatio - 1.6000f) < check) return "16:10";
        else if (Math.Abs(aspectRatio - 1.6667f) < check) return "5:3";
        else if (Math.Abs(aspectRatio - 1.7778f) < check) return "16:9";
        else if (Math.Abs(aspectRatio - 2.0556f) < check) return "37:18";
        else if (Math.Abs(aspectRatio - 2.1667f) < check) return "19.5:9";
        else if (Math.Abs(aspectRatio - 2.3889f) < check) return "21:9";

        // reverse
        else if (Math.Abs(aspectRatio - 0.8000f) < check) return "4:5";
        else if (Math.Abs(aspectRatio - 0.7500f) < check) return "3:4";
        else if (Math.Abs(aspectRatio - 0.6667f) < check) return "2:3";
        else if (Math.Abs(aspectRatio - 0.6250f) < check) return "10:16";
        else if (Math.Abs(aspectRatio - 0.6000f) < check) return "3:5";
        else if (Math.Abs(aspectRatio - 0.5625f) < check) return "9:16";
        else if (Math.Abs(aspectRatio - 0.4865f) < check) return "18:37";
        else if (Math.Abs(aspectRatio - 0.4615f) < check) return "9:19.5";
        else if (Math.Abs(aspectRatio - 0.4186f) < check) return "9:21";

        else return "";
    }


    public static T DeserializeBytes<T>(byte[] bytes)
    {
        T binaryData = default;

#if !UNITY_WSA
        MemoryStream stream = new MemoryStream(bytes);
        try
        {
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            // Deserialize the hashtable from the file and
            // assign the reference to the local variable.
            binaryData = (T)formatter.Deserialize(stream);
        }
        catch (SerializationException e)
        {
            binaryData = default;
            Debug.LogError("Failed to deserialize. Reason: " + e.Message);
        }
        finally
        {
            stream.Close();
        }

        return binaryData;
#else
        Debug.LogError("VHUtils.DeserializeBytes() - not supported on this platform");
        return binaryData;
#endif
    }


    public static void DisplayObject(GameObject obj, MonoBehaviour coroutineRunner, float displayTime, bool startsOn)
    {
        if (coroutineRunner != null)
        {
            obj.SetActive(startsOn);
            coroutineRunner.StartCoroutine(DisplayObjectCoroutine(obj, displayTime, startsOn));
        }
    }

    static IEnumerator DisplayObjectCoroutine(GameObject obj, float displayTime, bool startsOn)
    {
        yield return new WaitForSeconds(displayTime);
        obj.SetActive(!startsOn);
    }


    public static void ApplicationQuit()
    {
        if (Application.isEditor)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");
#endif
        }
        else
        {
            Application.Quit();
        }
    }

    public static void SceneManagerLoadScene(string sceneName)
    {
        // helper function for Application.LoadLevel(), for backward compatibility

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    public static void SceneManagerLoadScene(int sceneBuildIndex)
    {
        // helper function for Application.LoadLevel(), for backward compatibility

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneBuildIndex);
    }

    public static AsyncOperation SceneManagerLoadSceneAsync(string sceneName)
    {
        // helper function for Application.LoadLevelAsync(), for backward compatibility

        return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);
    }

    public static string SceneManagerActiveSceneName()
    {
        // helper function for Application.loadedLevelName, for backward compatibility

        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    }

#if UNITY_EDITOR
    public static string EditorSceneManagerActiveSceneName()
    {
        // helper function for EditorApplication.currentScene, for backward compatibility

        return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
    }
#endif


    /// <summary>
    /// used for obtaining a value that was passed in with an argument, i.e. -config toolkitConfig.ini
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public static string GetCommandLineArgumentValue(string arg)
    {
        string argDash = "-" + arg;
        string[] arguments = GetCommandLineArgs(); // [0] is the name of the executable
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == argDash)
            {
                if ((i + 1) < arguments.Length && !String.IsNullOrEmpty(arguments[i + 1]))
                    return arguments[i + 1];
            }
        }

        return null;
    }

    public static string ParseCommandLineArg(string arg)
    {
        //string argDash = "--" + arg;
        string[] split = arg.Split('=');
        if (split.Length == 2)
        {
            return split[1];
        }
        return split[0];
    }

    /// <summary>
    /// used for checking flag arguments i.e. nographics
    /// </summary>
    /// <param name="arg"></param>
    /// <returns>true if the argument flag was set</returns>
    public static bool HasCommandLineArgument(string arg)
    {
        string argDash = "-" + arg;
        return Array.Exists<string>(GetCommandLineArgs(), s => s == argDash);
    }


    public static string[] GetCommandLineArgs()
    {
        // non-desktop platforms return NULL, which we shouldn't have to check on each call

        if (IsIOS() || IsAndroid() || IsUWP())
        {
            return new string[0];
        }
        else
        {
#if !UNITY_WSA
            return System.Environment.GetCommandLineArgs();
#else
            return new string[0];
#endif
        }
    }


    public static bool IsUnity5OrGreater()
    {
        // this could probably be expanded to return a specific version number, etc.  but this fits our needs right now, since so many changes were done in unity 5
        return true;
    }


    public static bool Is64Bit()
    {
#if UNITY_64 || UNITY_EDITOR_64
        return true;
#else
        return false;
#endif
    }


    public static bool IsEditor()
    {
        return Application.isEditor;
    }


    [Obsolete("Web Player is deprecated as of Unity 5.3.  Please move away from Web Player specific code")]
    public static bool IsWebPlayer()
    {
        return false;
    }


    public static bool IsWindows()
    {
        return Application.platform == RuntimePlatform.WindowsPlayer ||
               Application.platform == RuntimePlatform.WindowsEditor ||
               Application.platform == RuntimePlatform.WindowsServer;
    }


    public static bool IsOSX()
    {
        return Application.platform == RuntimePlatform.OSXPlayer ||
               Application.platform == RuntimePlatform.OSXEditor;
    }


    public static bool IsWindows8OrGreater()
    {
        // win10 has the same version number unless the app has been 'manifested for Win10'
        // links, in case we ever care:
        // https://msdn.microsoft.com/library/windows/desktop/ms724832.aspx
        // https://msdn.microsoft.com/en-us/library/windows/desktop/dn481241.aspx
        // https://msdn.microsoft.com/library/windows/desktop/ms724451(v=vs.85).aspx

        // we use this for deciding which TTS voice to use.  This could be enhanced to return a version class, and then you could compare it against const's for different windows versions, etc.
#if !UNITY_WSA
        System.Version win8version = new System.Version(6, 2, 9200, 0);
        return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version >= win8version;
#else
        return false;
#endif
    }


    public static bool IsWindows10OrGreater()
    {
        // see notes above for IsWindows8OrGreater()
#if !UNITY_WSA
        System.Version win10version = new System.Version(10, 0, 0, 0);
        return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version >= win10version;
#else
        return false;
#endif
    }


    public static bool IsIOS()
    {
        if (IsEditor())
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.iOS;
#endif
        }

        return Application.platform == RuntimePlatform.IPhonePlayer;
    }


    public static bool IsAndroid()
    {
        if (IsEditor())
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android;
#endif
        }

        return Application.platform == RuntimePlatform.Android;
    }


    public static bool IsWebGL()
    {
        if (IsEditor())
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL;
#endif
        }

        return Application.platform == RuntimePlatform.WebGLPlayer;
    }


    public static bool IsUWP()
    {
        if (IsEditor())
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WSAPlayer;
#endif
        }

#if UNITY_WSA
        return true;
#else
        return false;
#endif
    }


    public static bool IsLinux()
    {
        if (IsEditor())
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.StandaloneLinux64;
#endif
        }

        return Application.platform == RuntimePlatform.LinuxPlayer || 
               Application.platform == RuntimePlatform.LinuxServer;
    }


    public static bool IsHeadless()
    {
        return SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;
    }


    public static bool IsDedicatedServer()
    {
#if UNITY_SERVER
        return true;
#else
        return false;
#endif
    }


    public static string GetLocalIpAddress()
    {
        string localIp = "";

        if (!VHUtils.IsWebGL())
        {
            // on some windows machines, an exception is thrown.  Unsure why.  For now, just catch it and move on, not important.
            try
            {
                foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        localIp = ip.ToString();
                        break;
                    }
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Debug.LogWarningFormat("DebugInfo.Awake() - SocketException caught when trying to resolve ip address: {0}", e);
            }
        }

        return localIp;
    }


    public static string GetSVNRevision(string path)
    {
        // this function will return the output from svnversion if the given path is a svn working folder.
        // eg:
        // string version = GetSVNRevision(Application.dataPath + "/../../../");
        // version will contain one of the following:
        // 4123:4168     mixed revision working copy
        // 4168M         modified working copy
        // 4123S         switched working copy
        // 4123P         partial working copy, from a sparse checkout
        // 4123:4168MS   mixed revision, modified, switched working copy
        //
        // if folder is not a svn working copy, it will return "" empty string
        // this function will only run on windows, if run on any other platform, it will return "" empty string

        string versionText = "";

        if (!(VHUtils.IsWindows() || VHUtils.IsOSX()))
            return versionText;

        // check to see if folder is a svn working copy
        try
        {
            if (!Directory.Exists(path))
                return versionText;

#if !UNITY_WSA
            // run 'svnversion' on the folder
            System.Diagnostics.Process svn = new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = "svnversion",
                    Arguments = "-n " + path,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
#endif

            string output = "";

#if !UNITY_WSA
            svn.Start();
            while (!svn.StandardOutput.EndOfStream)
            {
                output += svn.StandardOutput.ReadLine();
            }
            svn.WaitForExit();
#endif

            // svnversion .
            // 4123:4168     mixed revision working copy
            // 4168M         modified working copy
            // 4123S         switched working copy
            // 4123P         partial working copy, from a sparse checkout
            // 4123:4168MS   mixed revision, modified, switched working copy
            // Unversioned directory

            if (!output.Contains("Unversioned"))
            {
                versionText = output;
            }
        }
        catch (Exception)
        {
            // if svnversion isn't in the path, or any other error we encounter
        }

        return versionText;
    }


    [Serializable]
    public class UnityCloudBuildManifest
    {
        // https://build.cloud.unity3d.com/support/guides/manifest/

        // {"cloudBuildTargetName":"android","buildNumber":41,"scmCommitId":"7347","scmBranch":"/core/Monticello","buildStartTime":"3/15/2016 1:08:07 AM","projectId":"ictfromunityads32798/monticello","bundleId":"edu.usc.ict.monticello","unityVersion":"5.3.3f1"}

        public string cloudBuildTargetName;  // The name of the project build target that was built. Currently, this will correspond to the platform, as either "default-web", "default-ios", or "default-android".
        public int buildNumber;  // The Unity Cloud Build number corresponding to this build
        public string scmCommitId;          // Commit or changelist built by UCB
        public string scmBranch;  // Name of the branch that was built
        public string buildStartTime;  // The UTC timestamp when the build process was started
        public string projectId;  // The UCB project identifier
        public string bundleId;  // (iOS and Android only) The bundleIdentifier configured in Unity Cloud Build
        public string unityVersion;  // The version of Unity used by UCB to create the build
        public string xcodeVersion;  // (iOS only) The version of XCode used to build the project
    }


    public static UnityCloudBuildManifest GetBuildInfo()
    {
        // try and get version info from the resource file generated by Unity Cloud build server.
        // ref: https://build.cloud.unity3d.com/support/guides/manifest/
        // if that doesn't exist, try and get version info from svn.
        // otherwise, fill with some default info

        UnityCloudBuildManifest unityCloudBuildManifest = null;

        string versionText = "";

        var unityCloudBuildManifestText = (TextAsset)Resources.Load("UnityCloudBuildManifest.json");
        if (unityCloudBuildManifestText != null)
        {
            try
            {
                unityCloudBuildManifest = JsonUtility.FromJson<UnityCloudBuildManifest>(unityCloudBuildManifestText.text);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        else
        {
            // either built locally (not Unity Cloud), or run from editor via svn sandbox

            // check to see if folder is a svn working copy
            string svnWorkingFolder = "";
            versionText = VHUtils.GetSVNRevision(Application.dataPath + svnWorkingFolder);
        }

        if (unityCloudBuildManifest == null)
        {
            unityCloudBuildManifest = new UnityCloudBuildManifest();
            unityCloudBuildManifest.scmCommitId = versionText;
            unityCloudBuildManifest.unityVersion = Application.unityVersion;
        }

        return unityCloudBuildManifest;
    }

    public static bool IsSmartbodyAvailable()
    {
        return Type.GetType("SmartbodyExternals") != null;
    }

    public static bool IsIndexInRange(int index, int arraySize)
    {
        bool isInRange = index >= 0 && index < arraySize;
        if (!isInRange)
        {
            Debug.LogErrorFormat("Error: Index not in range. Index {0} is out of range 0-{1}", index, arraySize);
        }
        return isInRange;
    }

    public static T[] SubArray<T>(this T[] data, int index, int length)
    {
        T[] result = new T[length];
        Array.Copy(data, index, result, 0, length);
        return result;
    }
}
}
