using System;
using System.Collections;
using UnityEngine;

namespace Ride.Samples
{
    public class SamplesCoreFramesPerSecondCounter : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        FramesPerSecondCounter m_framesPerSecondCounter;
        int slowdownMS = 0;

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_framesPerSecondCounter = Globals.api.GetSystem<FramesPerSecondCounter>();
        }

        protected override void Update()
        {
            base.Update();

            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < slowdownMS) // Waste 50ms per frame
            {
                // Do nothing, just waste time
            }
        }

        public void OnGUIFramesPerSecond()
        {
            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label($"<b>FPS:</b>", 120);
                m_debugMenu.Label($"{m_framesPerSecondCounter.Fps}");
            }

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label($"<b>Smoothed FPS:</b>", 120);
                m_debugMenu.Label($"{m_framesPerSecondCounter.SmoothFps}");
            }

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label($"<b>Average FPS:</b>", 120);
                m_debugMenu.Label($"{m_framesPerSecondCounter.AverageFps}");
            }

            using (m_debugMenu.Horizontal())
            {
                m_debugMenu.Label($"Slow: {slowdownMS}ms");
                slowdownMS = (int)m_debugMenu.HorizontalSlider((float)slowdownMS, 0, 100);
            }
        }
    }
}
