using UnityEngine;
using Ride.Sensing;
using Ride.UI;
using VHAssets;

namespace Ride.Samples
{
    /// <summary>
    /// Demonstrates and manages different visual/audio sensing systems (e.g., AWS Rekognition) in a sample Unity scene.
    /// Provides webcam integration and GUI controls for runtime configuration and data display.
    /// </summary>
    public class SamplesCognitionSensingSystem : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;

        [Header("Sensing")]
        [SerializeField] SensingProcessor m_sensingProcessor;
        [SerializeField] VHWebCam m_vhWebCam;
        [SerializeField] RideRawImage m_webcamRawImage;
        [SerializeField] SensingSystemAWSRekognition m_awsRekognitionSystem;
        //[SerializeField] SensingSystemAzureFace m_azureFaceSystem; // Removing for now, as Azure has deprecated the main sensing components we implement here
        [SerializeField] Audio.MicrophoneAudioSystem m_microphoneAudio;

        private ISensingSystem m_currentSensing;

        private int m_webCamIndex = 0;
        private int m_sensingMode = 0;
        private bool m_webcamToggle = false;
        private bool m_webcamDebugToggle = false;
        private RectTransform m_webcamPreviewRoot;
        private Vector2 m_webcamPreviewRootAnchoredPosition;
        private bool m_hasWebcamPreviewRootAnchoredPosition;
        static readonly string[] s_webCamFacingOptions = { "Any", "Front", "Back" };
        static readonly string[] s_webCamRotationOptions = { "0", "90", "180", "270" };

        /// <summary>
        /// Initializes sensing systems and sets the default system and processor on scene start.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_awsRekognitionSystem = Globals.api.GetSystem<SensingSystemAWSRekognition>();
            //m_azureFaceSystem = Globals.api.GetSystem<SensingSystemAzureFace>(); // Removing for now, as Azure has deprecated the main sensing components we implement here
            m_currentSensing = m_awsRekognitionSystem;
            m_webCamIndex = m_vhWebCam.currentDevice;
            m_sensingProcessor.SetSensingSystems(m_currentSensing);
        }

        /// <summary>
        /// Updates webcam preview orientation each frame while preserving the base sample update behavior.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            UpdateWebcamPreviewOrientation();
        }

        /// <summary>
        /// Renders the main GUI sections for sensing system interaction.
        /// </summary>
        public void OnGUISensing()
        {
            OnGUISelectSensingMode();
            OnGUIConfigureSensing();
        }

        /// <summary>
        /// Displays a selection grid to switch between available sensing systems (e.g., AWS or Azure).
        /// </summary>
        public void OnGUISelectSensingMode()
        {
            int sensingMode = m_debugMenu.SelectionGrid(m_sensingMode, new string[] { "AWS",}, 1);            
            if (m_sensingMode == sensingMode) { return; }

            m_sensingMode = sensingMode;
            if (m_sensingMode == 0) { m_currentSensing = m_awsRekognitionSystem; }
            //if (m_sensingMode == 1) { m_currentSensing = m_azureFaceSystem; }
        }

        /// <summary>
        /// Renders GUI controls for webcam device selection, webcam toggling, sensing start/stop, and displays real-time sensing data.
        /// </summary>
        public void OnGUIConfigureSensing()
        {
            if (m_vhWebCam.deviceNames.Length <= 0)
            {
                m_debugMenu.Label($"No camera devices found");
                m_debugMenu.Label($"or not authorized");
                return;
            }

            int webCamIndex = m_debugMenu.SelectionGrid(m_webCamIndex, m_vhWebCam.deviceNames, 2);
            if (webCamIndex != m_webCamIndex)
            {
                m_webCamIndex = webCamIndex;
                StopSensingProcessor();
                m_vhWebCam.SetCurrentDevice(m_webCamIndex);
            }

            OnGUIWebcamDebug();

            m_debugMenu.Space();

            if (m_debugMenu.Button(m_webcamToggle ? "Webcam On" : "Webcam Off"))
                OnToggleWebcam();

            m_debugMenu.Space();

            OnGUISelectSensingMode();

            if (m_debugMenu.Button(m_sensingProcessor.IsProcessing ? "Sensing On" : "Sensing Off"))
            {
                if (m_sensingProcessor.IsProcessing)
                    StopSensingProcessor();
                else
                    StartSensingProcessor();
            }

            if (m_sensingProcessor.IsProcessing)
            {
                m_debugMenu.Label($"HeadRoll: {m_sensingProcessor.headResponse.roll:0.0}");
                m_debugMenu.Label($"Age: {m_sensingProcessor.characteristicsResponse.age}");
                m_debugMenu.Label($"Glasses: {m_sensingProcessor.characteristicsResponse.glasses}");
                m_debugMenu.Label($"Gender: {m_sensingProcessor.characteristicsResponse.gender}");
            }

            m_debugMenu.Space();
        }

        /// <summary>
        /// Stops the sensing processor and halts all active sensing tasks.
        /// </summary>
        void StopSensingProcessor()
        {
            m_sensingProcessor.StopProcessing();
        }

        /// <summary>
        /// Starts the sensing processor with the currently selected sensing system and webcam material.
        /// </summary>
        void StartSensingProcessor()
        {
            if (m_sensingProcessor.IsProcessing)
                return;

            m_webcamRawImage.m_image.material = m_vhWebCam.renderMaterial;
            m_webcamRawImage.texture = m_vhWebCam.renderMaterial != null ? m_vhWebCam.renderMaterial.mainTexture : null;
            m_vhWebCam.ApplyRawImageOrientation(m_webcamRawImage.m_image);

            m_sensingProcessor.SetSensingSystems(m_currentSensing);
            m_sensingProcessor.StartProcessing();
        }

        /// <summary>
        /// Toggles the webcam on or off and starts/stops the sensing processor accordingly.
        /// </summary>
        public void OnToggleWebcam()
        {
            m_webcamToggle = !m_webcamToggle;
            if (m_webcamToggle && !m_sensingProcessor.IsProcessing)
                StartSensingProcessor();
            else if (!m_webcamToggle)
                StopSensingProcessor();

            m_webcamRawImage.Show(m_webcamToggle);
        }

        /// <summary>
        /// Applies the current webcam orientation metadata to the preview and resets preview offsets when webcam display is disabled.
        /// </summary>
        void UpdateWebcamPreviewOrientation()
        {
            if (m_webcamToggle)
            {
                m_vhWebCam.ApplyRawImageOrientation(m_webcamRawImage.m_image);
                UpdateWebcamPreviewOffset();
            }
            else
            {
                ResetWebcamPreviewOffset();
            }
        }

        /// <summary>
        /// Offsets the preview root when sideways webcam rotation would otherwise overlap the surrounding UI.
        /// </summary>
        void UpdateWebcamPreviewOffset()
        {
            RectTransform previewRoot = GetWebcamPreviewRoot();
            if (previewRoot == null)
                return;

            if (!m_hasWebcamPreviewRootAnchoredPosition)
            {
                m_webcamPreviewRootAnchoredPosition = previewRoot.anchoredPosition;
                m_hasWebcamPreviewRootAnchoredPosition = true;
            }

            Vector2 anchoredPosition = m_webcamPreviewRootAnchoredPosition;
            if (IsWebcamPreviewSideways())
            {
                Rect imageRect = m_webcamRawImage.m_image.rectTransform.rect;
                anchoredPosition.y -= Mathf.Max(0, (imageRect.width - imageRect.height) * 0.5f);
            }

            previewRoot.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// Restores the preview root to its original anchored position after webcam display or orientation correction is disabled.
        /// </summary>
        void ResetWebcamPreviewOffset()
        {
            RectTransform previewRoot = GetWebcamPreviewRoot();
            if (previewRoot != null && m_hasWebcamPreviewRootAnchoredPosition)
                previewRoot.anchoredPosition = m_webcamPreviewRootAnchoredPosition;
        }

        /// <summary>
        /// Gets and caches the RectTransform that should move when the rotated webcam preview needs extra spacing.
        /// </summary>
        /// <returns>The webcam preview root RectTransform, or null if the preview hierarchy is unavailable.</returns>
        RectTransform GetWebcamPreviewRoot()
        {
            if (m_webcamPreviewRoot == null && m_webcamRawImage != null && m_webcamRawImage.m_image != null)
                m_webcamPreviewRoot = m_webcamRawImage.m_image.rectTransform.parent as RectTransform;

            return m_webcamPreviewRoot;
        }

        /// <summary>
        /// Determines whether the active webcam preview is using a sideways native orientation correction.
        /// </summary>
        /// <returns>True when orientation correction is enabled and the effective rotation is 90 or 270 degrees.</returns>
        bool IsWebcamPreviewSideways()
        {
            return m_vhWebCam != null && m_vhWebCam.nativeOrientationCorrectionEnabled && IsSidewaysRotation(m_vhWebCam.effectiveVideoRotationAngle);
        }

        /// <summary>
        /// Draws webcam diagnostics and simulation controls inside the sample debug menu foldout.
        /// </summary>
        void OnGUIWebcamDebug()
        {
            m_webcamDebugToggle = m_debugMenu.Toggle(m_webcamDebugToggle, m_webcamDebugToggle ? "- Webcam Debug" : "+ Webcam Debug");
            if (!m_webcamDebugToggle)
                return;

            m_debugMenu.Label("Webcam Facing");
            int webCamFacing = m_debugMenu.SelectionGrid((int)m_vhWebCam.preferredFacing, s_webCamFacingOptions, 3);
            if (webCamFacing != (int)m_vhWebCam.preferredFacing)
            {
                StopSensingProcessor();
                m_vhWebCam.SetPreferredFacing((VHWebCam.CameraFacing)webCamFacing);
                m_webCamIndex = m_vhWebCam.currentDevice;
            }

            bool forceOrientationCorrection = m_debugMenu.Toggle(m_vhWebCam.debugForceNativeOrientationCorrection, m_vhWebCam.debugForceNativeOrientationCorrection ? "Sim Orientation ON" : "Sim Orientation OFF");
            if (forceOrientationCorrection != m_vhWebCam.debugForceNativeOrientationCorrection)
                m_vhWebCam.SetDebugNativeOrientationCorrection(forceOrientationCorrection);

            m_debugMenu.Label("Sim Rotation");
            int rotationIndex = GetWebCamRotationIndex(m_vhWebCam.debugVideoRotationAngle);
            int newRotationIndex = m_debugMenu.SelectionGrid(rotationIndex, s_webCamRotationOptions, 4);
            if (newRotationIndex != rotationIndex)
            {
                m_vhWebCam.SetDebugNativeOrientationCorrection(true);
                m_vhWebCam.SetDebugOrientationMetadata(GetWebCamRotationAngle(newRotationIndex), m_vhWebCam.debugVideoVerticallyMirrored);
            }

            bool verticalMirror = m_debugMenu.Toggle(m_vhWebCam.debugVideoVerticallyMirrored, m_vhWebCam.debugVideoVerticallyMirrored ? "Sim MirrorY ON" : "Sim MirrorY OFF");
            if (verticalMirror != m_vhWebCam.debugVideoVerticallyMirrored)
            {
                m_vhWebCam.SetDebugNativeOrientationCorrection(true);
                m_vhWebCam.SetDebugOrientationMetadata(m_vhWebCam.debugVideoRotationAngle, verticalMirror);
            }

            m_debugMenu.Label($"Device: {m_vhWebCam.currentDevice} {(m_vhWebCam.isFrontFacing ? "Front" : "Back")}");
            m_debugMenu.Label($"Raw Rotation: {m_vhWebCam.videoRotationAngle} MirrorY: {m_vhWebCam.videoVerticallyMirrored}");
            m_debugMenu.Label($"Effective Rotation: {m_vhWebCam.effectiveVideoRotationAngle} MirrorY: {m_vhWebCam.effectiveVideoVerticallyMirrored}");
            m_debugMenu.Label($"Capture: {m_sensingProcessor.captureWidth}x{m_sensingProcessor.captureHeight}");
        }

        /// <summary>
        /// Converts a rotation angle into the debug-menu selection index used by the webcam rotation grid.
        /// </summary>
        /// <param name="rotationAngle">Rotation angle in degrees.</param>
        /// <returns>The selection index for 0, 90, 180, or 270 degrees.</returns>
        static int GetWebCamRotationIndex(int rotationAngle)
        {
            switch (((rotationAngle % 360) + 360) % 360)
            {
                case 90: return 1;
                case 180: return 2;
                case 270: return 3;
                default: return 0;
            }
        }

        /// <summary>
        /// Converts a debug-menu rotation selection index into a rotation angle in degrees.
        /// </summary>
        /// <param name="rotationIndex">Selection index from the webcam rotation grid.</param>
        /// <returns>The corresponding rotation angle, clamped to 0, 90, 180, or 270 degrees.</returns>
        static int GetWebCamRotationAngle(int rotationIndex)
        {
            return Mathf.Clamp(rotationIndex, 0, 3) * 90;
        }

        /// <summary>
        /// Determines whether a rotation angle represents a sideways webcam preview orientation.
        /// </summary>
        /// <param name="rotationAngle">Rotation angle in degrees.</param>
        /// <returns>True when the normalized rotation is 90 or 270 degrees.</returns>
        static bool IsSidewaysRotation(int rotationAngle)
        {
            int rotation = ((rotationAngle % 360) + 360) % 360;
            return rotation == 90 || rotation == 270;
        }
    }
}
