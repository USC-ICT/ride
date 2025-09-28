using UnityEngine;

namespace VHAssets
{
public abstract class ICharacter : MonoBehaviour
{
    #region Variables

    #endregion

    #region Properties
    public abstract string CharacterName { get; }
    public abstract AudioSource Voice { get ; }
    #endregion

    #region Functions
    void Start()
    {
    }

    //public abstract void MoveCharacter(string direction, float fSpeed, float fLrps, float fFadeOutTime);
    //public abstract void WalkTo(string waypoint, bool isRunning);
    //public abstract void WalkImmediate(string locomotionPrefix, double velocity, double turn, double strafe);
    public abstract void PlayAudio(AudioSpeechFile audioId);
    public abstract void PlayXml(string xml);
    public abstract void PlayXml(AudioSpeechFile xml);
    public abstract void Transform(Transform trans);
    public abstract void Transform(Vector3 pos, Quaternion rot);
    public abstract void Transform(float y, float p);
    public abstract void Transform(float x, float y, float z);
    public abstract void Transform(float x, float y, float z, float h, float p, float r);
    public abstract void Rotate(float h);
    /// <summary>
    /// startTime should be a normalized value (0-1)
    /// </summary>
    /// <param name="posture"></param>
    /// <param name="startTime"></param>
    public abstract void PlayPosture(string posture, float startTime);
    public abstract void PlayAnim(string animName);
    public abstract void PlayAnim(string animName, float readyTime, float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime);
    //public abstract void PlayFAC(int au, SmartbodyManager.FaceSide side, float weight, float time);
    public abstract void PlayViseme(string viseme, float weight);
    public abstract void PlayViseme(string viseme, float weight, float totalTime, float blendTime);
    public abstract void Nod(float amount, float repeats, float time);
    public abstract void Shake(float amount, float repeats, float time);
    public abstract void Gaze(string gazeAt);
    public abstract void Gaze(string gazeAt, float headSpeed);
    public abstract void Gaze(string gazeAt, float headSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange);
    public abstract void Gaze(string gazeAt, string targetBone, CharacterDefines.GazeDirection gazeDirection,
        CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut, string gazeHandleName, float duration);
    public abstract void StopGaze();
    public abstract void StopGaze(float fadoutTime);
    public abstract void Saccade(CharacterDefines.SaccadeType type, bool finish, float duration);
    public abstract void Saccade(CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude);
    public abstract void StopSaccade();
    /*public abstract void StateChange(string state, string mode, string wrapMode, string scheduleMode);
    public abstract void StateChange(string state, string mode, string wrapMode, string scheduleMode, float x);
    public abstract void StateChange(string state, string mode, string wrapMode, string scheduleMode, float x, float y, float z);
    public abstract string GetCurrentStateName();
    public abstract Vector3 GetCurrentStateParams();
    public abstract bool IsStateScheduled(string stateName);
    public abstract float GetAuValue(string auName);
    public abstract void Express(string uttID, string uttNum, string text);
    public abstract void Express(string uttID, string uttNum, string text, string target);*/
    #endregion
}
}
