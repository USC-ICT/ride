namespace Ride.Networking
{
    /// <summary>Interface for using voice communication.</summary>
    public interface INetworkVoice
    {
        /// <summary>Get/Set the whether you are recording or not.</summary>
        bool isRecording { get; set; }

        /// <summary>Get/Set whether you are transmitting audio.</summary>
        bool transmitEnabled { get; set; }

        /// <summary>True if the recorder is transmitting right now.</summary>
        bool isCurrentlyTransmitting { get; }

        /// <summary>Average microphone volume over the last 0.5 seconds.</summary>
        float currentAvgVolume { get; }

        /// <summary>Highest microphone volume recorded over the last 0.5 seconds.</summary>
        float currentPeakVolume { get; }

        /// <summary>Members listening to the same channel will hear each other.</summary>
        byte channel { get; set; }

        /// <summary>Voice data will be recorded when the input is greater than or equal to this value. Range [0 - 1].</summary>
        float detectionThreshold { get; set; }

        /// <summary>Get/set if voice detection is used.</summary>
        bool isVoiceDetectionEnabled { get; set; }

        /// <summary>Name of the device that will do the recording.</summary>
        string microphoneDeviceName { get; set; }

        /// <summary>Get/Sets the microphone network id for steaming.</summary>
        int deviceId { get; set; }

        /// <summary>Start streaming voice data from the current device.</summary>
        void StartRecording();

        /// <summary>Stop streaming voice data.</summary>
        void StopRecording();
    }
}
