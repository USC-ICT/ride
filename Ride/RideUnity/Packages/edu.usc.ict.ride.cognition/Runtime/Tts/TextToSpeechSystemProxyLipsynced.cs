using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.TextToSpeech
{
    /// <summary>
    /// Base class for TTS implementations that do not provide phoneme/word timing data, and must use a proxy instead
    /// </summary>
    public abstract class TextToSpeechSystemProxyLipsynced : TextToSpeechSystemLipsynced
    {
        [SerializeField] private bool m_useProxy = false;
        [SerializeField] private List<IpaDictionary> m_ipaDictionaries = new();
        ILipsyncMapper m_lipsyncMapper;

        public override void SystemAwake()
        {
            base.SystemInit();
            m_lipsyncMapper = GetComponent<ILipsyncMapper>();

            if (m_lipsyncMapper == null && m_useProxy)
            {
                Debug.Log("Proxy ILipsyncMapper not found, falling back to auto lipsyncer");
                m_useProxy = false;
            }

        }

        protected override void StartLipsyncGeneration(string voice, string text)
        {
            if (m_useProxy)
                m_lipsyncMapper.GenerateAudioSpeechMap(string.Empty, text, OnProxyAudioSpeechGeneration);
            else
                StartCoroutine(WaitForTTSCompletion(() =>
                {
                    AudioSpeechMap audioSpeechMap = LipsyncAutoScheduler.CreateProxySpeechMap(text, generatedAudioLength, ResolveIpaDictionaries());
                    string xml = TextToSpeechXMLBuilder.BuildSpeechXML(audioSpeechMap);
                    LogSpeechXmlDebug(audioSpeechMap, xml, "Auto");
                    CompleteLipsyncGeneration(xml);
                }));
        }

        protected virtual void OnProxyAudioSpeechGeneration(AudioSpeechMap audioSpeechMap)
        {
            StartCoroutine(WaitForTTSCompletion(() =>
            {
                string xml = audioSpeechMap != null ? TextToSpeechXMLBuilder.BuildSpeechXML(audioSpeechMap) : string.Empty;
                xml = LipsyncAutoScheduler.RescaleLipsyncTime(xml, generatedAudioLength);
                LogSpeechXmlDebug(audioSpeechMap, xml, "Proxy");
                CompleteLipsyncGeneration(xml);
            }));
        }

        protected virtual IEnumerator WaitForTTSCompletion(System.Action callback)
        {
            while (textToSpeechProcessing) yield return null;

            callback?.Invoke();
        }

        private IReadOnlyList<IpaDictionary> ResolveIpaDictionaries()
        {
            if (m_ipaDictionaries != null && m_ipaDictionaries.Count > 0)
                return m_ipaDictionaries;

            Transform root = transform.root;
            if (root == null)
                return System.Array.Empty<IpaDictionary>();

            IpaDictionary[] discovered = root.GetComponentsInChildren<IpaDictionary>(true);
            return discovered ?? System.Array.Empty<IpaDictionary>();
        }
    }
}
