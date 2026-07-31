using System;

namespace Ride.Sensing
{
    /// <summary>
    /// Coordinate space used by face rectangles and landmarks in a sensing frame.
    /// </summary>
    public enum SensingCoordinateSpace
    {
        Unknown = 0,
        Pixels = 1,
        Normalized = 2
    }

    /// <summary>
    /// Provider-neutral sensing capabilities. Providers may implement any subset.
    /// </summary>
    [Flags]
    public enum SensingCapability
    {
        None = 0,
        FaceBounds = 1 << 0,
        FaceLandmarks = 1 << 1,
        HeadPose = 1 << 2,
        Gaze = 1 << 3,
        Emotions = 1 << 4,
        ActionUnits = 1 << 5,
        Characteristics = 1 << 6,
        BodyPose = 1 << 7,
        BodyGestures = 1 << 8,
        AudioProsody = 1 << 9,
        VoiceActivity = 1 << 10
    }

    /// <summary>
    /// Named score used for open-ended sensing results such as emotions or action units.
    /// </summary>
    [Serializable]
    public struct SensingNamedScore
    {
        public string name;
        public double score;
        public double minimum;
        public double maximum;

        public SensingNamedScore(string name, double score, double minimum = 0, double maximum = 1)
        {
            this.name = name;
            this.score = score;
            this.minimum = minimum;
            this.maximum = maximum;
        }

        /// <summary>
        /// Returns this score normalized to the range zero through one.
        /// </summary>
        public double NormalizedScore
        {
            get
            {
                if (maximum <= minimum)
                    return 0;

                double normalized = (score - minimum) / (maximum - minimum);
                return Math.Max(0, Math.Min(1, normalized));
            }
        }
    }

    /// <summary>
    /// Per-face result in a provider-neutral sensing frame.
    /// </summary>
    [Serializable]
    public class SensingFaceResult
    {
        public bool hasFace;
        public FaceRectangle faceRectangle;
        public RideVector2[] landmarks = Array.Empty<RideVector2>();
        public double pitch;
        public double roll;
        public double yaw;
        public double gazePitch;
        public double gazeYaw;
        public SensingNamedScore[] emotions = Array.Empty<SensingNamedScore>();
        public SensingNamedScore[] actionUnits = Array.Empty<SensingNamedScore>();
        public SensingCharacteristicsResponse characteristics;
        /// <summary>Provider confidence normalized to the range zero through one.</summary>
        public double confidence;
    }

    /// <summary>
    /// Provider-neutral sensing response for one captured frame.
    /// </summary>
    [Serializable]
    public class SensingFrameResponse : SensingResponse
    {
        public string provider;
        public double timestamp;
        public SensingCapability capabilities;
        public SensingCoordinateSpace coordinateSpace;
        public SensingFaceResult[] faces = Array.Empty<SensingFaceResult>();
        public string rawJson;

        public SensingFrameResponse(string response) : base(response)
        {
            rawJson = response;
        }

        public SensingFaceResult PrimaryFace
        {
            get
            {
                if (faces == null || faces.Length == 0)
                    return null;

                SensingFaceResult primaryFace = faces[0];
                for (int i = 1; i < faces.Length; i++)
                {
                    if (faces[i] != null && (primaryFace == null || faces[i].confidence > primaryFace.confidence))
                        primaryFace = faces[i];
                }

                return primaryFace;
            }
        }
    }

    /// <summary>
    /// Interface for providers that can analyze a frame once and return all supported sensing data.
    /// </summary>
    public interface ISensingFrameSystem : ISensingSystem
    {
        SensingCapability Capabilities { get; }

        void AnalyzeFrame(object input, Action<SensingFrameResponse> onComplete);
    }
}
