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
            m_sensingProcessor.SetSensingSystems(m_currentSensing);
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
            m_webcamRawImage.texture = m_vhWebCam.renderMaterial.mainTexture;

            Application.RequestUserAuthorization(UserAuthorization.WebCam);

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
    }
}
