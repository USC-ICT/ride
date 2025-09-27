
namespace Ride.GenerativeAI
{
    /// <summary>
    /// Provides support for Stability AI Stable Diffusion image generation (https://stability.ai/stablediffusion).
    /// </summary>
    public class StableDiffusion
    {
        /// <summary>
        /// Stability AI Stable Diffusion request data struct
        /// </summary>
        [System.Serializable]
        public struct StableDiffusionRequest
        {
            public int cfg_scale;
            public string clip_guidance_preset;
            public int height;
            public int width;            
            public int samples;
            public int steps;
            public SDTextPrompts[] text_prompts;           
        }

        public struct SDTextPrompts
        {
            public string text;
            public int weight;
        }

        /// <summary>
        /// Stability AI Stable Diffusion response data struct
        /// </summary>
        [System.Serializable]
        public struct StableDiffusionResponse
        {
            public SDArtifact[] artifacts;
        }

        public struct SDArtifact
        {
            public string base64;
            public string finishReason;
            public string seed;
        }
    }
}