using UnityEngine;
using VHAssets;
using Ride.Audio;

namespace Ride
{
    /// <summary>
    /// Controls head-nodding listening behavior based on live microphone input.
    ///
    /// This component monitors a <see cref="MicrophoneAudioSystem"/> while recording is active
    /// and triggers subtle head nod gestures when speech is detected, using a simple
    /// time-based state machine driven from <c>Update()</c>.
    ///
    /// The controller is designed to be safe for dynamic asset load/unload scenarios:
    /// - It does not use coroutines.
    /// - All runtime behavior can be stopped deterministically via <see cref="ResetLoadedAsset"/>.
    /// - It remains enabled at all times but becomes inert when not actively listening.
    ///
    /// Initialization follows the standard RIDE loadable-asset pattern:
    /// - For non-loadable characters, initialization occurs automatically in <c>Start()</c>.
    /// - For loadable characters, initialization is deferred until
    ///   <c>InitializeLoadedAsset()</c> is invoked (typically via <c>RideCatalogAsset</c>).
    ///
    /// The listening session automatically terminates when:
    /// - Recording stops,
    /// - The maximum number of nod responses is reached,
    /// - Or <see cref="StopListening"/> is called explicitly.
    ///
    /// Note that a single "nod response" may consist of multiple small nod motions,
    /// depending on randomized parameters.
    /// </summary>
    public class ListeningController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip(
            "Optional HeadController used to perform nod gestures while listening. " +
            "If left unassigned, the component will try GetComponent on this GameObject; " +
            "if still missing, it will search children recursively. Logs an error if not found."
        )]
        [SerializeField] HeadController m_headController;

        [Header("Settings")]
        [Tooltip("Time interval to check microphone input.")]
        [SerializeField] float m_listenTickTime = 0.1f;

        [Tooltip("Amount of head movement for nodding.")]
        [SerializeField] float m_nodAmt = 0.25f;

        [Tooltip("Duration of each nod animation.")]
        [SerializeField] float m_nodTime = 1.2f;

        [Tooltip("Maximum number of nodding actions before stopping.")]
        [SerializeField] int m_maxNodCount = 10;

        [Tooltip("Cooldown time after nodding before listening resumes.")]
        [SerializeField] float m_nodCooldown = 1.2f;

        #endregion

        private enum ListeningState
        {
            Idle,
            WaitingForSpeech,
            WaitingForSilence,
            Cooldown
        }

        private ListeningState m_state = ListeningState.Idle;
        private MicrophoneAudioSystem m_recorderSystem;
        private float m_micThreshold;
        private float m_nextTickTime;
        private float m_cooldownEndTime;
        private int m_nodCountTotal;


        /// <summary>Indicates whether the system is currently listening.</summary>
        public bool IsListening => m_state != ListeningState.Idle;


        private void Start()
        {
            if (!TryGetComponent(out ILoadableAsset _))
                InitializeLoadedAsset();
        }

        public void InitializeLoadedAsset()
        {
            if (m_headController == null)
                m_headController = GetComponent<HeadController>();

            if (m_headController == null)
                m_headController = GetComponentInChildren<HeadController>(true);

            if (m_headController == null)
                Debug.LogError($"{nameof(ListeningController)}: No HeadController found. Assign one, or add a HeadController component to this GameObject or its children.", this);
        }

        public void ResetLoadedAsset()
        {
            // Stop any ongoing listening behavior so we stop driving head motion
            // while the character art is being unloaded / reloaded.
            StopListening();
        }

        private void Update()
        {
            if (m_state == ListeningState.Idle || m_recorderSystem == null)
                return;

            if (!m_recorderSystem.IsRecording)
            {
                StopListening();
                return;
            }

            if (Time.time < m_nextTickTime)
                return;

            switch (m_state)
            {
                case ListeningState.WaitingForSpeech:
                    if (!m_recorderSystem.IsDeviceSilent(m_micThreshold))
                        m_state = ListeningState.WaitingForSilence;
                    break;

                case ListeningState.WaitingForSilence:
                    if (m_recorderSystem.IsDeviceSilent(m_micThreshold))
                    {
                        TriggerNod();
                        m_state = ListeningState.Cooldown;
                        m_cooldownEndTime = Time.time + m_nodCooldown;
                    }
                    break;

                case ListeningState.Cooldown:
                    if (Time.time >= m_cooldownEndTime)
                    {
                        if (m_nodCountTotal >= m_maxNodCount)
                        {
                            StopListening();
                            return;
                        }

                        m_state = ListeningState.WaitingForSpeech;
                    }
                    break;
            }

            m_nextTickTime = Time.time + m_listenTickTime;
        }

        /// <summary>
        /// Starts the listening behavior using a microphone system and volume threshold.
        /// </summary>
        /// <param name="recorderSystem">The microphone system used to detect sound.</param>
        /// <param name="micThreshold">The volume threshold for detecting speech.</param>
        public void StartListening(MicrophoneAudioSystem recorderSystem, float micThreshold)
        {
            StopListening();

            m_recorderSystem = recorderSystem;
            m_micThreshold = micThreshold;

            m_nodCountTotal = 0;
            m_state = ListeningState.WaitingForSpeech;
            m_nextTickTime = Time.time + m_listenTickTime;
        }

        /// <summary>
        /// Stops the listening routine if it is active.
        /// </summary>
        public void StopListening()
        {
            m_state = ListeningState.Idle;
            m_recorderSystem = null;

            m_micThreshold = 0f;
            m_nextTickTime = 0f;
            m_cooldownEndTime = 0f;
        }

        private void TriggerNod()
        {
            int nodCountRandom = UnityEngine.Random.Range(1, 3);
            float nodAmtRandom = UnityEngine.Random.Range(m_nodAmt * 0.8f, m_nodAmt * 1.2f);
            float nodTimeRandom = UnityEngine.Random.Range(m_nodTime * 0.9f, m_nodTime * 1.1f);

            for (int i = 0; i < nodCountRandom; i++)
                m_headController.NodHead(nodAmtRandom, 1, nodTimeRandom);

            m_nodCountTotal++;
        }
    }
}
