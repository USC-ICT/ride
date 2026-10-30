using UnityEngine;

#if UNITY_IOS && !UNITY_EDITOR
using AOT;
using System.Runtime.InteropServices;
#endif

namespace Ride.UI
{
    /// <summary>
    /// Application-scoped sound on/off state, and the platform configuration needed to make
    /// it the only thing that governs whether the user hears audio.
    /// </summary>
    /// <remarks>
    /// On iOS the hardware ring/silent switch silences an application unless its audio
    /// session declares a category the switch does not govern. This class configures such a
    /// category, after which the switch no longer affects the application and this mute
    /// state is authoritative. The switch's position is not readable on iOS and is not
    /// needed: the state exposed here is the application's own, set by the user through an
    /// in-application control such as <see cref="RideAudioMuteToggle"/>.
    ///
    /// Add this RideSystem to a RIDE systems prefab so its lifetime and configuration are
    /// explicit. Playback starts audible on a first run by default, preserving application
    /// behavior from before this feature was added; that default and preference persistence
    /// are configurable in the Inspector. Raising the hardware volume while muted can be
    /// treated as intent to unmute.
    ///
    /// On platforms without a comparable restriction the mute state still applies through
    /// <see cref="AudioListener.volume"/>; only the session configuration is skipped.
    /// </remarks>
    public class RideAppAudio : RideSystemMonoBehaviour
    {
        private const string MutedPreferenceKey = "Ride.AppAudio.Muted";

        [Header("Mute State")]
        [Tooltip("Mute application audio when there is no saved user preference.")]
        [SerializeField] private bool m_mutedByDefault;

        [Tooltip("Remember the user's mute choice between runs.")]
        [SerializeField] private bool m_rememberMutedState = true;

        [Header("iOS Audio Session")]
        [Tooltip("Configure the iOS audio session so the in-app mute state governs whether audio is heard.")]
        [SerializeField] private bool m_configureIOSAudioSession = true;

        [Tooltip("Allow audio from other applications to continue while this application is active.")]
        [SerializeField] private bool m_allowMixingWithOtherApps;

        [Tooltip("Treat an increase in the iOS hardware output volume as a request to unmute.")]
        [SerializeField] private bool m_unmuteWhenOutputVolumeIncreases = true;

        private bool m_muted;
        private float m_lastOutputVolume = -1f;

        private static RideAppAudio s_nativeCallbackTarget;

        private delegate void OutputVolumeChangedCallback(float volume);

        /// <summary>Raised whenever <see cref="Muted"/> changes, with the new value.</summary>
        public event System.Action<bool> OnMutedChanged;

        /// <summary>
        /// Whether application audio is silenced. Setting this applies immediately, persists
        /// the choice, and raises <see cref="OnMutedChanged"/>.
        /// </summary>
        public bool Muted
        {
            get => m_muted;
            set
            {
                if (m_muted == value) return;

                m_muted = value;
                if (m_rememberMutedState)
                {
                    PlayerPrefs.SetInt(MutedPreferenceKey, value ? 1 : 0);
                    PlayerPrefs.Save();
                }

                ApplyMuteState();
                OnMutedChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// The device's current output volume, from 0 to 1, or -1 where the platform does not
        /// report it. This is the hardware volume level, not the mute state.
        /// </summary>
        public float OutputVolume => RideUtils.IsIOS() ? RideAudioSession_GetOutputVolume() : -1f;

        /// <inheritdoc />
        public override void SystemAwake()
        {
            base.SystemAwake();

            m_muted = m_rememberMutedState
                ? PlayerPrefs.GetInt(MutedPreferenceKey, m_mutedByDefault ? 1 : 0) == 1
                : m_mutedByDefault;

            ConfigurePlatformAudio();
            ApplyMuteState();
        }

        /// <inheritdoc />
        public override void SystemShutdown()
        {
            ShutdownPlatformAudio();
            OnMutedChanged = null;

            // Do not leave Unity's process-wide listener muted when this system goes away.
            AudioListener.volume = 1f;
            base.SystemShutdown();
        }

        /// <summary>Toggles <see cref="Muted"/>.</summary>
        public void ToggleMuted() => Muted = !Muted;

        private void ApplyMuteState() => AudioListener.volume = m_muted ? 0f : 1f;

        private void ConfigurePlatformAudio()
        {
            if (!m_configureIOSAudioSession || !RideUtils.IsIOS()) return;

            s_nativeCallbackTarget = this;
            RideAudioSession_Configure(m_allowMixingWithOtherApps);
            RideAudioSession_SetOutputVolumeCallback(HandleOutputVolumeChanged);
            m_lastOutputVolume = RideAudioSession_GetOutputVolume();
        }

        private void ShutdownPlatformAudio()
        {
            if (!m_configureIOSAudioSession || !RideUtils.IsIOS()) return;

            RideAudioSession_SetOutputVolumeCallback(null);
            if (s_nativeCallbackTarget == this) s_nativeCallbackTarget = null;
        }

        private void OnOutputVolumeChanged(float volume)
        {
            bool increased = m_lastOutputVolume >= 0f && volume > m_lastOutputVolume;
            m_lastOutputVolume = volume;

            if (m_unmuteWhenOutputVolumeIncreases && increased && m_muted) Muted = false;
        }

#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void RideAudioSession_Configure(bool allowMixing);

        [DllImport("__Internal")]
        private static extern float RideAudioSession_GetOutputVolume();

        [DllImport("__Internal")]
        private static extern void RideAudioSession_SetOutputVolumeCallback(OutputVolumeChangedCallback callback);

        [MonoPInvokeCallback(typeof(OutputVolumeChangedCallback))]
        private static void HandleOutputVolumeChanged(float volume) => s_nativeCallbackTarget?.OnOutputVolumeChanged(volume);
#else
        private static void RideAudioSession_Configure(bool allowMixing) { }

        private static float RideAudioSession_GetOutputVolume() => -1f;

        private static void RideAudioSession_SetOutputVolumeCallback(OutputVolumeChangedCallback callback) { }

        private static void HandleOutputVolumeChanged(float volume) { }
#endif
    }
}
