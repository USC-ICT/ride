using System;

namespace Ride.Sensing
{
    /// <summary>
    /// Rectangular boundary that encloses a detected face.
    /// </summary>
    [Serializable]
    public struct FaceRectangle
    {
        public float top;
        public float left;
        public float width;
        public float height;

        public FaceRectangle(float _top, float _left, float _width, float _height) { top = _top; left = _left; width = _width; height = _height; }
    }

    /// <summary>
    /// Request wrapper for analyzing head orientation and occlusion.
    /// </summary>
    [Serializable]
    public class SensingHeadRequest : SensingRequest
    {
        public object input;

        public SensingHeadRequest(object input)
        {
            this.input = input;
        }
    }

    /// <summary>
    /// Response with head pose data and facial occlusion information.
    /// </summary>
    [Serializable]
    public class SensingHeadResponse : SensingResponse
    {
        public bool normalizedVectors;
        public RideVector2[] landmarks;
        public FaceRectangle faceRectangle;
        public double pitch;
        public double roll;
        public double yaw;
        public bool foreheadOccluded;
        public bool eyeOccluded;
        public bool mouthOccluded;
        public SensingHeadResponse(string response) : base(response) { }

        public SensingHeadResponse(string response, double pitch, double roll, double yaw, bool foreheadOccluded, bool eyeOccluded, bool mouthOccluded, RideVector2[] landmarks = null) : base(response)
        {
            this.pitch = pitch;
            this.roll = roll;
            this.yaw = yaw;
            this.foreheadOccluded = foreheadOccluded;
            this.eyeOccluded = eyeOccluded;
            this.mouthOccluded = mouthOccluded;
            this.landmarks = landmarks;
        }
    }

    /// <summary>
    /// Interface for analyzing head orientation and occlusion from visual input.
    /// </summary>
    public interface ISensingHeadSystem : ISensingSystem
    {
        /// <summary>
        /// Analyzes the head pose and facial occlusion state from a given image.
        /// </summary>
        /// <param name="input">The image data to analyze.</param>
        /// <param name="onComplete">Callback triggered after analysis completes.</param>
        void AnalyzeHead(object input, Action<SensingResponse> onComplete);
    }
}
