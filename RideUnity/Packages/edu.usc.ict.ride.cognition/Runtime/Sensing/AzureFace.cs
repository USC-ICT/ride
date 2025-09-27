namespace Ride.Sensing
{
    /// <summary>
    /// Data structures used by Azure Cognitive Services Face API:
    /// https://docs.microsoft.com/en-us/azure/cognitive-services/face/.
    /// </summary>
    public class AzureFace
    {
        /// <summary>
        /// A complete face detection result from Azure, including geometry and attributes.
        /// </summary>
        [System.Serializable]
        public struct AzureFaceResponse
        {
            public string faceId;
            public FaceRectangle faceRectangle;
            public FaceLandmarks faceLandmarks;
            public FaceAttributes faceAttributes;
        }

        [System.Serializable]
        public struct FaceAttributes
        {
            public double smile;
            public HeadPose headPose;
            public string gender;
            public double age;
            public FacialHair facialHair;
            public string glasses;
            public Emotion emotion;
            public Blur blur;
            public Exposure exposure;
            public Noise noise;
            public Makeup makeup;
            public Accessories[] accessories;
            public Occlusion occlusion;
            public Hair hair;
        }

        [System.Serializable]
        public struct HeadPose
        {
            public double pitch;
            public double roll;
            public double yaw;
        }

        [System.Serializable]
        public struct FacialHair
        {
            public double moustache;
            public double beard;
            public double sideburns;
        }

        [System.Serializable]
        public struct Emotion
        {
            public double anger;
            public double contempt;
            public double disgust;
            public double fear;
            public double happiness;
            public double neutral;
            public double sadness;
            public double surprise;
        }

        [System.Serializable]
        public struct Blur
        {
            public string blurLevel;
            public double value;
        }

        [System.Serializable]
        public struct Exposure
        {
            public string exposureLevel;
            public double value;
        }

        [System.Serializable]
        public struct Noise
        {
            public string noiseLevel;
            public double value;
        }

        [System.Serializable]
        public struct Makeup
        {
            public bool eyeMakeup;
            public bool lipMakeup;
        }

        [System.Serializable]
        public struct Accessories
        {
            public string type;
            public double confidence;
        }

        [System.Serializable]
        public struct Occlusion
        {
            public bool foreheadOccluded;
            public bool eyeOccluded;
            public bool mouthOccluded;
        }

        [System.Serializable]
        public struct Hair
        {
            public double bald;
            public bool invisible;
            public HairColor[] hairColor;
        }

        [System.Serializable]
        public struct HairColor
        {
            public string color;
            public double confidence;
        }

        [System.Serializable]
        public struct FaceLandmarks
        {
            public RideVector2 pupilLeft;
            public RideVector2 pupilRight;
            public RideVector2 noseTip;
            public RideVector2 mouthLeft;
            public RideVector2 mouthRight;
            public RideVector2 eyebrowLeftOuter;
            public RideVector2 eyebrowLeftInner;
            public RideVector2 eyeLeftOuter;
            public RideVector2 eyeLeftTop;
            public RideVector2 eyeLeftBottom;
            public RideVector2 eyeLeftInner;
            public RideVector2 eyebrowRightInner;
            public RideVector2 eyebrowRightOuter;
            public RideVector2 eyeRightInner;
            public RideVector2 eyeRightTop;
            public RideVector2 eyeRightBottom;
            public RideVector2 eyeRightOuter;
            public RideVector2 noseRootLeft;
            public RideVector2 noseRootRight;
            public RideVector2 noseLeftAlarTop;
            public RideVector2 noseRightAlarTop;
            public RideVector2 noseLeftAlarOutTip;
            public RideVector2 noseRightAlarOutTip;
            public RideVector2 upperLipTop;
            public RideVector2 upperLipBottom;
            public RideVector2 underLipTop;
            public RideVector2 underLipBottom;

            public RideVector2[] ToArray()
            {
                return new RideVector2[]{
                        pupilLeft,
                        pupilRight,
                        noseTip,
                        mouthLeft,
                        mouthRight,
                        eyebrowLeftOuter,
                        eyebrowLeftInner,
                        eyeLeftOuter,
                        eyeLeftTop,
                        eyeLeftBottom,
                        eyeLeftInner,
                        eyebrowRightInner,
                        eyebrowRightOuter,
                        eyeRightInner,
                        eyeRightTop,
                        eyeRightBottom,
                        eyeRightOuter,
                        noseRootLeft,
                        noseRootRight,
                        noseLeftAlarTop,
                        noseRightAlarTop,
                        noseLeftAlarOutTip,
                        noseRightAlarOutTip,
                        upperLipTop,
                        upperLipBottom,
                        underLipTop,
                        underLipBottom,
                };
            }
        }
    }
}