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
                    CompleteLipsyncGeneration(LipsyncAutoScheduler.CreateSchedule(text, generatedAudioLength))));
        }

        protected virtual void OnProxyAudioSpeechGeneration(AudioSpeechMap audioSpeechMap)
        {
            StartCoroutine(WaitForTTSCompletion(() =>
                CompleteLipsyncGeneration(LipsyncAutoScheduler.RescaleLipsyncTime(TextToSpeechXMLBuilder.BuildSpeechXML(audioSpeechMap), generatedAudioLength))));
        }

        protected virtual IEnumerator WaitForTTSCompletion(System.Action callback)
        {
            while (textToSpeechProcessing) yield return null;

            callback?.Invoke();
        }
    }
}
