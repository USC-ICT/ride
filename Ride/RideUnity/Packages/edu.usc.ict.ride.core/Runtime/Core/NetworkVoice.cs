using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ride.Networking
{
    /// <summary>
    /// Unity-facing abstract base class for Ride voice-chat components.
    /// </summary>
    /// <remarks>
    /// This class exists so Ride code can depend on a backend-agnostic <see cref="INetworkVoice"/> component while concrete networking
    /// packages provide the actual recorder implementation. In the current codebase, <see cref="PhotonNetworkVoice"/> is the only concrete implementation.
    /// </remarks>
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
