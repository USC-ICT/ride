using System;

namespace Ride.GenerativeAI
{
    /// <summary>
    /// Holds image generation request to be sent, typically a user prompt.
    /// </summary>
    public class GenerativeAIImagePrompt : GenerativeAIRequest
    {
        public string prompt;
        public ImageResponseType imageResponseType;

        public GenerativeAIImagePrompt(string prompt)
        {
            this.prompt = prompt;
        }

        public GenerativeAIImagePrompt(string prompt, ImageResponseType imageResponseType)
        {
            this.prompt = prompt;
            this.imageResponseType = imageResponseType;
        }
    }

    /// <summary>
    /// Holds image generation response, array of URLs to generated results or raw data.
    /// </summary>
    public class GenerativeAIImageResult : GenerativeAIResponse
    {
        public string serviceProvider = "unknown";
        public ImageResponseType imageResponseType;
        public string[] m_generatedImageResults;

        public GenerativeAIImageResult(string response) : base(response) { }

        public GenerativeAIImageResult(string response, ImageResponseType imageResponseType, string[] results) : base(response)
        {
            this.response = response;
            this.m_generatedImageResults = results;
            this.imageResponseType = imageResponseType;
        }

        public GenerativeAIImageResult(string response, string serviceProvider, ImageResponseType imageResponseType, string[] results) : base(response)
        {
            this.response = response;
            this.serviceProvider = serviceProvider;            
            this.imageResponseType = imageResponseType; 
            this.m_generatedImageResults = results;
        }
    }

    /// <summary>
    /// Whether the result is a URL to download the image from or Base64 data that needs to be converted
    /// </summary>
    public enum ImageResponseType
    {
        URL = 0,
        Base64 = 1
    }

    /// <summary>
    /// Interface for calling image generation service.
    /// </summary>
    public interface IGenerativeAIImageSystem : IGenerativeAISystem
    {
        /// <summary>
        /// Request service to generate image based in input prompt.
        /// </summary>
        /// <param name="prompt">Prompt to generate image</param>
        /// <param name="onComplete">Delegate to execute on successful request</param>
        void GenerateImage(string prompt, Action<GenerativeAIImageResult> onComplete);
    }
}
