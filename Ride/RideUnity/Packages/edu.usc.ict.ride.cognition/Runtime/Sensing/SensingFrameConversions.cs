using System;

namespace Ride.Sensing
{
    /// <summary>
    /// Converts provider-neutral frame results into the legacy sensing response types.
    /// </summary>
    public static class SensingFrameConversions
    {
        /// <summary>
        /// Converts a face result into the legacy head-pose response.
        /// </summary>
        /// <param name="frame">Frame containing coordinate-space and raw-response metadata.</param>
        /// <param name="face">Face to convert.</param>
        /// <returns>A head response, or an empty response when no face is supplied.</returns>
        public static SensingHeadResponse ToHeadResponse(SensingFrameResponse frame, SensingFaceResult face)
        {
            string rawResponse = frame != null ? frame.rawJson : "empty";
            if (face == null)
                return new SensingHeadResponse(rawResponse);

            return new SensingHeadResponse(rawResponse, face.pitch, face.roll, face.yaw, false, false, false, face.landmarks)
            {
                faceRectangle = face.faceRectangle,
                normalizedVectors = frame != null && frame.coordinateSpace == SensingCoordinateSpace.Normalized
            };
        }

        /// <summary>
        /// Converts normalized named emotion scores into the legacy emotion response.
        /// </summary>
        /// <param name="frame">Frame containing raw-response metadata.</param>
        /// <param name="face">Face to convert.</param>
        /// <returns>An emotion response with unsupported emotions left at zero.</returns>
        public static SensingEmotionResponse ToEmotionResponse(SensingFrameResponse frame, SensingFaceResult face)
        {
            var response = new SensingEmotionResponse(frame != null ? frame.rawJson : "empty");
            if (face?.emotions == null)
                return response;

            foreach (var score in face.emotions)
            {
                string name = score.name != null ? score.name.ToLowerInvariant() : string.Empty;
                double value = score.NormalizedScore;

                if (name == "anger" || name == "angry") response.anger = value;
                else if (name == "contempt") response.contempt = value;
                else if (name == "disgust" || name == "disgusted") response.disgust = value;
                else if (name == "fear" || name == "fearful") response.fear = value;
                else if (name == "happiness" || name == "happy") response.happiness = value;
                else if (name == "neutral" || name == "calm") response.neutral = value;
                else if (name == "sadness" || name == "sad") response.sadness = value;
                else if (name == "surprise" || name == "surprised") response.surprise = value;
            }

            return response;
        }

        /// <summary>
        /// Tests whether a response advertises every requested capability.
        /// </summary>
        /// <param name="frame">Frame to inspect.</param>
        /// <param name="capability">Capability flags that must all be present.</param>
        /// <returns><c>true</c> when the requested capabilities are present.</returns>
        public static bool HasCapability(SensingFrameResponse frame, SensingCapability capability)
        {
            return frame != null && (frame.capabilities & capability) == capability;
        }
    }
}
