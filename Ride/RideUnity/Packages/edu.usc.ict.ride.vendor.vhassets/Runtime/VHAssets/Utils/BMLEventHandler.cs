using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;

namespace VHAssets
{
    /// <summary>
    /// MonoBehaviour wrapper for parsing BML/XML input and triggering Cutscene generation.
    /// </summary>
    /// <remarks>
    /// Handles loading character behavior markup, coordinating BMLParser, and instantiating Cutscene objects.
    /// Intended to be attached to a Unity GameObject representing a character or controller.
    /// </remarks>
    public class BMLEventHandler : MonoBehaviour
    {
        public delegate void OnCutsceneCreated(Cutscene cs);

        public ICharacterController m_CharacterController;
        public Cutscene m_CutscenePrefab;
        public bool m_TrimBMLTimingWhenParsing = false;

        protected BMLParser m_BMLParser;

        OnCutsceneCreated m_OnCutsceneCreated;


        /// <summary>
        /// Initializes the BMLParser and configures it using character controller type and project settings.
        /// </summary>
        /// <remarks>
        /// Automatically sets the parser's event category to Mecanim if the character uses a Mecanim manager.
        /// Also configures parser timing behavior from serialized settings.
        /// </remarks>
        public virtual void Start()
        {
            m_BMLParser = new BMLParser(OnParsedBMLTiming, OnParsedWordTiming, OnParsedVisemeTiming, OnParsedBMLEvent, OnFinishedReading, OnParsedCustomEvent);

            if (m_CharacterController.GetCharacterControllerType() == "MecanimManager")
                m_BMLParser.EventCategoryName = GenericEventNames.Mecanim;

            m_BMLParser.TrimBMLTimingWhenParsing = m_TrimBMLTimingWhenParsing;
        }

        public virtual void InitializeLoadedAsset()
        {
            // Note we receive this message, but Start() may have already run.
            // Don't use the RIDE pattern of initializing after asset load, to avoid bringing in ILoadableAsset dependency.

            if (m_BMLParser != null)
                return;

            m_BMLParser = new BMLParser(OnParsedBMLTiming, OnParsedWordTiming, OnParsedVisemeTiming, OnParsedBMLEvent, OnFinishedReading, OnParsedCustomEvent);

            if (m_CharacterController != null && m_CharacterController.GetCharacterControllerType() == "MecanimManager")
                m_BMLParser.EventCategoryName = GenericEventNames.Mecanim;

            m_BMLParser.TrimBMLTimingWhenParsing = m_TrimBMLTimingWhenParsing;
        }

        public virtual void ResetLoadedAsset()
        {
            // Destroy any Cutscenes currently parented under this handler.
            // These can keep events alive which may reference unloaded character objects.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child == null)
                    continue;

                var cs = child.GetComponent<Cutscene>();
                if (cs != null)
                    Destroy(child.gameObject);
            }

            m_BMLParser = null;  // Drop parser reference so it can be GC'd (and so we re-create cleanly on reload).
            m_OnCutsceneCreated = null;  // Also clear any callbacks to avoid accidentally holding references.
        }


        #region Public API

        public bool LoadXMLString(string character, string xmlStr) => m_BMLParser.LoadXMLString(character, xmlStr);
        public bool LoadXMLBMLStrings(string character, string xmlStr, string bmlStr) => m_BMLParser.LoadXMLBMLStrings(character, xmlStr, bmlStr);
        public bool LoadXMLBMLStrings(string character, AudioSpeechFile speech) => m_BMLParser.LoadXMLBMLStrings(character, speech);
        public void AddOnCutsceneCreatedCallback(OnCutsceneCreated cb) => m_OnCutsceneCreated += cb;
        public void RemoveOnCutsceneCreatedCallback(OnCutsceneCreated cb) => m_OnCutsceneCreated -= cb;
        public void SetBMLTimings(List<BMLParser.BMLTiming> bmlTimings) => m_BMLParser.SetBMLTimings(bmlTimings);

        #endregion

        #region Parser Callbacks

        protected virtual void OnParsedBMLTiming(BMLParser.BMLTiming bmlTiming) { }
        protected virtual void OnParsedWordTiming(BMLParser.WordTimingData wordTiming) { }
        protected virtual void OnParsedVisemeTiming(BMLParser.LipData lipData) { }
        protected virtual void OnParsedBMLEvent(XmlTextReader reader, string eventType, CutsceneEvent ce)
        {
            /*if (eventType == "speech")
            {
                ce.ChangedEventFunction("PlayAudio", 5);
                ce.SetParameters(reader);
            }*/
        }

        protected virtual void OnParsedCustomEvent(XmlTextReader reader) { }

        /// <summary>
        /// Called by the BMLParser when XML parsing is complete and events are ready to play.
        /// </summary>
        /// <param name="succeeded">True if parsing was successful; false otherwise.</param>
        /// <param name="createdEvents">List of CutsceneEvents generated from the XML content.</param>
        /// <remarks>
        /// Instantiates a Cutscene from the prefab, assigns metadata to each event, and invokes any registered callbacks.
        /// Speech events are optionally excluded if IgnoreSpeechEvent is enabled in the BMLParser.
        /// </remarks>
        protected virtual void OnFinishedReading(bool succeeded, List<CutsceneEvent> createdEvents)
        {
            if (m_CutscenePrefab == null)
            {
                Debug.LogError($"{nameof(BMLEventHandler)} ({name}): Cutscene prefab is null.");
                return;
            }

            if (m_CharacterController == null)
            {
                Debug.LogError($"{nameof(BMLEventHandler)} ({name}): CharacterController is null.");
                return;
            }

            Cutscene cs = Instantiate(m_CutscenePrefab);

            foreach (var ce in createdEvents)
            {
                bool ignoreSpeech = m_BMLParser != null && m_BMLParser.IgnoreSpeechEvent;
                if (ignoreSpeech && ce.FunctionName == "PlayAudio")
                    continue;

                //Debug.Log($"OnFinishedReading() - {ce.Name} - {ce.StartTime} - {ce.Length}");

                ce.SetMetaData(m_CharacterController);
                cs.AddEvent(ce);
            }

            m_OnCutsceneCreated?.Invoke(cs);

            cs.transform.SetParent(transform);

            //Debug.Log($"OnFinishedReading() before cs.Play() - {succeeded} - {createdEvents.Count}");

            cs.Play();
            cs.AddOnFinishedCutsceneCallback(OnFinishedCutscene);
        }

        protected virtual void OnFinishedCutscene(Cutscene cs) => Destroy(cs.gameObject);
    }

    #endregion
}
