using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Contains general-purpose utility functions for visibility, raycasting, input,
    /// scene control, agent proximity checks, preferences, system info, mesh validation,
    /// and ballistic calculations. Used widely across simulation and UI systems in RIDE.
    /// </summary>
    public static class RideUtils
    {
        #region Visibility and Frustum

        /// <summary>Returns an array of all game objects in the scene that have colliders in the given frustum.</summary>
        /// <param name="frustum"></param>
        /// <returns></returns>
        public static GameObject[] GetObjectsInView(RideFrustum frustum)
        {
            // find all the colliders in the scene and see if there bounds are in the frustum
            var visibleObjects = new List<GameObject>();
            var colliders = RideUtils.FindObjectsByType<Collider>();
            foreach (var collider in colliders)
            {
                if (frustum.Contains(collider.bounds))
                    visibleObjects.Add(collider.gameObject);
            }

            return visibleObjects.ToArray();
        }

        public static GameObject[] GetObjectsInView(RideVector3 viewerPos, RideVector3 viewerDirection, RideFrustum frustum, IEnumerable<GameObject> gameObjects)
        {
            // find all the colliders in the scene and see if there bounds are in the frustum
            var visibleObjects = new List<GameObject>();
            foreach (var go in gameObjects)
            {
                if (IsObjectInView(frustum, go) && !IsObjectOccluded(viewerPos, go.GetComponent<ISpatialObject>()))
                    visibleObjects.Add(go);
            }

            return visibleObjects.ToArray();
        }

        public static IEnumerable<ISpatialObject> GetObjectsInView(ISpatialObject viewer, IEnumerable<ISpatialObject> objects) =>
            GetObjectsInView(60, 1.549296f, 0.3f, 1000f, viewer.position, viewer.rotation, objects);

        public static IEnumerable<ISpatialObject> GetObjectsInView(float fov, float aspect, float near, float far, RideVector3 viewerPos, RideQuaternion viewerRot, IEnumerable<ISpatialObject> objects)
        {
            // find all the colliders in the scene and see if there bounds are in the frustum
            var visibleObjects = new List<ISpatialObject>();
            var frustum = RideFrustum.GetFrustum(fov, aspect, near, far, viewerPos, viewerRot);
            foreach (var obj in objects)
            {
                bool isObjectInView = frustum.Contains(obj.bounds);
                bool isObjectOccluded = IsObjectOccluded(viewerPos, obj);
                if (isObjectInView && !isObjectOccluded)
                    visibleObjects.Add(obj);
            }

            return visibleObjects.ToArray();
        }

        /// <summary>Returns a set of VisibleObjects from the viewers perspective.</summary>
        /// <param name="fov"></param>
        /// <param name="aspect"></param>
        /// <param name="near"></param>
        /// <param name="far"></param>
        /// <param name="viewer"></param>
        /// <returns></returns>
        public static VisibleObject[] GetVisibleObjects(float fov, float aspect, float near, float far, RideVector3 viewerPos, RideQuaternion viewerRot)
        {
            var visibleObjects = new List<VisibleObject>();
            var frustum = RideFrustum.GetFrustum(fov, aspect, near, far, viewerPos, viewerRot);
            var objectsInView = GetObjectsInView(frustum);
            foreach (var objectInView in objectsInView)
            {
                var visibleCollider = objectInView.GetComponent<Collider>();
                if (visibleCollider == null)
                {
                    Debug.LogWarning($"Gameobject {objectInView.name} is visible but doesn't have a collider");
                    continue;
                }

                var viewerToObject = (RideVector3)objectInView.transform.position - viewerPos;
                visibleObjects.Add(new VisibleObject()
                {
                    distance = viewerToObject.magnitude,
                    direction = viewerToObject,
                    bounds = visibleCollider.bounds
                });
            }

            return visibleObjects.ToArray();
        }

        /// <summary>Returns true if the testObject is inside the frustum. The testObject requires a collider component.</summary>
        /// <param name="frustum"></param>
        /// <param name="testObject"></param>
        /// <returns></returns>
        public static bool IsObjectInView(RideFrustum frustum, GameObject testObject)
        {
            var collider = testObject.GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogError($"Can't test if gameobject {testObject.name} is in view because it doesn't have a collider");
                return false;
            }

            return frustum.Contains(collider.bounds);
        }

        /// <summary>Tests if the testObject is occluded from the viewer.</summary>
        /// <param name="viewerPosition">The positon of the viewer</param>
        /// <param name="testObject">The object being tested for occlusion</param>
        /// <returns>True if there if testObject is occluded from viewerPosition</returns>
        public static bool IsObjectOccluded(RideVector3 viewerPosition, ISpatialObject testObject) =>
            IsObjectOccluded(viewerPosition, testObject, out _);

        public static bool IsObjectOccluded(RideVector3 viewerPosition, ISpatialObject testObject, RideVector3 offsetPosition) =>
            IsObjectOccluded(viewerPosition, testObject, offsetPosition, out _);

        public static bool IsObjectOccluded(RideVector3 viewerPosition, ISpatialObject testObject, out ISpatialObject firstHit) =>
            IsObjectOccluded(viewerPosition, testObject, RideVector3.zero, out firstHit);

        /// <summary>
        /// Determines if the given test object is occluded from the viewer by any other object along the line of sight.
        /// </summary>
        /// <param name="viewerPosition">The world-space origin of the viewer.</param>
        /// <param name="testObject">The spatial object being tested for visibility.</param>
        /// <param name="firstHit">Outputs the first ISpatialObject hit along the ray path.</param>
        /// <param name="offsetPosition">Offset from the test object's position (e.g. eye or center height).</param>
        /// <returns>True if an object blocks the view to the test object. False if clear line of sight exists.</returns>
        public static bool IsObjectOccluded(RideVector3 viewerPosition, ISpatialObject testObject, RideVector3 offsetPosition, out ISpatialObject firstHit)
        {
            firstHit = default;

            RideVector3 targetPosition = testObject.position + offsetPosition;
            RideVector3 direction = targetPosition - viewerPosition;
            direction = direction.normalized;
            float distance = direction.magnitude;
            RaycastHit[] hits = Physics.RaycastAll(viewerPosition, direction, distance);

            if (hits.Length == 0)
                return false;

            float closestDistance = float.MaxValue;
            ISpatialObject closestHit = null;
            foreach (var hit in hits)
            {
                var hitSpatial = hit.transform.GetComponent<ISpatialObject>();
                if (hitSpatial == null || RideVector3.Distance(hit.transform.position, viewerPosition) < 0.01f)
                    continue;

                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestHit = hitSpatial;
                }
            }

            firstHit = closestHit;
            return firstHit != null && firstHit != testObject;
        }

        #endregion

        #region Raycasting and Hit Tests

        public static bool GetFirstHit(RideVector3 viewerPosition, ISpatialObject testObject)
        {
            RideVector3 dir = testObject.position - viewerPosition;
            if (Physics.SphereCast(new Ray(viewerPosition, dir.normalized), 0.25f, out RaycastHit hit, dir.magnitude))
            {
                //Debug.Log("hit: " + hit.transform.name);
                return hit.transform.GetComponent<ISpatialObject>() == testObject;
            }

            //Debug.Log("hit nothing!");

            return false;
        }

        /// <summary>Tests if you hit testObject with the ray cast.</summary>
        /// <param name="rayCastCam">The camera to do the ray casting from</param>
        /// <param name="screenPos">The screen position that creates the origin and direction of the ray</param>
        /// <param name="testObject">The object to test the ray against</param>
        /// <param name="spreadFactor">the amount to modify the the axis of direction</param>
        /// <returns>True if the ray hit the test object</returns>
        public static bool IsHit(ICamera rayCastCam, RideVector3 screenPos, ISpatialObject testObject, float spreadFactor = 0)
        {
            RideRay ray = rayCastCam.ScreenPointToRay(screenPos);
            Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity);
            return hit.collider != null && hit.collider.GetComponent<ISpatialObject>() == testObject;
        }

        /// <summary>Tests if you hit something with the ray cast.</summary>
        /// <param name="rayCastCam">The camera to do the ray casting from</param>
        /// <param name="screenPos">The screen position that creates the origin and direction of the ray</param>
        /// <param name="mask">Layer mask</param>
        /// <param name="spreadFactor">the amount to modify the the axis of direction</param>
        /// <returns>True if the ray hit something</returns>
        public static bool IsHit(ICamera rayCastCam, RideVector3 screenPos, RideLayerMask mask, float spreadFactor = 0)
        {
            RideRay ray = rayCastCam.ScreenPointToRay(screenPos);
            return RideMath.GetRaycastHit(ray.origin, ray.direction, mask, spreadFactor).isHit;
        }

        /// <summary>Tests if you hit something with the ray cast.</summary>
        /// <param name="origin">origin of the ray</param>
        /// <param name="direction">direction of the ray</param>
        /// <param name="mask">layer mask</param>
        /// <param name="spreadFactor">the amount to modify the the axis of direction</param>
        /// <returns>True if the ray hit something</returns>
        public static bool IsHit(RideVector3 origin, RideVector3 direction, RideLayerMask mask, float spreadFactor = 0) =>
            RideMath.GetRaycastHit(origin, direction, mask, spreadFactor).isHit;

        #endregion

        #region Agent Queries

        /// <summary>Find the closest ally to agent.</summary>
        /// <param name="agent"></param>
        /// <returns>The closest ally</returns>
        public static RideID GetClosestAlly(RideID agent) => GetClosestAgent(agent, Globals.api.scenarioSystem.GetAgents(Globals.api.agentSystem.GetAgentTeam(agent)));

        /// <summary>Find the Closest enemy to agent.</summary>
        /// <param name="agent"></param>
        /// <returns>The closest agent</returns>
        public static RideID GetClosestEnemy(RideID agent) => GetClosestAgent(agent, Globals.api.scenarioSystem.GetAgents(Globals.api.agentSystem.GetAgentTeam(agent) == Team.Blue ? Team.Red : Team.Blue));

        /// <summary>Find the closest agent to agent. Use all agents in the scenario.</summary>
        /// <param name="agent"></param>
        /// <returns>The closest agent</returns>
        public static RideID GetClosestAgent(RideID agent) => GetClosestAgent(agent, Globals.api.scenarioSystem.GetAgents());

        /// <summary>Find the agent id of the closest agent to agent.</summary>
        /// <param name="agent">The agent where distance will be measured to</param>
        /// <param name="agents">The list of agents to find the closest</param>
        /// <returns>The closest agent</returns>
        public static RideID GetClosestAgent(RideID agent, IEnumerable<RideID> agents)
        {
            RideVector3 myPos = Globals.api.agentSystem.GetAgentPosition(agent);
            var agentList = agents.Where(u => u != agent).ToList();
            return ClosestAgentFromPosition(myPos, agentList);
        }

        /// <summary>Find the agent id of the closest agent to a position.</summary>
        /// <param name="pos">The agent where distance will be measured to</param>
        /// <param name="agents">The list of agents to find the closest</param>
        /// <returns>The closest agent</returns>
        public static RideID ClosestAgentFromPosition(RideVector3 pos, List<RideID> agentList)
        {
            RideID closestAgent = RideID.Null;
            float closest = float.MaxValue;
            foreach (RideID other in agentList)
            {
                RideVector3 otherPos = Globals.api.agentSystem.GetAgentPosition(other);
                float dist = (pos - otherPos).magnitude;
                if (closest > dist)
                {
                    closest = dist;
                    closestAgent = other;
                }
            }

            //if (closestAgent == RideID.Null)
            //    Debug.LogError("Couldn't find an agent close to " + agent);

            return closestAgent;
        }

        public static IEnumerable<RideID> GetAgentsInSight(RideID agent, Team team)
        {
            var losAgents = new List<RideID>();
            var enemySpatials = new List<ISpatialObject>();
            var others = Globals.api.scenarioSystem.GetAgents(team);
            foreach (var other in others)
                enemySpatials.Add(Globals.api.componentSystem.GetComponent<ISpatialObject>(other));

            var agentSpatial = Globals.api.componentSystem.GetComponent<ISpatialObject>(agent);
            var othersInView = RideUtils.GetObjectsInView(agentSpatial, enemySpatials);

            RideVector3 position = Globals.api.agentSystem.GetAgentPosition(agent);
            foreach (var otherInView in othersInView)
            {
                if (!RideUtils.IsObjectOccluded(position, otherInView))
                {
                    int index = enemySpatials.FindIndex(s => s == otherInView);
                    losAgents.Add(others.ElementAt(index));
                }
            }

            return losAgents;
        }

        #endregion

        #region Input

        /// <summary>
        /// Returns true during the frame when the user starts pressing the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool GetKeyDown(KeyCode key) => Input.GetKeyDown(key);

        /// <summary>
        /// Returns true whenever the user is pressing the key down
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool GetKey(KeyCode key) => Input.GetKey(key);

        /// <summary>
        /// Returns true whenever during the first frame that the user release the key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool GetKeyUp(KeyCode key) => Input.GetKeyUp(key);

        /// <summary>
        /// Tests if the mouse is over UI elements
        /// </summary>
        /// <returns>True if the mount is over a UI element, otherwise false</returns>
        static public bool IsMouseOverUI()
        {
            return UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        private static int s_imguiKeyboardControl;
        private static string s_imguiFocusedControlName;
        private static float s_lastImguiFocusUpdateTime = float.NegativeInfinity;

        /// <summary>
        /// Caches the current IMGUI keyboard focus state so it can be queried reliably from non-IMGUI update loops.
        /// Call this from any active <c>OnGUI()</c> path that should contribute IMGUI focus information for systems such as
        /// camera controllers or debug hotkey suppressors.
        /// </summary>
        public static void UpdateImguiFocus()
        {
            s_imguiKeyboardControl = GUIUtility.keyboardControl;
            s_imguiFocusedControlName = GUI.GetNameOfFocusedControl();
            s_lastImguiFocusUpdateTime = Time.unscaledTime;
        }

        /// <summary>
        /// Determines whether any UI input field currently has focus, such as a text box or other selectable input element.
        /// This is useful to prevent player input (e.g., movement, debug hotkeys) from being triggered while the user is typing.
        /// </summary>
        /// <remarks>
        /// This method checks both Unity UI (`Selectable`) elements selected by the current EventSystem, and IMGUI focus.
        /// It prefers recently cached IMGUI focus gathered from <see cref="UpdateImguiFocus"/>, and falls back to direct IMGUI
        /// queries when that cached data is stale. For named IMGUI elements, the control must be named with "input" in its
        /// control name for this method to return true.
        ///
        /// For IMGUI usage, call <see cref="UpdateImguiFocus"/> from an active <c>OnGUI()</c> path for the most reliable
        /// results. If you want named IMGUI controls to count as input, call <see cref="GUI.SetNextControlName(string)"/>
        /// with a name containing "input" before the relevant control is rendered.
        ///
        /// See: https://docs.unity3d.com/ScriptReference/GUI.GetNameOfFocusedControl.html
        /// </remarks>
        /// <returns>
        /// Returns <c>true</c> if a UI or IMGUI input control is currently focused; otherwise, <c>false</c>.
        /// </returns>
        public static bool DoesInputHaveFocus()
        {
            // Is there an input box that has focus anywhere in the system?
            // This is useful to determine if debug keys should be active,
            // or if player movement keys should be active.

            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                var selected = eventSystem.currentSelectedGameObject;
                if (selected != null && selected.GetComponent<UnityEngine.UI.Selectable>() != null)
                    return true;
            }

            const float ImguiFocusCacheDurationSeconds = 0.5f;

            // use cached variables if they were updated recently, otherwise query IMGUI directly.
            // This allows non-IMGUI update loops to have reliable focus information without depending on the timing of OnGUI calls,
            // while still allowing IMGUI focus to be detected for cases where UpdateImguiFocus is not called.
            int keyboardControl = s_imguiKeyboardControl;
            string focusedControl = s_imguiFocusedControlName;

            // Ignore stale cached IMGUI focus so inactive UI does not block input indefinitely.
            bool imguiFocusIsFresh = (Time.unscaledTime - s_lastImguiFocusUpdateTime) <= ImguiFocusCacheDurationSeconds;
            if (!imguiFocusIsFresh)
            {
                keyboardControl = GUIUtility.keyboardControl;
                focusedControl = GUI.GetNameOfFocusedControl();
            }

            if (keyboardControl != 0)
                return true;

            // you need to manually call GUI.SetNextControlName("input") before any element you want this to return true
            // see https://docs.unity3d.com/ScriptReference/GUI.GetNameOfFocusedControl.html
            if (!string.IsNullOrEmpty(focusedControl) &&
                focusedControl.IndexOf("input", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        #endregion

        #region Time and Scene Utilities

        /// <summary>
        /// The amount of time elapsed in seconds since last frame.
        /// </summary>
        /// <returns></returns>
        public static float GetDeltaTime() => Time.deltaTime;

        public static float GetSmoothDeltaTime() => Time.smoothDeltaTime;

        public static float GetTimeScale() => Time.timeScale;

        /// <summary>
        /// Get the number of frames since the start of the scene.
        /// </summary>
        /// <returns></returns>
        public static int GetFrameCount() => Time.frameCount;

        /// <summary>
        /// Get the number of seconds since the scene was loaded
        /// </summary>
        /// <returns></returns>
        public static float GetSecondsSinceSceneLoad() => Time.timeSinceLevelLoad;

        public static float GetRealtimeSinceStartup() => Time.realtimeSinceStartup;

        /// <summary>
        /// Returns the name of the current scene
        /// </summary>
        /// <returns></returns>
        public static string GetSceneName() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        /// <summary>
        ///
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public static void LoadScene(string sceneName) => UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

        /// <summary>
        ///
        /// </summary>
        /// <param name="sceneName"></param>
        /// <returns></returns>
        public static AsyncOperation LoadSceneAsync(string sceneName) => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

        /// <summary>
        /// Finds loaded objects of type <typeparamref name="T"/> using a Unity-version-compatible API.
        /// </summary>
        /// <typeparam name="T">The object type to search for.</typeparam>
        /// <returns>An array of matching loaded objects.</returns>
        public static T[] FindObjectsByType<T>() where T : UnityEngine.Object
        {
#if UNITY_6000_4_OR_NEWER
            return GameObject.FindObjectsByType<T>();
#else
            return GameObject.FindObjectsByType<T>(FindObjectsSortMode.None);
#endif
        }

        /// <summary>
        /// Finds loaded objects of type <typeparamref name="T"/> using a Unity-version-compatible API and inactive filter.
        /// </summary>
        /// <typeparam name="T">The object type to search for.</typeparam>
        /// <param name="findObjectsInactive">Whether inactive objects should be included in the results.</param>
        /// <returns>An array of matching loaded objects.</returns>
        public static T[] FindObjectsByType<T>(FindObjectsInactive findObjectsInactive) where T : UnityEngine.Object
        {
#if UNITY_6000_4_OR_NEWER
            return GameObject.FindObjectsByType<T>(findObjectsInactive);
#else
            return GameObject.FindObjectsByType<T>(findObjectsInactive, FindObjectsSortMode.None);
#endif
        }

        /// <summary>
        /// Returns a Unity object identifier as a <see cref="ulong"/> using a Unity-version-compatible API.
        /// </summary>
        /// <param name="gameObject">The Unity object to identify.</param>
        /// <returns>The object's Unity identifier represented as a <see cref="ulong"/>.</returns>
        public static ulong EntityIdToULong(UnityEngine.Object gameObject)
        {
#if UNITY_6000_4_OR_NEWER
            return EntityId.ToULong(gameObject.GetEntityId());
#else
            return unchecked((ulong)gameObject.GetHashCode());
#endif
        }

        #endregion

        #region System Information

        /// <summary>
        /// Return the name of the current device running RIDE. Removes '\<' and '\>' characters
        /// </summary>
        /// <returns>Device name</returns>
        public static string GetDeviceNameNormalized()
        {
            string name = SystemInfo.deviceName;
            if (string.IsNullOrEmpty(name))
                return "UnknownDevice";

            // Remove angle brackets (e.g., "<unknown>" on Android)
            name = name.Replace("<", "").Replace(">", "").Trim();
            if (name.Length == 0)
                return "UnknownDevice";

            // normalize case eg, "DEVICE" => "Device"
            name = char.ToUpper(name[0]) + name.Substring(1).ToLower();
            return name;
        }

        /// <summary>
        /// Returns the name of the user who is logged in. If not availabe, it uses GetDeviceNameNormalized
        /// </summary>
        /// <returns>User name</returns>
        public static string GetUserName() => string.IsNullOrEmpty(Environment.UserName) ? GetDeviceNameNormalized() : Environment.UserName;
        public static string GetOS() => SystemInfo.operatingSystem;
        public static int GetNumProcessors() => SystemInfo.processorCount;
        public static float GetSystemMemorySize() => SystemInfo.systemMemorySize / 1000.0f;
        public static string GetGraphicsDeviceName() => SystemInfo.graphicsDeviceName;
        public static int GetGraphicsMemorySize() => SystemInfo.graphicsMemorySize;
        public static bool IsEditor() => Application.isEditor;

        /// <summary>
        /// Returns whether the current platform is Windows 
        /// </summary>
        /// <returns>Boolean</returns>
        public static bool IsWindows()
        {
            return Application.platform == RuntimePlatform.WindowsPlayer ||
                   Application.platform == RuntimePlatform.WindowsEditor ||
                   Application.platform == RuntimePlatform.WindowsServer;
        }

        /// <summary>
        /// Returns whether the current platform is Mac OS 
        /// </summary>
        /// <returns>Boolean</returns>
        public static bool IsOSX()
        {
            return Application.platform == RuntimePlatform.OSXPlayer ||
                   Application.platform == RuntimePlatform.OSXEditor;
        }

        /// <summary>
        /// Returns whether the current platform is iOS
        /// </summary>
        /// <returns>Boolean</returns>
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

        /// <summary>
        /// Returns whether the current platform is Android 
        /// </summary>
        /// <returns>Boolean</returns>
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

        /// <summary>
        /// Returns whether the current platform is WebGL 
        /// </summary>
        /// <returns>Boolean</returns>
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

        /// <summary>
        /// Returns whether the current platform is Univeral Windows Platform (UWP) 
        /// </summary>
        /// <returns>Boolean</returns>
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

        /// <summary>
        /// Returns whether the current platform is Linux 
        /// </summary>
        /// <returns>Boolean</returns>
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

        /// <summary>
        /// Returns whether RIDE is runnign headless 
        /// </summary>
        /// <returns>Boolean</returns>
        public static bool IsHeadless() => SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null;

        /// <summary>
        /// Returns whether RIDE is running on a dedicated Unity server 
        /// </summary>
        /// <returns>Boolean</returns>
        public static bool IsDedicatedServer()
        {
#if UNITY_SERVER
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// Get the current width of the screen in pixels
        /// </summary>
        /// <returns></returns>
        public static int GetResolutionWidth() => Screen.width;

        /// <summary>
        /// Get the current height of the screen in pixels
        /// </summary>
        /// <returns></returns>
        public static int GetResolutionHeight() => Screen.height;

        /// <summary>
        /// Finds the index of the current screen resolution from <see cref="Screen.resolutions"/>,
        /// matching width, height, and refresh rate within a specified tolerance.
        /// </summary>
        /// <param name="refreshRateTolerance">Tolerance (in Hz) for matching refresh rates.</param>
        /// <returns>Index of the closest matching resolution; returns 0 if no match found.</returns>
        public static int FindCurrentResolutionIndex(float refreshRateTolerance = 2.0f)
        {
            var current = Screen.currentResolution;
            for (int i = 0; i < Screen.resolutions.Length; i++)
            {
                var res = Screen.resolutions[i];
                bool resolutionMatch = res.width == current.width &&
                                       res.height == current.height;
#if UNITY_2022_2_OR_NEWER
                bool refreshMatch = Math.Abs(res.refreshRateRatio.value - current.refreshRateRatio.value) < refreshRateTolerance;
#else
                bool refreshMatch = Math.Abs((float)Screen.resolutions[i].refreshRate - Screen.currentResolution.refreshRate) < 2;
#endif

                if (resolutionMatch && refreshMatch)
                    return i;
            }

            return 0; // Fallback to default if no match
        }

        private static readonly (float ratio, string label)[] m_knownAspectRatios =
        {
            (1.0f,      "1:1"),
            (1.25f,     "5:4"),
            (1.3333f,   "4:3"),
            (1.5f,      "3:2"),
            (1.6f,      "16:10"),
            (1.6667f,   "5:3"),
            (1.7778f,   "16:9"),
            (2.0556f,   "37:18"),
            (2.1667f,   "19.5:9"),
            (2.3889f,   "21:9"),

            // reverse
            (0.8f,      "4:5"),
            (0.75f,     "3:4"),
            (0.6667f,   "2:3"),
            (0.625f,    "10:16"),
            (0.6f,      "3:5"),
            (0.5625f,   "9:16"),
            (0.4865f,   "18:37"),
            (0.4615f,   "9:19.5"),
            (0.4186f,   "9:21")
        };

        /// <summary>
        /// Returns a human-readable label (e.g., "16:9", "4:3") for a known aspect ratio
        /// that approximately matches the given value.
        /// <para/>
        /// The match is based on a fixed tolerance and includes both standard and reversed ratios.
        /// <para/>
        /// See: https://en.wikipedia.org/wiki/Display_resolution#Aspect_ratios
        /// See: https://en.wikipedia.org/wiki/List_of_common_resolutions
        /// </summary>
        /// <param name="aspectRatio">The width-to-height ratio (e.g., 1920f / 1080f)</param>
        /// <returns>A label such as "16:9" if matched, or "Unknown" if no close match is found.</returns>
        public static string GetCommonAspectText(float aspectRatio)
        {
            const float tolerance = 0.04f;

            foreach (var (ratio, label) in m_knownAspectRatios)
                if (Mathf.Abs(aspectRatio - ratio) < tolerance)
                    return label;

            return "Unknown";
        }

        /// <summary>
        /// Scales a normalized Rect (0-1 range) to screen pixel dimensions using <see cref="Screen.width"/> and <see cref="Screen.height"/>.
        /// </summary>
        /// <param name="r">A Rect in normalized screen-space (0-1)</param>
        /// <returns>A Rect scaled to pixel resolution</returns>
        public static Rect ScaleToRes(Rect r) =>
            new Rect(r.x * Screen.width, r.y * Screen.height, r.width * Screen.width, r.height * Screen.height);

        /// <summary>
        /// Returns the local IPv4 address of the machine. If not available, returns 127.0.0.1.
        /// Returns loopback for WebGL or if no suitable address is found.
        /// </summary>
        /// <returns>The resolved local IP address, or 127.0.0.1 as fallback.</returns>
        public static string GetLocalIpAddress()
        {
            const string defaultIP = "127.0.0.1";

            if (IsWebGL())
                return defaultIP;

            // on some windows machines, an exception is thrown.  Unsure why.  For now, just catch it and move on, not important.
            try
            {
                foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch (System.Net.Sockets.SocketException e)
            {
                Debug.LogWarning($"DebugInfo.Awake() - SocketException caught when trying to resolve ip address: {e}");
            }

            return defaultIP;
        }

        #endregion

        #region Application Control

        /// <summary>
        /// Open a website using the given url in your default browser
        /// </summary>
        /// <param name="url"></param>
        static public void OpenURL(string url) => Application.OpenURL(url);

        /// <summary>
        /// Exit the application
        /// </summary>
        static public void QuitApplication()
        {
            if (Application.isEditor)
            {
#if UNITY_EDITOR
#if UNITY_6000_0_OR_NEWER
                UnityEditor.EditorApplication.ExitPlaymode();
#else
                UnityEditor.EditorApplication.ExecuteMenuItem("Edit/Play");
#endif
#endif
            }
            else
            {
                Application.Quit();
            }
        }

        /// <summary>
        /// Returns true if the application is playing
        /// </summary>
        /// <returns>Returns true if the application is playing</returns>
        static public bool IsApplicationPlaying() => Application.isPlaying;

        /// <summary>
        /// Returns true if you are using a debug/development build
        /// </summary>
        /// <returns>true if you are using a debug/development build</returns>
        public static bool IsDebugBuild() => Debug.isDebugBuild;

        /// <summary>
        /// Loads the specified scene by name using Unity's <see cref="UnityEngine.SceneManagement.SceneManager.LoadScene(string)"/>.
        /// <para/>
        /// See: https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html
        /// </summary>
        public static void SceneManagerLoadScene(string sceneName) => UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);

        /// <summary>
        /// Loads the specified scene by build index using Unity's <see cref="UnityEngine.SceneManagement.SceneManager.LoadScene(int)"/>.
        /// <para/>
        /// See: https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadScene.html
        /// </summary>
        public static void SceneManagerLoadScene(int sceneBuildIndex) => UnityEngine.SceneManagement.SceneManager.LoadScene(sceneBuildIndex);

        /// <summary>
        /// Asynchronously loads the specified scene by name using Unity's <see cref="UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(string)"/>.
        /// <para/>
        /// See: https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html
        /// </summary>
        public static AsyncOperation SceneManagerLoadSceneAsync(string sceneName) => UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName);

        /// <summary>
        /// Returns the name of the currently active scene using Unity's <see cref="UnityEngine.SceneManagement.SceneManager.GetActiveScene"/>.
        /// <para/>
        /// See: https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.GetActiveScene.html
        /// </summary>
        public static string SceneManagerActiveSceneName() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        /// <summary>
        /// Returns the name of the currently active scene in the Unity Editor using <see cref="UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene"/>.
        /// Returns an empty string outside of the editor.
        /// <para/>
        /// See: https://docs.unity3d.com/ScriptReference/EditorSceneManager.GetActiveScene.html
        /// </summary>
        public static string EditorSceneManagerActiveSceneName()
        {
#if UNITY_EDITOR
            return UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
#else
            return "";
#endif
        }

        #endregion

        #region Mesh Validation

        /// <summary>
        /// Checks if all mesh triangles lie on the same plane.
        /// </summary>
        /// <param name="mesh">The mesh to be checked.</param>
        /// <returns>Returns true if all mesh triangles lie on the same plane.</returns>
        public static bool IsMeshCoplanar(Mesh mesh)
        {
            var vertices = mesh.vertices; //mesh.vertices creates a new array every time called so lets cache it.
            var triangles = mesh.triangles;
            Plane plane = new Plane(vertices[triangles[0]], vertices[triangles[1]], vertices[triangles[2]]);
            foreach (var vert in vertices)
            {
                if (plane.ClosestPointOnPlane(vert) != vert)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if mesh has at least four distinct vertices.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="go"></param>
        /// <returns>True if mesh has at least four distinct vertices. False otherwise.</returns>
        public static bool IsMeshValid(Mesh mesh) => new HashSet<Vector3>(mesh.vertices).Count >= 4;

        public static bool IsMeshClean(Mesh mesh)
        {
            var vertices = mesh.vertices;  // mesh.vertices creates a new array so cache it.
            var triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];

                if (vertices[a] == vertices[b] || vertices[a] == vertices[c] || vertices[b] == vertices[c])
                {
                    //Debug.LogWarning("mesh is not clean");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Preferences

        /// <summary>
        /// Sets a preference on the local machine
        /// </summary>
        /// <param name="pref">The preference name</param>
        /// <param name="value">The valye of the preference</param>
        public static void SetPreference(string pref, bool value) => PlayerPrefs.SetInt(pref, value ? 1 : 0);

        /// <summary>
        /// Sets a preference on the local machine
        /// </summary>
        /// <param name="pref">The preference name</param>
        /// <param name="value">The valye of the preference</param>
        public static void SetPreference(string pref, int value) => PlayerPrefs.SetInt(pref, value);

        /// <summary>
        /// Sets a preference on the local machine
        /// </summary>
        /// <param name="pref">The preference name</param>
        /// <param name="value">The valye of the preference</param>
        public static void SetPreference(string pref, string value) => PlayerPrefs.SetString(pref, value);

        /// <summary>
        /// Gets a preference setting from the local machine
        /// </summary>
        /// <returns></returns>
        public static bool GetBoolPreference(string pref) => PlayerPrefs.GetInt(pref) != 0;

        /// <summary>
        /// Gets a preference from the local machine
        /// </summary>
        /// <param name="pref">The preference name</param>
        /// <returns></returns>
        public static string GetPreference(string pref) => PlayerPrefs.GetString(pref);

        #endregion

        #region Physics and Ballistics

        /// <summary>
        /// Applies one integration step using <see href="https://en.wikipedia.org/wiki/Heun%27s_method">Heun's method</see>
        /// (improved Euler method) to estimate the next position and velocity of a projectile.
        /// 
        /// This method averages the initial and predicted derivatives to reduce integration error,
        /// and assumes constant acceleration (e.g., gravity). Designed for use in basic ballistic motion.
        /// </summary>
        /// <param name="h">Timestep in seconds</param>
        /// <param name="currentPosition">The current world-space position of the projectile</param>
        /// <param name="currentVelocity">The current velocity vector of the projectile</param>
        /// <param name="projectileMass">Mass of the projectile (currently unused unless drag is added)</param>
        /// <param name="newPosition">Output: estimated next position</param>
        /// <param name="newVelocity">Output: estimated next velocity</param>
        public static void Heuns(float h, RideVector3 currentPosition, RideVector3 currentVelocity, float projectileMass, out RideVector3 newPosition, out RideVector3 newVelocity)
        {
            // NOTE: projectileMass is currently unused; reserved for future drag support

            //Init Acceleration and Gravity
            RideVector3 accelerationFactorEuler = Physics.gravity;
            RideVector3 accelerationFactorHuen = Physics.gravity;

            //Current Velocity
            RideVector3 velocityFactor = currentVelocity;

            ///COULD ADD WIND VELOCITY HERE IF YOU WANTED TO///
            //Wind Velocity
            //velocityFactor += new RideVector3(2f, 0f, 3f);

            //Euler Forward
            RideVector3 pos_E = currentPosition + velocityFactor * h;

            //accelerationFactorEuler += BallisticTrajectory.CalculateDrag(currentVelocity, projectileMass);

            RideVector3 vel_E = currentVelocity + accelerationFactorEuler * h;

            //Heuns Method
            RideVector3 pos_H = currentPosition + (velocityFactor + vel_E) * h * 0.5f;

            //accelerationFactorHuen += BallisticTrajectory.CalculateDrag(vel_E, projectileMass);

            RideVector3 vel_H = currentVelocity + (accelerationFactorEuler + accelerationFactorHuen) * h * 0.5f;

            newPosition = pos_H;
            newVelocity = vel_H;
        }

        #endregion

        #region General Utilities

        /// <summary>
        /// a becomes b and b becomes a
        /// </summary>
        /// <typeparam name="T">The type of the values to swap</typeparam>
        /// <param name="a">Value a</param>
        /// <param name="b">Value b</param>
        public static void Swap<T>(ref T a, ref T b)
        {
// ignore warning, for this generic function, to prevent against undefined behavior
#pragma warning disable IDE0180  // Use tuple to swap values
            T temp = a;
            a = b;
            b = temp;
#pragma warning restore IDE0180
        }

        #endregion
    }
}
