using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using UnityEngine;
using UnityEngine.Networking;

namespace VHAssets
{
    /// <summary>
    /// BMLParser is responsible for reading BML and XML input, parsing character behavior tags,
    /// and creating CutsceneEvents for playback. Supports SmartBody and Mecanim event types.
    /// </summary>
    /// <remarks>
    /// Supports syncing gestures to speech via timing markers and animation sync points.
    /// Handles gesture, speech, gaze, face, saccade, posture, and custom tags.
    /// Use LoadXMLString or LoadFile to initiate parsing.
    /// </remarks>
    public class BMLParser
    {
        #region Constants
        static readonly string[] EventXmlNames =
        {
            "sbm:animation",
            "animation",
            "gaze",
            "head",
            "saccade",
            "face",
            "text",
            "event",
            "sbm:event",
            "speech",
            "gesture",
            "body"
        };

        const string start = "start";
        const string stroke = "stroke";
        const string relax = "relax";
        const string end = "end";

        //const string Speech = "speech";
        const string Participant = "participant";

        class PendingSyncEvent
        {
            public CutsceneEvent ce;
            public string syncPointName;
            public string timing;

            public PendingSyncEvent(CutsceneEvent _ce, string _syncPointName, string _timing) { ce = _ce; syncPointName = _syncPointName; timing = _timing; }
        }

        [Serializable]
        public class BMLTiming
        {
            public string id;
            public float time;
            public string text;

            public BMLTiming(string _id, float _time, string _text) { id = _id; time = _time; text = _text; }
        }

        [Serializable]
        public class LipData
        {
            public string viseme = "";
            public float articulation = 1.0f;
            public float startTime;
            public float readyTime;
            public float relaxTime;
            public float endTime;

            public LipData(string _viseme, float _articulation, float _startTime, float _readyTime, float _relaxTime, float _endTime) { viseme = _viseme; articulation = _articulation; startTime = _startTime; readyTime = _readyTime; relaxTime = _relaxTime; endTime = _endTime; }
        }

        [Serializable]
        public class CurveData
        {
            public class SlopeData
            {
                public float mr;
                public float dr;
                public float ml;
                public float dl;

                public SlopeData(float _ml, float _mr, float _dl, float _dr) { ml = _ml; mr = _mr; dl = _dl; dr = _dr; }
            }

            public readonly string name = ""; // i.e. BMP
            public readonly string owner = "";
            public readonly int numKeys = 0;
            public readonly Quaternion[] curveKeys;
            public readonly SlopeData[] m_SlopeData;

            public CurveData(string _name, string _owner, int _numKeys)
            {
                name = _name;
                owner = _owner;
                numKeys = _numKeys;

                if (numKeys > 0)
                {
                    curveKeys = new Quaternion[numKeys];
                    m_SlopeData = new SlopeData[numKeys];
                }
            }

            public void Set(int keyIndex, float _ml, float _mr, float _dl, float _dr) => m_SlopeData[keyIndex] = new SlopeData(_ml, _mr, _dl, _dr);

            public SlopeData GetSlopeData(int key)
            {
                if (key < 0 || key > numKeys)
                {
                    Debug.LogError($"Bad Key {key} for viseme {name}");
                    return null;
                }

                return m_SlopeData[key];
            }

            public void AddKey(Quaternion key, int keyIndex)
            {
                if (keyIndex < 0 || keyIndex >= numKeys)
                {
                    Debug.LogError($"bad keyIndex {keyIndex}. Has to be in range 0-{numKeys - 1}");
                    return;
                }

                curveKeys[keyIndex] = key;
            }

            public void AddKey(float time, float articulation, int keyIndex) => AddKey(new Quaternion(time, articulation, 0, 0), keyIndex);

            public float GetTime(int key)
            {
                if (key < 0 || key > numKeys)
                {
                    Debug.LogError($"Bad Key {key} for viseme {name}");
                    return 0;
                }

                return curveKeys[key].x;
            }

            public float GetArticulation(int key)
            {
                if (key < 0 || key > numKeys)
                {
                    Debug.LogError($"Bad Key {key} for viseme {name}");
                    return 0;
                }

                return curveKeys[key].y;
            }

            public float GetSlopeIn(int key)
            {
                if (key < 0 || key > numKeys)
                {
                    Debug.LogError($"Bad Key {key} for viseme {name}");
                    return 0;
                }

                return curveKeys[key].z;
            }

            public float GetSlopeOut(int key)
            {
                if (key < 0 || key > numKeys)
                {
                    Debug.LogError($"Bad Key {key} for viseme {name}");
                    return 0;
                }

                return curveKeys[key].w;
            }

            public float GetSpan() => numKeys > 1 ? curveKeys[numKeys - 1].x - curveKeys[0].x : 0;

            public void SortKeysByTime() => Array.Sort(curveKeys, (a, b) => a.x < b.x ? -1 : 1);

            public void PrintCurve()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append($"Curve Name {name}. Num Keys {numKeys} ");
                for (int i = 0; i < numKeys; i++)
                    builder.Append($" Time {GetTime(i)} Weight {GetArticulation(i)} ");

                Debug.Log(builder.ToString());
            }
        }

        public delegate void OnParsedBMLTiming(/*string id, float time, string text*/BMLTiming bmlTiming);
        public delegate void OnParsedVisemeTiming(LipData lipData);
        public delegate void OnParsedCurveData(CurveData curveData);
        public delegate void OnParsedBMLEvent(XmlTextReader reader, string eventType, CutsceneEvent ce);
        public delegate void OnFinishedReading(bool succeeded, List<CutsceneEvent> createdEvents);
        public delegate void OnReadBMLFile(string bmlFileName);
        public delegate void OnParsedCustomEvent(XmlTextReader reader);
        #endregion

        #region Variables
        OnParsedBMLTiming m_ParsedBMLTimingCB;
        OnParsedVisemeTiming m_ParsedVisemeTimingCB;
        OnParsedCurveData m_ParsedCurveDataCB;
        OnParsedBMLEvent m_ParsedBMLEventCB;
        OnFinishedReading m_FinishedReadingCB;
        OnReadBMLFile m_ReadBmlFileCB;
        OnParsedCustomEvent m_ParsedCustomEventCB;
        List<PendingSyncEvent> m_PendingSyncEvents = new();
        List<CutsceneEvent> m_CreatedEvents = new();

        static List<BMLTiming> m_BMLTimings = new();

        AudioSpeechFile m_Utterance;
        string m_LoadPath = "";
        string m_Character = "";
        string m_SpeechId = "";
        bool m_ReadBMLFile;
        bool m_TrimBMLTimingWhenParsing = true;
        string m_CachedXml = "";
        string m_EventCategoryName = GenericEventNames.SmartBody;
        string m_LoadPathSubFolder = "";

        string lastGestureName = "";
        string currentGestureName = "";
        float lastEventTime = 0;
        //float timeThreshold = 2.0f;
        #endregion

        #region Properties
        public string EventCategoryName
        {
            get { return m_EventCategoryName; }
            set
            {
                m_EventCategoryName = value;
                if (m_EventCategoryName != GenericEventNames.SmartBody && m_EventCategoryName != GenericEventNames.Mecanim)
                    m_EventCategoryName = GenericEventNames.SmartBody;
            }
        }

        public bool IgnoreSpeechEvent => m_Utterance != null;
        public string LoadPathSubFolder { get => m_LoadPathSubFolder; set => m_LoadPathSubFolder = value; }
        public bool TrimBMLTimingWhenParsing { get => m_TrimBMLTimingWhenParsing; set => m_TrimBMLTimingWhenParsing = value; }
        #endregion


        #region Constructors

        public BMLParser(OnParsedBMLTiming parsedBMLTimingCB, OnParsedVisemeTiming parsedVisemeTimingCB, OnParsedBMLEvent parsedBMLEventCB, OnFinishedReading finishedReadingCB, OnParsedCustomEvent parsedCustomEventCB)
        {
            m_ParsedBMLTimingCB = parsedBMLTimingCB;
            m_ParsedVisemeTimingCB = parsedVisemeTimingCB;
            m_ParsedBMLEventCB = parsedBMLEventCB;
            m_FinishedReadingCB = finishedReadingCB;
            m_ParsedCustomEventCB = parsedCustomEventCB;
        }

        public BMLParser(OnParsedBMLTiming parsedBMLTimingCB, OnParsedVisemeTiming parsedVisemeTimingCB, OnParsedCurveData parsedCurveDataCB)
        {
            m_ParsedBMLTimingCB = parsedBMLTimingCB;
            m_ParsedVisemeTimingCB = parsedVisemeTimingCB;
            m_ParsedCurveDataCB = parsedCurveDataCB;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Parses an XML string containing character behavior markup for a specific character.
        /// </summary>
        /// <param name="character">Name of the character (used to find GameObject and sync animation timing).</param>
        /// <param name="xmlStr">XML string representing the BML/SmartBody markup.</param>
        /// <returns>True if parsing succeeded, false otherwise.</returns>
        /// <remarks>
        /// If an AudioSpeechFile is present, speech events will be ignored during parsing.
        /// Automatically invokes callbacks upon completion.
        /// </remarks>
        public bool LoadXMLString(string character, string xmlStr)
        {
            m_Character = character;
            m_ReadBMLFile = !IgnoreSpeechEvent;
            m_CachedXml = xmlStr;

            ClearParsingState();

    #if !UNITY_WSA
            bool succeeded = true;
            XmlTextReader reader = null;

            try
            {
                reader = CreateXmlTextReader(xmlStr);
                ParseBMLEvents(reader);
            }
            catch (Exception e)
            {
                succeeded = false;
                Debug.LogError($"LoadXMLString() - Failed to parse XML. Error: {e.Message} {e.InnerException}. stack: {e.StackTrace}");
            }
            finally
            {
                reader?.Close();
            }

            FinishedReadingXML(succeeded);
            return succeeded;
    #else
            Debug.LogError("BMLParser.LoadXMLString() - not implemented on this platform.");
            return false;
    #endif
        }

        /// <summary>
        /// Adjusts a start time using a named sync point embedded within an animation clip.
        /// </summary>
        /// <param name="startTime">The original start time of the event.</param>
        /// <param name="syncPoint">The name of the sync point to align with (e.g., "strokeStartTime").</param>
        /// <param name="characterName">The name of the character GameObject in the scene.</param>
        /// <param name="animName">The name of the animation clip to search for the sync point.</param>
        /// <returns>Start time adjusted by the sync point time, or original time if not found.</returns>
        /// <remarks>
        /// If no matching clip or sync point is found, logs a warning and returns the original start time.
        /// </remarks>
        public static float OffsetStartTimeBySyncPoint(float startTime, string syncPoint, string characterName, string animName)
        {
            if (string.IsNullOrEmpty(syncPoint))
                return startTime;

            if (string.IsNullOrEmpty(animName))
            {
                Debug.LogWarning($"OffsetStartTimeBySyncPoint() - animName is null for syncPoint: {syncPoint}");
                return startTime;
            }

            // Attempt to locate the target animation clip
            var characterObj = GameObject.Find(characterName);
            if (characterObj == null)
            {
                Debug.LogWarning($"OffsetStartTimeBySyncPoint() - Could not find GameObject for character: {characterName}");
                return startTime;
            }

            var animator = characterObj.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning($"OffsetStartTimeBySyncPoint() - No Animator component on character: {characterName}");
                return startTime;
            }

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
            {
                Debug.LogWarning($"OffsetStartTimeBySyncPoint() - Animator has no controller for character: {characterName}");
                return startTime;
            }

            var animClips = controller.animationClips;
            if (animClips == null || animClips.Length == 0)
            {
                Debug.LogWarning($"OffsetStartTimeBySyncPoint() - No animation clips found for character: {characterName}");
                return startTime;
            }

            foreach (var clip in animClips)
            {
                if (clip == null || !string.Equals(clip.name, animName, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var evt in clip.events)
                {
                    if (evt == null || !string.Equals(evt.functionName, syncPoint, StringComparison.OrdinalIgnoreCase))
                        continue;

                    float offset = evt.time;
                    float result = startTime - offset;

                    Debug.Log($"OffsetStartTimeBySyncPoint() - Found syncPoint '{syncPoint}' in anim '{clip.name}' at time {offset:F3}, originalStart={startTime:F3}, result={result:F3}");

                    return result;
                }
            }

            Debug.LogWarning($"OffsetStartTimeBySyncPoint() - Could not find syncPoint '{syncPoint}' in anim '{animName}' for character '{characterName}'");

            return startTime;
        }

        #endregion

        #region File Loaders

        /// <summary>
        /// Loads a file from disk and parses its contents as either XML or BML based on extension.
        /// </summary>
        /// <param name="filePathAndName">Full or relative path to the .xml or .bml file.</param>
        /// <returns>True if the file was successfully loaded and parsed.</returns>
        /// <remarks>
        /// Supports both XML event structure and BML sync timing formats.  
        /// If the file extension is .bml or .bml.txt, it is parsed as sync data only.
        /// </remarks>
        public bool LoadFile(string filePathAndName)
        {
            string fileExt = Path.GetExtension(filePathAndName).ToLower();

            // if we're given a path that contains .bml and the file doesn't exist,
            // check to see if .bml.txt exists and use that
            if (!File.Exists(filePathAndName))
            {
                if (fileExt == ".bml" && File.Exists(filePathAndName + ".txt"))
                    filePathAndName += ".txt";
                else
                    return false;
            }

            bool success = false;
            if (fileExt == ".xml" || filePathAndName.ToLower().EndsWith(".xml.txt"))
                success = LoadXMLFile(filePathAndName);
            else if (fileExt == ".bml" || filePathAndName.ToLower().EndsWith(".bml.txt"))
                success = LoadBMLFile(filePathAndName);
            else
                Debug.LogError($"Couldn't load {filePathAndName} because it's not a supported file extension");

            return success;
        }

        /// <summary>
        /// Loads an XML file from disk and parses it as a character event sequence.
        /// </summary>
        /// <param name="filePathAndName">Path to the XML file.</param>
        /// <returns>True if the file was found and parsed without error.</returns>
        bool LoadXMLFile(string filePathAndName)
        {
            m_LoadPath = filePathAndName;
            bool succeeded = true;

    #if !UNITY_WSA
            m_ReadBMLFile = !IgnoreSpeechEvent;
            FileStream xml = null;
            XmlTextReader reader = null;

            try
            {
                xml = new FileStream(filePathAndName, FileMode.Open, FileAccess.Read);
                reader = new XmlTextReader(xml);
                ParseBMLEvents(reader);
            }
            catch (Exception e)
            {
                succeeded = false;
                Debug.LogError($"Failed when loading {filePathAndName}. Error: {e.Message} {e.InnerException}");
            }
            finally
            {
                xml?.Close();
                reader?.Close();
            }

            FinishedReadingXML(succeeded);
    #else
            Debug.LogError($"BMLParser.LoadXMLFile() - not implemented on this platform.");
    #endif

            return succeeded;
        }

        /// <summary>
        /// Read a bml file, internal only
        /// </summary>
        /// <param name="filePathAndName"></param>
        /// <returns></returns>
        bool LoadBMLFile(string filePathAndName)
        {
            bool succeeded = true;

    #if !UNITY_WSA
            FileStream xml = null;
            XmlTextReader reader = null;
            try
            {
                xml = new FileStream(filePathAndName, FileMode.Open, FileAccess.Read);
                reader = new XmlTextReader(xml);
                ReadBML(reader);
            }
            catch (Exception e)
            {
                succeeded = false;
                Debug.LogError($"Failed when loading {filePathAndName}. Error: {e.Message}");
            }
            finally
            {
                xml?.Close();
                reader?.Close();

                FinishedReadingBML(succeeded);
            }
    #else
            Debug.LogError($"BMLParser.LoadBMLFile() - not implemented on this platform.");
    #endif

            return succeeded;
        }

        /// <summary>
        /// Loads and parses both XML and BML strings for a character.
        /// </summary>
        /// <param name="character">Name of the character to assign to parsed events.</param>
        /// <param name="xmlStr">XML content representing event structure and tags.</param>
        /// <param name="bmlStr">BML content used to extract sync markers (e.g., visemes, syncs).</param>
        /// <returns>True if XML parsing succeeded; BML parsing always runs first.</returns>
        public bool LoadXMLBMLStrings(string character, string xmlStr, string bmlStr)
        {
            LoadBMLString(bmlStr, false);
            return LoadXMLString(character, xmlStr);
        }

        public bool LoadXMLBMLStrings(string character, AudioSpeechFile speech)
        {
            m_Utterance = speech;
            return LoadXMLBMLStrings(character, speech.ConvertedXml, speech.BmlText);
        }

        /// <summary>
        /// Parses BML timing data from a raw string input.
        /// </summary>
        /// <param name="bmlStr">BML-formatted string.</param>
        /// <returns>True if parsing succeeded.</returns>
        /// <remarks>
        /// Intended for cases where sync markers are provided separately from XML content.
        /// </remarks>
        public bool LoadBMLString(string bmlStr) => LoadBMLString(bmlStr, true);

        public bool LoadBMLString(string bmlStr, bool skipBOM)
        {
            bool succeeded = true;

    #if !UNITY_WSA
            XmlTextReader reader = null;
            StringReader bml = null;

            try
            {
                bml = new StringReader(bmlStr);
                if (skipBOM)
                    bml.Read(); // skip BOM see this link for more detail: http://answers.unity3d.com/questions/10904/xmlexception-text-node-canot-appear-in-this-state.html

                reader = new XmlTextReader(bml);
                ReadBML(reader);
            }
            catch (Exception e)
            {
                succeeded = false;
                Debug.LogError($"Failed when loading. Error: {e.Message} {e.InnerException}. bmlStr {bmlStr}");
            }
            finally
            {
                bml?.Close();
                reader?.Close();

                FinishedReadingBML(succeeded);
            }
    #else
            Debug.LogError($"BMLParser.LoadBMLString() - not implemented on this platform.");
    #endif

            return succeeded;
        }

        /// <summary>
        /// Creates and returns a configured XmlTextReader from a raw XML string.
        /// </summary>
        /// <param name="xml">Raw XML input string.</param>
        /// <returns>XmlTextReader positioned at the start of the document.</returns>
        /// <remarks>
        /// Normalizes encoding and trimming options to improve cross-platform compatibility.
        /// </remarks>
        XmlTextReader CreateXmlTextReader(string xmlStr)
        {
            var stringReader = new StringReader(xmlStr);
            return new XmlTextReader(stringReader);
        }

        IEnumerator WaitForBML(UnityWebRequest www)
        {
            while (!www.isDone) { yield return new WaitForEndOfFrame(); Debug.Log("still waiting"); }
            //Debug.Log("www.text: " + www.text);
            LoadBMLString(www.downloadHandler.text);
        }

        void FinishedReadingBML(bool succeeded)
        {
            if (string.IsNullOrEmpty(m_LoadPath))
            {
                // no xml is associated, but we still want to tell the receiver that we're done
                m_FinishedReadingCB?.Invoke(succeeded, m_CreatedEvents);

                //foreach (var cutsceneevent in m_CreatedEvents)
                //    Debug.Log($"FinishedReadingBML() - {cutsceneevent.Name} - {cutsceneevent.EventType} - {cutsceneevent.FunctionName} - {cutsceneevent.StartTime} - {cutsceneevent.Length}");
            }
        }

        #endregion

        #region Internal Parsing Pipeline

        /// <summary>
        /// Reads the xml file line by line and creates events based off the node type listed in EventXmlNames
        /// </summary>
        /// <param name="reader"></param>
        void ParseBMLEvents(XmlTextReader reader)
        {
            // 8/1/2022 - EDF - If we are doing pre-recorded, it should be loading from the scene, or via Resources.Load(),
            // not downloading from a url.  However, this block may still be needed at some point in the future, in some refactored form.
    #if false
            if (VHUtils.IsWebGL())
            {
                // First we need to check if a BML file has to be loaded in order to find timing markers for events in the xml
                if (m_ReadBMLFile && !m_BMLFileHasBeenRead)
                {
                    while (reader.Read())
                    {
                        switch (reader.Name)
                        {
                            case "speech":
                                m_SpeechId = reader["id"];
                                m_LoadPath =  reader["ref"];
                                var url = string.Format("https://example.com/vhweb/Sounds/{0}", Path.ChangeExtension(m_LoadPath, ".bml"));
                                UnityWebRequest www = UnityWebRequest.Get(url);
                                www.SendWebRequest();
                                GameObject.Find("GenericEvents").GetComponent<MonoBehaviour>().StartCoroutine(WaitForBML(www));
                                return;
                        }
                    }

                    // if you've gotten this far, the reader needs to be reset because it didn't find any speech
                    reader.Close();
                    xml = new StringReader(m_CachedXml);
                    reader = new XmlTextReader(xml);
                }
            }
    #endif

            lastGestureName = "";
            currentGestureName = "";
            lastEventTime = 0;

            while (reader.Read())
            {
                if (reader.NodeType != XmlNodeType.Element)
                    continue;

                string tag = reader.Name.ToLower();

                if (IsKnownEventType(tag))
                    CreateEvent(reader, reader.Name);
                else if (tag == Participant)
                    AssignParticipantId(reader);
                else
                    m_ParsedCustomEventCB?.Invoke(reader);
            }
        }

        void FinishedReadingXML(bool succeeded)
        {
            //foreach (var cutsceneevent in m_CreatedEvents)
            //    Debug.Log($"FinishedReadingXML() before ResolvePendingSyncEvent() - {cutsceneevent.Name} - {cutsceneevent.EventType} - {cutsceneevent.FunctionName} - {cutsceneevent.StartTime} - {cutsceneevent.Length}");

            // handled the pending events first
            m_PendingSyncEvents.ForEach(c => ResolvePendingSyncEvent(c));

            //foreach (var cutsceneevent in m_CreatedEvents)
            //    Debug.Log($"FinishedReadingXML() after ResolvePendingSyncEvent() - {cutsceneevent.Name} - {cutsceneevent.EventType} - {cutsceneevent.FunctionName} - {cutsceneevent.StartTime} - {cutsceneevent.Length}");

            // then do the callback
            m_FinishedReadingCB?.Invoke(succeeded, m_CreatedEvents);

            //foreach (var cutsceneevent in m_CreatedEvents)
            //    Debug.Log($"FinishedReadingXML() after m_FinishedReadingCB - {cutsceneevent.Name} - {cutsceneevent.EventType} - {cutsceneevent.FunctionName} - {cutsceneevent.StartTime} - {cutsceneevent.Length}");

            // now reset all the data
            m_Utterance = null;
            m_BMLTimings.Clear();
            m_PendingSyncEvents.Clear();
            m_CreatedEvents.Clear();
            m_Character = string.Empty;
            m_ReadBMLFile = false;
            //m_CachedXml = string.Empty;
            m_LoadPath = "";
        }

        /// <summary>
        /// Called after all events have been read from the xml file. Handles timing adjustments
        /// for events that are timed based off of other events in the xml file
        /// </summary>
        /// <param name="cache"></param>
        void ResolvePendingSyncEvent(PendingSyncEvent cache)
        {
            // typical format stroke=[event name]:start+[time offset]
            string[] colonSplit = cache.timing.Split(':');
            if (colonSplit.Length != 2)
                return;

            #if false
            bool useMathOperation = true;
            string[] mathOpSplit = colonSplit[1].Split('+');
            if (mathOpSplit.Length != 2)
            {
                mathOpSplit = colonSplit[1].Split('-');
                if (mathOpSplit.Length != 2)
                {
                    useMathOperation = false;
                }
            }
            #endif

            string eventSyncPointName = cache.syncPointName;
            string parentSyncPointName = colonSplit[1];

            //m_CreatedEvents.ForEach(ce => Debug.Log($"ResolvePendingSyncEvent() created events: {ce.Name} - {ce.StartTime} - {ce.Length}"));

            // the name of the event is the first half
            CutsceneEvent parentTimer = m_CreatedEvents.Find(ce => string.Compare(ce.Name, colonSplit[0], true) == 0);

            //if (parentTimer != null)
            //    Debug.Log($"ResolvePendingSyncEvent() found parent by name - {colonSplit[0]}");

            // if not found above, try again, searching by UniqueId
            if (parentTimer == null)
            {
                parentTimer = m_CreatedEvents.Find(ce => string.Compare(ce.UniqueId, cache.ce.UniqueId, true) == 0);

                //if (parentTimer != null)
                //    Debug.Log($"ResolvePendingSyncEvent() found parent by id - {cache.ce.Name} - {cache.ce.UniqueId}");
            }

            float eventTime = ParseEventStartTime(cache.timing);
            if (eventSyncPointName == start || eventSyncPointName == stroke)
                cache.ce.StartTime = eventTime;
            else if (eventSyncPointName == relax)
                cache.ce.StartTime = eventTime;
            else
                cache.ce.Length = eventTime - cache.ce.StartTime;


            // EDF - old manual parsing code using the parentTimer it searched for.
            // not sure if this ever worked right.  Replaced with the block above calling ParseEventStartTime() instead.
            #if false
            if (parentTimer != null)
            {
                float offset = 0;
                if (useMathOperation)
                {
                    float.TryParse(mathOpSplit[1], out offset);
                }

                float eventTime = 0;
                if (parentSyncPointName == start || parentSyncPointName == stroke)
                {
                    eventTime = parentTimer.StartTime + offset;
                }
                else
                {
                    // EDF - modified this code to use StartTime instead of EndTime
                    // Was seeing better lining up of animations with utterance when using stroke=T<x> format inside the BML.
                    // Unsure if this will cause side-effects on other event types
                    //eventTime = parentTimer.EndTime + offset;
                    eventTime = parentTimer.StartTime + offset;
                }

                Debug.Log($"ResolvePendingSyncEvent() - {cache.ce.Name} - {cache.ce.UniqueId} - {eventSyncPointName} - {cache.timing} - {parentSyncPointName} - {cache.ce.StartTime} - {parentTimer.StartTime} - {offset} - {eventTime}");

                if (eventSyncPointName == start || eventSyncPointName == stroke)
                {
                    cache.ce.StartTime = eventTime;
                }
                else
                {
                    cache.ce.Length = eventTime - cache.ce.StartTime;
                }
            }
            #endif
        }

        #endregion

        #region Event Creation

        /// <summary>
        /// Routes an XML tag to the appropriate tag-specific event creation method.
        /// </summary>
        /// <param name="reader">The active XmlTextReader positioned at the event tag.</param>
        /// <param name="type">The name of the current tag (e.g., "speech", "gesture").</param>
        /// <remarks>
        /// Supports tags such as "speech", "face", "head", "gaze", "posture", "saccade", "animation", and "custom".
        /// Adds parsed events to m_CreatedEvents. Unknown or unsupported tags are logged and skipped.
        /// </remarks>
        void CreateEvent(XmlTextReader reader, string type)
        {
            var ce = CreateNewEvent(reader, type);
            if (ce == null)
                return;

            int functionOverload = ParseOverload(reader);

            // Tag-specific handling
            switch (type)
            {
                case "sbm:animation":
                case "animation":
                    HandleAnimationTag(ce, functionOverload);
                    break;

                case "gaze":
                    HandleGazeTag(reader, ce, functionOverload);
                    break;

                case "head":
                    HandleHeadTag(reader, ce, functionOverload);
                    break;

                case "saccade":
                    ce.ChangedEventFunction("Saccade", functionOverload);
                    break;

                case "face":
                    ce.ChangedEventFunction("PlayFAC", functionOverload);
                    break;

                case "sbm:event":
                case "event":
                    ParseVhmsgEvent(reader, type, ce, functionOverload);
                    break;

                case "gesture":
                    ce.ChangedEventFunction("Gesture", functionOverload);
                    break;

                case "body":
                    ce.ChangedEventFunction("Posture", functionOverload);
                    break;

                case "speech":
                    HandleSpeechTag(reader, ce, ref functionOverload);
                    break;
            }

            ce.SetParameters(reader);
            SetCharacterParam(ce, m_Character);
            RegisterSyncPointDependencies(reader, ce);

            //Debug.Log($"CreateEvent() - {ce.Name} - {ce.StartTime} - {ce.Length}");

            m_ParsedBMLEventCB?.Invoke(reader, reader.Name, ce);
        }

        /// <summary>
        /// Creates a CutsceneEvent for a given XML tag and sets timing, ID, and category metadata.
        /// </summary>
        /// <param name="reader">The XmlTextReader positioned on a valid tag.</param>
        /// <param name="eventType">The name of the tag (e.g., "gesture", "face").</param>
        /// <returns>The constructed CutsceneEvent, or null if skipped due to duplication.</returns>
        /// <remarks>
        /// Handles ID generation, duration calculation, deduplication of gestures, and optional sync offset via AnimationEvent.
        /// </remarks>
        CutsceneEvent CreateNewEvent(XmlTextReader reader, string eventType)
        {
            float startTime = GetEventStartTime(reader);

            // Avoid duplicate gestures
            if (ShouldSkipGesture(eventType, reader, startTime))
                return null;

            string id = reader["id"];
            if (string.IsNullOrEmpty(id))
                id = $"{eventType}_{Guid.NewGuid()}";

            // Adjust start time using sync point if this is an animation
            if (IsAnimationEvent(eventType))
            {
                string syncPoint = "strokeStartTime"; // convention used in AnimationEvent
                string animName = reader["name"];

                startTime = OffsetStartTimeBySyncPoint(startTime, syncPoint, m_Character, animName);
            }

            var ce = new CutsceneEvent
            {
                Name = id,
                StartTime = startTime,
                Length = GetEventLength(reader, startTime)
            };

            ChangedCutsceneEventType(m_EventCategoryName, ce);

            m_CreatedEvents.Add(ce);
            return ce;
        }

        static int ParseOverload(XmlTextReader reader)
        {
            if (!int.TryParse(reader["mm:overload"], out int functionOverload))
                functionOverload = 0;
            return functionOverload;
        }

        static void HandleAnimationTag(CutsceneEvent ce, int functionOverload) => ce.ChangedEventFunction("PlayAnim", functionOverload);

        static void HandleGazeTag(XmlTextReader reader, CutsceneEvent ce, int functionOverload)
        {
            bool isAdvanced = reader["mm:advanced"] == "true" || reader["advanced"] == "true";
            ce.ChangedEventFunction(isAdvanced ? "GazeAdvanced" : "Gaze", functionOverload);
        }

        static void HandleHeadTag(XmlTextReader reader, CutsceneEvent ce, int functionOverload)
        {
            string type = reader["type"];
            if (string.Equals(type, "NOD", StringComparison.OrdinalIgnoreCase))
                ce.ChangedEventFunction("Nod", functionOverload);
            else if (string.Equals(type, "SHAKE", StringComparison.OrdinalIgnoreCase))
                ce.ChangedEventFunction("Shake", functionOverload);
            else
                ce.ChangedEventFunction("Tilt", functionOverload); // toss
        }

        /// <summary>
        /// Parses a &lt;speech&gt; tag and creates a corresponding CutsceneEvent with utterance timing.
        /// </summary>
        /// <param name="reader">XmlTextReader positioned at a speech tag.</param>
        /// <remarks>
        /// Optionally ignores speech if m_IgnoreSpeechEvent is true.
        /// </remarks>
        void HandleSpeechTag(XmlTextReader reader, CutsceneEvent ce, ref int functionOverload)
        {
            m_SpeechId = reader["id"];

            // only set the load path if it wasn't already set. You'll get here if LoadXmlString was called
            if (string.IsNullOrEmpty(m_LoadPath))
                m_LoadPath = reader["ref"];

            //string fileName = Path.ChangeExtension(m_LoadPath, ".bml");
            string fileName;
            fileName = m_LoadPath.Replace(".xml.txt", ".bml");
            fileName = fileName.Replace(".xml", ".bml");
            fileName = Path.ChangeExtension(fileName, ".bml");  // this last case is if the provided path has no extension

            if (VHUtils.IsWebGL())
                functionOverload = 1;

            if (m_EventCategoryName == GenericEventNames.Mecanim)
                functionOverload = 2;

            ce.ChangedEventFunction("PlayAudio", functionOverload);

            if (!VHUtils.IsWebGL() && m_ReadBMLFile)
            {
                // default to creating a path
                string path = $"{Application.dataPath}/{m_LoadPathSubFolder}/{fileName}";
                if (m_LoadPath.Contains("/") || m_LoadPath.Contains("\\"))
                    path = fileName;  // in this case, use the xml's path

                LoadFile(path);
            }
        }

        void RegisterSyncPointDependencies(XmlTextReader reader, CutsceneEvent ce)
        {
            if (!string.IsNullOrEmpty(reader[start]))
                m_PendingSyncEvents.Add(new PendingSyncEvent(ce, start, reader[start]));

            if (!string.IsNullOrEmpty(reader[stroke]))
                m_PendingSyncEvents.Add(new PendingSyncEvent(ce, stroke, reader[stroke]));

            if (!string.IsNullOrEmpty(reader[relax]))
                m_PendingSyncEvents.Add(new PendingSyncEvent(ce, relax, reader[relax]));

            if (!string.IsNullOrEmpty(reader[end]))
                m_PendingSyncEvents.Add(new PendingSyncEvent(ce, end, reader[end]));
        }

        /// <summary>
        /// Parses a VHMsg-formatted event and converts it to a CutsceneEvent instance.
        /// </summary>
        /// <param name="type">The category to assign (e.g., "sbm:gesture").</param>
        /// <returns>True if parsing succeeded, false if message format was invalid.</returns>
        /// <remarks>
        /// Only used for debugging or testing messages formatted in VHMsg command structure.
        /// </remarks>
        static void ParseVhmsgEvent(XmlTextReader xml, string type, CutsceneEvent ce, int overload)
        {
            string message = xml["message"];
            if (message.IndexOf("saccade") != -1)
            {
                // this is a saccade event
                if (!string.IsNullOrEmpty(xml["mm:stopSaccade"]) || !string.IsNullOrEmpty(xml["stopSaccade"]))
                    ce.ChangedEventFunction("StopSaccade");
                else
                    ce.ChangedEventFunction("Saccade");
            }
            else if (message.IndexOf("viseme") != -1)
            {
                ce.ChangedEventFunction("PlayViseme", overload);
            }
            else if (message.IndexOf("gazefade out") != -1)
            {
                ce.ChangedEventFunction("StopGaze", overload);
            }
            else
            {
                // event start times are usually based off of other events using event names. Because of this,
                // we need to cache this event, and later try to find the event that it's parented to

                ChangedCutsceneEventType(GenericEventNames.Common, ce);

                // EDF - 9/16/2024 - changed this to a simple Marker event so that console isn't spammed with vhmsg events (vrAgentSpeech partial)
                //ce.ChangedEventFunction("SendVHMsg");
                ce.ChangedEventFunction("Marker");
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Extracts the event's start time from known XML attributes (start, stroke, relax).
        /// </summary>
        /// <param name="reader">The XmlTextReader positioned on the current event tag.</param>
        /// <returns>First matching time value, or 0 if none found.</returns>
        /// <remarks>
        /// Used by gesture and animation events where timing can be defined by different stages.
        /// </remarks>
        static float GetEventStartTime(XmlTextReader reader)
        {
            string[] tags = { start, stroke, relax };
            foreach (var tag in tags)
            {
                if (float.TryParse(reader[tag], out float time))
                    return time;
            }

            return 0;
        }

        /// <summary>
        /// Calculates the duration of an event based on 'end' attribute and provided start time.
        /// </summary>
        /// <param name="reader">The XmlTextReader with access to the 'end' attribute.</param>
        /// <param name="startTime">The event's start time (already parsed).</param>
        /// <returns>Computed duration, with a minimum enforced of 0.01f.</returns>
        static float GetEventLength(XmlTextReader reader, float startTime)
        {
            if (float.TryParse(reader["end"], out float endTime))
            {
                float length = endTime - startTime;
                return length < 0.01f ? 0.01f : length;
            }

            return 0.01f;
        }

        /// <summary>
        /// Determines whether the current gesture is a duplicate and should be skipped.
        /// </summary>
        /// <param name="eventType">The XML tag name (should be "gesture").</param>
        /// <param name="reader">Reader used to extract gesture lexeme and compare start times.</param>
        /// <param name="startTime">Parsed start time of the gesture.</param>
        /// <returns>True if gesture is considered a duplicate; otherwise false.</returns>
        /// <remarks>
        /// Used to eliminate repeated gestures with identical names and near-identical timing.
        /// </remarks>
        bool ShouldSkipGesture(string eventType, XmlTextReader reader, float startTime)
        {
            if (!string.Equals(eventType, "gesture", StringComparison.OrdinalIgnoreCase))
                return false;

            currentGestureName = reader["lexeme"] ?? "";
            if (currentGestureName == lastGestureName && Mathf.Abs(startTime - lastEventTime) < 0.01f)
            {
                Debug.Log($"Skipping duplicate gesture: {currentGestureName}");
                return true;
            }

            lastGestureName = currentGestureName;
            lastEventTime = startTime;
            return false;
        }

        /// <summary>
        /// Determines whether a given event tag represents an animation event.
        /// </summary>
        /// <param name="eventType">The XML tag name.</param>
        /// <returns>True if the event is 'animation' or 'sbm:animation'.</returns>
        static bool IsAnimationEvent(string eventType)
        {
            return string.Equals(eventType, "animation", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(eventType, "sbm:animation", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resets internal state before or after a parse operation.
        /// </summary>
        /// <remarks>
        /// Clears pending sync events and created events.  
        /// Does not reset character or callback delegates.
        /// Should be called before starting a new parse.
        /// </remarks>
        void ClearParsingState()
        {
            m_PendingSyncEvents.Clear();
            m_CreatedEvents.Clear();
        }

        #endregion

        #region Utility / Static API

        /// <summary>
        /// Updates the event category used for filtering and grouping in the Cutscene system.
        /// </summary>
        public static void ChangedCutsceneEventType(string newType, CutsceneEvent ce)
        {
            ce.EventType = newType;

            // TODO: THIS IS A HACK! get a reference to a generic events object!
            var genericEventsGO = GameObject.Find("GenericEvents").GetComponentsInChildren<GenericEvents>();
            if (genericEventsGO == null)
            {
                Debug.LogError($"BMLParser doesn't have a GenericEvents componenent anywhere");
                return;
            }

            MonoBehaviour targetComponent = null;
            foreach (var ge in genericEventsGO)
            {
                if (ge.GetEventType() == newType)
                {
                    targetComponent = ge;
                    break;
                }
            }

            if (targetComponent != null)
                ce.SetFunctionTargets(targetComponent.gameObject, targetComponent);
            else
                ce.SetFunctionTargets(null, null);
        }

        /// <summary>
        /// Sets the active character and optional speech identifier for all future events.
        /// </summary>
        public void SetCharacterParam(CutsceneEvent ce, string characterName)
        {
            if (ce == null)
                return;

            if (ce.EventType == GenericEventNames.SmartBody || ce.EventType == GenericEventNames.Mecanim)
            {
                var characterParam = ce.FindParameter("character");
                if (characterParam != null)
                {
                    if (characterParam.objData == null && !string.IsNullOrEmpty(characterName))
                    {
                        if (ce.EventType == GenericEventNames.SmartBody)
                        {
                            var sceneCharacter = VHUtils.FindCharacter(characterName, ce.Name);
                            if (sceneCharacter != null)
                                characterParam.SetObjData(sceneCharacter);
                        }

                        if (characterParam.objData == null)
                        {
                            if (string.IsNullOrEmpty(characterParam.stringData))
                                characterParam.stringData = characterName;
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Event {ce.Name} doesn't have a character param?");
                }
            }
        }

        /// <summary>
        /// Sets externally provided BML timing markers for use in sync expression evaluation.
        /// </summary>
        /// <param name="bmlTimings">A list of BMLTiming entries defining sync points (e.g., visemes, T0, T1).</param>
        public void SetBMLTimings(List<BMLTiming> bmlTimings)
        {
            //foreach (var timing in bmlTimings)
            //    Debug.Log($"SetBMLTimings() - {timing.time} - {timing.id} - {timing.text}");

            m_BMLTimings = bmlTimings;
        }

        #endregion


        public void AddOnReadBMLFileCB(OnReadBMLFile cb) => m_ReadBmlFileCB += cb;

        void ReadBML(XmlTextReader reader)
        {
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (reader.Name == "sync" || reader.Name == "mark")
                        {
                            string id = reader["id"];
                            float time = float.Parse(reader["time"]);

                            // NOTE I removed this because it was skipping the next line unless the current line was structed like: <sync id="T2" time="0.435" />test
                            // if the "test" wasn't there, then the line would be skipped. Now the word at the end of the line is no longer getting parsed, so
                            // that needs to be fixed. This, however, is malformed xml.
                            if (TrimBMLTimingWhenParsing)
                                reader.ReadInnerXml();

                            if (m_ParsedBMLTimingCB != null)
                            {
                                var bmlTiming = new BMLTiming(id, time, TrimBMLTimingWhenParsing ? reader.Value.Trim() : "");
                                m_BMLTimings.Add(bmlTiming);
                                m_ParsedBMLTimingCB(bmlTiming);
                            }
                        }
                        else if (reader.Name == "lips")
                        {
                            float.TryParse(reader["start"], out float start);
                            float.TryParse(reader["end"], out float end);
                            float.TryParse(reader["ready"], out float ready);
                            float.TryParse(reader["relax"], out float relax);
                            float.TryParse(reader["articulation"], out float articulation);

                            var lipData = new LipData(reader["viseme"], articulation, start, ready, relax, end);
                            m_ParsedVisemeTimingCB?.Invoke(lipData);
                        }
                        else if (reader.Name == "curve")
                        {
                            int.TryParse(reader["num_keys"], out int numKeys);

                            if (numKeys > 0)
                            {
                                var curveData = new CurveData(reader["name"], reader["owner"], numKeys);
                                string curveString = reader.ReadString();
                                curveString = curveString.Trim();
                                string[] curves = curveString.Split(' ');

                                //Debug.Log("numKeys: " + numKeys + " curves.Length: " + curves.Length);
                                for (int i = 0; i < curves.Length; i += 4)
                                {
                                    curveData.AddKey(
                                        new Quaternion(
                                            float.Parse(curves[i]),
                                            float.Parse(curves[i + 1]),
                                            float.Parse(curves[i + 2]),
                                            float.Parse(curves[i + 3])),
                                        i / 4);
                                }

                                m_ParsedCurveDataCB?.Invoke(curveData);
                            }
                        }
                        break;
                }
            }
        }

        bool IsKnownEventType(string tag) => Array.Exists(EventXmlNames, name => name == tag);

        /// <summary>
        /// Assigns the 'participant' attribute from XML (if present) to the CutsceneEvent's character field.
        /// </summary>
        /// <param name="reader">XML reader positioned at the current event tag.</param>
        /// <remarks>
        /// If 'participant' is missing, uses the globally set m_CharacterName as fallback.
        /// </remarks>
        void AssignParticipantId(XmlTextReader reader)
        {
            if (string.IsNullOrEmpty(m_Character))
                m_Character = reader["id"];
        }

        /// <summary>
        /// Parses the start time of an event, including BML sync expressions like 'T0+0.5'.
        /// </summary>
        /// <param name="startTime">A raw start time value or a sync expression.</param>
        /// <returns>Evaluated time in seconds, or 0 if parsing fails.</returns>
        /// <remarks>
        /// Handles sync expressions of the format: 'SpeechID:SyncMarker±Offset'.
        /// Matches against m_BMLTimings collected from parsed BML data.
        /// </remarks>
        float ParseEventStartTime(string startTime)
        {
            if (!float.TryParse(startTime, out float eventStart))
            {
                if (!string.IsNullOrEmpty(startTime))
                {
                    // looks for timing markers that were read from the bml
                    string[] split = startTime.Split(':');
                    for (int i = 0; i < split.Length; i++)
                    {
                        if (split[i].IndexOf(m_SpeechId) != -1)
                        {
                            string timing = split[i + 1];
                            float offset = 0;
                            if (timing.Contains("+"))
                            {
                                string[] newSplit = timing.Split('+');
                                timing = newSplit[0];
                                float.TryParse(newSplit[1], out offset);
                            }
                            else if (timing.Contains("-"))
                            {
                                string[] newSplit = timing.Split('-');
                                timing = newSplit[0];
                                float.TryParse(newSplit[1], out offset);
                            }

                            BMLTiming bmlTiming = m_BMLTimings.Find(t => t.id == timing);
                            if (bmlTiming != null)
                                eventStart = bmlTiming.time + offset;

                            break;
                        }
                    }
                }
            }

            return eventStart;
        }
    }
}
