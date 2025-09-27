using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Tracks current, smoothed, average, min, and max FPS using Ride delta time.
    /// - Average computed from a moving window (queue)
    /// - Min/Max reflect the window contents
    /// </summary>
    public class FramesPerSecondCounter : RideSystemMonoBehaviour, IFramePerSecondCounter
    {
        [SerializeField]
        [Tooltip("Number of entries to keep in the moving average window. Larger numbers will generate a smoother average over time.")]
        private int m_numFpsEntries = 50;

        [SerializeField]
        [Tooltip("Initial value used to populate the moving average buffer.")]
        private int m_initialAverageFps = 20;

        [SerializeField]
        [Tooltip("Enable or disable min/max FPS tracking. Disabling this avoids full-history scans.")]
        private bool m_enableMinMaxTracking = true;

        private Queue<float> m_frameTimeHistory;
        private float m_totalFrameTime;
        private float m_fps;
        private float m_smoothFps;
        private float m_averageFps;
        private float m_minFps;
        private float m_maxFps;
        private float m_minDeltaTime; // inverse of max fps
        private float m_maxDeltaTime; // inverse of min fps

        public float Fps => m_fps;
        public float SmoothFps => m_smoothFps;    // this uses Unity's smoothDeltaTime to compute.  A little more sporatic than our computed average
        public float AverageFps => m_averageFps;  // this computes our own average based on the past m_numFpsEntries frames. Ends up being a little more smooth than SmoothFps.
        public float MinFps => m_minFps;
        public float MaxFps => m_maxFps;

        public override void SystemInit()
        {
            base.SystemInit();

            float averageDelta = 1.0f / m_initialAverageFps;
            m_frameTimeHistory = new Queue<float>(Enumerable.Repeat(averageDelta, m_numFpsEntries));
            m_totalFrameTime = averageDelta * m_numFpsEntries;
            m_averageFps = m_initialAverageFps;
            m_minFps = m_initialAverageFps;
            m_maxFps = m_initialAverageFps;
            m_minDeltaTime = averageDelta;
            m_maxDeltaTime = averageDelta;
        }

        public override void SystemUpdate(float dt)
        {
            base.SystemUpdate(dt);

            float adjustedDeltaTime;
            float adjustedSmoothDeltaTime;

            if (RideUtils.GetDeltaTime() == 0 || RideUtils.GetTimeScale() == 0)
            {
                // Fallback to real-time-based estimate to avoid divide-by-zero or paused time
                adjustedDeltaTime = RideUtils.GetRealtimeSinceStartup() / RideMath.Max(1, RideUtils.GetFrameCount());
                adjustedSmoothDeltaTime = adjustedDeltaTime;
            }
            else
            {
                adjustedDeltaTime = RideUtils.GetDeltaTime() / RideUtils.GetTimeScale();
                adjustedSmoothDeltaTime = RideUtils.GetSmoothDeltaTime() / RideUtils.GetTimeScale();
            }

            m_fps = adjustedDeltaTime > 0f ? 1f / adjustedDeltaTime : 0f;
            m_smoothFps = adjustedSmoothDeltaTime > 0f ? 1f / adjustedSmoothDeltaTime : 0f;

            UpdateAverage(adjustedDeltaTime);
        }

        /// <summary>
        /// Updates the internal frame time buffer and recalculates average, min, and max FPS as needed.
        /// </summary>
        /// <param name="deltaTime">The delta time to record for the current frame.</param>
        private void UpdateAverage(float deltaTime)
        {
            m_frameTimeHistory.Enqueue(deltaTime);
            m_totalFrameTime += deltaTime;

            if (m_frameTimeHistory.Count > m_numFpsEntries)
            {
                float removed = m_frameTimeHistory.Dequeue();
                m_totalFrameTime -= removed;

                if (m_enableMinMaxTracking &&
                    (RideMath.Approximately(removed, m_minDeltaTime) ||
                     RideMath.Approximately(removed, m_maxDeltaTime)))
                    RecalculateMinMax();
            }

            m_averageFps = m_numFpsEntries / m_totalFrameTime;

            if (deltaTime < m_minDeltaTime)
            {
                m_minDeltaTime = deltaTime;
                m_maxFps = 1f / deltaTime;
            }

            if (deltaTime > m_maxDeltaTime)
            {
                m_maxDeltaTime = deltaTime;
                m_minFps = 1f / deltaTime;
            }
        }

        /// <summary>
        /// Recomputes the minimum and maximum delta times (and their corresponding FPS values)
        /// by scanning the full frame time history buffer.
        /// Only called when a previously known min/max value is removed.
        /// </summary>
        private void RecalculateMinMax()
        {
            m_minDeltaTime = float.MaxValue;
            m_maxDeltaTime = float.MinValue;

            foreach (float deltaTime in m_frameTimeHistory)
            {
                if (deltaTime < m_minDeltaTime) m_minDeltaTime = deltaTime;
                if (deltaTime > m_maxDeltaTime) m_maxDeltaTime = deltaTime;
            }

            m_maxFps = 1f / m_minDeltaTime;
            m_minFps = 1f / m_maxDeltaTime;
        }
    }
}
