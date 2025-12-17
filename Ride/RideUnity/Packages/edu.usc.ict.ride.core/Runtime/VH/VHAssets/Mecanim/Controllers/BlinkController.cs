using Ride;
using UnityEngine;

using Random = UnityEngine.Random;

namespace VHAssets
{
/// <summary>
/// Controls automatic and manual blinking for a character, using either an
/// animation clip, Mecanim blend tree parameters, or blend shapes.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BlinkController"/> is a simple, update-driven blink driver
/// that avoids coroutines and keeps its behavior deterministic and easy to
/// reason about. It periodically triggers a blink after a random interval
/// between <see cref="m_MinBlinkInterval"/> and <see cref="m_MaxBlinkInterval"/>,
/// and performs the blink over time using a small internal state machine.
/// </para>
///
/// <para>
/// The controller supports three blink modes:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///     <see cref="BlinkMode.Animation"/> – plays a named Mecanim animation clip
///     (<see cref="m_BlinkAnimName"/>) on the character's face layer. In this
///     mode the controller does not drive any parameters or blend shapes; the
///     animation is responsible for the eyelid motion.
///     </description>
///   </item>
///   <item>
///     <description>
///     <see cref="BlinkMode.BlendTree"/> – drives one or more float parameters
///     (see <see cref="m_EyeLidControllerParams"/>) on the attached
///     <see cref="Animator"/>. These parameters are typically used by a Mecanim
///     blend tree to pose the eyelids. The values are driven smoothly between
///     0 and <see cref="m_BlinkBlendMax"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///     <see cref="BlinkMode.BlendShape"/> – drives one or more blend shapes on a
///     <see cref="SkinnedMeshRenderer"/> (see <see cref="m_EyeLidBlendShapes"/>
///     and <see cref="m_BlendShapeSkinnedMeshName"/>). The blend shape weights
///     are driven smoothly between 0 and <see cref="m_BlinkBlendMax"/>.
///     </description>
///   </item>
/// </list>
///
/// <para>
/// The blink motion itself is implemented as a simple state machine updated in
/// <see cref="Update"/>:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="BlinkState.Idle"/> – waiting for the next blink time.</description></item>
///   <item><description><see cref="BlinkState.BlinkClosing"/> – lids close over <see cref="m_BlinkLength"/> seconds.</description></item>
///   <item><description><see cref="BlinkState.BlinkOpening"/> – lids open over <see cref="m_BlinkLength"/> seconds.</description></item>
/// </list>
///
/// <para>
/// The controller exposes a public <see cref="Blink"/> method which can be used
/// by other systems (for example, a speech, emotion, or gaze controller) to
/// force an immediate blink. Calling <see cref="Blink"/> while a blink is in
/// progress will restart the blink from fully open; blinks are not queued.
/// </para>
///
/// <para>
/// The <see cref="IsBlinkingOn"/> property can be used to enable or disable
/// automatic blinking at runtime. When blinking is disabled, the controller
/// snaps the eyelids back to a fully open state so the character cannot become
/// "stuck" in a half-blink. Disabling or disabling the component also resets
/// the eyelids to an open state.
/// </para>
///
/// <para>
/// This controller intentionally overwrites whatever values other systems may
/// have written to the configured eyelid parameters or blend shapes during a
/// blink. It does not attempt to preserve or blend with previous values, in
/// order to keep the implementation simple and predictable. If another system
/// needs to pose the eyelids, it should either use a different parameter /
/// blend shape set or coordinate with this controller to disable blinking when
/// appropriate.
/// </para>
///
/// <para>
/// To use this controller:
/// </para>
/// <list type="number">
///   <item>
///     <description>
///     Add <see cref="BlinkController"/> to the character root and assign the
///     appropriate <see cref="BlinkMode"/>.
///     </description>
///   </item>
///   <item>
///     <description>
///     For <see cref="BlinkMode.BlendTree"/>, configure <see cref="m_EyeLidControllerParams"/>
///     to match the animator parameter names used by the eyelid blend tree.
///     </description>
///   </item>
///   <item>
///     <description>
///     For <see cref="BlinkMode.BlendShape"/>, set <see cref="m_BlendShapeSkinnedMeshName"/>
///     and <see cref="m_EyeLidBlendShapes"/> to match the eyelid blend shape
///     mesh and blend shape names.
///     </description>
///   </item>
///   <item>
///     <description>
///     Optionally drive <see cref="IsBlinkingOn"/> and <see cref="Blink"/> from
///     higher-level logic to control when the character blinks.
///     </description>
///   </item>
/// </list>
///
/// <para>
/// This controller is intended to be lightweight and self-contained. It does
/// not allocate garbage each frame and does not start or stop coroutines at
/// runtime. All timing is handled in <see cref="Update"/> using simple float
/// accumulators.
/// </para>
/// </remarks>
public class BlinkController : MonoBehaviour
{
    #region Constants
    protected enum BlinkMode
    {
        Animation,
        BlendTree,
        BlendShape
    }

    protected enum BlinkState
    {
        Idle,
        BlinkClosing,
        BlinkOpening
    }
    #endregion

    #region Variables
    [SerializeField] float m_MinBlinkInterval = 4.0f;
    [SerializeField] float m_MaxBlinkInterval = 8.0f;
    [Tooltip("The time in seconds it takes for the eyelid to close or open, therefore the full length of the blink will be twice this number.")]
    [SerializeField] float m_BlinkLength = 0.2f;
    [SerializeField] bool m_IsBlinkingOn = true;
    [SerializeField] protected BlinkMode m_BlinkMode = BlinkMode.BlendTree;
    [SerializeField] protected string m_BlinkAnimName = "";
    [SerializeField] float m_BlinkBlendMax = 1.0f;
    [SerializeField] protected string[] m_EyeLidControllerParams = new string[] { "045_blink_lf", "045_blink_rt" };
    [SerializeField] protected string[] m_EyeLidBlendShapes = new string[] { "045_blink_lf", "045_blink_rt" };
    [SerializeField] protected string m_BlendShapeSkinnedMeshName = "";
    public SkinnedMeshRenderer skinnedMeshRenderer;

    Animator m_Animator;

    private bool m_assetInitialized = false;

    private BlinkState m_state = BlinkState.Idle;
    private float m_nextBlinkTime;
    private float m_blinkProgress;
    #endregion

    #region Debug

    [Header("Debug")]
    public bool m_debugBlink;

    #endregion

    #region Properties
    public bool IsBlinkingOn
    {
        get { return m_IsBlinkingOn; }
        set
        {
            if (m_IsBlinkingOn == value)
                return;

            m_IsBlinkingOn = value;

            if (!m_assetInitialized)
                return;

            if (!m_IsBlinkingOn)
                ResetBlinkImmediate();  // Stop blinking and open eyes fully so we never get "stuck".
            else
                ScheduleNextBlink();  // Restart blink schedule when turned back on.
        }
    }

    public float BlinkValue
    {
        get
        {
            // Animation mode does not drive params directly, and we do not try to infer
            // blink value from the clip. Just return 0 for now.
            if (m_BlinkMode == BlinkMode.Animation)
                return 0f;

            // When idle, the blink has finished and m_blinkProgress should be zero.
            if (m_state == BlinkState.Idle)
                return 0f;

            //Debug.Log($"BlinkValue: {m_blinkProgress}. State: {m_state}. Mode: {m_BlinkMode}");

            // Match the same shaping used in ApplyBlinkWeights, but normalized to [0..1].
            float t = Mathf.Clamp01(m_blinkProgress);
            float v = Mathf.SmoothStep(0.0f, 1.0f, t);
            return v;
        }
    }

    #endregion

    #region Unity Event Functions

    void Start()
    {
        if (!TryGetComponent(out ILoadableAsset loadedAsset))
            InitializeLoadedAsset();

        ScheduleNextBlink();
    }

    void OnEnable()
    {
        if (m_assetInitialized)
            ScheduleNextBlink();
    }

    void OnDisable()
    {
        if (!m_assetInitialized)
            return;

        // Ensure we never leave eyelids in a half-blink when disabled.
        ResetBlinkImmediate();
    }

    void Update()
    {
        if (m_assetInitialized && m_IsBlinkingOn)
            UpdateBlink();

#if UNITY_EDITOR
        DebugUpdate();
#endif
    }
    #endregion

    #region Functions
    public void InitializeLoadedAsset()
    {
        m_Animator = GetComponentInChildren<Animator>();
        if (m_Animator == null)
            Debug.LogError("Blink Controller needs and animator");

        if (skinnedMeshRenderer == null)
        {
            var go = VHUtils.FindChildRecursive(gameObject, m_BlendShapeSkinnedMeshName);
            if (go != null)
                skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
        }

        m_assetInitialized = true;
    }

    private void ScheduleNextBlink()
    {
        m_nextBlinkTime = Time.time + Random.Range(m_MinBlinkInterval, m_MaxBlinkInterval);
        m_state = BlinkState.Idle;
        m_blinkProgress = 0.0f;
    }

    private void UpdateBlink()
    {
        switch (m_state)
        {
            case BlinkState.Idle:
                if (Time.time >= m_nextBlinkTime)
                    Blink();
                break;

            case BlinkState.BlinkClosing:
                AdvanceBlinkClosing();
                break;

            case BlinkState.BlinkOpening:
                AdvanceBlinkOpening();
                break;
        }
    }

    private void AdvanceBlinkClosing()
    {
        m_blinkProgress += Time.deltaTime / m_BlinkLength;

        if (m_blinkProgress >= 1.0f)
        {
            m_blinkProgress = 1.0f;
            ApplyBlinkWeights(m_blinkProgress);
            m_state = BlinkState.BlinkOpening;
            return;
        }

        ApplyBlinkWeights(m_blinkProgress);
    }

    private void AdvanceBlinkOpening()
    {
        m_blinkProgress -= Time.deltaTime / m_BlinkLength;

        if (m_blinkProgress <= 0.0f)
        {
            m_blinkProgress = 0.0f;
            ApplyBlinkWeights(m_blinkProgress);
            ScheduleNextBlink();
            return;
        }

        ApplyBlinkWeights(m_blinkProgress);
    }

    /// <summary>
    /// t is the blink factor [0..1]. We simply drive eyelids between 0 and m_BlinkBlendMax.
    /// This overrides any previous controller state.
    /// </summary>
    protected virtual void ApplyBlinkWeights(float t)
    {
        if (m_BlinkMode == BlinkMode.Animation)
            return; // Nothing to do; animation mode uses clips only.

        float v = Mathf.SmoothStep(0.0f, m_BlinkBlendMax, t);

        int count = (m_BlinkMode == BlinkMode.BlendTree)
            ? m_EyeLidControllerParams.Length
            : m_EyeLidBlendShapes.Length;

        for (int i = 0; i < count; i++)
            SetEyeLidWeight(GetLidName(i), v);
    }

    private string GetLidName(int index)
    {
        return (m_BlinkMode == BlinkMode.BlendTree)
            ? m_EyeLidControllerParams[index]
            : m_EyeLidBlendShapes[index];
    }

    private void PlayAnimationBlink()
    {
        var mc = GetComponent<MecanimCharacter>();
        int faceLayer = (mc != null) ? mc.FaceLayerIndex : 0;
        m_Animator.Play(m_BlinkAnimName, faceLayer);
    }

    /// <summary>
    /// Snap state to "no blink", used when we disable or turn blinking off.
    /// </summary>
    private void ResetBlinkImmediate()
    {
        m_state = BlinkState.Idle;
        m_blinkProgress = 0.0f;
        ApplyBlinkWeights(0.0f);
    }

    public void Blink()
    {
        if (!m_assetInitialized)
            return;

        // Guard against degenerate config.
        if (m_BlinkLength <= 0.0f)
        {
            // Just snap a quick blink and reschedule.
            ApplyBlinkWeights(0.0f);
            ScheduleNextBlink();
            return;
        }

        // Animation mode: play the clip and reschedule the next random blink.
        if (m_BlinkMode == BlinkMode.Animation)
        {
            PlayAnimationBlink();
            ScheduleNextBlink();
            return;
        }

        // BlendTree or BlendShape mode:
        // We override any previous state and start a new blink from fully open.
        m_blinkProgress = 0.0f;
        ApplyBlinkWeights(0.0f);      // Explicitly open first.
        m_state = BlinkState.BlinkClosing;
    }

    void SetEyeLidWeight(string lidName, float weight)
    {
        if (m_BlinkMode == BlinkMode.BlendTree)
        {
            if (m_Animator != null)
                m_Animator.SetFloat(lidName, weight);
        }
        else if (m_BlinkMode == BlinkMode.BlendShape)
        {
            if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
                return;

            int index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(lidName);
            if (index >= 0)
                skinnedMeshRenderer.SetBlendShapeWeight(index, weight);
        }
    }

    #endregion


    #region Debug Functions

    void DebugUpdate()
    {
        if (m_debugBlink)
        {
            Debug.Log("Blink!");
            Blink();
            m_debugBlink = false;
        }
    }

    #endregion
}
}
