using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride;

namespace VHAssets
{
public class MecanimManager : ICharacterController
{
    #region Constants
    public static readonly Dictionary<string, string[]> VisemeMap = new()
    {
        { "Face_neutral", new string[] { "Face_neutral" } },
        { "FV" ,          new string[] { "FV" } },
        { "open" ,        new string[] { "open" } },
        { "PBM" ,         new string[] { "PBM" } },
        { "ShCh" ,        new string[] { "ShCh" } },
        { "tBack" ,       new string[] { "tBack" } },
        { "tRoof" ,       new string[] { "tRoof" } },
        { "tTeeth" ,      new string[] { "tTeeth" } },
        { "W" ,           new string[] { "W" } },
        { "wide",         new string[] { "wide" } },

        { "au_1",         new string[] { "001_inner_brow_raiser_lf", "001_inner_brow_raiser_rt" } },
        { "au_2",         new string[] { "002_outer_brow_raiser_lf", "002_outer_brow_raiser_rt" } },
        { "au_4",         new string[] { "004_brow_lowerer_lf", "004_brow_lowerer_rt" } },
        { "au_5",         new string[] { "005_upper_lid_raiser" } },
        { "au_6",         new string[] { "006_cheek_raiser" } },
        { "au_7",         new string[] { "007_lid_tightener" } },
        { "au_10",        new string[] { "010_upper_lip_raiser" } },
        { "au_12",        new string[] { "012_lip_corner_puller_lf", "012_lip_corner_puller_rt" } },
        { "au_14",        new string[] { "014_smile_lf", "014_smile_rt" } },
        { "au_25",        new string[] { "025_lips_part" } },
        { "au_26",        new string[] { "026_jaw_drop" } },
        { "au_45",        new string[] { "045_blink_lf", "045_blink_rt" } },
        { "au_100",       new string[] { "100_small_smile" } },
        { "au_112",       new string[] { "112_happy" } },
        { "au_124",       new string[] { "124_disgust" } },
        { "au_126",       new string[] { "126_fear" } },
        { "au_127",       new string[] { "127_surprise" } },
        { "au_129",       new string[] { "129_angry" } },
        { "au_130",       new string[] { "130_sad" } },
        { "au_131",       new string[] { "131_contempt" } },
        { "au_132",       new string[] { "132_browraise1" } },
        { "au_133",       new string[] { "133_browraise2" } },
        { "au_134",       new string[] { "134_hurt_brows" } },
        { "au_136",       new string[] { "136_furrow" } },
    };
    #endregion

    #region Fields
    private bool m_includeInactiveInCache = true;
    private readonly Dictionary<string, MecanimCharacter> m_characterByNameCache = new(StringComparer.Ordinal);
    private bool m_characterCacheDirty = true;

    private AudioSpeechFile[] m_speechFiles;
    private static MecanimManager g_instance;
    #endregion

    #region Properties
    public int NumCharacters { get { EnsureCharacterCache(); return m_characterByNameCache.Count; } }
    #endregion

    #region Functions
    public static MecanimManager Get()
    {
        //Debug.Log("SmartbodyManager.Get()");

        if (g_instance == null)
            g_instance = FindAnyObjectByType<MecanimManager>();

        return g_instance;
    }

    void Awake()
    {
        m_characterCacheDirty = true;

        m_speechFiles = RideUtils.FindObjectsByType<AudioSpeechFile>();
    }

    public override string GetCharacterControllerType() => "MecanimManager";

    public void FindAudioFiles()
    {
        m_speechFiles = RideUtils.FindObjectsByType<AudioSpeechFile>();
    }

    public void AddCharacter(MecanimCharacter mecAnimCharacter)
    {
        if (mecAnimCharacter == null)
            return;

        m_characterCacheDirty = true;  // Mark dirty so we re-scan and resolve duplicates consistently.
    }

    public void RemoveCharacter(MecanimCharacter mecAnimCharacter)
    {
        if (mecAnimCharacter == null)
            return;

        m_characterCacheDirty = true;  // Mark dirty so we re-scan and resolve duplicates consistently.
    }

    public void RemoveCharacter(string character)
    {
        var ch = GetCharacterByName(character);
        if (ch != null)
            RemoveCharacter(ch);
        else
            Debug.LogError($"Can't find mecanim character {character}");
    }

    public void RemoveAllCharacters()
    {
        m_characterByNameCache.Clear();
        m_characterCacheDirty = true;
    }

    public MecanimCharacter GetCharacterByName(string character)
    {
        if (string.IsNullOrEmpty(character))
            return null;

        EnsureCharacterCache();

        if (TryGetCharacterFromCache(character, out var ch))
            return ch;

        // Defensive fallback: cache could be stale due to runtime spawn/destroy/disable
        // without a scene event firing. Rebuild once and try again.
        m_characterCacheDirty = true;
        EnsureCharacterCache();

        if (TryGetCharacterFromCache(character, out ch))
            return ch;

        Debug.LogWarning($"[MecanimManager] Can't find character {character} in ICharacterController {name}");
        return null;
    }

    private GameObject GetPawn(string pawnName)
    {
        var pawn = GameObject.Find(pawnName);
        if (pawn == null)
            Debug.LogWarning($"{name} can't find pawn with name: {pawnName}");

        return pawn;
    }

    private AudioSpeechFile GetSpeechFile(string fileName)
    {
        if (fileName == "realtime_stream_silent")
            return null;

        if (fileName == "unused")
            fileName = "UnnamedAudioClip";

        var speechFile = Array.Find(m_speechFiles, s => s.name == fileName);
        if (speechFile == null)
            Debug.LogWarning($"Can't find AudioSpeechFile: {fileName}");

        return speechFile;
    }

    public static string[] GetAnimatorParametersForViseme(string viseme)
    {
        var parameters = Array.Empty<string>();
        if (VisemeMap.ContainsKey(viseme))
            parameters = VisemeMap[viseme];

        return parameters;
    }

    public AudioSource GetCharacterVoice(string character)
    {
        AudioSource src = null;
        var ch = GetCharacterByName(character);
        if (ch != null)
            src = ch.Voice;

        return src;
    }

    private void PlayAudioClip(string character, AudioClip clip)
    {
        var src = GetCharacterVoice(character);
        if (src != null)
        {
            src.clip = clip;
            src.Play();
        }
    }

    public string GetAnimation(string character, GestureUtils.Lexeme lexeme, GestureUtils.Type type) =>
        GetCharacterByName(character).GetAnimation(lexeme, type);

    public string GetAnimation(string character, string lexeme, string type) =>
        GetCharacterByName(character).GetAnimation(lexeme, type);


    public void SetCharacterFloatParam(string character, string paramName, float paramData) =>
        GetCharacterByName(character).SetFloatParam(paramName, paramData);

    public void SetCharacterFloatParam(string character, string paramName, float paramData, float blendInTime) =>
        GetCharacterByName(character).SetFloatParam(paramName, paramData, blendInTime);

    public void SetCharacterBoolParam(string character, string paramName, bool paramData) =>
        GetCharacterByName(character).SetBoolParam(paramName, paramData);

    public void SetCharacterIntParam(string character, string paramName, int paramData) =>
        GetCharacterByName(character).SetIntParam(paramName, paramData);
    #endregion

    #region ICharacterController Functions
    public override void SBRunPythonScript(string script)
    {
        /*string command = string.Format(@"scene.run('{0}')", script);
        PythonCommand(command);*/
    }

    public override void SBMoveCharacter(string character, string direction, float fSpeed, float fLrps, float fFadeOutTime)
    {
        /*string command = string.Format(@"scene.command('sbm test loco char {0} {1} spd {2} rps {3} time {4}')", character, direction, fSpeed, fLrps, fFadeOutTime);
        PythonCommand(command);*/
    }

    public override void SBWalkTo(string character, string waypoint, bool isRunning)
    {
        /*string run = isRunning ? @"manner=""run""" : "";
        string message = string.Format(@"bml.execBML('{0}', '<locomotion target=""{1}"" facing=""{2}"" {3} />')", character, waypoint, waypoint, run);
        PythonCommand(message);*/
    }

    public override void MoveTo(string character, Transform destination) =>
        GetCharacterByName(character).MoveTo(destination.position);

    public override void SBWalkImmediate(string character, string locomotionPrefix, double velocity, double turn, double strafe)
    {
        //<sbm:states mode="schedule" loop="true" name="allLocomotion" x="100" y ="0" z="0"/>
        /*string message = string.Format(@"bml.execBML('{0}', '<sbm:states mode=""schedule"" loop=""true"" sbm:startnow=""true"" name=""{1}"" x=""{2}"" y =""{3}"" z=""{4}"" />')", character, locomotionPrefix, velocity, turn, strafe);
        PythonCommand(message);*/
    }

    public override void SBPlayAudio(string character, string audioId)
    {
        var speechFile = GetSpeechFile(audioId);
        if (speechFile != null)
            GetCharacterByName(character).PlayAudio(speechFile);
    }

    public override void SBPlayAudio(string character, string audioId, string text)
    {
        var speechFile = GetSpeechFile(audioId);
        if (speechFile != null)
            GetCharacterByName(character).PlayAudio(speechFile);
    }

    public override void SBPlayAudio(string character, AudioClip audioId)
    {
        var speechFile = GetSpeechFile(audioId.name);
        if (speechFile != null)
            GetCharacterByName(character).PlayAudio(speechFile);
    }

    public override void SBPlayAudio(string character, AudioClip audioId, string text)
    {
        var speechFile = GetSpeechFile(audioId.name);
        if (speechFile != null)
            GetCharacterByName(character).PlayAudio(speechFile);
    }

    public override void SBPlayAudio(string character, AudioSpeechFile audioId) => GetCharacterByName(character).PlayAudio(audioId);
    public override void SBPlayXml(string character, string xml) => GetCharacterByName(character).PlayXml(xml);
    public override void SBPlayXml(string character, AudioSpeechFile xml) => GetCharacterByName(character).PlayXml(xml.ConvertedXml);

    public override void SBTransform(string character, Transform transform) => SBTransform(character, transform.position, transform.rotation);
    public override void SBTransform(string character, Vector3 pos, Quaternion rot) => GetCharacterByName(character).transform.SetLocalPositionAndRotation(pos, rot);
    public override void SBTransform(string character, double y, double p) { }
    public override void SBTransform(string character, double x, double y, double z) => GetCharacterByName(character).transform.position = new Vector3((float)x, (float)y, (float)z);
    public override void SBTransform(string character, double x, double y, double z, double h, double p, double r) => SBTransform(character, new Vector3((float)x, (float)y, (float)z), Quaternion.Euler(new Vector3((float)p, (float)h, (float)r)));
    public override void SBRotate(string character, double h) => GetCharacterByName(character).transform.Rotate(0, (float)h, 0);

    public override void SBPosture(string character, string posture, float startTime) => GetCharacterByName(character).PlayPosture(posture);

    public override void SBPlayAnim(string character, string animName)
    {
        /*string message = string.Format(@"bml.execBML('{0}', '<animation name=""{1}""/>')", character, animName);
        PythonCommand(message);*/
        GetCharacterByName(character).PlayAnim(animName);
    }

    public override void SBPlayAnim(string character, string animName, float readyTime, float strokeStartTime, float emphasisTime, float strokeTime, float relaxTime)
    {
        SBPlayAnim(character, animName);
    }

    public override void SBPlayFAC(string character, int au, CharacterDefines.FaceSide side, float weight, float time)
    {
        string fac = $"au_{au}";
        if (VisemeMap.ContainsKey(fac))
        {
            var facNames = VisemeMap[fac];
            ParseFacLeftRightNames(facNames, out string leftName, out string rightName);

            if ((side == CharacterDefines.FaceSide.left || side == CharacterDefines.FaceSide.both) && !string.IsNullOrEmpty(leftName))
                GetCharacterByName(character).PlayViseme(leftName, weight, time, 0.25f); // smartbody default

            if ((side == CharacterDefines.FaceSide.right || side == CharacterDefines.FaceSide.both) && !string.IsNullOrEmpty(rightName))
                GetCharacterByName(character).PlayViseme(rightName, weight, time, 0.25f); // smartbody default
        }
        else
        {
            Debug.LogError($"au {au} doesn't exist in the VisemeMap");
        }
    }

    private void ParseFacLeftRightNames(string[] facNames, out string left, out string right)
    {
        left = Array.Find(facNames, s => s.Contains("_lf"));
        if (string.IsNullOrEmpty(left))
            left = facNames[0];

        right = Array.Find(facNames, s => s.Contains("_rt"));
        if (string.IsNullOrEmpty(right))
            right = facNames[0];
    }

    public override void SBPlayViseme(string character, string viseme, float weight) => GetCharacterByName(character).PlayViseme(viseme, weight);
    public override void SBPlayViseme(string character, string viseme, float weight, float totalTime, float blendTime) => GetCharacterByName(character).PlayViseme(viseme, weight, totalTime, blendTime);

    public void SetVisemeWeightMultiplier(string character, float multiplier)
    {
        var ch = GetCharacterByName(character);
        if (ch != null)
            ch.SetVisemeWeightMultiplier(multiplier);
    }

    public void SetExpressionWeightMultiplier(string character, float multiplier)
    {
        var ch = GetCharacterByName(character);
        if (ch != null)
            ch.SetExpressionWeightMultiplier(multiplier);
    }

    public override void SBNod(string character, float amount, float repeats, float time) => GetCharacterByName(character).Nod(amount, repeats, time);
    public override void SBShake(string character, float amount, float repeats, float time) => GetCharacterByName(character).Shake(amount, repeats, time);
    public override void SBTilt(string character, float amount, float repeats, float time) => Tilt(character, amount, repeats, time);
    public void Tilt(string character, float amount, float repeats, float time) => GetCharacterByName(character).Tilt(amount, repeats, time);

    public override void SBGaze(string character, string gazeAt)
    {
        var pawn = GetPawn(gazeAt);
        if (pawn != null)
            GetCharacterByName(character).SetGazeTarget(pawn);
    }

    public override void SBGaze(string character, string gazeAt, float neckSpeed)
    {
        var pawn = GetPawn(gazeAt);
        if (pawn != null)
            GetCharacterByName(character).Gaze(gazeAt, neckSpeed);
    }

    public override void SBGaze(string character, string gazeAt, float neckSpeed, float eyeSpeed, CharacterDefines.GazeJointRange jointRange)
    {
        var pawn = GetPawn(gazeAt);
        if (pawn != null)
            GetCharacterByName(character).Gaze(gazeAt, neckSpeed, eyeSpeed, jointRange);
    }

    public override void SBGaze(string character, string gazeAt, string targetBone, CharacterDefines.GazeDirection gazeDirection,
        CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut, string gazeHandleName, float duration)
    {
        var pawn = GetPawn(gazeAt);
        if (pawn != null)
        {
            var c = GetCharacterByName(character);
            c.Gaze(gazeAt, headSpeed, eyeSpeed, jointRange);
            if (duration > 0)
                c.StopGazeLater(duration, fadeOut);
        }
    }

    public void GazeSpeed(string character, string gazeAt, float headSpeed, float eyesSpeed, float bodySpeed)
    {
        var pawn = GetPawn(gazeAt);
        if (pawn != null)
            GetCharacterByName(character).SetGazeTargetWithSpeed(pawn, headSpeed, eyesSpeed, bodySpeed);
    }

    public void GazeTime(string character, string gazeAt, float headFadeInTime, float eyesFadeInTime, float bodyFadeInTime)
    {
        var pawn = GetPawn(gazeAt);
        if (pawn != null)
            GetCharacterByName(character).SetGazeTargetWithTime(pawn, headFadeInTime, eyesFadeInTime, bodyFadeInTime);
    }

    public void SetGazeWeights(string character, float head, float eyes, float body) => GetCharacterByName(character).SetGazeWeights(head, eyes, body);

    public override void SBStopGaze(string character) => GetCharacterByName(character).StopGaze();
    public override void SBStopGaze(string character, float fadoutTime) => GetCharacterByName(character).StopGaze(fadoutTime);

    public override void SBSaccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration) =>
        GetCharacterByName(character).SetSaccadeBehaviour(type);

    public override void SBSaccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude) =>
        GetCharacterByName(character).Saccade(direction, magnitude, duration);

    public override void SBStopSaccade(string character) => GetCharacterByName(character).StopSaccade();

    public void SetSaccadeBehaviour(string character, CharacterDefines.SaccadeType behaviour) => GetCharacterByName(character).SetSaccadeBehaviour(behaviour);

    public void PlaySaccade(string character, float direction, float magnitude, float duration) => GetCharacterByName(character).Saccade(direction, magnitude, duration);

    public override void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode)
    {
        /*string message = string.Format(@"bml.execBML('{0}', '<sbm:states name=""{1}"" mode=""{2}"" sbm:wrap-mode=""{3}"" sbm:schedule-mode=""{4}""/>')", character, state, mode, wrapMode, scheduleMode);
        PythonCommand(message);*/
    }

    public override void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode, float x)
    {
        /*string message = string.Format(@"bml.execBML('{0}', '<sbm:states name=""{1}"" mode=""{2}"" sbm:wrap-mode=""{3}"" sbm:schedule-mode=""{4}"" x=""{5}""/>')", character, state, mode, wrapMode, scheduleMode, x.ToString());
        PythonCommand(message);*/
    }

    public override void SBStateChange(string character, string state, string mode, string wrapMode, string scheduleMode, float x, float y, float z)
    {
        /*string message = string.Format(@"bml.execBML('{0}', '<sbm:states name=""{1}"" mode=""{2}"" sbm:wrap-mode=""{3}"" sbm:schedule-mode=""{4}"" x=""{5}"" y=""{6}"" z=""{7}""/>')", character, state, mode, wrapMode, scheduleMode, x.ToString(), y.ToString(), z.ToString());
        PythonCommand(message);*/
    }

    public override string SBGetCurrentStateName(string character)
    {
        /*string pyCmd = string.Format(@"scene.getStateManager().getCurrentState('{0}')", character);
        return PythonCommand<string>(pyCmd);*/
        return string.Empty;
    }

    public override Vector3 SBGetCurrentStateParams(string character)
    {
        /*Vector3 ret = new Vector3();
        string pyCmd = string.Empty;

        pyCmd = string.Format(@"scene.getStateManager().getCurrentStateParameters('{0}').getData(0)", character);
        ret.x = PythonCommand<float>(pyCmd);

        pyCmd = string.Format(@"scene.getStateManager().getCurrentStateParameters('{0}').getData(1)", character);
        ret.y = PythonCommand<float>(pyCmd);

        pyCmd = string.Format(@"scene.getStateManager().getCurrentStateParameters('{0}').getData(2)", character);
        ret.z = PythonCommand<float>(pyCmd);

        return ret;*/
        return Vector3.zero;
    }

    public override bool SBIsStateScheduled(string character, string stateName)
    {
        /*string pyCmd = string.Format(@"scene.getStateManager().isStateScheduled('{0}', '{1}')", character, stateName);
        return PythonCommand<bool>(pyCmd);*/
        return false;
    }

    public override float SBGetAuValue(string character, string auName)
    {
        /*string pyCmd = string.Format(@"scene.getCharacter('{0}').getSkeleton().getJointByName('{1}').getPosition().getData(0)", character, auName);
        return PythonCommand<float>(pyCmd);*/
        return 0;
    }

    public override void SBExpress(string character, string uttID, string uttNum, string text) => SBExpress(character, uttID, uttNum, text, "user");

    public override void SBExpress(string character, string uttID, string uttNum, string text, string target)
    {
        /*string message = string.Format("vrExpress {0} user 1303332588320-{2}-1 <?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>"
            + "<act><participant id=\"{0}\" role=\"actor\" /><fml><turn start=\"take\" end=\"give\" /><affect type=\"neutral\" "
            + "target=\"addressee\"></affect><culture type=\"neutral\"></culture><personality type=\"neutral\"></personality></fml>"
            + "<bml><speech id=\"sp1\" ref=\"{1}\" type=\"application/ssml+xml\">{3}</speech></bml></act>", character, uttID, uttNum, text);
        PythonCommand(message);*/
    }

    public override void SBGesture(string character, string gestureName) => SBPlayAnim(character, gestureName);

    public override void SBGesture(string character, string lexeme, string lexemeType, GestureUtils.Handedness hand, GestureUtils.Style style, GestureUtils.Emotion emotion,
        string target, bool additive, string jointRange, float perlinFrequency, float perlinScale, float readyTime, float strokeStartTime,
        float emphasisTime, float strokeTime, float relaxTime)
    {
    }

    public override ICharacter[] GetControlledCharacters()
    {
        EnsureCharacterCache();

        // Only return non-null + active, consistent with lookup rules.
        var list = new List<ICharacter>(m_characterByNameCache.Count);
        foreach (var kvp in m_characterByNameCache)
        {
            var ch = kvp.Value;
            if (ch != null && ch.gameObject.activeSelf)
                list.Add(ch);
        }

        return list.ToArray();
    }

    public override ICharacter GetCharacter(string character) => GetCharacterByName(character);

    private void EnsureCharacterCache()
    {
        if (!m_characterCacheDirty)
            return;

        RebuildCharacterCache();
    }

    private void RebuildCharacterCache()
    {
        m_characterByNameCache.Clear();

        FindObjectsInactive inactiveMode = m_includeInactiveInCache ? FindObjectsInactive.Include : FindObjectsInactive.Exclude;
        var mecanimCharacters = RideUtils.FindObjectsByType<MecanimCharacter>(inactiveMode);

        foreach (var character in mecanimCharacters)
        {
            if (character == null)
                continue;

            var name = character.CharacterName;
            if (string.IsNullOrEmpty(name))
                continue;

            // If duplicates exist, prefer the first active one and warn.
            if (!m_characterByNameCache.ContainsKey(name))
            {
                m_characterByNameCache.Add(name, character);
            }
            else
            {
                var existing = m_characterByNameCache[name];
                if (existing == null || (!existing.gameObject.activeSelf && character.gameObject.activeSelf))
                    m_characterByNameCache[name] = character;

                Debug.LogWarning($"[MecanimManager] Duplicate CharacterName '{name}' detected. Using '{m_characterByNameCache[name].name}'.");
            }
        }

        m_characterCacheDirty = false;
    }

    private bool TryGetCharacterFromCache(string character, out MecanimCharacter ch)
    {
        ch = null;

        if (!m_characterByNameCache.TryGetValue(character, out var candidate))
            return false;

        if (candidate == null)
        {
            m_characterCacheDirty = true;  // Destroyed object; mark dirty so the next query rebuilds.
            return false;
        }

        // only activeSelf counts.
        if (!candidate.gameObject.activeSelf)
            return false;

        ch = candidate;
        return true;
    }
    #endregion
}
}
