using System;
using System.Collections;
using System.IO;
using UnityEngine;
using VHAssets;


namespace Ride.Sensing
{
    /// <summary>
    /// Unity component wrapper to interact with the active <see cref="ISensingSystem"/>.
    /// Handles continuous or one-shot visual sensing using webcam input.
    /// </summary>
    public class SensingProcessor : RideSystemMonoBehaviour
    {
        [Flags]
        public enum ProcessSetting
        {
            HEAD = 1,
            EMOTION = 2,
            CHARACTERISTICS = 4
        }

        [Header("References")]
        [SerializeField] VHWebCam webCam;

        [Header("Settings")]
        public ProcessSetting processSetting;

        [Range(1, 100)]
        [SerializeField] int m_imageQuality = 25;

        [Tooltip("Frequency of API calls")]
        [SerializeField] float m_apiCallFrequency = 3.1f;

        [Range(0, 1.0f)]
        [SerializeField] float m_neutralThreshold = 0.8f; // Lower neutral emotion "confidence" so it's easier to check non-neutral emotions.

        [Header("Output Response")]
        public string emotion = "None";
        public SensingEmotionResponse emotionResponse;
        public SensingHeadResponse headResponse;
        public SensingCharacteristicsResponse characteristicsResponse;

        bool m_processing;
        byte[] m_imageData;
        SensingFrameResponse m_frameResponse;

        ISensingEmotionSystem m_sensingEmotionSystem;
        ISensingHeadSystem m_sensingHeadSystem;
        ISensingCharacteristicsSystem m_sensingCharacteristicsSystem;
        ISensingFrameSystem m_sensingFrameSystem;

        public event Action onFrameProcessed;
        public event Action onEmotionProcessed;
        public event Action onHeadProcessed;
        public event Action onChaaracteristicsProcessed;

        public Material RenderMaterial => webCam.renderMaterial;

        public bool IsProcessing { get { return m_processing; } }

        public SensingFrameResponse frameResponse
        {
            get => m_frameResponse;
            private set => m_frameResponse = value;
        }

        /// <summary>
        /// Assigns a single system that implements all sensing interfaces:
        /// </summary>
        public void SetSensingSystems(ISensingEmotionSystem sensingEmotionSystem = null, ISensingHeadSystem sensingHeadSystem = null, ISensingCharacteristicsSystem sensingCharacteristicsSystem = null)
        {
            m_sensingEmotionSystem = sensingEmotionSystem;
            m_sensingCharacteristicsSystem = sensingCharacteristicsSystem;
            m_sensingHeadSystem = sensingHeadSystem;
            m_sensingFrameSystem = sensingEmotionSystem as ISensingFrameSystem
                ?? sensingHeadSystem as ISensingFrameSystem
                ?? sensingCharacteristicsSystem as ISensingFrameSystem;
            ClearResponses();
        }

        /// <summary>
        /// Assigns a single system that implements all sensing interfaces:
        /// </summary>
        public void SetSensingSystems(ISensingSystem sensingSystem)
        {
            m_sensingFrameSystem = sensingSystem as ISensingFrameSystem;
            m_sensingEmotionSystem = sensingSystem as ISensingEmotionSystem;
            m_sensingCharacteristicsSystem = sensingSystem as ISensingCharacteristicsSystem;
            m_sensingHeadSystem = sensingSystem as ISensingHeadSystem;
            ClearResponses();
        }

        /// <summary>
        /// Starts continuous webcam processing. Captures and sends images every <see cref="m_apiCallFrequency"/> seconds
        /// to the active sensing systems.
        /// </summary>
        public void StartProcessing()
        {
            m_processing = true;
            webCam.Play();
            StartCoroutine(Processing());
        }

        /// <summary>
        /// Stops webcam input and halts continuous sensing.
        /// </summary>
        /// <seealso cref="StartProcessing"/>
        public void StopProcessing()
        {
            webCam.Stop();
            m_processing = false;
        }

        /// <summary>
        /// Coroutine that repeatedly sends image data to sensing services.
        /// </summary>
        IEnumerator Processing()
        {
            while (m_processing)
            {
                if (webCam.isPlaying)
                    ProcessSingleScreenshot(false);

                yield return new WaitForSeconds(m_apiCallFrequency); // Free Azure account allows for only 20 calls per minute
            }
        }

        /// <summary>
        /// Captures a screenshot and sends it to all selected sensing systems.
        /// </summary>
        /// <param name="saveToDisk">If true, saves the captured image as a JPG file.</param>
        public void ProcessSingleScreenshot(bool saveToDisk = true)
        {
            CaptureScreenshot(saveToDisk);

            if (m_sensingFrameSystem != null)
            {
                m_sensingFrameSystem.AnalyzeFrame(m_imageData, OnCompleteFrame);
                return;
            }

            if (processSetting.HasFlag(ProcessSetting.HEAD)) m_sensingHeadSystem?.AnalyzeHead(m_imageData, OnCompleteHead);
            if (processSetting.HasFlag(ProcessSetting.EMOTION)) m_sensingEmotionSystem?.AnalyzeEmotions(m_imageData, OnCompleteEmotion);
            if (processSetting.HasFlag(ProcessSetting.CHARACTERISTICS)) m_sensingCharacteristicsSystem?.AnalyzeCharacteristics(m_imageData, OnCompleteCharacteristics);
        }

        /// <summary>
        /// Called when a provider-neutral frame analysis completes.
        /// </summary>
        /// <param name="response">The response containing all provider-supported frame data.</param>
        void OnCompleteFrame(SensingFrameResponse response)
        {
            if (response == null)
                return;

            frameResponse = response;
            headResponse = null;
            emotionResponse = null;
            characteristicsResponse = null;
            emotion = "None";

            var face = response.PrimaryFace;
            if (face == null || !face.hasFace)
            {
                onFrameProcessed?.Invoke();
                return;
            }

            if (processSetting.HasFlag(ProcessSetting.HEAD)
                && (SensingFrameConversions.HasCapability(response, SensingCapability.HeadPose)
                    || SensingFrameConversions.HasCapability(response, SensingCapability.FaceLandmarks)
                    || SensingFrameConversions.HasCapability(response, SensingCapability.FaceBounds)))
            {
                headResponse = SensingFrameConversions.ToHeadResponse(response, face);
                onHeadProcessed?.Invoke();
            }

            if (processSetting.HasFlag(ProcessSetting.EMOTION)
                && SensingFrameConversions.HasCapability(response, SensingCapability.Emotions))
            {
                emotionResponse = SensingFrameConversions.ToEmotionResponse(response, face);
                UpdateEmotion();
                onEmotionProcessed?.Invoke();
            }

            if (processSetting.HasFlag(ProcessSetting.CHARACTERISTICS)
                && SensingFrameConversions.HasCapability(response, SensingCapability.Characteristics)
                && face.characteristics != null)
            {
                characteristicsResponse = face.characteristics;
                onChaaracteristicsProcessed?.Invoke();
            }

            onFrameProcessed?.Invoke();
        }

        /// <summary>
        /// Called when emotion sensing completes.
        /// </summary>
        /// <param name="response">The response containing emotion scores.</param>
        void OnCompleteEmotion(SensingResponse response)
        {
            emotionResponse = (SensingEmotionResponse)response;
            UpdateEmotion();
            onEmotionProcessed?.Invoke();
        }

        /// <summary>
        /// Called when head pose sensing completes.
        /// </summary>
        /// <param name="response">The response containing head pose data.</param>
        void OnCompleteHead(SensingResponse response)
        {
            headResponse = (SensingHeadResponse)response;
            onHeadProcessed?.Invoke();
        }

        /// <summary>
        /// Called when characteristics sensing completes.
        /// </summary>
        /// <param name="response">The response containing characteristic data.</param>
        void OnCompleteCharacteristics(SensingResponse response)
        {
            characteristicsResponse = (SensingCharacteristicsResponse)response;
            onChaaracteristicsProcessed?.Invoke();
        }

        void ClearResponses()
        {
            frameResponse = null;
            emotionResponse = null;
            headResponse = null;
            characteristicsResponse = null;
            emotion = "None";
        }

        /// <summary>
        /// Captures the current webcam frame and converts it to JPG bytes.
        /// </summary>
        /// <param name="saveToDisk">If true, writes the file to disk.</param>
        void CaptureScreenshot(bool saveToDisk)
        {
            Texture2D photo = new Texture2D(webCam.texWidth, webCam.texHeight);
            photo.SetPixels32(webCam.GetPixels32());
            photo.Apply();

            m_imageData = photo.EncodeToJPG(m_imageQuality); // TODO: test further and perhaps parameterize quality (0 - 100)
            if (saveToDisk)
                File.WriteAllBytes($"Photo_{System.DateTime.Now}.jpg",
                    m_imageData);
        }

        /// <summary>
        /// Loads an image file and returns it as a byte array.
        /// </summary>
        /// <param name="imageFilePath">Path to the image on disk.</param>
        /// <returns>Byte array of the image content.</returns>
        byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        /// <summary>
        /// Check which emotion has the highest confidence
        /// Probably a better way if we can create an emotion lookup instead of these ifs/elifs
        /// </summary>
        void UpdateEmotion()
        {

            double value = emotionResponse.anger;

            emotion = value == 0 ? "None" : "Anger";

            if (value < emotionResponse.contempt)
            {
                value = emotionResponse.contempt;
                emotion = "Contempt";
            }

            if (value < emotionResponse.disgust)
            {
                value = emotionResponse.disgust;
                emotion = "Disgust";
            }

            if (value < emotionResponse.fear)
            {
                value = emotionResponse.fear;
                emotion = "Fear";
            }

            if (value < emotionResponse.happiness)
            {
                value = emotionResponse.happiness;
                emotion = "Happy";
            }

            if (value < emotionResponse.sadness)
            {
                value = emotionResponse.sadness;
                emotion = "Sadness";
            }

            if (value < emotionResponse.surprise)
            {
                value = emotionResponse.surprise;
                emotion = "Surprise";
            }

            if (emotionResponse.neutral < m_neutralThreshold) return;

            if (value < emotionResponse.neutral)
            {
                value = emotionResponse.neutral;
                emotion = "Neutral";
            }
        }
    }
}
