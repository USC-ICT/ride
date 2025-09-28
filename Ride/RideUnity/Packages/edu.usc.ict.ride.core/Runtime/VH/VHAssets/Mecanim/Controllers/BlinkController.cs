using System.Collections;
using Ride;
using UnityEngine;

using Random = UnityEngine.Random;

namespace VHAssets
{
public class BlinkController : MonoBehaviour
{
    #region Constants
    enum BlinkMode
    {
        Animation,
        BlendTree,
        BlendShape
    }
    #endregion

    #region Variables
    [SerializeField] float m_MinBlinkInterval = 4.0f;
    [SerializeField] float m_MaxBlinkInterval = 8.0f;
    [Tooltip("The time in seconds it takes for the eyelid to close or open, therefore the full length of the blink will be twice this number.")]
    [SerializeField] float m_BlinkLength = 0.2f;
    [SerializeField] bool m_IsBlinkingOn = true;
    [SerializeField] BlinkMode m_BlinkMode = BlinkMode.BlendTree;
    [SerializeField] string m_BlinkAnimName = "";
    [SerializeField] float m_BlinkBlendMax = 1.0f;
    [SerializeField] string[] m_EyeLidControllerParams = new string[] { "045_blink_lf", "045_blink_rt" };
    [SerializeField] string[] m_EyeLidBlendShapes = new string[] { "045_blink_lf", "045_blink_rt" };
    [SerializeField] string m_BlendShapeSkinnedMeshName = "";
    public SkinnedMeshRenderer skinnedMeshRenderer;

    float m_currentBlinkProgress;    // the current 'weight' of the blink 
    float m_BlinkPeriod = 1;
    Animator m_Animator;

    private bool m_assetInitialized = false;

    #endregion

    #region Debug

    [Header("Debug")]
    public bool m_debugBlink;

    #endregion

    #region Properties
    public bool IsBlinkingOn
    {
        get { return m_IsBlinkingOn; }
        set { m_IsBlinkingOn = value; }
    }

    #endregion

    #region Unity Event Functions

    void Start()
    {
        if (!TryGetComponent(out ILoadableAsset loadedAsset))
            InitializeLoadedAsset();
        if (IsBlinkingOn)
            StartCoroutine(BlinkUpdate());
    }

    void OnEnable()
    {
        if(m_assetInitialized)
            StartCoroutine(BlinkUpdate());
    }

#if UNITY_EDITOR
    void Update()
    {
        DebugUpdate();
    }
#endif

    #endregion

    #region Functions
    public void InitializeLoadedAsset()
    {
        m_Animator = GetComponentInChildren<Animator>();
        if (m_Animator == null)
            Debug.LogError("Blink Controller needs and animator");

        if (skinnedMeshRenderer != null) return;

        GameObject go = VHUtils.FindChildRecursive(gameObject, m_BlendShapeSkinnedMeshName);
        if (go != null)
            skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();

        m_assetInitialized = true;
    }

    IEnumerator BlinkUpdate()
    {
        while (true)
        {
            m_BlinkPeriod = Random.Range(m_MinBlinkInterval, m_MaxBlinkInterval);

            yield return new WaitForSeconds(m_BlinkPeriod);

            if (m_assetInitialized && IsBlinkingOn)
            {
                Blink();
            }
        }
    }

    public void Blink()
    {
        switch (m_BlinkMode)
        {
            case BlinkMode.Animation:
                HandleAnimationBlink();
                break;

            case BlinkMode.BlendTree:
                HandleBlendTreeBlink();
                break;

            case BlinkMode.BlendShape:
                HandleBlendShapeBlink();
                break;
        }
    }

    void HandleAnimationBlink()
    {
        m_Animator.Play(m_BlinkAnimName, GetComponent<MecanimCharacter>().FaceLayerIndex);
    }

    void HandleBlendTreeBlink()
    {
        StartCoroutine(PerformBlink(m_BlinkLength));
    }


    void HandleBlendShapeBlink()
    {
        StartCoroutine(PerformBlink(m_BlinkLength));
    }


    /// <summary>
    /// Perform a blink by ramping the strength of a 'blink pose' up and then down.
    /// </summary>
    IEnumerator PerformBlink(float blinkSpeed)
    {
        float referenceTime;
        float[] weights = GetCurrentEyeLidWeights();

        // Blink 'closing
        referenceTime = Time.time;
        m_currentBlinkProgress = 0.0f;

        while (m_currentBlinkProgress <= 1.0f)
        {
            m_currentBlinkProgress = (Time.time - referenceTime) / blinkSpeed;

            for (int i = 0; i < weights.Length; i++)
            {
                SetEyeLidWeight(GetEyeLidName(i), Mathf.SmoothStep(weights[i], m_BlinkBlendMax, m_currentBlinkProgress));
            }

            yield return new WaitForEndOfFrame();
        }

        // Blink 'opening'
        m_currentBlinkProgress = 1.0f;
        referenceTime = Time.time;

        while (m_currentBlinkProgress >= 0.0f)
        {
            m_currentBlinkProgress = 1 - ((Time.time - referenceTime) / blinkSpeed);

            for (int i = 0; i < weights.Length; i++)
            {
                SetEyeLidWeight(GetEyeLidName(i), Mathf.SmoothStep(weights[i], m_BlinkBlendMax, m_currentBlinkProgress));
            }

            yield return new WaitForEndOfFrame();
        }

        m_currentBlinkProgress = 0.0f;
    }

    string GetEyeLidName(int index)
    {
        return m_BlinkMode == BlinkMode.BlendTree ? m_EyeLidControllerParams[index] : m_EyeLidBlendShapes[index];
    }

    float [] GetCurrentEyeLidWeights()
    {
        float [] weights = null;
        if (m_BlinkMode == BlinkMode.BlendTree)
        {
            weights = new float[m_EyeLidControllerParams.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = m_Animator.GetFloat(m_EyeLidControllerParams[i]);
            }
        }
        else if (m_BlinkMode == BlinkMode.BlendShape)
        {
            weights = new float[m_EyeLidBlendShapes.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                int index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(m_EyeLidBlendShapes[i]);
                weights[i] = skinnedMeshRenderer.GetBlendShapeWeight(index);
            }
        }
        return weights;
    }

    void SetEyeLidWeight(string lidName, float weight)
    {
        if (m_BlinkMode == BlinkMode.BlendTree)
        {
            m_Animator.SetFloat(lidName, weight);
        }
        else if (m_BlinkMode == BlinkMode.BlendShape)
        {
            int index = skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(lidName);
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
