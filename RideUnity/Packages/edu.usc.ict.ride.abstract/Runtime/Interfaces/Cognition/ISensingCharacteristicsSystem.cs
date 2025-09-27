using System;

namespace Ride.Sensing
{
    /// <summary>
    /// Request wrapper for analyzing facial characteristics (e.g. age, gender).
    /// </summary>
    [Serializable]
    public class SensingCharacteristicsRequest : SensingRequest
    {
        public Object input;

        public SensingCharacteristicsRequest(Object input)
        {
            this.input = input;
        }
    }

    /// <summary>
    /// Response for facial characteristics analysis, including age, gender, and facial features.
    /// </summary>
    [Serializable]
    public class SensingCharacteristicsResponse : SensingResponse
    {
        public string gender;
        public double age;
        public string hairColor;
        public string glasses;
        public double moustache;
        public double beard;

        public SensingCharacteristicsResponse(string response) : base(response) { }

        public SensingCharacteristicsResponse(string response, string gender, double age, string hairColor, string glasses, double moustache, double beard) : base(response)
        {
            this.gender = gender;
            this.age = age;
            this.hairColor = hairColor;
            this.glasses = glasses;
            this.moustache = moustache;
            this.beard = beard;
        }
    }

    /// <summary>
    /// Interface for analyzing human facial characteristics.
    /// </summary>
    public interface ISensingCharacteristicsSystem : ISensingSystem
    {
        /// <summary>
        /// Sends an image for analysis to detect physical characteristics such as age and gender.
        /// </summary>
        /// <param name="input">The input image to analyze.</param>
        /// <param name="onComplete">Callback triggered when analysis is complete.<see cref="SensingResponse"/></param>
        void AnalyzeCharacteristics(Object input, Action<SensingResponse> onComplete);
    }
}
