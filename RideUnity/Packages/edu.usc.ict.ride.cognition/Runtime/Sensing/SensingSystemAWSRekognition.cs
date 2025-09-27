using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
#if !UNITY_WEBGL
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
#endif

namespace Ride.Sensing
{
    /// <summary>
    /// Amazon Rekognition-based implementation of sensing.
    /// Supports detection of emotions, head pose, and facial characteristics.
    /// </summary>
    public class SensingSystemAWSRekognition : SensingSystemUnity
    {
        /// <summary>
        /// Represents state for an AWS Rekognition request of a specific type.
        /// </summary>
        private class RequestState
        {
            public bool processing;
            public AWSRekognitionServiceRequest nextRequest; //Qued request

            //Requests that are marked outdated 
            //Meaning that these were sent while we were still processing a previous request
            public List<AWSRekognitionServiceRequest> outdatedRequests;

#if !UNITY_WEBGL
            public Action<Action<SensingResponse>, FaceDetail> onResponseDelegate;
#endif
        }

        /// <summary>
        /// Represents a single queued AWS Rekognition service request.
        /// </summary>
        private class AWSRekognitionServiceRequest
        {
            public string requestType;
#if !UNITY_WEBGL
            public DetectFacesRequest requestData;
#endif
            public Action<SensingResponse> onCompleteDelegate;
        }

#if !UNITY_WEBGL
        protected AmazonRekognitionClient m_rekognitionClient;
        public bool ConnectionActive => m_rekognitionClient != null;
#endif

        Dictionary<string, RequestState> m_requestLookup = new Dictionary<string, RequestState>();

        //Fixed Params for the 3 types of sensing
        readonly string[] m_headRequestParameters = new string[] { "DEFAULT", "FACE_OCCLUDED" };
        readonly string[] m_emotionRequestParameters = new string[] { "DEFAULT", "EMOTIONS" };
        readonly string[] m_characteristicsRequestParameters = new string[] { "DEFAULT", "SMILE", "EYEGLASSES", "BEARD", "MUSTACHE", "GENDER", "AGE_RANGE", "SUNGLASSES" };

        /// <inheritdoc/>
        public override void SystemInit()
        {
            base.SystemInit();

#if !UNITY_WEBGL
            var configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            string accessKey = configSystem.config.awsRekognition.accessKey;
            string secretKey = configSystem.config.awsRekognition.secretKey;
            m_rekognitionClient = new AmazonRekognitionClient(accessKey, secretKey, Amazon.RegionEndpoint.USWest2);
#endif
        }

        /// <inheritdoc/>
        public override async void Request(string uri, object input, Action<SensingResponse> onComplete)
        {
#if !UNITY_WEBGL
            //Check if we are already processing a request of this type
            if (m_requestLookup[uri].processing) return;

            m_requestLookup[uri].processing = true;

            DetectFacesRequest request = (DetectFacesRequest)input;

            DetectFacesResponse response = await m_rekognitionClient.DetectFacesAsync(request);

            FaceDetail face = null;

            if (response.FaceDetails != null && response.FaceDetails.Count > 0)
                face = response.FaceDetails[0];

            //Add the callbacks from all the outdated requests to this one, so any pending listeners recieve the latest repsonse.
            foreach (var req in m_requestLookup[uri].outdatedRequests)
            {
                onComplete += req.onCompleteDelegate;
            }

            m_requestLookup[uri].onResponseDelegate?.Invoke(onComplete, face);

            m_requestLookup[uri].outdatedRequests.Clear();

            m_requestLookup[uri].processing = false;

            //If we have another request in the queue, send it
            if (m_requestLookup[uri].nextRequest != null)
            {
                Request(m_requestLookup[uri].nextRequest.requestType, m_requestLookup[uri].nextRequest.requestData, m_requestLookup[uri].nextRequest.onCompleteDelegate);
                m_requestLookup[uri].nextRequest = null;
            }
#else
            await Task.Delay(0);
#endif
        }

        /// <inheritdoc/>
        public override void AnalyzeCharacteristics(object input, Action<SensingResponse> onComplete)
        {
#if !UNITY_WEBGL
            AddRequestToQueue(new AWSRekognitionServiceRequest()
            {
                requestType = "AnalyzeCharacteristics",
                requestData = CreateRequest((byte[])input, m_characteristicsRequestParameters),
                onCompleteDelegate = onComplete
            }
            , SendCharacteristicsResponse);
#endif
        }

        /// <inheritdoc/>
        public override void AnalyzeEmotions(object input, Action<SensingResponse> onComplete)
        {
#if !UNITY_WEBGL
            AddRequestToQueue(new AWSRekognitionServiceRequest()
            {
                requestType = "AnalyzeEmotions",
                requestData = CreateRequest((byte[])input, m_emotionRequestParameters),
                onCompleteDelegate = onComplete
            }
            , SendEmotionResponse);
#endif
        }

        /// <inheritdoc/>
        public override void AnalyzeHead(object input, Action<SensingResponse> onComplete)
        {
#if !UNITY_WEBGL
            AddRequestToQueue(new AWSRekognitionServiceRequest()
            {
                requestType = "AnalyzeHead",
                requestData = CreateRequest((byte[])input, m_headRequestParameters),
                onCompleteDelegate = onComplete
            }
            , SendHeadResponse);
#endif
        }

        /// <summary>
        /// Adds a generic AWS service request to the queue with a callback
        /// </summary>
        /// <param name="request">The request to process.</param>
        /// <param name="onResponseDelegate"> Callback that receives the <see cref="FaceDetail"/> result and invokes the appropriate<see cref="SensingResponse"/> via user-defined delegate. </param>
#if !UNITY_WEBGL
        void AddRequestToQueue(AWSRekognitionServiceRequest request, System.Action<Action<SensingResponse>, FaceDetail> onResponseDelegate)
        {
            // Check if a request of this type is already in the queue or being processed
            if (!m_requestLookup.TryGetValue(request.requestType, out RequestState requestState))
            {
                requestState = new RequestState()
                {
                    processing = false,
                    nextRequest = null,
                    outdatedRequests = new List<AWSRekognitionServiceRequest>(),
                    onResponseDelegate = onResponseDelegate
                };

                m_requestLookup.Add(request.requestType, requestState);
            }
            
            // If we are not processing any requesst of this type,  send this request to the erver.
            if (!requestState.processing)
            {
                Request(request.requestType, request.requestData, request.onCompleteDelegate);
                return;
            }

            // Otherwise, if a qued request exisits, mark it outdated
            if (requestState.nextRequest != null)
            {
                requestState.outdatedRequests.Add(requestState.nextRequest);
            }

            // Mark this request as the next one in the queue
            requestState.nextRequest = request;
        }

        /// <summary>
        /// Constructs a <see cref="DetectFacesRequest"/> from raw image bytes and requested attributes.
        /// </summary>
        /// <param name="image">Raw byte array representing the image.</param>
        /// <param name="attributes">AWS Rekognition face attribute types to detect.</param>
        /// <returns>A configured <see cref="DetectFacesRequest"/> object.</returns>
        DetectFacesRequest CreateRequest(byte[] image, string[] attributes)
        {
            return new DetectFacesRequest()
            {
                Image = new Image()
                {
                    Bytes = new MemoryStream(image)
                },
                // Attributes can be "ALL" or "DEFAULT". 
                // "DEFAULT": BoundingBox, Confidence, Landmarks, Pose, and Quality.
                // "ALL": See https://docs.aws.amazon.com/sdkfornet/v3/apidocs/items/Rekognition/TFaceDetail.html
                Attributes = new List<String>(attributes)
            };
        }

        /// <summary>
        /// Processes the head pose and occlusion response from Rekognition and populates a <see cref="SensingHeadResponse"/>.
        /// </summary>
        /// <param name="onComplete">Callback to invoke with the completed response.</param>
        /// <param name="face">Face detail returned from Rekognition.</param>
        void SendHeadResponse(Action<SensingResponse> onCompleteDelegate, FaceDetail face)
        {
            if(face == null)
            {
                onCompleteDelegate?.Invoke(m_sensingHeadResponse);
                return;
            }
            m_sensingHeadResponse.normalizedVectors = true;
            m_sensingHeadResponse.pitch = face.Pose.Pitch;
            m_sensingHeadResponse.roll = face.Pose.Roll;
            m_sensingHeadResponse.yaw = face.Pose.Yaw;
            m_sensingHeadResponse.foreheadOccluded = face.FaceOccluded.Value;
            m_sensingHeadResponse.eyeOccluded = face.FaceOccluded.Value;
            m_sensingHeadResponse.mouthOccluded = face.FaceOccluded.Value;
            m_sensingHeadResponse.landmarks = ParseLandMarksResponse(face.Landmarks);
            m_sensingHeadResponse.faceRectangle = new FaceRectangle()
            {
                width = face.BoundingBox.Width,
                height = face.BoundingBox.Height,
                left = face.BoundingBox.Left,
                top = face.BoundingBox.Top
            };

            onCompleteDelegate?.Invoke(m_sensingHeadResponse);
        }

        /// <summary>
        /// Converts AWS Rekognition emotion data into a <see cref="SensingEmotionResponse"/> and invokes the result callback.
        /// </summary>
        /// <param name="onComplete">Callback to invoke with the populated <see cref="SensingEmotionResponse"/>.</param>
        /// <param name="face">The <see cref="FaceDetail"/> object returned by AWS Rekognition.</param>
        void SendEmotionResponse(Action<SensingResponse> onCompleteDelegate, FaceDetail face)
        {
            if(face == null)
            {
                onCompleteDelegate?.Invoke(m_sensingEmotionResponse);
                return;
            }

            m_sensingEmotionResponse.anger = 0;
            m_sensingEmotionResponse.contempt = 0;
            m_sensingEmotionResponse.disgust = 0;
            m_sensingEmotionResponse.fear = 0;
            m_sensingEmotionResponse.happiness = 0;
            m_sensingEmotionResponse.neutral = 0;
            m_sensingEmotionResponse.sadness = 0;
            m_sensingEmotionResponse.surprise = 0;

            //Set emotion float value ONLY if we detect that emotion in the response
            foreach (Emotion emotion in face.Emotions)
            {
                m_sensingEmotionResponse.anger = emotion.Type == EmotionName.ANGRY ? emotion.Confidence : m_sensingEmotionResponse.anger;
                m_sensingEmotionResponse.disgust = emotion.Type == EmotionName.DISGUSTED ? emotion.Confidence : m_sensingEmotionResponse.disgust;
                m_sensingEmotionResponse.fear = emotion.Type == EmotionName.FEAR ? emotion.Confidence : m_sensingEmotionResponse.fear;
                m_sensingEmotionResponse.happiness = emotion.Type == EmotionName.HAPPY ? emotion.Confidence : m_sensingEmotionResponse.happiness;
                m_sensingEmotionResponse.neutral = emotion.Type == EmotionName.CALM ? emotion.Confidence : m_sensingEmotionResponse.neutral;
                m_sensingEmotionResponse.sadness = emotion.Type == EmotionName.SAD ? emotion.Confidence : m_sensingEmotionResponse.sadness;
                m_sensingEmotionResponse.surprise = emotion.Type == EmotionName.SURPRISED ? emotion.Confidence : m_sensingEmotionResponse.surprise;
            }

            onCompleteDelegate?.Invoke(m_sensingEmotionResponse);
        }

        /// <summary>
        /// Populates a <see cref="SensingCharacteristicsResponse"/> using AWS Rekognition <see cref="FaceDetail"/> data.
        /// </summary>
        /// <param name="onComplete">Callback to return the characteristics analysis result.</param>
        /// <param name="face">The <see cref="FaceDetail"/> data to extract attributes from.</param>
        void SendCharacteristicsResponse(Action<SensingResponse> onCompleteDelegate, FaceDetail face)
        {

            if(face == null)
            {
                onCompleteDelegate?.Invoke(m_sensingCharacteristicsResponse);
                return;
            }

            m_sensingCharacteristicsResponse.gender = face.Gender.Value.Value;
            m_sensingCharacteristicsResponse.age = (face.AgeRange.High + face.AgeRange.Low) / 2;
            m_sensingCharacteristicsResponse.glasses = ParseGlassesReponse(face.Eyeglasses, face.Sunglasses);
            m_sensingCharacteristicsResponse.moustache = face.Mustache.Confidence;
            m_sensingCharacteristicsResponse.beard = face.Beard.Confidence;


            onCompleteDelegate?.Invoke(m_sensingCharacteristicsResponse);
        }

        /// <summary>
        /// Determines the most appropriate glasses label ("Eyeglasses", "Sunglasses", or "None").
        /// </summary>
        /// <param name="eyeglasses">The <see cref="Eyeglasses"/> attribute returned by AWS Rekognition.</param>
        /// <param name="sunglasses">The <see cref="Sunglasses"/> attribute returned by AWS Rekognition.</param>
        /// <returns>A user-friendly string representing the detected glasses type.</returns>
        string ParseGlassesReponse(Eyeglasses eyeglasses, Sunglasses sunglasses)
        {
            string response = "None";

            if (eyeglasses.Value && !sunglasses.Value) response = "Eyeglasses";

            else if (!eyeglasses.Value && sunglasses.Value) response = "Sunglasses";

            else if (eyeglasses.Value && sunglasses.Value)
            {
                if (eyeglasses.Confidence > sunglasses.Confidence)
                    response = "Eyeglasses";
                else
                    response = "Sunglasses";
            }

            return response;
        }

        /// <summary>
        /// Converts AWS Rekognition <see cref="Landmark"/> data into a <see cref="RideVector2"/> array.
        /// </summary>
        /// <param name="landmarks">A list of facial <see cref="Landmark"/> points from Rekognition.</param>
        /// <returns>An array of <see cref="RideVector2"/> with normalized face geometry positions.</returns>
        RideVector2[] ParseLandMarksResponse(List<Landmark> landmarks)
        {
            RideVector2[] rideLandmarks = new RideVector2[landmarks.Count];

            for (int i = 0; i < rideLandmarks.Length; i++)
            {
                rideLandmarks[i].x = landmarks[i].X;
                rideLandmarks[i].y = landmarks[i].Y;
            }

            return rideLandmarks;
        }
#endif
    }
}
