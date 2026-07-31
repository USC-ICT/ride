using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace Ride.Sensing
{
    /// <summary>
    /// Local OpenFace 3.0 sensing provider backed by the RIDE OpenFace HTTP bridge.
    /// </summary>
    public class SensingSystemOpenFace : RideSystemMonoBehaviour, ISensingFrameSystem, ISensingHeadSystem
    {
        [Header("OpenFace Local Service")]
        [SerializeField] string m_analyzeUri = "http://127.0.0.1:5101/analyze";
        [SerializeField] float m_timeoutSeconds = 10f;
        [SerializeField] bool m_logRawResponse;

        readonly Queue<OpenFaceSensingServiceRequest> m_requestQueue = new Queue<OpenFaceSensingServiceRequest>();
        HttpClient m_httpClient;
        bool m_requestInProgress;

        /// <inheritdoc/>
        public SensingCapability Capabilities => OpenFaceSensingResponseParser.Capabilities;

        /// <inheritdoc/>
        public override void SystemUpdate(float dt)
        {
            base.SystemUpdate(dt);

            if (m_requestInProgress || m_requestQueue.Count == 0)
                return;

            var request = m_requestQueue.Dequeue();
            while (m_requestQueue.Count > 0)
            {
                var outdatedRequest = request;
                request = m_requestQueue.Dequeue();
                request.onCompleteDelegate += outdatedRequest.onCompleteDelegate;
            }

            SendRequest(m_analyzeUri, request.content, request.onCompleteDelegate);
        }

        /// <inheritdoc/>
        public override void SystemShutdown()
        {
            base.SystemShutdown();
            m_httpClient?.Dispose();
            m_httpClient = null;
        }

        /// <inheritdoc/>
        public void AnalyzeFrame(object input, Action<SensingFrameResponse> onComplete)
        {
            if (input is not byte[] content || content.Length == 0)
            {
                onComplete?.Invoke(OpenFaceSensingResponseParser.CreateEmpty("OpenFace input must contain image bytes."));
                return;
            }

            m_requestQueue.Enqueue(new OpenFaceSensingServiceRequest
            {
                content = content,
                onCompleteDelegate = onComplete
            });
        }

        /// <inheritdoc/>
        public void AnalyzeHead(object input, Action<SensingResponse> onComplete)
        {
            AnalyzeFrame(input, response =>
                onComplete?.Invoke(SensingFrameConversions.ToHeadResponse(response, response?.PrimaryFace)));
        }

        /// <inheritdoc/>
        public void Request(string uri, object input, Action<SensingResponse> onComplete)
        {
            if (input is not byte[] content || content.Length == 0)
            {
                onComplete?.Invoke(OpenFaceSensingResponseParser.CreateEmpty("OpenFace input must contain image bytes."));
                return;
            }

            SendRequest(uri, content, response => onComplete?.Invoke(response));
        }

        async void SendRequest(string uri, byte[] imageBytes, Action<SensingFrameResponse> onComplete)
        {
#if UNITY_WEBGL
            await System.Threading.Tasks.Task.Delay(0);
            onComplete?.Invoke(OpenFaceSensingResponseParser.CreateEmpty("OpenFace local sensing is not available in WebGL."));
#else
            m_requestInProgress = true;
            var stopwatch = Stopwatch.StartNew();
            SensingFrameResponse sensingResponse;

            var request = new OpenFaceAnalyzeRequest
            {
                image_base64 = Convert.ToBase64String(imageBytes),
                include_landmarks = true,
                include_gaze = true,
                include_emotions = true,
                include_action_units = true
            };

            try
            {
                EnsureHttpClient();
                string jsonPayload = JsonConvert.SerializeObject(request);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                HttpResponseMessage httpResponse = await m_httpClient.PostAsync(uri, content);
                string responseBody = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    UnityEngine.Debug.LogWarning($"OpenFace local sensing request failed ({(int)httpResponse.StatusCode}): {responseBody}");
                    sensingResponse = OpenFaceSensingResponseParser.CreateEmpty(responseBody);
                }
                else
                {
                    if (m_logRawResponse)
                        UnityEngine.Debug.Log($"OpenFace local sensing response: {responseBody}");

                    sensingResponse = OpenFaceSensingResponseParser.Parse(responseBody);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"OpenFace local sensing request failed after {stopwatch.ElapsedMilliseconds} ms: {e}");
                sensingResponse = OpenFaceSensingResponseParser.CreateEmpty(e.Message);
            }
            finally
            {
                stopwatch.Stop();
                m_requestInProgress = false;
            }

            try
            {
                onComplete?.Invoke(sensingResponse);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"OpenFace sensing result handler failed: {e}");
            }
#endif
        }

        void EnsureHttpClient()
        {
            if (m_httpClient != null)
                return;

            m_httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(Math.Max(1f, m_timeoutSeconds))
            };
        }

        struct OpenFaceSensingServiceRequest
        {
            public byte[] content;
            public Action<SensingFrameResponse> onCompleteDelegate;
        }

        class OpenFaceAnalyzeRequest
        {
            public string image_base64;
            public bool include_landmarks;
            public bool include_gaze;
            public bool include_emotions;
            public bool include_action_units;
        }
    }
}
