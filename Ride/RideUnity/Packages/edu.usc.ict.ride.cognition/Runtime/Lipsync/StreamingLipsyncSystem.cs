using System;
using System.Collections.Generic;
using Ride.TextToSpeech;
using VHAssets;
using UnityEngine;

namespace Ride.Conversation
{
    /// <summary>
    /// Approximate realtime lipsync scheduler for streaming assistant transcript chunks.
    /// </summary>
    public class StreamingLipsyncSystem : RideSystemMonoBehaviour
    {
        [SerializeField] private float m_minChunkDurationSeconds = 0.20f;
        [SerializeField] private bool m_enableAudioLevelFallback = true;
        [SerializeField] private float m_audioLevelSilenceThreshold = 0.012f;
        [SerializeField] private float m_audioLevelOpenGain = 8.0f;
        [SerializeField] private float m_audioLevelMaxOpenWeight = 0.60f;
        [SerializeField] private float m_audioLevelSmoothingSpeed = 10.0f;
        [SerializeField] private float m_phoneticScheduleFallbackSuppressionSeconds = 0.12f;
        [SerializeField] private float m_scheduleLookaheadSeconds = 0.02f;
        [SerializeField] private List<IpaDictionary> m_ipaDictionaries = new List<IpaDictionary>();

        private MecanimCharacter m_character;
        private bool m_playbackStarted;
        private bool m_audioFallbackWasDriving;
        private float m_assignedAudioSeconds;
        private float m_playbackSeconds;
        private float m_currentAudioLevel;
        private float m_currentOpenWeight;
        private float m_phoneticScheduleActiveUntilTime;
        private readonly Queue<PendingChunk> m_pendingChunks = new Queue<PendingChunk>();

        private sealed class PendingChunk
        {
            public string Text;
            public float AudioReceivedSeconds;
        }

        /// <summary>
        /// Assigns the character that should receive realtime viseme playback.
        /// </summary>
        /// <param name="character">The character to animate.</param>
        public void BindTarget(MecanimCharacter character)
        {
            m_character = character;
        }

        /// <summary>
        /// Clears all queued lipsync data and resets playback state for a new stream.
        /// </summary>
        public void ResetStream()
        {
            m_playbackStarted = false;
            m_assignedAudioSeconds = 0f;
            m_playbackSeconds = 0f;
            m_currentAudioLevel = 0f;
            m_currentOpenWeight = 0f;
            m_phoneticScheduleActiveUntilTime = 0f;
            m_audioFallbackWasDriving = false;
            m_pendingChunks.Clear();
            m_character?.StopLipSyncPerformance();
        }

        /// <summary>
        /// Stops current realtime lipsync immediately and clears queued transcript chunks.
        /// </summary>
        public void Interrupt()
        {
            m_playbackStarted = false;
            m_assignedAudioSeconds = 0f;
            m_playbackSeconds = 0f;
            m_currentAudioLevel = 0f;
            m_currentOpenWeight = 0f;
            m_phoneticScheduleActiveUntilTime = 0f;
            m_audioFallbackWasDriving = false;
            m_pendingChunks.Clear();
            m_character?.StopLipSyncPerformance();
        }

        /// <summary>
        /// Marks assistant audio playback as started and schedules any queued transcript chunks.
        /// </summary>
        public void NotifyAudioPlaybackStarted()
        {
            m_playbackStarted = true;
            m_playbackSeconds = 0f;
            FlushPendingChunks();
        }

        /// <summary>
        /// Marks assistant audio playback as finished and returns the mouth to rest.
        /// </summary>
        public void NotifyAudioPlaybackFinished()
        {
            m_playbackStarted = false;
            m_assignedAudioSeconds = 0f;
            m_playbackSeconds = 0f;
            m_currentAudioLevel = 0f;
            m_currentOpenWeight = 0f;
            m_phoneticScheduleActiveUntilTime = 0f;
            m_audioFallbackWasDriving = false;
            m_pendingChunks.Clear();
            m_character?.StopLipSyncPerformance();
        }

        /// <summary>
        /// Updates the current assistant audio level used by the fallback jaw-open driver.
        /// </summary>
        /// <param name="level">Current normalized audio output level.</param>
        public void SetAudioLevel(float level)
        {
            m_currentAudioLevel = Mathf.Max(0f, level);
        }

        /// <summary>
        /// Updates the current assistant audio playback position in seconds.
        /// </summary>
        /// <param name="playbackSeconds">Elapsed assistant audio playback time.</param>
        public void SetPlaybackSeconds(float playbackSeconds)
        {
            m_playbackSeconds = Mathf.Max(0f, playbackSeconds);
        }

        protected override void Update()
        {
            base.Update();

            if (m_playbackStarted)
                FlushPendingChunks();

            if (!m_enableAudioLevelFallback || m_character == null)
                return;

            bool phoneticScheduleActive = Time.time <= m_phoneticScheduleActiveUntilTime || m_pendingChunks.Count > 0;
            if (phoneticScheduleActive)
            {
                if (m_audioFallbackWasDriving)
                {
                    m_currentOpenWeight = 0f;
                    m_audioFallbackWasDriving = false;
                    m_character.PlayViseme("open", 0f);
                }

                return;
            }

            float targetOpenWeight = 0f;
            if (m_playbackStarted)
            {
                float normalizedLevel = Mathf.Max(0f, m_currentAudioLevel - m_audioLevelSilenceThreshold) * m_audioLevelOpenGain;
                targetOpenWeight = Mathf.Clamp(normalizedLevel, 0f, m_audioLevelMaxOpenWeight);
            }

            m_currentOpenWeight = Mathf.MoveTowards(
                m_currentOpenWeight,
                targetOpenWeight,
                Mathf.Max(0.01f, m_audioLevelSmoothingSpeed) * Time.deltaTime);

            if (m_playbackStarted || m_currentOpenWeight > 0.001f)
            {
                m_audioFallbackWasDriving = m_currentOpenWeight > 0.001f;
                m_character.PlayViseme("open", m_currentOpenWeight);
            }
        }

        /// <summary>
        /// Queues a stable assistant transcript chunk for approximate phonetic lipsync scheduling.
        /// </summary>
        /// <param name="text">Assistant transcript text to schedule.</param>
        /// <param name="audioReceivedSeconds">Assistant audio duration received so far.</param>
        public void AppendChunk(string text, float audioReceivedSeconds)
        {
            if (m_character == null || string.IsNullOrWhiteSpace(text))
                return;

            m_pendingChunks.Enqueue(new PendingChunk
            {
                Text = text,
                AudioReceivedSeconds = Mathf.Max(0f, audioReceivedSeconds)
            });

            if (m_playbackStarted)
                FlushPendingChunks();
        }

        private void FlushPendingChunks()
        {
            while (m_playbackStarted && m_pendingChunks.Count > 0)
            {
                if (m_playbackSeconds + Mathf.Max(0f, m_scheduleLookaheadSeconds) < m_assignedAudioSeconds)
                    break;

                PendingChunk chunk = m_pendingChunks.Dequeue();
                ScheduleChunk(chunk.Text, chunk.AudioReceivedSeconds);
            }
        }

        private void ScheduleChunk(string text, float audioReceivedSeconds)
        {
            float receivedDuration = audioReceivedSeconds - m_assignedAudioSeconds;
            float duration = Mathf.Max(m_minChunkDurationSeconds, receivedDuration, EstimateChunkDurationSeconds(text));
            AudioSpeechMap map = LipsyncAutoScheduler.CreateProxySpeechMap(text, duration, ResolveIpaDictionaries());
            if (map == null || map.WordTimingList == null || map.WordTimingList.Count == 0)
                return;

            List<TtsReader.WordTiming> timings = BuildWordTimings(map, 0f);
            if (timings.Count == 0)
                return;

            m_assignedAudioSeconds = Mathf.Max(m_assignedAudioSeconds + duration, audioReceivedSeconds);
            m_phoneticScheduleActiveUntilTime = Mathf.Max(
                m_phoneticScheduleActiveUntilTime,
                Time.time + duration + Mathf.Max(0f, m_phoneticScheduleFallbackSuppressionSeconds));
            m_audioFallbackWasDriving = false;
            m_character.PlayRealtimeAudio(timings);
        }

        private static float EstimateChunkDurationSeconds(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0.20f;

            string trimmed = text.Trim();
            string[] words = trimmed.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            float wordDuration = words.Length * 0.24f;
            float charDuration = trimmed.Length / 14.0f;
            float punctuationPause = 0f;

            if (trimmed.EndsWith(".") || trimmed.EndsWith("!") || trimmed.EndsWith("?"))
                punctuationPause = 0.20f;
            else if (trimmed.EndsWith(",") || trimmed.EndsWith(";") || trimmed.EndsWith(":"))
                punctuationPause = 0.10f;

            return Mathf.Clamp(Mathf.Max(wordDuration, charDuration) + punctuationPause, 0.20f, 1.80f);
        }

        private IReadOnlyList<IpaDictionary> ResolveIpaDictionaries()
        {
            if (m_ipaDictionaries != null && m_ipaDictionaries.Count > 0)
                return m_ipaDictionaries;

            m_ipaDictionaries = new List<IpaDictionary>();
            foreach (IpaDictionary dictionary in Resources.FindObjectsOfTypeAll<IpaDictionary>())
            {
                if (dictionary == null || !dictionary.gameObject.scene.isLoaded || !dictionary.gameObject.activeInHierarchy)
                    continue;

                m_ipaDictionaries.Add(dictionary);
            }

            return m_ipaDictionaries;
        }

        private static List<TtsReader.WordTiming> BuildWordTimings(AudioSpeechMap map, float leadSeconds)
        {
            var timings = new List<TtsReader.WordTiming>(map.WordTimingList.Count);
            double lead = Math.Max(0.0, leadSeconds);

            for (int i = 0; i < map.WordTimingList.Count; i++)
            {
                WordTimingData word = map.WordTimingList[i];
                var timing = new TtsReader.WordTiming(word.Text, (float)(word.Start + lead), (float)(word.End + lead));
                timings.Add(timing);
            }

            for (int i = 0; i < map.VisemeList.Count; i++)
            {
                GenerateAudioReplyViseme viseme = map.VisemeList[i];
                int targetWordIndex = FindOwningWordIndex(map.WordTimingList, viseme.start);
                if (targetWordIndex < 0 || targetWordIndex >= timings.Count)
                    continue;

                timings[targetWordIndex].m_VisemesUsed.Add(
                    new TtsReader.VisemeData((float)(viseme.start + lead), (float)viseme.articulation, viseme.type));
            }

            return timings;
        }

        private static int FindOwningWordIndex(IReadOnlyList<WordTimingData> words, double timeSeconds)
        {
            for (int i = 0; i < words.Count; i++)
            {
                if (timeSeconds >= words[i].Start && timeSeconds <= words[i].End)
                    return i;
            }

            if (words.Count == 0)
                return -1;

            return words.Count - 1;
        }
    }
}
