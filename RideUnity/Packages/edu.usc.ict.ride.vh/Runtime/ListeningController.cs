using System.Collections;
using UnityEngine;
using VHAssets;
using Ride.Audio;

/// <summary>
/// Controls listening behavior based on microphone activity, triggering head nods during speech.
/// </summary>
[RequireComponent(typeof(HeadController))]
public class ListeningController : MonoBehaviour
{
    #region Serialized Fields

    [Header("References")]
    [SerializeField] HeadController m_headController;

    [Header("Settings")]
    [Tooltip("Time interval to check microphone input.")]
    [SerializeField] float m_listenTickTime = 0.1f;

    [Tooltip("Amount of head movement for nodding.")]
    [SerializeField] float m_nodAmt = 0.25f;

    [Tooltip("Duration of each nod animation.")]
    [SerializeField] float m_nodTime = 1.2f;

    [Tooltip("Maximum number of nodding actions before stopping.")]
    [SerializeField] int m_maxNodCount = 5;

    [Tooltip("Cooldown time after nodding before listening resumes.")]
    [SerializeField] float m_nodCooldown = 1.2f;

    #endregion

    private Coroutine m_listeningRoutine;

    /// <summary>
    /// Indicates whether the system is currently listening.
    /// </summary>
    public bool IsListening => m_listeningRoutine != null;

    /// <summary>
    /// Ensures a HeadController reference is assigned.
    /// </summary>
    private void Awake()
    {
        m_headController = m_headController != null ? m_headController : GetComponent<HeadController>();
    }

    /// <summary>
    /// Starts the listening behavior using a microphone system and volume threshold.
    /// </summary>
    /// <param name="recorderSystem">The microphone system used to detect sound.</param>
    /// <param name="micThreshold">The volume threshold for detecting speech.</param>
    public void StartListening(MicrophoneAudioSystem recorderSystem, float micThreshold)
    {
        StopListening();
        m_listeningRoutine = StartCoroutine(ListeningBehaviorRoutine(recorderSystem, micThreshold));
    }

    /// <summary>
    /// Stops the listening routine if it is active.
    /// </summary>
    public void StopListening()
    {
        if (m_listeningRoutine == null) return;
        StopCoroutine(m_listeningRoutine);
        m_listeningRoutine = null;
    }

    /// <summary>
    /// Coroutine that listens for microphone input and triggers head nods when speech is detected.
    /// </summary>
    /// <param name="recorderSystem">The microphone system to monitor.</param>
    /// <param name="micThreshold">Threshold to determine silence or speech.</param>
    /// <returns>An enumerator for coroutine execution.</returns>
    private IEnumerator ListeningBehaviorRoutine(MicrophoneAudioSystem recorderSystem, float micThreshold)
    {
        yield return new WaitForSeconds(m_listenTickTime);

        int nodCountTotal = 0;

        while (recorderSystem.IsRecording)
        {
            //Wait while silent
            while (recorderSystem.IsDeviceSilent(micThreshold))
                yield return null;

            //Wait while speaking
            while (!recorderSystem.IsDeviceSilent(micThreshold))
                yield return new WaitForSeconds(m_listenTickTime);

            int nodCountRandom = Random.Range(1, 3);
            float nodAmtRandom = UnityEngine.Random.Range(m_nodAmt * 0.8f, m_nodAmt * 1.2f);
            float nodTimeRandom = UnityEngine.Random.Range(m_nodTime * 0.9f, m_nodTime * 1.1f);

            for (int i = 0; i < nodCountRandom; i++)
            {
                Debug.Log($"ListeningBehaviorRoutine() - nodCount: {nodCountRandom}, nodAmount: {nodAmtRandom}, nodTime: {nodTimeRandom}");
                m_headController.NodHead(nodAmtRandom, 1, nodTimeRandom);
            }

            nodCountTotal++;

            if (nodCountTotal >= m_maxNodCount)
                yield break;

            yield return new WaitForSeconds(nodTimeRandom + m_nodCooldown);
        }
    }
}
