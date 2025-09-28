using System;

namespace Ride.Sensing
{
    /// <summary>
    /// Base class for any sensing system that implements all three sensing types:
    /// emotions, head pose, and facial characteristics.
    /// </summary>
    public abstract class SensingSystemUnity : RideSystemMonoBehaviour, ISensingEmotionSystem, ISensingHeadSystem, ISensingCharacteristicsSystem
    {
        protected SensingEmotionResponse m_sensingEmotionResponse = new SensingEmotionResponse("empty");
        protected SensingHeadResponse m_sensingHeadResponse = new SensingHeadResponse("empty");
        protected SensingCharacteristicsResponse m_sensingCharacteristicsResponse = new SensingCharacteristicsResponse("empty");

        /// <inheritdoc/>
        public abstract void Request(string uri, object input, Action<SensingResponse> onComplete);

        /// <inheritdoc/>
        public abstract void AnalyzeCharacteristics(object input, Action<SensingResponse> onComplete);

        /// <inheritdoc/>
        public abstract void AnalyzeEmotions(object input, Action<SensingResponse> onComplete);

        /// <inheritdoc/>
        public abstract void AnalyzeHead(object input, Action<SensingResponse> onComplete);
    }
}
