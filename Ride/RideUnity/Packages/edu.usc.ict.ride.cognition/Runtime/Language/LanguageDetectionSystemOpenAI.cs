using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Ride.NLP
{
    /// <summary>
    /// Result produced by a language detection system.
    /// </summary>
    public struct LanguageDetectionResult
    {
        public bool success;
        public string language;
        public float confidence;
        public string source;
        public string provider;
        public string model;
        public string details;
    }

    /// <summary>
    /// Provider-agnostic interface for detecting the language of user text.
    /// </summary>
    public interface ILanguageDetectionSystem
    {
        void DetectLanguage(string text, string inputSource, Action<LanguageDetectionResult> onComplete);
    }

    /// <summary>
    /// OpenAI-backed text language detector used as a fallback when ASR providers do not return language metadata.
    /// </summary>
    public class LanguageDetectionSystemOpenAI : RideSystemMonoBehaviour, ILanguageDetectionSystem
    {
        private const string DefaultModel = "gpt-4o-mini";
        private const float DefaultMinimumConfidence = 0.45f;
        private static LanguageDetectionSettings s_settings = LanguageDetectionSettings.Default;

        public static LanguageDetectionSettings Settings
        {
            get => NormalizeSettings(s_settings);
            set => s_settings = NormalizeSettings(value);
        }

        public void DetectLanguage(string text, string inputSource, Action<LanguageDetectionResult> onComplete)
        {
            _ = DetectLanguageAsync(text, inputSource, onComplete);
        }

        private async Task DetectLanguageAsync(string text, string inputSource, Action<LanguageDetectionResult> onComplete)
        {
            var result = new LanguageDetectionResult
            {
                success = false,
                source = $"OpenAI fallback ({inputSource})",
                provider = "OpenAI",
                model = GetModel(),
                details = "not run",
            };

            if (string.IsNullOrWhiteSpace(text))
            {
                result.details = "empty text";
                onComplete?.Invoke(result);
                return;
            }

            bool isWebGL = RideUtils.IsWebGL() && !RideUtils.IsEditor();
            string uri = isWebGL ? ConfigurationSystemUnity.GetOpenAIProxyEndpoint() : GetEndpoint();
            string apiKey = isWebGL ? string.Empty : GetApiKey();

            if (string.IsNullOrWhiteSpace(uri))
            {
                result.details = "OpenAI endpoint missing";
                onComplete?.Invoke(result);
                return;
            }

            if (!isWebGL && string.IsNullOrWhiteSpace(apiKey))
            {
                result.details = "OpenAI key missing";
                onComplete?.Invoke(result);
                return;
            }

            var messages = new[]
            {
                new OpenAIMessage
                {
                    role = "system",
                    content = "Detect the primary natural language of the user's text. Return JSON only: {\"language\":\"<BCP-47 or ISO 639-1 code>\",\"confidence\":0.0}. Use \"unknown\" when the text is too short or ambiguous."
                },
                new OpenAIMessage
                {
                    role = "user",
                    content = text
                }
            };

            var requestBody = new OpenAIQuestion
            {
                model = result.model,
                messages = messages,
                temperature = 0,
                max_tokens = 40,
                n = 1,
                stream = false,
            };

            string payload = RideIO.JsonSerializeNoObjRef(requestBody);

            using var webRequest = new UnityWebRequest(uri, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            if (!isWebGL)
                webRequest.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            var operation = webRequest.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            string response = webRequest.downloadHandler?.text ?? string.Empty;
            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                result.details = string.IsNullOrWhiteSpace(response) ? webRequest.error : response;
                Debug.LogWarning($"[LanguageDetection OpenAI] Request failed: {result.details}");
                onComplete?.Invoke(result);
                return;
            }

            if (!TryParseLanguageResponse(response, out string language, out float confidence, out string details))
            {
                result.details = details;
                Debug.LogWarning($"[LanguageDetection OpenAI] Failed to parse response: {details}");
                onComplete?.Invoke(result);
                return;
            }

            result.language = NormalizeLanguage(language);
            result.confidence = Mathf.Clamp01(confidence);
            result.details = details;
            result.success = !string.IsNullOrWhiteSpace(result.language) &&
                !string.Equals(result.language, "unknown", StringComparison.OrdinalIgnoreCase) &&
                result.confidence >= GetMinimumConfidence();

            Debug.Log($"[LanguageDetection OpenAI] source='{inputSource}' language='{(string.IsNullOrWhiteSpace(result.language) ? "unknown" : result.language)}' confidence={result.confidence:F2} success={result.success}");
            onComplete?.Invoke(result);
        }

        private static bool TryParseLanguageResponse(string response, out string language, out float confidence, out string details)
        {
            language = string.Empty;
            confidence = 0f;
            details = string.Empty;

            try
            {
                JObject responseObject = JObject.Parse(response);
                string content = responseObject["choices"]?[0]?["message"]?["content"]?.ToString();
                if (string.IsNullOrWhiteSpace(content))
                {
                    details = "empty OpenAI message content";
                    return false;
                }

                JObject contentObject = JObject.Parse(ExtractJsonObject(content));
                language = contentObject["language"]?.ToString() ?? string.Empty;
                confidence = contentObject["confidence"]?.Value<float>() ?? 0f;
                details = content.Trim();
                return true;
            }
            catch (Exception e)
            {
                details = $"{e.GetType().Name}: {e.Message}";
                return false;
            }
        }

        private static string ExtractJsonObject(string content)
        {
            content = content.Trim();
            int start = content.IndexOf('{');
            int end = content.LastIndexOf('}');
            if (start >= 0 && end >= start)
                return content.Substring(start, end - start + 1);

            return content;
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return string.Empty;

            return language.Trim();
        }

        private static LanguageDetectionSettings GetSettings()
        {
            return Settings;
        }

        private static string GetEndpoint()
        {
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            return configSystem != null ? configSystem.Config.openAIChatGPT.endpoint : string.Empty;
        }

        private static string GetApiKey()
        {
            var configSystem = Systems.Get<ConfigurationSystemUnity>();
            return configSystem != null ? configSystem.Config.openAIChatGPT.endpointKey : string.Empty;
        }

        private static string GetModel()
        {
            string model = GetSettings().model;
            return string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
        }

        private static float GetMinimumConfidence()
        {
            float minimumConfidence = GetSettings().minimumConfidence;
            return minimumConfidence > 0f ? minimumConfidence : DefaultMinimumConfidence;
        }

        private static LanguageDetectionSettings NormalizeSettings(LanguageDetectionSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.provider))
                settings.provider = LanguageDetectionSettings.Default.provider;
            if (string.IsNullOrWhiteSpace(settings.model))
                settings.model = DefaultModel;
            if (settings.minimumConfidence <= 0f)
                settings.minimumConfidence = DefaultMinimumConfidence;

            return settings;
        }
    }
}
