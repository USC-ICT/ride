using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Networking
{
    abstract public class NetworkVoice : MonoBehaviour, INetworkVoice
    {
        public abstract bool isRecording { get; set; }
        public abstract bool transmitEnabled { get; set; }
        public abstract bool isCurrentlyTransmitting { get; }
        public abstract float currentAvgVolume { get; }
        public abstract float currentPeakVolume { get; }
        public abstract byte channel { get; set; }
        public abstract float detectionThreshold { get; set; }
        public abstract bool isVoiceDetectionEnabled { get; set; }
        public abstract string microphoneDeviceName { get; set; }
        public abstract int deviceId { get; set; }

        public abstract void StartRecording();
        public abstract void StopRecording();
    }
}
