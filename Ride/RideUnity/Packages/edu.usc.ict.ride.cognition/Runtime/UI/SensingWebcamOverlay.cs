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
        Vector2Int m_sourceSize;
        Vector2 m_scaledSize;
        Vector2 m_frameStartSize;

        List<Transform> landmarks = new List<Transform>();
        private void Awake()
        {
            EnsureLandmarkCapacity(landmarkCount);

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

            int detectedLandmarkCount = processor.headResponse.landmarks.Length;
            EnsureLandmarkCapacity(detectedLandmarkCount);

            // Store raw webcam image size
            int imageWidth = processor.captureWidth > 0 ? processor.captureWidth : webcamImage.texture.width;
            int imageHeight = processor.captureHeight > 0 ? processor.captureHeight : webcamImage.texture.height;
            m_rawSize = new Vector2Int(imageWidth, imageHeight);
            m_sourceSize = GetSourceSize();

            // Store scaled webcam image size
            m_scaledSize = webcamImage.rectTransform.rect.size;

            // Place landmark UI objects
            for (int i = 0; i < detectedLandmarkCount; i++)
            {
                Vector2 rectPosition = processor.headResponse.landmarks[i];

                // If position data is normalized (0-1) then multiply with raw size
                if (processor.headResponse.normalizedVectors)
                {
                    rectPosition.x *= m_rawSize.x;
                    rectPosition.y *= m_rawSize.y;
                }

                ((RectTransform)landmarks[i]).anchoredPosition = MapCapturePointToPreview(rectPosition);

                landmarks[i].gameObject.SetActive(true);
            }

            if (detectedLandmarkCount < landmarks.Count)
            {
                for (int i = detectedLandmarkCount; i < landmarks.Count; i++)
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

            Rect mappedFaceRect = MapCaptureRectToPreview(faceRect);
            Vector2 center = mappedFaceRect.center;

            faceFrameRect.sizeDelta = mappedFaceRect.size;
            faceFrameRect.localEulerAngles = Vector3.zero;
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

        /// <summary>
        /// Gets the webcam texture size used by the preview before native orientation correction is applied.
        /// </summary>
        /// <returns>The source webcam texture size, or the current capture size if the texture is unavailable.</returns>
        Vector2Int GetSourceSize()
        {
            if (webcamImage != null && webcamImage.texture != null && webcamImage.texture.width > 0 && webcamImage.texture.height > 0)
                return new Vector2Int(webcamImage.texture.width, webcamImage.texture.height);

            return m_rawSize;
        }

        /// <summary>
        /// Maps a face rectangle from detector capture coordinates into the unrotated overlay preview space.
        /// </summary>
        /// <param name="faceRect">Face rectangle returned by the sensing provider in capture coordinates.</param>
        /// <returns>The preview-space rectangle enclosing the transformed face bounds.</returns>
        Rect MapCaptureRectToPreview(FaceRectangle faceRect)
        {
            Vector2 topLeft = MapCapturePointToPreview(new Vector2(faceRect.left, faceRect.top));
            Vector2 topRight = MapCapturePointToPreview(new Vector2(faceRect.left + faceRect.width, faceRect.top));
            Vector2 bottomLeft = MapCapturePointToPreview(new Vector2(faceRect.left, faceRect.top + faceRect.height));
            Vector2 bottomRight = MapCapturePointToPreview(new Vector2(faceRect.left + faceRect.width, faceRect.top + faceRect.height));

            float minX = Mathf.Min(topLeft.x, topRight.x, bottomLeft.x, bottomRight.x);
            float maxX = Mathf.Max(topLeft.x, topRight.x, bottomLeft.x, bottomRight.x);
            float minY = Mathf.Min(topLeft.y, topRight.y, bottomLeft.y, bottomRight.y);
            float maxY = Mathf.Max(topLeft.y, topRight.y, bottomLeft.y, bottomRight.y);
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// Maps a detector point from top-left-origin capture coordinates to the overlay's preview coordinates.
        /// </summary>
        /// <param name="captureTopLeftPoint">Detector point in capture-image coordinates with a top-left origin.</param>
        /// <returns>The corresponding point in the overlay RectTransform's local preview space.</returns>
        Vector2 MapCapturePointToPreview(Vector2 captureTopLeftPoint)
        {
            Vector2 captureBottomLeftPoint = new Vector2(captureTopLeftPoint.x, m_rawSize.y - captureTopLeftPoint.y);
            if (!ShouldApplyPreviewOrientation())
                return ScalePoint(captureBottomLeftPoint, m_rawSize);

            Vector2 sourcePoint = CaptureToSourcePoint(captureBottomLeftPoint);
            return SourceToPreviewPoint(sourcePoint);
        }

        /// <summary>
        /// Converts a point from oriented capture space back into the original webcam texture space.
        /// </summary>
        /// <param name="captureBottomLeftPoint">Detector point in capture coordinates after conversion to a bottom-left origin.</param>
        /// <returns>The matching point in the uncorrected webcam texture coordinate space.</returns>
        Vector2 CaptureToSourcePoint(Vector2 captureBottomLeftPoint)
        {
            float sourceWidth = m_sourceSize.x;
            float sourceHeight = m_sourceSize.y;
            Vector2 sourcePoint;

            switch (GetPreviewRotation())
            {
                case 90:
                    sourcePoint = new Vector2(captureBottomLeftPoint.y, sourceHeight - captureBottomLeftPoint.x);
                    break;
                case 180:
                    sourcePoint = new Vector2(sourceWidth - captureBottomLeftPoint.x, sourceHeight - captureBottomLeftPoint.y);
                    break;
                case 270:
                    sourcePoint = new Vector2(sourceWidth - captureBottomLeftPoint.y, captureBottomLeftPoint.x);
                    break;
                default:
                    sourcePoint = captureBottomLeftPoint;
                    break;
            }

            if (GetPreviewVerticallyMirrored())
                sourcePoint.y = sourceHeight - sourcePoint.y;

            return sourcePoint;
        }

        /// <summary>
        /// Converts an original webcam texture point into the displayed RawImage preview coordinate space.
        /// </summary>
        /// <param name="sourcePoint">Point in the uncorrected webcam texture coordinate space.</param>
        /// <returns>The point after preview mirroring, scaling, and rotation are applied.</returns>
        Vector2 SourceToPreviewPoint(Vector2 sourcePoint)
        {
            Vector2 displayPoint = sourcePoint;
            if (GetPreviewVerticallyMirrored())
                displayPoint.y = m_sourceSize.y - displayPoint.y;

            Vector2 scaledPoint = ScalePoint(displayPoint, m_sourceSize);
            return RotatePreviewPoint(scaledPoint);
        }

        /// <summary>
        /// Scales a point from a source image size into the current preview RectTransform size.
        /// </summary>
        /// <param name="point">Point in the source image coordinate space.</param>
        /// <param name="size">Width and height of the source image coordinate space.</param>
        /// <returns>The point scaled into preview coordinates, or zero if the source size is invalid.</returns>
        Vector2 ScalePoint(Vector2 point, Vector2Int size)
        {
            if (size.x <= 0 || size.y <= 0)
                return Vector2.zero;

            return new Vector2(point.x * m_scaledSize.x / size.x, point.y * m_scaledSize.y / size.y);
        }

        /// <summary>
        /// Applies the RawImage preview rotation to an overlay point around the preview center.
        /// </summary>
        /// <param name="position">Unrotated preview-space point.</param>
        /// <returns>The preview-space point after the active native rotation correction is applied.</returns>
        Vector2 RotatePreviewPoint(Vector2 position)
        {
            int rotation = GetPreviewRotation();
            if (rotation == 0)
                return position;

            Vector2 center = m_scaledSize * 0.5f;
            Vector2 offset = position - center;
            switch (rotation)
            {
                case 90: return center + new Vector2(offset.y, -offset.x);
                case 180: return center - offset;
                case 270: return center + new Vector2(-offset.y, offset.x);
                default: return position;
            }
        }

        /// <summary>
        /// Determines whether the overlay should account for native webcam orientation correction.
        /// </summary>
        /// <returns>True when the active webcam preview is using native orientation correction.</returns>
        bool ShouldApplyPreviewOrientation() => processor != null && processor.WebCam != null && processor.WebCam.nativeOrientationCorrectionEnabled;

        /// <summary>
        /// Gets the normalized native rotation currently applied to the webcam preview.
        /// </summary>
        /// <returns>The active preview rotation in degrees, normalized to 0, 90, 180, or 270.</returns>
        int GetPreviewRotation()
        {
            if (!ShouldApplyPreviewOrientation())
                return 0;

            return NormalizeRotation(processor.WebCam.effectiveVideoRotationAngle);
        }

        /// <summary>
        /// Determines whether the active webcam preview is vertically mirrored by native orientation metadata.
        /// </summary>
        /// <returns>True when the preview should apply vertical mirroring.</returns>
        bool GetPreviewVerticallyMirrored() => ShouldApplyPreviewOrientation() && processor.WebCam.effectiveVideoVerticallyMirrored;

        /// <summary>
        /// Normalizes a rotation angle to one of the right-angle rotations supported by the webcam preview.
        /// </summary>
        /// <param name="rotationAngle">Rotation angle in degrees.</param>
        /// <returns>A normalized rotation angle of 0, 90, 180, or 270; unsupported angles return 0.</returns>
        static int NormalizeRotation(int rotationAngle)
        {
            int rotation = ((rotationAngle % 360) + 360) % 360;
            return rotation % 90 == 0 ? rotation : 0;
        }
        void EnsureLandmarkCapacity(int requiredCount)
        {
            while (landmarks.Count < requiredCount)
            {
                Transform landmark = Instantiate(landmarkPrefab, GetLandmarkParent());
                landmark.gameObject.SetActive(false);
                landmarks.Add(landmark);
            }
        }

        /// <summary>
        /// Gets the transform that should contain runtime-created landmark overlay objects.
        /// </summary>
        /// <returns>The landmark prefab's parent when available, otherwise the webcam image transform or this transform.</returns>
        Transform GetLandmarkParent()
        {
            if (landmarkPrefab != null && landmarkPrefab.parent != null)
                return landmarkPrefab.parent;

            return webcamImage != null ? webcamImage.transform : transform;
        }

        public void UpdateEmotionDisplay()
        {
            emotionText.text = processor.emotion;
        }
    }
}
