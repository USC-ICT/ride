using UnityEngine;

namespace VHAssets
{
public abstract class ICharacterController : MonoBehaviour
{
    public abstract string GetCharacterControllerType();
    public abstract void SBRunPythonScript(string script);
    public abstract void SBMoveCharacter(string character, string direction, float fSpeed, float fLrps, float fFadeOutTime);
    public abstract void SBWalkTo(string character, string waypoint, bool isRunning);
    public abstract void MoveTo(string character, Transform destination);
    public abstract void SBWalkImmediate(string character, string locomotionPrefix, double velocity, double turn, double strafe);
    public abstract void SBPlayAudio(string character, string audioId);
    public abstract void SBPlayAudio(string character, string audioId, string text);
    public abstract void SBPlayAudio(string character, AudioClip audioId);
    public abstract void SBPlayAudio(string character, AudioClip audioId, string text);
    public abstract void SBPlayAudio(string character, AudioSpeechFile audioId);
    public abstract void SBPlayXml(string character, string xml);
    public abstract void SBPlayXml(string character, AudioSpeechFile xml);
    public abstract void SBTransform(string character, Transform transform);
    public abstract void SBTransform(string character, Vector3 pos, Quaternion rot);
    public abstract void SBTransform(string character, double y, double p);
    public abstract void SBTransform(string character, double x, double y, double z);
    public abstract void SBTransform(string character, double x, double y, double z, double h, double p, double r);
    public abstract void SBRotate(string character, double h);
    public abstract void SBPosture(string character, string posture, float startTime);
    public abstract void SBPlayAnim(string character, string animName);
    public abstract void SBPlayAnim(string character, string animName, float readyTime, float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime);
    public abstract void SBPlayFAC(string character, int au, CharacterDefines.FaceSide side, float weight, float time);
    public abstract void SBPlayViseme(string character, string viseme, float weight);
    public abstract void SBPlayViseme(string character, string viseme, float weight, float totalTime, float blendTime);
    public abstract void SBNod(string character, float amount, float repeats, float time);
    public abstract void SBShake(string character, float amount, float repeats, float time);
    public abstract void SBTilt(string character, float amount, float repeats, float time);
    public abstract void SBGaze(string character, string gazeAt);
    public abstract void SBGaze(string character, string gazeAt, float neckSpeed);
    public abstract void SBGaze(string character, string gazeAt, float neckSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange);
    public abstract void SBGaze(string character, string gazeAt, string targetBone, CharacterDefines.GazeDirection gazeDirection,
        CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut, string gazeHandleName, float duration);
    public abstract void SBStopGaze(string character);
    public abstract void SBStopGaze(string character, float fadoutTime);
    public abstract void SBSaccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration);
    public abstract void SBSaccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude);
    public abstract void SBStopSaccade(string character);
    public abstract void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode);
    public abstract void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode, float x);
    public abstract void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode, float x, float y, float z);
    public abstract string SBGetCurrentStateName(string character);
    public abstract Vector3 SBGetCurrentStateParams(string character);
    public abstract bool SBIsStateScheduled(string character, string stateName);
    public abstract float SBGetAuValue(string character, string auName);
    public abstract void SBExpress(string character, string uttID, string uttNum, string text);
    public abstract void SBExpress(string character, string uttID, string uttNum, string text, string target);
    public abstract void SBGesture(string character, string gestureName);
    public abstract void SBGesture(string character, string lexeme, string lexemeType, GestureUtils.Handedness hand, GestureUtils.Style style, GestureUtils.Emotion emotion,
        string target, bool additive, string jointRange, float perlinFrequency, float perlinScale, float readyTime, float strokeStartTime,
        float emphasisTime, float strokeTime, float relaxTime);
    public abstract ICharacter [] GetControlledCharacters();
    public abstract ICharacter GetCharacter(string character);
}
}
