using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;


namespace Ride.Sensing
{
    /// <summary>
    /// Microsoft Azure Face-based implementation of <see cref="SensingSystemUnity"/>.
    /// Supports emotion, head pose, and facial characteristic analysis using Azure’s REST API.
    /// </summary>
    public class SensingSystemAzureFace : SensingSystemUnity
    {
        private class RequestState
        {
            public bool processsing;
            public AzureSensingServiceRequest? nextRequest;
            public AzureFace.AzureFaceResponse response;
        }

        public string m_uri;
        public string m_endpointKey;

        Queue<AzureSensingServiceRequest> m_requestQueue = new Queue<AzureSensingServiceRequest>();
        Dictionary<string, RequestState> m_responseLookup = new Dictionary<string, RequestState>();

        readonly string m_headRequestParamters = "&returnFaceLandmarks=true&returnFaceAttributes=headPose";
        readonly string m_emotionRequestParamters = "&returnFaceAttributes=emotion";
        readonly string m_characteristicrequestParamters = "&returnFaceAttributes=smile,facialHair,glasses,hair,makeup,occlusion,accessories,blur,exposure,noise";

        private struct AzureSensingServiceRequest
        {
            public string requestURI;
            public string action;
            public byte[] content;
            public Action<SensingResponse> onCompleteDelegate;
        };

        /// <inheritdoc/>
        public override void SystemInit()
        {
            var configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();

            m_uri = configSystem.config.azureFace.endpoint + "/face/v1.0/detect?returnFaceId=true";
            m_endpointKey = configSystem.config.azureFace.endpointKey;

            base.SystemInit();
        }

        /// <inheritdoc/>
        override public void SystemUpdate(float dt)
        {
            while (m_requestQueue.Count != 0)  // If items in the queue
            {
                AzureSensingServiceRequest request = m_requestQueue.Dequeue();

                if (!m_responseLookup.TryGetValue(request.requestURI, out RequestState requestState) || !requestState.processsing)
                    Request(request.requestURI, request.content, request.onCompleteDelegate);

                WaitForRequestComplete(request);
            }
        }

        /// <inheritdoc/>
        public override async void Request(string uri, object input, Action<SensingResponse> onComplete)
        {
            if (!m_responseLookup.TryAdd(uri, new RequestState()
            {
                nextRequest = null,
                processsing = true
            }))
                m_responseLookup[uri].processsing = true;

            // Call web service. TODO, refactor to generic TSSIO POST version
            HttpResponseMessage response;
            byte[] byteData = (byte[])input;

            string contentString;
            using (var client = new HttpClient())

            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Headers.Add("Ocp-Apim-Subscription-Key", m_endpointKey);
                request.Content = new ByteArrayContent(byteData);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                response = await client.SendAsync(request);
                contentString = await response.Content.ReadAsStringAsync();
            }

            // Process response
            if (contentString != "[]")  // No face found
            {
                //UnityEngine.Debug.Log(contentString);
                if (contentString.Contains("error"))    //TODO: This is temporary fix as Azure has deprecated emotion/gender/age etc processing. Would need to organize sensing api to remove these features.
                    return;
                m_responseLookup[uri].response = RideIO.JsonDeserialize<AzureFace.AzureFaceResponse[]>(contentString)[0]; // Parse first face only for now
            }

            m_responseLookup[uri].processsing = false;

            if (m_responseLookup[uri].nextRequest != null)
            {
                AzureSensingServiceRequest newRequest = (AzureSensingServiceRequest)m_responseLookup[uri].nextRequest;
                m_responseLookup[uri].nextRequest = null;

                AddRequestToQueue(newRequest);
            }
        }

        /// <inheritdoc/>
        public override void AnalyzeEmotions(object input, Action<SensingResponse> onComplete)
        {
            AddRequestToQueue(new AzureSensingServiceRequest()
            {
                requestURI = m_uri + m_emotionRequestParamters,
                action = "AnalyzeEmotions",
                content = (byte[])input,
                onCompleteDelegate = onComplete
            });
        }

        /// <inheritdoc/>
        public override void AnalyzeHead(object input, Action<SensingResponse> onComplete)
        {
            AddRequestToQueue(new AzureSensingServiceRequest()
            {
                requestURI = m_uri + m_headRequestParamters,
                action = "AnalyzeHead",
                content = (byte[])input,
                onCompleteDelegate = onComplete
            });
        }

        /// <inheritdoc/>
        public override void AnalyzeCharacteristics(object input, Action<SensingResponse> onComplete)
        {
            AddRequestToQueue(new AzureSensingServiceRequest()
            {
                requestURI = m_uri + m_characteristicrequestParamters,
                action = "AnalyzeCharacteristics",
                content = (byte[])input,
                onCompleteDelegate = onComplete
            });
        }

        /// <summary>
        /// Enqueues a request or schedules it for execution depending on current processing state.
        /// </summary>
        /// <param name="request">The sensing service request to add.</param>
        void AddRequestToQueue(AzureSensingServiceRequest request)
        {
            if (!m_responseLookup.TryGetValue(request.requestURI, out RequestState requestState))
            {
                m_requestQueue.Enqueue(request);
                return;
            }

            if (requestState.nextRequest != null)
            {
                if (requestState.processsing)
                    WaitForRequestComplete((AzureSensingServiceRequest)requestState.nextRequest); //Discarding qued request if busy
                else
                    m_requestQueue.Enqueue((AzureSensingServiceRequest)requestState.nextRequest); //Otherwise q this request
            }

            requestState.nextRequest = request;
        }

        /// <summary>
        /// Asynchronously waits for an Azure request to complete, then invokes the correct response callback.
        /// </summary>
        /// <param name="request">The original request to resolve.</param>
        private async void WaitForRequestComplete(AzureSensingServiceRequest request)
        {
            while (m_responseLookup[request.requestURI].processsing) { await Task.Delay(50); }

            // When done, invoke appropriate delegate
            if (request.action.Equals("AnalyzeEmotions"))
            {
                AzureFace.Emotion emotion = m_responseLookup[request.requestURI].response.faceAttributes.emotion;
                m_sensingEmotionResponse.anger = emotion.anger;
                m_sensingEmotionResponse.contempt = emotion.contempt;
                m_sensingEmotionResponse.disgust = emotion.disgust;
                m_sensingEmotionResponse.fear = emotion.fear;
                m_sensingEmotionResponse.happiness = emotion.happiness;
                m_sensingEmotionResponse.neutral = emotion.neutral;
                m_sensingEmotionResponse.sadness = emotion.sadness;
                m_sensingEmotionResponse.surprise = emotion.surprise;

                request.onCompleteDelegate?.Invoke(m_sensingEmotionResponse);
            }

            if (request.action.Equals("AnalyzeHead"))
            {
                AzureFace.FaceAttributes faceAttributes = m_responseLookup[request.requestURI].response.faceAttributes;
                m_sensingHeadResponse.pitch = faceAttributes.headPose.pitch;
                m_sensingHeadResponse.roll = faceAttributes.headPose.roll;
                m_sensingHeadResponse.yaw = faceAttributes.headPose.yaw;
                m_sensingHeadResponse.foreheadOccluded = faceAttributes.occlusion.foreheadOccluded;
                m_sensingHeadResponse.eyeOccluded = faceAttributes.occlusion.eyeOccluded;
                m_sensingHeadResponse.mouthOccluded = faceAttributes.occlusion.mouthOccluded;
                m_sensingHeadResponse.landmarks = m_responseLookup[request.requestURI].response.faceLandmarks.ToArray();
                m_sensingHeadResponse.faceRectangle = m_responseLookup[request.requestURI].response.faceRectangle;

                request.onCompleteDelegate?.Invoke(m_sensingHeadResponse);
            }

            if (request.action.Equals("AnalyzeCharacteristics"))
            {
                AzureFace.FaceAttributes faceAttributes = m_responseLookup[request.requestURI].response.faceAttributes;

                m_sensingCharacteristicsResponse.gender = faceAttributes.gender;
                m_sensingCharacteristicsResponse.age = faceAttributes.age;
                m_sensingCharacteristicsResponse.glasses = faceAttributes.glasses;
                m_sensingCharacteristicsResponse.moustache = faceAttributes.facialHair.moustache;
                m_sensingCharacteristicsResponse.beard = faceAttributes.facialHair.beard;

                request.onCompleteDelegate?.Invoke(m_sensingCharacteristicsResponse);
            }
        }
    }
}
