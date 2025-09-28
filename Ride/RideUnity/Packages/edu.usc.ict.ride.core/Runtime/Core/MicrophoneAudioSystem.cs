using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride.Audio;
using System;

namespace Ride.Audio
{
    /// <summary>
    /// Analyzes input microphone device audio
    /// </summary>
    public class MicrophoneAudioSystem : RideSystemMonoBehaviour, IDeviceAudioSystem<AudioStreamUnity>
    {
        [SerializeField] int m_frequency = 44100;
        [SerializeField] int m_recordLength = 10;
        [SerializeField] string m_currentDevice = "";

        AudioStreamUnity m_audioStream = new AudioStreamUnity();

        float[] m_sampleBuffer;

        public event Action<AudioStreamUnity> OnStartedRecording;
        public event Action<AudioStreamUnity> OnFinishedRecording;

        public bool IsRecording => MicrophoneIsRecording(DeviceName);

        public int DeviceCount => MicrophoneDevices().Length;

        public bool CanRecord => !string.IsNullOrEmpty(DeviceName) && DeviceCount > 0;

        public string DeviceName => m_currentDevice;


        public override void SystemInit()
        {
            base.SystemInit();
            SetRecordingDevice();
        }

        public void SetRecordingDevice(string deviceName)
        {
            if (DoesDeviceExist(deviceName))
            {
                m_currentDevice = deviceName;

            }
        }

        public void SetRecordingDevice(int deviceIndex)
        {
            if (!(deviceIndex < 0 || deviceIndex >= DeviceCount))
            {
                SetRecordingDevice(MicrophoneDevices()[deviceIndex]);
            }

        }

            public void SetRecordingDevice()
        {
            SetRecordingDevice(0);
        }

        public AudioStreamUnity StartRecording()
        {
            if (!CanRecord) return null;

            m_audioStream.Clip = MicrophoneStart(DeviceName, true, m_recordLength, m_frequency);

            if (m_sampleBuffer == null)
            {
                m_sampleBuffer = new float[m_audioStream.Clip.samples];
            }

            OnStartedRecording?.Invoke(m_audioStream);
            return m_audioStream;
        }

        public void StopRecording()
        {
            if (!IsRecording) return;

            // only use audio data up to where the current pos of the mic is
            int micPos = MicrophoneGetPosition(DeviceName);
            if (micPos <= 0) return;

            // extract clip bugger data up to the point of the mic pos
            float[] clipData = new float[micPos];
            m_audioStream.Clip.GetData(clipData, 0);

            // copy the data into a new clip
            AudioClip clip = AudioClip.Create("micStreamingAudio", micPos, m_audioStream.Clip.channels, m_audioStream.Clip.frequency, false);
            clip.SetData(clipData, 0);

            var completedUtterance = new AudioStreamUnity(clip);

            MicrophoneEnd(DeviceName);

            m_audioStream.StreamComplete = true;

            OnFinishedRecording?.Invoke(completedUtterance);
        }

        public float GetRecordingVolumeLevel()
        {
            return GetRecordingVolumeLevel(MicrophoneGetPosition(DeviceName) - 1);
        }

        public float GetRecordingVolumeLevel(int micPos)
        {
            float volume = 0;
            if (IsRecording && m_sampleBuffer != null)
            {
                m_audioStream.Clip.GetData(m_sampleBuffer, 0);
                int pos = Mathf.Clamp(micPos, 0, m_sampleBuffer.Length);
                volume = Mathf.Clamp01(Mathf.Abs(m_sampleBuffer[pos]));
            }
            return volume;
        }

        bool DoesDeviceExist(string deviceName)
        {
            return !Array.TrueForAll<string>(MicrophoneDevices(), s => s != deviceName);
        }

        public bool IsDeviceSilent(float silenceThreshold)
        {
            float micVolume = GetRecordingVolumeLevel();
            return micVolume >= -silenceThreshold && micVolume <= silenceThreshold;
        }

        #region  Microphone Wrapper
        bool MicrophoneIsRecording(string deviceName)
        {
#if UNITY_WEBGL
        return false;
#else
            return Microphone.devices.Length > 0 ? Microphone.IsRecording(deviceName) : false;
#endif
        }

        AudioClip MicrophoneStart(string deviceName, bool loop, int lengthSec, int frequency)
        {
#if UNITY_WEBGL
        return null;
#else
            return Microphone.Start(deviceName, loop, lengthSec, frequency);
#endif
        }

        void MicrophoneEnd(string deviceName)
        {
#if UNITY_WEBGL
#else
            Microphone.End(deviceName);
#endif
        }

        int MicrophoneGetPosition(string deviceName)
        {
#if UNITY_WEBGL
        return 0;
#else
            return Microphone.GetPosition(deviceName);
#endif
        }

        string[] MicrophoneDevices()
        {
#if UNITY_WEBGL
        return new string [] { };
#else
            return Microphone.devices;
#endif
        }
        #endregion
    }
}