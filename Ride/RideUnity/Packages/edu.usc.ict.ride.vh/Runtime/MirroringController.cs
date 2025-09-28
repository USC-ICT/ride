using System.Collections.Generic;
using UnityEngine;
using VHAssets;

/// <summary>
/// Controls facial emotion mirroring for a character.
/// </summary>
[RequireComponent(typeof(MecanimCharacter))]
public class MirroringController : MonoBehaviour
{
    #region Serialized Fields

    [Tooltip("Reference to the MecanimCharacter component responsible for animation.")]
    [SerializeField] MecanimCharacter m_character;

    [Tooltip("Duration to blend into a new emotion.")]
    [SerializeField] float blendInTime = 0.5f;

    [Tooltip("Duration to blend out of the current emotion.")]
    [SerializeField] float blendOutTime = 0.5f;

    /// <summary>
    /// Name of the previously active emotion.
    /// </summary>
    [SerializeField] protected string m_prevEmotion = string.Empty;

    /// <summary>
    /// Name of the currently active emotion.
    /// </summary>
    [SerializeField] protected string m_currentEmotion = "Neutral";

    #endregion

    /// <summary>
    /// Maps emotion names to corresponding Mecanim parameter names.
    /// </summary>
    readonly Dictionary<string, string> m_emotionParamMap = new Dictionary<string, string>
    {
        { "Happy", "112_happy" },
        { "Anger", "129_angry" },
        { "Surprise", "127_surprise" },
        { "Contempt", "131_contempt" },
        { "Disgust", "124_disgust" },
        { "Fear", "126_fear" },
        { "Sadness", "130_sad" },
        { "Neutral", "face_neutral" }
    };

    /// <summary>
    /// Initializes the controller and assigns the MecanimCharacter reference if missing.
    /// </summary>
    protected virtual void Start()
    {
        m_character = m_character != null ? m_character : GetComponent<MecanimCharacter>();
        m_currentEmotion = "Neutral";
    }

    /// <summary>
    /// Changes the character's facial expression to mirror the given emotion.
    /// Blends out the previous emotion and blends into the new one.
    /// </summary>
    /// <param name="emotion">The name of the emotion to mirror.</param>
    public void MirrorEmotion(string emotion)
    {
        if (emotion == m_currentEmotion) return;

        if (m_currentEmotion != string.Empty && m_emotionParamMap.ContainsKey(m_currentEmotion))
        {
            BlendEmotion(m_currentEmotion, 0, blendOutTime);
        }

        m_prevEmotion = m_currentEmotion;
        m_currentEmotion = emotion;

        if (!m_emotionParamMap.ContainsKey(emotion)) return;

        BlendEmotion(m_currentEmotion, 1, blendInTime);
    }

    /// <summary>
    /// Blends the specified emotion parameter to a target value over time.
    /// </summary>
    /// <param name="emotion">The emotion name to blend.</param>
    /// <param name="value">The target blend value (0 or 1).</param>
    /// <param name="blendTime">The duration of the blend.</param>
    protected virtual void BlendEmotion(string emotion, float value, float blendTime)
    {
        m_character.SetFloatParam(m_emotionParamMap[emotion], value, blendTime);
    }

    /// <summary>
    /// Stops any currently mirrored emotion and returns the character to a neutral expression.
    /// </summary>
    public void StopMirroring()
    {
        if (m_currentEmotion != string.Empty && m_currentEmotion != "Neutral")
        {
            BlendEmotion(m_currentEmotion, 0, blendOutTime);
            BlendEmotion("Neutral", 1, blendInTime);
        }

        m_prevEmotion = string.Empty;
        m_currentEmotion = "Neutral";
    }
}
