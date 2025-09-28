using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ride.UI;

namespace Ride.Sensing
{
    /// <summary>
    /// Overlay UI for sensing information
    /// </summary>
    public class SensingWebcamOverlay : MonoBehaviour
    {
        [Header ("References")]
        public SensingProcessor processor;
        public RideTextTMPro emotionText;
        public RawImage webcamImage;
        public Transform landmarkPrefab;
        public RectTransform faceFrameRect;
        public RectTransform axisRootRect;

        [Header ("Settings")]
        public int landmarkCount = 27;

        Vector2Int m_rawSize;
        Vector2 m_scaledSize;
        Vector2 m_frameStartSize;

        List<Transform> landmarks = new List<Transform>();
        private void Awake()
        {
            // Create landmark object pool
            for (int i = 0; i < landmarkCount; i++)
            {
                Transform landmark = Instantiate(landmarkPrefab, webcamImage.transform);
                landmark.gameObject.SetActive(false);

                landmarks.Add(landmark);
            }

            m_frameStartSize = faceFrameRect.sizeDelta;
        }

        private void OnEnable()
        {
            processor.onHeadProcessed += UpdateSensingDisplay;
            processor.onEmotionProcessed += UpdateEmotionDisplay;
        }

        private void OnDisable()
        {
            processor.onHeadProcessed -= UpdateSensingDisplay;
            processor.onEmotionProcessed -= UpdateEmotionDisplay;
        }

        public void UpdateSensingDisplay()
        {
            if (processor.headResponse.landmarks == null) return;

            // Store raw webcam imahe size
            m_rawSize = new Vector2Int(webcamImage.texture.width, webcamImage.texture.height);

            // Store scaled webcam image size
            m_scaledSize = webcamImage.rectTransform.rect.size;

            // Calcualte scale factor from above
            Vector2 scaleFactor = new Vector2(m_scaledSize.x / m_rawSize.x, m_scaledSize.y / m_rawSize.y);

            // Place landmark UI objects
            for (int i = 0; i < processor.headResponse.landmarks.Length; i++)
            {
                Vector2 rectPosition = processor.headResponse.landmarks[i];

                // If position data is normalized (0-1) then multiply with raw size
                if (processor.headResponse.normalizedVectors)
                {
                    rectPosition.x *= m_rawSize.x;
                    rectPosition.y *= m_rawSize.y;
                }

                // Since positional data is based on the raw image size, we have to rescale it
                rectPosition.x *= scaleFactor.x;

                // From top-left Y origin to bottom-left Y origin
                rectPosition.y = (m_rawSize.y - rectPosition.y) * scaleFactor.y;

                ((RectTransform)landmarks[i]).anchoredPosition = rectPosition;

                landmarks[i].gameObject.SetActive(true);
            }

            if (processor.headResponse.landmarks.Length < landmarkCount)
            {
                for (int i = processor.headResponse.landmarks.Length; i < landmarkCount; i++)
                {
                    landmarks[i].gameObject.SetActive(false);
                }
            }

            FaceRectangle faceRect = processor.headResponse.faceRectangle;

            // If position data is normalized (0-1) then multiply with raw size
            if (processor.headResponse.normalizedVectors)
            {
                faceRect.height *= m_rawSize.y;
                faceRect.width *= m_rawSize.x;

                faceRect.top *= m_rawSize.y;
                faceRect.left *= m_rawSize.x;
            }

            // Since positional data is based on the raw image size, we have to rescale it
            Vector2 center = new Vector2(faceRect.left, faceRect.top);
            center.x *= scaleFactor.x;
            center.y = (m_rawSize.y - center.y) * scaleFactor.y;  //From top-left Y origin to bottom-left Y origin

            Vector2 size = new Vector2(faceRect.width, faceRect.height);
            size.x *= scaleFactor.x;
            size.y *= scaleFactor.y;

            center.x += size.x / 2;
            center.y -= size.y / 2;

            faceFrameRect.sizeDelta = size;
            faceFrameRect.anchoredPosition = center;
            faceFrameRect.gameObject.SetActive(true);

            axisRootRect.anchoredPosition = center;
            axisRootRect.gameObject.SetActive(true);

            // Matching the head orientation UI Axis using rotation values for pitch, yaw and roll)
            axisRootRect.rotation = Quaternion.Euler((float)processor.headResponse.pitch, -(float)processor.headResponse.yaw, -(float)processor.headResponse.roll);

            // Scaling UI axis based on size of the face rect
            // Z scale stays as 1 for now (since face recct is 2D)
            Vector3 axisScale = new Vector3(faceFrameRect.sizeDelta.x / m_frameStartSize.x, faceFrameRect.sizeDelta.y / m_frameStartSize.y, 1);

            axisScale.z = (axisScale.x + axisScale.y) / 2;
            axisRootRect.localScale = axisScale;
        }

        public void UpdateEmotionDisplay()
        {
            emotionText.text = processor.emotion;
        }
    }
}