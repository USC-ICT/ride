using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Ride.Audio
{
    /// <summary>
    /// Interface for an audiostream container
    /// </summary>
    public interface IAudioStream
    {
        bool StreamComplete { get; }
        int Frequency { get; }
        int Channels { get; }
        int Samples { get; }
    }
    
    /// <summary>
    /// Interface for analysing device audio input
    /// </summary>
    /// <typeparam name="T">An IAudioStream Implementation</typeparam>
    public interface IDeviceAudioSystem<T> : IRideSystem where T : IAudioStream
    {
        bool IsRecording { get; }
        bool CanRecord { get; }
        int DeviceCount { get; }
        string DeviceName { get; }

        event System.Action<T> OnStartedRecording;
        event System.Action<T> OnFinishedRecording;

        T StartRecording();
        void StopRecording();

        bool IsDeviceSilent(float silenceThreshold);
        float GetRecordingVolumeLevel(int micPos = 0);
        void SetRecordingDevice();
        void SetRecordingDevice(string deviceName);
        void SetRecordingDevice(int deviceIndex);
    }
}