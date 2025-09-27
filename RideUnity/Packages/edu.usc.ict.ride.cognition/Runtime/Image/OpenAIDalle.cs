
namespace Ride.GenerativeAI
{
    /// <summary>
    /// Provides support for OpenAI DALL-E image generation (https://openai.com/dall-e-2).
    /// </summary>
    public class OpenAIDalle
    {
        /// <summary>
        /// OpenAI DALL-E request data struct
        /// </summary>
        [System.Serializable]
        public struct OpenAIDalleRequest
        {
            public string model;
            public string prompt;
            public int n;
            public string response_format;
            public string user;
        }

        /// <summary>
        /// OpenAI DALL-E response data struct
        /// </summary>
        [System.Serializable]
        public struct OpenAIDalleResponse
        {
            public int created;
            public OpenAIDalleData[] data;
        }

        /// <summary>
        /// OpenAI DALL-E request data support struct
        /// </summary>
        [System.Serializable]
        public struct OpenAIDalleData
        {
            public string url;
        }
    }
}