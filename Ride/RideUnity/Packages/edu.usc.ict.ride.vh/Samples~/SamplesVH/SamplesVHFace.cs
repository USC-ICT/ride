using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VHAssets;

namespace Ride.Samples
{
    /// <summary>
    /// Handles the Debug Menu interface for controlling facial expressions and camera positioning.
    /// Provides sliders for adjusting facial animations and buttons for nodding/shaking gestures.
    /// </summary>
    public class SamplesVHFace : RideMonoBehaviour
    {
        [SerializeField] private Camera m_camera; 
        [SerializeField] private AudioSpeechFile m_audioClip; 

        private DebugMenu m_debugMenu;
        private MecanimCharacter m_character;
        private Vector2 m_faceScroll;        
        private Vector3 m_cameraInitialPosition;
        private Quaternion m_cameraInitialRotation;
        private FacialAnimationPlayer m_facialAnimator;
        private FacialAnimationPlayer.EasingEquation m_selectedEasing;

        private Dictionary<string, float> m_visemeValues = new()
        {
            { "PBM", 0 },
            { "ShCh", 0 },
            { "W", 0 },
            { "open", 0 },
            { "tBack", 0 },
            { "tRoof", 0 },
            { "tTeeth", 0 },
            { "FV", 0 },
            { "wide", 0 },
        };


        /// <summary>
        /// Initializes references to the necessary systems when the script starts.
        /// Sets the default camera if not assigned.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Systems.Get<DebugMenu>();
            m_character = FindAnyObjectByType<MecanimCharacter>();

            if (m_camera == null)
                m_camera = Camera.main;

            m_cameraInitialPosition = m_camera.transform.localPosition;
            m_cameraInitialRotation = m_camera.transform.localRotation;

            if (m_character != null)
            {
                m_facialAnimator = m_character.GetComponentInChildren<FacialAnimationPlayer>();
                if (m_facialAnimator != null)
                    m_selectedEasing = m_facialAnimator.CurvePointEasingEquation;
            }
        }


        /// <summary>
        /// Handles the GUI layout for facial animation settings in the Debug Menu.
        /// Provides controls for adjusting facial expressions, nodding, and shaking.
        /// </summary>
        public void OnGUIFace()
        {
            using (var faceScrollView = new GUILayout.ScrollViewScope(m_faceScroll))
            {
                m_faceScroll = faceScrollView.scrollPosition;

                using (new GUILayout.HorizontalScope())
                {
                    m_debugMenu.Label($"<b>Camera</b>", 100);

                    if (m_debugMenu.Button("Head"))
                    {
                        var facePos = m_cameraInitialPosition; facePos.x += 0.1f; facePos.y += 0.1f; facePos.z -= 1;
                        m_camera.transform.SetPositionAndRotation(facePos, m_cameraInitialRotation);
                    }

                    if (m_debugMenu.Button("Body"))
                        m_camera.transform.SetPositionAndRotation(m_cameraInitialPosition, m_cameraInitialRotation);
                }

                m_debugMenu.Space();

                if (m_character != null)
                {
                    using (m_debugMenu.Horizontal())
                    {
                        m_debugMenu.Label("<b>Visemes</b>", 100);
                        if (m_debugMenu.Button("All Off", 60))
                            SetAllVisemes(m_character, 0f);
                    }

                    foreach (var visemeName in m_visemeValues.Keys.ToList())
                        DrawGUIFaceSlider(m_character, visemeName);

                    m_debugMenu.Space();

                    if (m_debugMenu.Button("Nod"))
                    {
                        float amount = 0.5f;
                        float numTimes = 2.0f;
                        float duration = 2.0f;
                        m_character.Nod(amount, numTimes, duration);
                    }

                    if (m_debugMenu.Button("Shake"))
                    {
                        float amount = 0.5f;
                        float numTimes = 2.0f;
                        float duration = 1.0f;
                        m_character.Shake(amount, numTimes, duration);
                    }

                    if (m_facialAnimator != null)
                    {
                        using (m_debugMenu.Horizontal())
                        {
                            m_debugMenu.Label("Easing:", 80);
                            if (m_debugMenu.Button(m_selectedEasing.ToString()))
                            {
                                var values = (FacialAnimationPlayer.EasingEquation[]) Enum.GetValues(typeof(FacialAnimationPlayer.EasingEquation));

                                int idx = Array.IndexOf(values, m_selectedEasing);
                                if (idx < 0) idx = 0;
                                idx = (idx + 1) % values.Length;

                                m_selectedEasing = values[idx];
                                m_facialAnimator.CurvePointEasingEquation = m_selectedEasing;
                            }
                        }

                        using (m_debugMenu.Horizontal())
                        {
                            m_debugMenu.Label("Smooth Curves", 100);
                            bool newValue = m_debugMenu.Toggle(m_facialAnimator.UseCurveSmoothing, m_facialAnimator.UseCurveSmoothing ? "Enabled" : "Disabled");
                            if (newValue != m_facialAnimator.UseCurveSmoothing)
                                m_facialAnimator.UseCurveSmoothing = newValue;
                        }

                        using (m_debugMenu.Horizontal())
                        {
                            m_debugMenu.Label("Coarticulation", 100);
                            bool newValue = m_debugMenu.Toggle(m_facialAnimator.EnableCoarticulation, m_facialAnimator.EnableCoarticulation ? "Enabled" : "Disabled");
                            if (newValue != m_facialAnimator.EnableCoarticulation)
                                m_facialAnimator.EnableCoarticulation = newValue;
                        }

                        GUI.enabled = m_facialAnimator.EnableCoarticulation;
                        using (m_debugMenu.Horizontal())
                        {
                            float sliderValue = m_debugMenu.HorizontalSlider(m_facialAnimator.CoarticulationStrength, 0f, 1f);
                            string textValue = sliderValue.ToString("0.00");
                            m_debugMenu.TextField(textValue, 80);
                            if (!Mathf.Approximately(sliderValue, m_facialAnimator.CoarticulationStrength))
                                m_facialAnimator.CoarticulationStrength = sliderValue;
                        }
                        GUI.enabled = true;

                        m_debugMenu.Space();
                    }

                    if (m_debugMenu.Button("Speak"))
                    {
                        //var ttsFile = AudioSpeechFile.CreateAudioSpeechFile(facefx, nvbgResult, m_audioClip);
                        m_character.PlayAudio(m_audioClip);
                        //m_character.PlayXml(ttsFile);
                    }

                    if (m_debugMenu.Button("Stop"))
                    {
                        m_character.StopAnim();
                        m_character.StopLipSyncPerformance();
                        m_character.StopAudio();
                    }
                }
            }
        }


        /// <summary>
        /// Displays sliders for controlling specific facial animations.
        /// Allows setting viseme strength for facial expressions.
        /// </summary>
        /// <param name="character">The character whose facial expression is being controlled.</param>
        /// <param name="name">The name of the facial animation parameter.</param>
        private void DrawGUIFaceSlider(MecanimCharacter character, string name)
        {
            using (m_debugMenu.Horizontal())
            {
                var currentValue = m_visemeValues[name];
                float newValue = currentValue;
                bool changed = false;

                m_debugMenu.Label(name.Substring(0, Math.Min(8, name.Length)), 60);

                if (m_debugMenu.Button("0", 30)) { newValue = 0f; changed = true; }
                if (m_debugMenu.Button("1", 30)) { newValue = 1f; changed = true; }

                float sliderValue = m_debugMenu.HorizontalSlider(newValue, 0f, 1f);
                if (!Mathf.Approximately(sliderValue, newValue))
                {
                    newValue = sliderValue;
                    changed = true;
                }

                string textValue = newValue.ToString("0.00");
                string newTextValue = m_debugMenu.TextField(textValue, 110);

                if (!string.Equals(newTextValue, textValue, StringComparison.Ordinal))
                {
                    if (float.TryParse(newTextValue, out var parsed))
                    {
                        newValue = Mathf.Clamp01(parsed);
                        changed = true;
                    }
                }

                if (changed && !Mathf.Approximately(newValue, currentValue))
                {
                    m_visemeValues[name] = newValue;
                    Viseme(character, name, newValue);
                }
            }
        }

        private void SetAllVisemes(MecanimCharacter character, float value)
        {
            foreach (var name in m_visemeValues.Keys.ToList())
            {
                m_visemeValues[name] = value;
                Viseme(character, name, value);
            }
        }

        void Viseme(MecanimCharacter character, string name, float amount)
        {
            m_visemeValues[name] = amount;
            float neutralAmount = ComputeNeutralAmountFromVisemes();

            character.PlayViseme(name, amount); 
            character.PlayViseme("face_neutral", neutralAmount);
        }

        private float ComputeNeutralAmountFromVisemes()
        {
            // Assumption: we treat all viseme weights as sharing the 0–1 budget,
            // so neutral = 1 - sum(visemeValues), clamped to [0,1].
            float total = 0f;

            foreach (var kvp in m_visemeValues)
                total += Mathf.Clamp01(kvp.Value);

            return Mathf.Clamp01(1f - total);
        }
    }
}
