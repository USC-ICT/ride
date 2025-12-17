using Ride;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
//[RequireComponent(typeof(Animator))]

//Name of class must be name of file as well

//[RequireComponent(typeof(FacialAnimationPlayer_Animator))]
//[RequireComponent(typeof(HeadController))]
//[RequireComponent(typeof(GazeController_IK))]
//[RequireComponent(typeof(SaccadeController))]
public class MecanimCharacter : ICharacter
{
    #region Variables
    [SerializeField] string m_StartingPosture = "";
    [SerializeField] int m_BaseLayerIndex = 0;
    [SerializeField] int m_UpperBodyLayerIndex = 0;
    [SerializeField] int m_FaceLayerIndex = 2;
    [SerializeField] string m_VoiceName = "Microsoft|David|Desktop";

    protected Animator animator;
    protected FacialAnimationPlayer m_FacialAnimator;
    protected HeadController m_HeadController;
    protected GazeController m_GazeController;
    protected SaccadeController m_SaccadeController;
    protected GestureMapDefinition m_GestureMap;
    protected LocomotionController m_LocoController;
    protected ILoadableAsset m_rideLoadableAsset;

    private bool m_assetInitialized = false;
    #endregion

    #region Properties
    public override string CharacterName
    {
        get { return name; }
    }

    public override AudioSource Voice
    {
        get { return GetComponentInChildren<AudioSource>(); }
    }

    public string VoiceName
    {
        get { return m_VoiceName; }
        set { m_VoiceName = value; }
    }

    public int BaseLayerIndex {  get { return m_BaseLayerIndex; } }
    public int UpperBodyLayerIndex { get { return m_UpperBodyLayerIndex; } }
    public int FaceLayerIndex { get { return m_FaceLayerIndex; } }
    string lastPosture { get; set; }
    #endregion

    #region Functions
    // Use this for initialization
    void Awake()
    {
        if(!TryGetComponent(out m_rideLoadableAsset))
            InitializeLoadedAsset();
    }

    public void InitializeLoadedAsset()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            Debug.LogError("MecanimCharacter " + name + " doesn't have an Animator component");
        else
        {
            // the animator needs this component in order to signal when it receives unity driver function messages
            // such as OnAvatarIK and OnStateIK
            AnimatorMessenger messenger = animator.GetComponent<AnimatorMessenger>();
            if (messenger == null)
                messenger = animator.gameObject.AddComponent<AnimatorMessenger>();
            messenger.SetMessengerTarget(this);

            if(TryGetComponent(out m_rideLoadableAsset))
            {
                animator.enabled = false;
                StartCoroutine(InitializeAnimator());
            }
        }
        m_FacialAnimator = GetComponent<FacialAnimationPlayer>();
        m_HeadController = GetComponent<HeadController>();
        m_GazeController = GetComponent<GazeController>();
        m_SaccadeController = GetComponent<SaccadeController>();
        m_GestureMap = GetComponent<GestureMapDefinition>();
        m_LocoController = GetComponent<LocomotionController>();

        if (!string.IsNullOrEmpty(m_StartingPosture))
            PlayPosture(m_StartingPosture);

        m_assetInitialized = true;
    }

    IEnumerator InitializeAnimator() //TODO: delegate to other component that listens for load
    {
        Animator childAnimator = GetChildAnimator();
        if (childAnimator != null && childAnimator.avatar != null && childAnimator.runtimeAnimatorController != null)
        {
            Debug.Log("MecanimCharacter InitializeAnimator(): Setting the avatar of the animator to that of the loaded art asset.");
            animator.avatar = childAnimator.avatar;
            animator.runtimeAnimatorController = childAnimator.runtimeAnimatorController;
        }
        else
            Debug.Log("MecanimCharacter InitializeAnimator(): No animator or avatar or runtime animator controller found on loaded art asset, using default animator avatar.");
        yield return new WaitForEndOfFrame();
        animator.enabled = true;
        animator.Rebind();
        animator.Update(0);
    }

    private Animator GetChildAnimator()
    {
        Animator childAnimator = null;
        Animator[] animators = GetComponentsInChildren<Animator>();
        foreach (Animator anim in animators)
            if (anim.gameObject != this.gameObject)
            {
                childAnimator = anim;
                break;
            }
        return childAnimator;
    }

    public string GetAnimation(GestureUtils.Lexeme lexeme, GestureUtils.Type type)
    {
        return GetAnimation(lexeme.ToString(), type.ToString());
    }

    public string GetAnimation(string lexeme, string type)
    {
        string animName = "";
        if (m_GestureMap != null)
        {
            animName = m_GestureMap.GetAnimation(lexeme, type);
        }
        else
        {
            Debug.LogErrorFormat("GetAnimation({0}, {1}) failed because character {2} has no gesture map", lexeme, type, name);
        }
        return animName;
    }

    public void SetFloatParam(string paramName, float paramData)
    {
        animator.SetFloat(paramName, paramData);
    }

    public void SetFloatParam(string paramName, float paramData, float blendInTime)
    {
        if (blendInTime == 0)
        {
            SetFloatParam(paramName, paramData);
        }
        else
        {
            StartCoroutine(SetFloatParamBlendIn_Internal(paramName, paramData, blendInTime));
        }
    }

    IEnumerator SetFloatParamBlendIn_Internal(string paramName, float paramData, float blendInTime)
    {
        blendInTime = Mathf.Abs(blendInTime);
        float timePassed = 0;
        float currVal = animator.GetFloat(paramName);
        while (timePassed <= blendInTime)
        {
            SetFloatParam(paramName, Mathf.Lerp(currVal, paramData, timePassed / blendInTime));
            yield return new WaitForEndOfFrame();
            timePassed += Time.deltaTime;
        }

        SetFloatParam(paramName, paramData);
    }

    public void SetBoolParam(string paramName, bool paramData)
    {
        animator.SetBool(paramName, paramData);
    }

    public void SetIntParam(string paramName, int paramData)
    {
        animator.SetInteger(paramName, paramData);
    }

    public void MoveTo(Vector3 destination)
    {
        if (m_LocoController != null)
        {
            m_LocoController.MoveTo(destination);
        }
    }

    IEnumerator DoMoveToPoint()
    {
        yield break;
    }

    public void PlayPosture(string postureName)
    {
        PlayPosture(postureName, 0, m_BaseLayerIndex);
    }

    public override void PlayPosture(string postureName, float startTime)
    {
        PlayPosture(postureName, startTime, m_BaseLayerIndex);
    }

    string GetCurrentPostureStateHash()
    {
        Debug.Log("animator.GetCurrentAnimatorClipInfoCount(0)  " + animator.GetCurrentAnimatorClipInfoCount(0) );
        AnimatorClipInfo[] info = animator.GetCurrentAnimatorClipInfo(0);
        if (info != null && info.Length > 0)
        {
            Debug.Log("THERE ARE CLIPS");
            
            return info[0].clip.name;
        }
        return lastPosture;
        //return animator.GetCurrentAnimatorStateInfo(m_BaseLayerIndex).fullPathHash;
    }

    public void PlayPosture(string postureName, float startTime, int layerIndex)
    {
        if (animator == null)
        {
            Debug.LogError("null animator: " + name);
        }
        lastPosture = postureName;// GetCurrentPostureStateHash();
        animator.CrossFadeInFixedTime(postureName, startTime, layerIndex);
    }

    void OnEnable()
    {
        if(m_assetInitialized)
            animator.Play(lastPosture, m_BaseLayerIndex);
        //Debug.Log("lastPosture: " + lastPosture);
    }

    void OnDisable()
    {
        //lastPosture = GetCurrentPostureStateHash();
        //Debug.Log("lastPosture: " + lastPosture);
    }

    public override void PlayAnim(string animName)
    {
        PlayAnim(animName, m_UpperBodyLayerIndex);
    }

    public void PlayAnim(string animName, int layer)
    {
        if (animator == null)
        {
            Debug.LogError("null animator: " + name);
        }

        //Debug.Log($"PlayAnim() - {animName}");

        animator.CrossFadeInFixedTime(animName, 0.5f, layer);
    }

    public override void PlayAnim(string animName, float readyTime, float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime)
    {
        PlayAnim(animName);
    }

    public void PlayAnim(string animName, float startDelay)
    {
        StartCoroutine(PlayAnimDelayed(startDelay, animName));
    }

    IEnumerator PlayAnimDelayed(float delay, string animName)
    {
        yield return new WaitForSeconds(delay);
        animator.CrossFadeInFixedTime(animName, 0.5f, m_UpperBodyLayerIndex);
    }

    /// <summary>
    /// Stops the current body animation from playing
    /// </summary>
    public void StopAnim()
    {
        animator.StopPlayback();
    }

    public override void PlayXml(string xml)
    {
        var bmlHandler = GetComponent<BMLEventHandler>();
        if (bmlHandler != null)
            bmlHandler.LoadXMLString(CharacterName, xml);
        else
            Debug.LogError($"PlayXml function failed on character {name}. Add BMLEventHandler to the gameobject.");
    }

    /// <summary>
    /// Entry point for parsing and playing a BML/XML string on this character.
    /// </summary>
    /// <param name="xml">The raw XML or BML string to parse and convert into CutsceneEvents.</param>
    /// <remarks>
    /// This method looks for a BMLEventHandler on the current GameObject and delegates the parsing logic to it.
    /// If the handler is not present, a runtime error is logged.
    ///
    /// Callstack for XML parsing and event creation:
    /// - MecanimCharacter.PlayXml()
    ///   - BMLEventHandler.SetBMLTimings()
    ///     - BMLParser.SetBMLTimings()
    ///   - BMLEventHandler.LoadXMLBMLStrings()
    ///     - BMLParser.LoadBMLString()
    ///       - BMLParser.FinishedReadingBML()
    ///         - BMLParser.OnFinishedReading()
    ///           - Cutscene.Play() - at this point, currently empty
    /// - MecanimCharacter.PlayXml() - second call
    ///   - BMLEventHandler.LoadXMLBMLStrings()
    ///     - BMLParser.LoadXMLString()
    ///       - BMLParser.ParseBMLEvents()
    ///         - BMLParser.CreateEvent()
    ///           - BMLParser.CreateNewEvent()
    ///             - BMLParser.OffsetStartTimeBySyncPoint()
    ///       - BMLParser.FinishedReadingXML()
    ///         - BMLParser.ResolvePendingSyncEvent()
    ///         - BMLEventHandler.OnFinishedReading()
    ///           - Cutscene.Play()
    /// - Cutscene.Play()
    ///   - Cutscene.Reset()
    ///     - Cutscene.SortEventsByTime()
    ///     - Cutscene.LoadEvents()
    ///   - Cutscene.StartPlaying()
    ///     - Cutscene.RunCutscene()
    ///       - CutsceneEvent.Fire()
    /// </remarks>
    public override void PlayXml(AudioSpeechFile xml)
    {
        // Set the BMLTimings before playing this xml to load in the timings for this utterance.
        // The BMLTimings are used in ParseEventStartTime() which is called within PlayXml()
        BMLEventHandler bmlHandler = GetComponent<BMLEventHandler>();
        bmlHandler.SetBMLTimings(xml.UtteranceTiming.m_Timings);

        //PlayXml(xml.ConvertedXml);
        bmlHandler.LoadXMLBMLStrings(CharacterName, xml);
    }

    public void PlayAU(int au, string side, float weight, float time)
    {
        //animator.SetFloat(au,
    }

    public override void PlayViseme(string viseme, float weight)
    {
        if (m_FacialAnimator != null)
            m_FacialAnimator.RampViseme(viseme, weight, 0f, 0f, 0f);  // Permanent set (until changed)
        else
            animator.SetFloat(viseme, weight);
    }

    public override void PlayViseme(string viseme, float weight, float totalTime, float blendTime)
    {
        if (m_FacialAnimator != null)
        {
            m_FacialAnimator.RampViseme(viseme, weight, 0, totalTime, blendTime);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a FacialAnimator component in order to PlayViseme", name);
        }
    }

    public void ResetViseme()
    {
        if (m_FacialAnimator != null)
        {
            m_FacialAnimator.ResetViseme();
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a FacialAnimator component in order to PlayViseme", name);
        }
    }

    public void SetVisemeWeightMultiplier(float multiplier)
    {
        if (m_FacialAnimator != null)
        {
            m_FacialAnimator.VisemeWeightMultiplier = multiplier;
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a FacialAnimator component in order to SetVisemeWeightMultiplier", name);
        }   
    }

    public override void PlayAudio(AudioSpeechFile speechFile)
    {
        // often times, the facial curves need to start before the audio starts playing
        // find the most negative curve start time and wait that long before playing the audio
        //float audioWaitTime = speechFile.UtteranceTiming.GetEarliestCurveTime();
        AudioSource src = Voice;
        if (src != null)
        {
            src.clip = speechFile.m_AudioClip;

            if (src.clip == null)
                Debug.LogErrorFormat("MecanimCharacter.PlayAudio() - clip is null");

            //src.PlayDelayed(Mathf.Abs(audioWaitTime));
            src.Play();
        }

        if (m_FacialAnimator != null)
        {
            m_FacialAnimator.Play(speechFile.UtteranceTiming);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a FacialAnimator component in order to animate the mouth while playing audio", name);
        }
    }

    /// <summary>
    /// Stops the current mouth animation performance and gracefully ramps visemes to 0
    /// </summary>
    public void StopLipSyncPerformance()
    {
        m_FacialAnimator.Stop();
    }

    public void StopAudio()
    {
        AudioSource src = Voice;
        if (src != null)
        {
            Debug.Log("Stop");
            src.Stop();
        }
    }

    public void PlayAudio(List<TtsReader.WordTiming> timings)
    {
        if (m_FacialAnimator != null)
        {
            m_FacialAnimator.Play(timings);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a FacialAnimator component in order PlayAudio", name);
        }
    }

    public void SetGazeTarget(GameObject gazeTarget)
    {
        if (gazeTarget != null)
        {
            if (m_GazeController != null)
            {
                m_GazeController.SetGazeTarget(gazeTarget);
            }
            else
            {
                Debug.LogErrorFormat("{0} requires a GazeController component in order SetGazeTarget", name);
            }
        }
    }

    GameObject FindGazeTarget(string gazeAt)
    {
        GameObject gazeTarget = GameObject.Find(gazeAt);
        if (gazeTarget == null)
        {
            Debug.LogError("Could not find gaze target " + gazeAt);
        }
        return gazeTarget;
    }
       

    public override void Gaze(string gazeAt)
    {
        GameObject gazeTarget = FindGazeTarget(gazeAt);
        if (gazeTarget != null)
        {
            SetGazeTarget(gazeTarget);
        }
    }

    public override void Gaze(string gazeAt, float headSpeed)
    {
        GameObject gazeTarget = FindGazeTarget(gazeAt);
        if (gazeTarget != null)
        {
            SetGazeTargetWithSpeed(gazeTarget, headSpeed, 0, 0);
        }
    }

    public override void Gaze(string gazeAt, float headSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange)
    {
        GameObject gazeTarget = FindGazeTarget(gazeAt);
        if (gazeTarget != null)
        {
            float bodySpeed = ((jointRange & CharacterDefines.GazeJointRange.CHEST) == CharacterDefines.GazeJointRange.CHEST) ? GazeController.DefaultBodyGazeSpeed : 0;
            SetGazeTargetWithSpeed(gazeTarget, headSpeed, eyeSpeed, bodySpeed);
        }
    }

    public override void Gaze(string gazeAt, string targetBone, CharacterDefines.GazeDirection gazeDirection, CharacterDefines.GazeJointRange jointRange,
        float angle, float headSpeed, float eyeSpeed, float fadeOut, string gazeHandleName, float duration)
    {
        Gaze(gazeAt, headSpeed, eyeSpeed, jointRange);
        if (duration > 0)
        {
            StopGazeLater(duration, fadeOut);
        }
    }

    public void StopGazeLater(float secondsToWait)
    {
        StartCoroutine(StopGazeLaterCR(secondsToWait, GazeController.DefaultFadeOutTime));
    }

    public void StopGazeLater(float secondsToWait, float fadeOutTime)
    {
        StartCoroutine(StopGazeLaterCR(secondsToWait, fadeOutTime));
    }
        
    IEnumerator StopGazeLaterCR(float secondsToWait, float fadeOutTime)
    {
        yield return new WaitForSeconds(secondsToWait);
        StopGaze(fadeOutTime);
    }

    public void SetGazeTargetWithSpeed(GameObject gazeTarget, float headSpeed, float eyesSpeed, float bodySpeed)
    {
        if (m_GazeController != null && m_assetInitialized)
        {
            m_GazeController.SetGazeTargetWithSpeed(gazeTarget, headSpeed, eyesSpeed, bodySpeed);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a GazeController component in order SetGazeTargetWithSpeed", name);
        }
    }

    public void SetGazeTargetWithTime(GameObject gazeTarget, float headFadeInTime, float eyesFadeInTime, float bodyFadeInTime)
    {
        if (m_GazeController != null)
        {
            m_GazeController.SetGazeTargetWithDuration(gazeTarget, headFadeInTime, eyesFadeInTime, bodyFadeInTime);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a GazeController component in order SetGazeTargetWithTime", name);
        }
    }

    public void SetGazeWeights(float head, float eyes, float body)
    {
        if (m_GazeController != null)
        {
            m_GazeController.HeadGazeWeight = head;
            m_GazeController.EyeGazeWeight = eyes;
            m_GazeController.BodyGazeWeight = body;
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a GazeController component in order SetGazeWeights", name);
        }
    }

    public override void StopGaze()
    {
        StopGaze(GazeController.DefaultFadeOutTime);
    }

    public override void StopGaze(float fadeoutTime)
    {
        if (m_GazeController != null)
        {
            m_GazeController.StopGaze(fadeoutTime);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a GazeController component in order StopGaze", name);
        } 
    }

    public void UpdateGaze()
    {
        if (m_GazeController != null)
        {
            m_GazeController.UpdateGaze();
        } 
    }

    public override void Nod(float amount, float numTimes, float duration)
    {
        //Debug.Log($"Nod() - {amount} - {numTimes} - {duration}");

        if (m_HeadController != null)
        {
            m_HeadController.NodHead(amount, numTimes, duration);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a HeadController component in order to Nod", name);
        }
    }

    public override void Shake(float amount, float numTimes, float duration)
    {
        if (m_HeadController != null)
        {
            m_HeadController.ShakeHead(amount, numTimes, duration);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a HeadController component in order to Shake", name);
        } 
    }

    public void Tilt(float amount, float numTimes, float duration)
    {
        if (m_HeadController != null)
        {
            m_HeadController.TiltHead(amount, numTimes, duration);
        }
        else
        {
            Debug.LogErrorFormat("{0} requires a HeadController component in order to Tilt", name);
        }
    }

    /// <summary>
    /// Stops all head movements from continuing and gracefully returns the neck to its original orientation
    /// </summary>
    public void StopHeadMovements()
    {
        m_HeadController.Stop();
    }

    public override void Saccade(CharacterDefines.SaccadeType type, bool finish, float duration)
    {
        SetSaccadeBehaviour(type);
    }

    public override void StopSaccade()
    {
        m_SaccadeController.SetBehaviourMode(CharacterDefines.SaccadeType.End);
    }

    public override void Saccade(CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude)
    {
        SetSaccadeBehaviour(type);
        m_SaccadeController.Perform(direction, magnitude, duration);
    }

    public void Saccade(float direction, float magnitude, float duration)
    {
        m_SaccadeController.Perform(direction, magnitude, duration);
    }

    public void SetSaccadeBehaviour(CharacterDefines.SaccadeType mode)
    {
        m_SaccadeController.SetBehaviourMode(mode);
    }

    public override void Transform(float x, float y, float z)
    {
        transform.position = new Vector3(x, y, z);
    }

    public override void Transform(float y, float p)
    {
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
        Vector3 currRot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(currRot.x, p, currRot.z);
    }

    public override void Transform(float x, float y, float z, float h, float p, float r)
    {
        Transform(x, y, z);
        transform.rotation = Quaternion.Euler(p, h, r);
    }

    public override void Transform(Transform trans)
    {
        transform.SetPositionAndRotation(trans.position, trans.rotation);
    }

    public override void Transform(Vector3 pos, Quaternion rot)
    {
        transform.SetPositionAndRotation(pos, rot);
    }

    public override void Rotate(float h)
    {
        Vector3 currRot = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(currRot.x, h, currRot.z);
    }
    #endregion
}
}
