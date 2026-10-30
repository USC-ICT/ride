namespace Ride.Networking
{
    /// <summary>Controls network voice capture and transmission for implementations such as RIDE Core's <c>NetworkVoice</c>.</summary>
    public interface INetworkVoice
    {
        /// <summary>Gets or sets whether microphone recording is enabled.</summary>
        bool isRecording { get; set; }

        /// <summary>Gets or sets whether recorded audio may be transmitted.</summary>
        bool transmitEnabled { get; set; }

        /// <summary>True if the recorder is transmitting right now.</summary>
        bool isCurrentlyTransmitting { get; }

        /// <summary>Average microphone volume over the last 0.5 seconds.</summary>
        float currentAvgVolume { get; }

        /// <summary>Highest microphone volume recorded over the last 0.5 seconds.</summary>
        float currentPeakVolume { get; }

        /// <summary>Gets or sets the voice channel; members on the same channel can hear one another.</summary>
        byte channel { get; set; }

        /// <summary>Voice data will be recorded when the input is greater than or equal to this value. Range [0 - 1].</summary>
        float detectionThreshold { get; set; }

        /// <summary>Gets or sets whether voice-activity detection controls transmission.</summary>
        bool isVoiceDetectionEnabled { get; set; }

        /// <summary>Gets or sets the name of the microphone used for recording.</summary>
        string microphoneDeviceName { get; set; }

        /// <summary>Gets or sets the network identifier of the microphone stream.</summary>
        int deviceId { get; set; }

        /// <summary>Start streaming voice data from the current device.</summary>
        void StartRecording();

        /// <summary>Stop streaming voice data.</summary>
        void StopRecording();
    }
}
