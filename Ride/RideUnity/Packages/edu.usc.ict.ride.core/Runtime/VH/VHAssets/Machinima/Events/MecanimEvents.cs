using System;
using System.IO;
using System.Xml;
using UnityEngine;

namespace VHAssets
{
public class MecanimEvents : GenericEvents
{
    #region Functions
    public override string GetEventType() { return GenericEventNames.Mecanim; }
    #endregion

    #region Events
    public class MecanimEvent_Base : ICutsceneEventInterface
    {
        #region Functions
        protected string GetObjectName(CutsceneEvent ce, string objectParamName)
        {
            CutsceneEventParam param = ce.FindParameter(objectParamName);
            UnityEngine.Object o = param.objData;
            return o != null ? o.name : param.stringData;
        }

        public static UnityEngine.Object FindObject(string assetPath, Type assetType, string fileExtension)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            UnityEngine.Object retVal = null;
#if UNITY_EDITOR
            retVal = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, assetType);
            if (retVal == null)
            {
                // try a project search, this is slow but doesn't require a media path
                //Debug.Log(string.Format("looking for: ", ));
                string dir = string.Format("{0}/{1}", Application.dataPath, assetPath);
                if (Directory.Exists(dir) || assetType == typeof(AudioClip))
                {
                    string[] files = VHFile.DirectoryWrapper.GetFiles(string.Format("{0}", Application.dataPath), assetPath + fileExtension, SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        files[0] = files[0].Replace("\\", "/"); // unity doesn't like backslashes in the asset path
                        files[0] = files[0].Replace(Application.dataPath, "");
                        files[0] = files[0].Insert(0, "Assets");
                        retVal = UnityEditor.AssetDatabase.LoadAssetAtPath(files[0], assetType);
                    }
                }
            }

            // if it's still null, it wasn't found at all
            if (retVal == null)
            {
                Debug.LogError(string.Format("Couldn't load {0} {1}", assetType, assetPath));
            }
#endif
            return retVal;
        }

        protected string CleanupAnimationName(string animName)
        {
            if (string.IsNullOrEmpty(animName))
            {
                return string.Empty;
            }

            string output = animName;
            int index = animName.IndexOf("@");
            if (index != -1)
            {
                output = animName.Substring(index + 1);
            }

            output = Path.GetFileNameWithoutExtension(output);
            return output;
        }

        #endregion
    }

    public class MecanimEvent_PlayAudio : MecanimEvent_Base
    {
        #region Functions

        public void PlayAudio(string character, AudioClip uttID)
        {
            MecanimManager.Get().SBPlayAudio(character, uttID);
        }

        public void PlayAudio(string character, AudioClip uttID, string text)
        {
            MecanimManager.Get().SBPlayAudio(character, uttID, text);
        }

        public void PlayAudio(string character, string uttID)
        {
            MecanimManager.Get().SBPlayAudio(character, uttID);
        }

        public void PlayAudio(string character, string uttID, string text)
        {
            MecanimManager.Get().SBPlayAudio(character, uttID, text);
        }

        public override string GetLengthParameterName() { return "uttID"; }

        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            if ((ce.FunctionOverloadIndex == 0 || ce.FunctionOverloadIndex == 1) && !IsParamNull(ce, 1))
            {
                length = Cast<AudioClip>(ce, 1).length;
            }
            return length;
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.FindParameter("uttID").stringData = ce.Name = reader["ref"];

            /*GameObject go = GameObject.Find(reader["ref"]);
            AudioSpeechFile speechFile = go.GetComponent<AudioSpeechFile>();//MecanimManager.Get().GetSpeechFile(reader["ref"]);
            if (speechFile != null)
            {
                ce.FindParameter("uttID").objData = speechFile.m_AudioClip;
            }
            if (speechFile == null)
            {
                Debug.LogError("Couldn't find audio clip: " + reader["ref"]);
            }*/

            ce.FindParameter("uttID").stringData = reader["ref"];

            /*if (ce.FindParameter("text") != null)
            {
                ce.FindParameter("text").stringData = "";// reader.ReadString(); // TODO: Figure out a way to parse this
            }*/
        }
        #endregion
    }


    public class MecanimEvent_Transform : MecanimEvent_Base
    {
        #region Functions
        public void Transform(string character, float x, float y, float z)
        {
            MecanimManager.Get().SBTransform(character, x, y, z);
        }

        public void Transform(string character, string transform)
        {
            GameObject transformGo = GameObject.Find(transform);
            if (transformGo != null)
            {
                Transform trans = transformGo.transform;
                Vector3 euler = trans.eulerAngles;
                MecanimManager.Get().SBTransform(character, trans.position.x, trans.position.y, trans.position.z, euler.x, euler.y, euler.z);
            }
            else
            {
                Debug.LogError("Couldn't find gameobject named " + transform);
            }
        }

        /*public override object SaveRewindData(CutsceneEvent ce)
        {
            if (Cast<ICharacter>(ce, 0) != null)
            {
                return SaveTransformHierarchy(Cast<ICharacter>(ce, 0).transform);
            }
            else
            {
                return null;
            }
        }

        public override void LoadRewindData(CutsceneEvent ce, object rData)
        {
            if (rData != null)
            {
                Transform rewindData = (Transform)rData;
                Transform characterData = null;
                ICharacter character = null;
                if (IsParamNull(ce, 0))
                {
                    character = FindCharacter(Param(ce, 0).stringData, ce.Name);
                    characterData = character.transform;
                }
                else
                {
                    character = Cast<ICharacter>(ce, 0);
                    characterData = character.transform;
                }

                characterData.position = rewindData.position;
                characterData.rotation = rewindData.rotation;
                SmartbodyManager.Get().QueueCharacterToUpload(character);
            }
        }*/
        #endregion
    }

    public class MecanimEvent_Posture : MecanimEvent_Base
    {
        #region Functions
        public void Posture(string character, AnimationClip motion)
        {
            MecanimManager.Get().SBPosture(character, character + "@" + motion.name, 0);
        }

        public void Posture(string character, string motion)
        {
            MecanimManager.Get().SBPosture(character, motion, 0);
        }

        public override string GetLengthParameterName() { return "motion"; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            AnimationClip motion = Cast<AnimationClip>(ce, 1);
            if (motion != null)
            {
                length = motion.length;
            }
            return length;
        }

        /*public override string GetXMLString(CutsceneEvent ce)
        {
            StringBuilder builder = new StringBuilder(string.Format(@"<body character=""{0}"" mm:ypos=""{1}"" mm:eventName=""{2}"" mm:overload=""{3}"" start=""{4}"" mm:length=""{5}""  />",
                GetObjectName(ce, "character"), ce.GuiPosition.y, ce.Name, ce.FunctionOverloadIndex, ce.StartTime, ce.Length));

            if (ce.FunctionOverloadIndex == 0)
            {
                AppendParam<SmartbodyMotion>(builder, ce, "posture", "motion");
            }
            else
            {
                AppendParam<string>(builder, ce, "posture", "motion");
            }

            //AppendParam<float>(builder, ce, "start", "startTime");
            return builder.ToString();
        }*/

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }

            ce.FindParameter("character").SetObjData(VHUtils.FindCharacter(reader["character"], ce.Name));
            ce.FindParameter("motion").stringData = CleanupAnimationName(reader["posture"]);

            if (!string.IsNullOrEmpty(reader["mm:length"]))
            {
                ce.Length = ParseFloat(reader["mm:length"], ref ce.Length);
            }
        }
        #endregion
    }

    public class MecanimEvent_PlayAnim : MecanimEvent_Base
    {
        #region Functions
        public void PlayAnim(string character, string motion)
        {
            MecanimManager.Get().SBPlayAnim(character, motion);
        }

        public void PlayAnim(string character, AnimationClip motion)
        {
            MecanimManager.Get().SBPlayAnim(character, motion.name);
        }

        public override string GetLengthParameterName() { return "motion"; }
        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            AnimationClip motion = Cast<AnimationClip>(ce, 1);
            if (motion != null)
            {
                length = motion.length;
            }
            return length;
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }

            ce.FindParameter("character").stringData = reader["character"];

            // TODO: our state names in the animation controller don't have the character name prefix, so we strip it if it's there.
            // this needs to be re-evaluated to see if there is a better way
            ce.Name = ce.FindParameter("motion").stringData = CleanupAnimationName(reader["name"]);
            //ce.Name = ce.FindParameter("motion").stringData = reader["name"];
            if (string.IsNullOrEmpty(ce.Name))
            {
                ce.Name = "No Animation Name";
            }


            if (!string.IsNullOrEmpty(reader["mm:length"]))
            {
                ce.Length = ParseFloat(reader["mm:length"], ref ce.Length);
            }
        }
        #endregion
    }

    public class MecanimEvent_Gesture : MecanimEvent_Base
    {
        #region Functions
        public void Gesture(string character, string lexeme, string type)
        {
            string animName = MecanimManager.Get().GetAnimation(character, lexeme, type);
            if (!string.IsNullOrEmpty(animName))
            {
                MecanimManager.Get().SBPlayAnim(character, animName);
            }
        }

        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }


        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else
            {
                ce.Name = reader["lexeme"] + " " + reader["type"];
            }

            ce.FindParameter("character").stringData = reader["character"];
            ce.FindParameter("lexeme").stringData = reader["lexeme"];
            ce.FindParameter("type").stringData = reader["type"];

            if (!string.IsNullOrEmpty(reader["mm:length"]))
            {
                ce.Length = ParseFloat(reader["mm:length"], ref ce.Length);
            }
        }
        #endregion
    }

    public class MecanimEvent_PlayFAC : MecanimEvent_Base
    {
        #region Functions
        public void PlayFAC(string character, int au, CharacterDefines.FaceSide side, float weight, float duration)
        {
            MecanimManager.Get().SBPlayFAC(character, au, side, weight, duration);
        }

        /*public void PlayFAC(string character, int au, SmartbodyManager.FaceSide side, float weight, float duration, float readyTime, float relaxTime)
        {
            MecanimManager.Get().SBPlayFAC(character, au, side, weight, duration, readyTime, relaxTime);
        }*/

        public override string GetLengthParameterName() { return "duration"; }

        /*public override string GetXMLString(CutsceneEvent ce)
        {
            float readyTime = 0, relaxTime = 0;
            if (ce.FunctionOverloadIndex == 0  || ce.FunctionOverloadIndex == 2)
            {
                return string.Format(@"<face type=""FACS"" mm:eventName=""{1}"" au=""{0}"" side=""{2}"" start=""{3}"" end=""{4}"" amount=""{5}"" mm:ypos=""{6}"" character=""{7}"" mm:overload=""{8}"" />",
                    ce.FindParameter("au").intData, ce.Name, ce.FindParameter("side").enumDataString, ce.StartTime, ce.EndTime, ce.FindParameter("weight").floatData,
                    ce.GuiPosition.y, GetObjectName(ce, "character"), ce.FunctionOverloadIndex);
            }
            else
            {
                readyTime = ce.FindParameter("readyTime").floatData;
                relaxTime = ce.FindParameter("relaxTime").floatData;

                return string.Format(@"<face type=""FACS"" mm:eventName=""{1}"" au=""{0}"" side=""{2}"" start=""{3}"" ready=""{4}"" relax=""{5}"" end=""{6}"" amount=""{7}"" mm:ypos=""{8}"" character=""{9}"" mm:overload=""{10}"" />",
                    ce.FindParameter("au").intData, ce.Name, ce.FindParameter("side").enumDataString, ce.StartTime, ce.StartTime + readyTime, ce.StartTime + relaxTime, ce.EndTime, ce.FindParameter("weight").floatData,
                    ce.GuiPosition.y, GetObjectName(ce, "character"), ce.FunctionOverloadIndex);
            }
        }*/

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.FindParameter("au").intData = int.Parse(reader["au"]);
            int au = ce.FindParameter("au").intData;

            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);


            /*ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            if (ce.FindParameter("character").objData == null)
            {
                ce.FindParameter("character").stringData = reader["character"];
            }*/

            ce.FindParameter("character").stringData = reader["character"];

            ce.FindParameter("weight").floatData = ParseFloat(reader["amount"], ref ce.FindParameter("weight").floatData);

            if (!string.IsNullOrEmpty(reader["side"]))
            {
                ce.FindParameter("side").SetEnumData((CharacterDefines.FaceSide)Enum.Parse(typeof(CharacterDefines.FaceSide), reader["side"], true));
            }

            if (!string.IsNullOrEmpty(reader["duration"]))
            {
                ce.FindParameter("duration").floatData = ce.Length = ParseFloat(reader["duration"], ref ce.FindParameter("duration").floatData);
            }
            else
            {
                float endTime = 1;
                endTime = ParseFloat(reader["end"], ref endTime);
                ce.FindParameter("duration").floatData = ce.Length = endTime - ce.StartTime;
            }

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else if (!string.IsNullOrEmpty(reader["id"]))
            {
                Debug.Log("FAC NAME: " + reader["id"]);
                ce.Name = reader["id"];
            }
            else
            {
                if (CharacterDefines.AUToFacialLookUp.ContainsKey(au))
                {
                    ce.Name = string.Format("FAC {0}", CharacterDefines.AUToFacialLookUp[au]);
                }
                else
                {
                    ce.Name = "FAC";
                }
            }
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "au")
            {
                param.intData = 26; // jaw
            }
            else if (param.Name == "weight")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "side")
            {
                param.SetEnumData((CharacterDefines.FaceSide)Enum.Parse(typeof(CharacterDefines.FaceSide), CharacterDefines.FaceSide.both.ToString()));
            }
        }
        #endregion
    }

    public class SmartbBodyEvent_PlayViseme : MecanimEvent_Base
    {
        #region Functions
        public void PlayViseme(string character, string viseme, float weight, float duration, float blendTime)
        {
            MecanimManager.Get().SBPlayViseme(character, viseme, weight, duration, blendTime);
        }

        public override string GetLengthParameterName() { return "duration"; }

        /*public override string GetXMLString(CutsceneEvent ce)
        {
            string character = GetObjectName(ce, "character");
            string viseme = ce.FindParameter("viseme").stringData;
            float weight = ce.FindParameter("weight").floatData;
            float blendTime = ce.FindParameter("blendTime").floatData;
            float duration = ce.FindParameter("duration").floatData;

            string messageStart = string.Format(@"scene.command('char {0} viseme {1} {2} {3}')", character, viseme, weight, blendTime);
            string messageStop = string.Format(@"scene.command('char {0} viseme {1} {2} {3}')", character, viseme, 0, blendTime);
            return string.Format(@"<event message=""{0}"" start=""{1}"" mm:ypos=""{2}"" mm:eventName=""{3}"" mm:messageType=""visemeStart"" character=""{4}"" viseme=""{5}"" weight=""{6}"" blendTime=""{7}"" duration=""{8}"" mm:overload=""{9}"" />",
                messageStart, ce.StartTime, ce.GuiPosition.y, ce.Name, character, viseme, weight, blendTime, duration, ce.FunctionOverloadIndex)
                 + string.Format(@"<event message=""{0}"" start=""{1}"" mm:ypos=""{2}"" mm:eventName=""{3}"" mm:messageType=""visemeStop""  character=""{4}"" viseme=""{5}"" weight=""{6}"" blendTime=""{7}"" duration=""{8}"" mm:overload=""{9}"" />",
                 messageStop, ce.StartTime + (duration - blendTime), ce.GuiPosition.y, ce.Name, character, viseme, weight, blendTime, duration, ce.FunctionOverloadIndex);
        }*/

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            float startTime;
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                if (float.TryParse(reader["start"], out startTime))
                {
                    ce.StartTime = startTime;
                }
            }
            else if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                if (float.TryParse(reader["stroke"], out startTime))
                {
                    ce.StartTime = startTime;
                }
            }

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else
            {
                ce.Name = reader["viseme"];
            }

            /*if (ce.FunctionOverloadIndex == 0)
            {
                ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            }
            else*/
            {
                ce.FindParameter("character").stringData = reader["character"];
            }

            ce.FindParameter("viseme").stringData = reader["viseme"];
            ce.FindParameter("weight").floatData = ParseFloat(reader["weight"], ref ce.FindParameter("weight").floatData);
            ce.FindParameter("blendTime").floatData = ParseFloat(reader["blendTime"], ref ce.FindParameter("blendTime").floatData);
            ce.Length = ce.FindParameter("duration").floatData = ParseFloat(reader["duration"], ref ce.FindParameter("duration").floatData);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "viseme")
            {
                param.stringData = "open";
            }
            else if (param.Name == "weight")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "duration")
            {
                param.floatData = 2.0f;
            }
            else if (param.Name == "blendTime")
            {
                param.floatData = 1.0f;
            }
        }
        #endregion
    }

    public class MecanimEvent_HeadMovement : MecanimEvent_Base
    {
        /*public override string GetXMLString(CutsceneEvent ce)
        {
            return string.Format(@"<head start=""{0}"" type=""{1}"" repeats=""{2}"" amount=""{3}"" mm:track=""{4}"" mm:ypos=""{5}"" mm:eventName=""{6}"" end=""{7}"" character=""{8}"" mm:overload=""{9}""/>",
                    ce.StartTime, "NOD", ce.FindParameter("repeats").floatData, ce.FindParameter("amount").floatData, "NOD", ce.GuiPosition.y, ce.Name,
                    ce.FindParameter("time").floatData + ce.StartTime, GetObjectName(ce, "character"), ce.FunctionOverloadIndex);
        }*/

        public override string GetLengthParameterName() { return "time"; }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.FindParameter("repeats").floatData = ParseFloat(reader["repeats"], ref ce.FindParameter("repeats").floatData);
            ce.FindParameter("amount").floatData = ParseFloat(reader["amount"], ref ce.FindParameter("amount").floatData);
            //ce.EventData.NodVelocity = ParseFloat(reader["velocity"]);
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);
            }

            ce.FindParameter("character").stringData = reader["character"];

            if (!string.IsNullOrEmpty(reader["duration"]))
            {
                float dur =  0;
                ce.FindParameter("time").floatData = ce.Length = (ParseFloat(reader["duration"], ref dur) - ce.StartTime);
            }
            else if (!string.IsNullOrEmpty(reader["end"]))
            {
                float endTime = 1;
                endTime = ParseFloat(reader["end"], ref endTime);
                if (endTime < 0)
                {
                    endTime = ce.StartTime + 1;
                }
                ce.FindParameter("time").floatData = ce.Length = endTime - ce.StartTime;
            }
            else
            {
                ce.FindParameter("time").floatData = ce.Length = 1;
            }

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else if (string.IsNullOrEmpty(ce.Name))
            {
                ce.Name = string.Format("Head " + reader["type"]);
            }
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "amount")
            {
                param.floatData = 1;
            }
            else if (param.Name == "repeats")
            {
                param.floatData = 2.0f;
            }
            else if (param.Name == "time")
            {
                param.floatData = 1.0f;
            }
        }
    }

    public class MecanimEvent_Nod : MecanimEvent_HeadMovement
    {
        #region Functions
        public void Nod(string character, float amount, float repeats, float time)
        {
            MecanimManager.Get().SBNod(character, amount, repeats, time);
        }
        #endregion
    }

    public class MecanimEvent_Shake : MecanimEvent_HeadMovement
    {
        #region Functions
        public void Shake(string character, float amount, float repeats, float time)
        {
            MecanimManager.Get().SBShake(character, amount, repeats, time);
        }
        #endregion
    }

    public class MecanimEvent_Tilt : MecanimEvent_HeadMovement
    {
        #region Functions
        public void Tilt(string character, float amount, float repeats, float time)
        {
            MecanimManager.Get().Tilt(character, amount, repeats, time);
        }
        #endregion
    }

    public class MecanimEvent_Gaze : MecanimEvent_Base
    {
        #region Functions
        public void Gaze(string character, string gazeAt, float headSpeed, float eyeSpeed, float bodySpeed)
        {
            MecanimManager.Get().SBGaze(character, gazeAt, headSpeed, eyeSpeed, CharacterDefines.GazeJointRange.HEAD_EYES);
        }

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            ce.FindParameter("character").stringData = reader["character"];

            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);
            string targetName = reader["target"];

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }

            // TODO: parse joint-speed param

            ce.FindParameter("gazeAt").stringData = targetName;
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "headSpeed")
            {
                param.floatData = GazeController.DefaultHeadGazeSpeed;
            }
            else if (param.Name == "eyesSpeed")
            {
                param.floatData = GazeController.DefaultEyeGazeSpeed;
            }
            else if (param.Name == "bodySpeed")
            {
                param.floatData = GazeController.DefaultBodyGazeSpeed;
            }
        }

        #endregion
    }

    public class MecanimEvent_GazeTime : MecanimEvent_Base
    {
        #region Functions
        public void GazeTime(string character, string gazeAt, float headFadeInTime, float eyesFadeInTime, float bodyFadeInTime)
        {
            //MecanimManager.Get().SBGaze(character, gazeAt, headFadeInTime);
        }

        public override string GetLengthParameterName() { return "headFadeInTime"; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            return Mathf.Max(Param(ce, 2).floatData, Param(ce, 3).floatData, Param(ce, 4).floatData);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "headFadeInTime")
            {
                param.floatData = 1;
            }
            else if (param.Name == "eyesFadeInTime")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "bodyFadeInTime")
            {
                param.floatData = 1.0f;
            }
        }

        #endregion
    }

    public class MecanimEvent_SetGazeWeight : MecanimEvent_Base
    {
        #region Functions
        public void SetGazeWeight(string character, float head, float eyes, float body)
        {
            MecanimManager.Get().SetGazeWeights(character, head, eyes, body);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "head")
            {
                param.floatData = 1;
            }
            else if (param.Name == "eyes")
            {
                param.floatData = 1;
            }
            else if (param.Name == "body")
            {
                param.floatData = 1;
            }
        }

        #endregion
    }


    public class MecanimEvent_GazeAdvanced : MecanimEvent_Base
    {
        #region Functions
        public void GazeAdvanced(string character, string gazeTarget, string targetBone, CharacterDefines.GazeDirection gazeDirection,
            CharacterDefines.GazeJointRange jointRange, float angle, float headSpeed, float eyeSpeed, float fadeOut, float duration)
        {
            if (character == null || gazeTarget == null)
            {
                return;
            }

            MecanimManager.Get().SBGaze(character, gazeTarget, targetBone, gazeDirection, jointRange, angle, headSpeed, eyeSpeed, fadeOut, "", duration);
        }


        public override string GetLengthParameterName() { return "duration"; }

        public override float CalculateEventLength (CutsceneEvent ce)
        {
            float len = 1;
            if (ce.DoesParameterExist("duration") && ce.FindParameter("duration").floatData > 0)
            {
                len = ce.FindParameter("duration").floatData + ce.FindParameter("fadeOut").floatData;
            }
            return len;
        }

        /*public override string GetXMLString(CutsceneEvent ce)
        {
            string targetBone = ce.FindParameter("targetBone").stringData;
            string jointRangeString = ce.FindParameter("jointRange").enumDataString;
            if (!string.IsNullOrEmpty(jointRangeString))
            {
                jointRangeString = jointRangeString.Replace("_", " ");
            }

            return string.Format(@"<gaze character=""{0}"" mm:eventName=""{1}"" target=""{2}"" angle=""{3}"" start=""{4}"" duration=""{5}"" headspeed=""{6}"" eyespeed=""{7}"" fadeout=""{8}"" sbm:joint-range=""{9}"" sbm:joint-speed=""{6} {7}"" mm:track=""{10}"" mm:ypos=""{11}"" direction=""{12}"" sbm:handle=""{13}"" targetBone=""{14}"" mm:advanced=""true""  mm:overload=""{15}""/>",
                    GetObjectName(ce, "character"), ce.Name, GazeTargetName(ce), ce.FindParameter("angle").floatData,
                    ce.StartTime, ce.Length, ce.FindParameter("headSpeed").floatData, ce.FindParameter("eyeSpeed").floatData, ce.FindParameter("fadeOut").floatData,
                    jointRangeString, "GAZE", ce.GuiPosition.y, ce.FindParameter("gazeDirection").enumDataString, "", targetBone, ce.FunctionOverloadIndex);
        }*/

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(reader["sbm:joint-range"]))
            {
                //ce.FindParameter("jointRange").SetEnumData((SmartbodyManager.GazeJointRange)Enum.Parse(typeof(SmartbodyManager.GazeJointRange), reader["sbm:joint-range"].ToString().Replace(" ", "_"), true));
                ce.FindParameter("jointRange").SetEnumData(CharacterDefines.ParseGazeJointRange(reader["sbm:joint-range"]));
            }

            if (!string.IsNullOrEmpty(reader["direction"]))
            {
                string direction = reader["direction"];
                if (reader["direction"].IndexOf(' ') != -1)
                {
                    string[] split = direction.Split(' ');
                    direction = split[0];
                }
                ce.FindParameter("gazeDirection").SetEnumData((CharacterDefines.GazeDirection)Enum.Parse(typeof(CharacterDefines.GazeDirection), direction, true));
            }

            ce.FindParameter("character").stringData = reader["character"];
            ce.FindParameter("angle").floatData = ParseFloat(reader["angle"], ref ce.FindParameter("angle").floatData);
            ce.FindParameter("headSpeed").floatData = ParseFloat(reader["headspeed"], ref ce.FindParameter("headSpeed").floatData);
            ce.FindParameter("eyeSpeed").floatData = ParseFloat(reader["eyespeed"], ref ce.FindParameter("eyeSpeed").floatData);
            ce.FindParameter("fadeOut").floatData = ParseFloat(reader["fadeout"], ref ce.FindParameter("fadeOut").floatData);
            //ce.FindParameter("gazeHandleName").stringData = reader["sbm:handle"];
            ce.FindParameter("targetBone").stringData = reader["targetBone"];
            ce.StartTime = ParseFloat(reader["start"], ref ce.StartTime);
            //ce.FindParameter("duration").floatData = ce.Length = ParseFloat(reader["duration"]);
            if (ce.Length == 0)
            {
                ce.Length = 1;
            }

            string targetName = reader["target"];
            int colonIndex = targetName.IndexOf(":");
            if (colonIndex != -1)
            {
                // there's a specific bone that needs to be looked at
                targetName = targetName.Remove(colonIndex);
            }

            ce.FindParameter("gazeTarget").stringData = targetName;
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            // Match defaults in GazeController
            if (param.Name == "eyeSpeed")
            {
                param.floatData = 100;
            }
            else if (param.Name == "headSpeed")
            {
                param.floatData = 90;
            }
            else if (param.Name == "bodySpeed")
            {
                param.floatData = 80;
            }
            else if (param.Name == "jointRange")
            {
                param.SetEnumData(CharacterDefines.GazeJointRange.EYES_NECK);
            }
            else if (param.Name == "targetBone")
            {
                param.SetEnumData(CharacterDefines.GazeTargetBone.NONE);
            }
            else if (param.Name == "gazeDirection")
            {
                param.SetEnumData(CharacterDefines.GazeDirection.NONE);
            }
            else if (param.Name == "duration")
            {
                param.floatData = 0;
            }
            else if (param.Name == "fadeOut")
            {
                param.floatData = 0.25f;
            }
        }
        #endregion
    }

    public class MecanimEvent_StopGaze : MecanimEvent_Base
    {
        #region Constants
        const float DefaultStopGazeTime = 1;
        #endregion

        #region Functions
        public void StopGaze(string character)
        {
            MecanimManager.Get().SBStopGaze(character, 1);
        }

        public void StopGaze(string character, float fadeOut)
        {
            MecanimManager.Get().SBStopGaze(character, fadeOut);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "fadeOut")
            {
                param.floatData = 1;
            }
        }

        /*public override string GetXMLString(CutsceneEvent ce)
        {
            float fadeOut = 1;
            if (ce.FunctionOverloadIndex == 1)
            {
                fadeOut = ce.FindParameter("fadeOut").floatData;
            }
            return string.Format(@"<event message=""sb scene.command('char {0} gazefade out {1}')"" character=""{0}"" stroke=""{2}"" mm:eventName=""{3}"" mm:ypos=""{4}""  mm:overload=""{5}""/>",
              GetObjectName(ce, "character"), fadeOut, ce.StartTime, ce.Name, ce.GuiPosition.y, ce.FunctionOverloadIndex);
        }*/

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                float.TryParse(reader["start"], out ce.StartTime);
            }
            else if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                float.TryParse(reader["stroke"], out ce.StartTime);
            }

            if (ce.FunctionOverloadIndex == 1)
            {
                ce.FindParameter("fadeOut").floatData = ParseFloat(reader["fadeOut"], ref ce.FindParameter("fadeOut").floatData );
            }


            ce.FindParameter("character").stringData = reader["character"];

            ce.Name = reader["mm:eventName"];
            if (string.IsNullOrEmpty(ce.Name))
            {
                ce.Name = "Stop Gaze";
            }
        }

        #endregion
    }

    public class MecanimEvent_StopSaccade : MecanimEvent_Base
    {
        #region Functions
        public void StopSaccade(string character)
        {
            MecanimManager.Get().SBStopSaccade(character);
        }

        /*public override string GetXMLString(CutsceneEvent ce)
        {
            return string.Format(@"<event message=""sbm bml char {0} &lt;saccade finish=&quot;true&quot; /&gt;"" stroke=""{1}"" mm:ypos=""{2}"" character=""{0}"" mm:stopSaccade=""true"" mm:eventName=""{3}"" />",
                    GetObjectName(ce, "character"), ce.StartTime, ce.GuiPosition.y, ce.Name);
        }*/

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                float.TryParse(reader["stroke"], out ce.StartTime);
            }

            ce.FindParameter("character").stringData = reader["character"];

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else
            {
                ce.Name = "Stop Saccade";
            }

        }
        #endregion
    }

    public class MecanimEvent_Saccade : MecanimEvent_Base
    {
        #region Functions
        public void Saccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration)
        {
            MecanimManager.Get().SBSaccade(character, type, finish, duration);
        }

        public void Saccade(string character, CharacterDefines.SaccadeType type, bool finish, float duration, float angleLimit, float direction, float magnitude)
        {
            MecanimManager.Get().SBSaccade(character, type, finish, duration, angleLimit, direction, magnitude);
        }

        public override string GetLengthParameterName() { return "duration"; }

        /*public override string GetXMLString(CutsceneEvent ce)
        {
            return string.Format(@"<event message=""sbm bml char {0} &lt;saccade mode=&quot;{1}&quot; /&gt;"" stroke=""{3}"" type=""{1}"" mm:track=""{4}"" mm:ypos=""{5}"" character=""{0}"" mm:eventName=""{6}""/>",
                    GetObjectName(ce, "character"), ce.FindParameter("type").enumDataString.ToLower(), 0, ce.StartTime, "Saccade", ce.GuiPosition.y, ce.Name);
        }*/

        public override void SetParameters(CutsceneEvent ce, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(reader["start"]))
            {
                float.TryParse(reader["start"], out ce.StartTime);
            }
            else if (!string.IsNullOrEmpty(reader["stroke"]))
            {
                float.TryParse(reader["stroke"], out ce.StartTime);
            }

            //ce.FindParameter("character").SetObjData(FindCharacter(reader["character"], ce.Name));
            ce.FindParameter("character").stringData = reader["character"];

            ce.FindParameter("type").SetEnumData((CharacterDefines.SaccadeType)Enum.Parse(typeof(CharacterDefines.SaccadeType), reader["type"], true));
            if (!string.IsNullOrEmpty(reader["duration"]))
            {
                ce.FindParameter("duration").floatData = ParseFloat(reader["duration"], ref ce.FindParameter("duration").floatData);
            }

            if (!string.IsNullOrEmpty(reader["mm:eventName"]))
            {
                ce.Name = reader["mm:eventName"];
            }
            else
            {
                ce.Name = "Saccade " + reader["type"];
            }
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "duration")
            {
                param.floatData = 1;
            }
        }
        #endregion
    }

    public class MecanimEvent_Express : MecanimEvent_Base
    {
        #region Functions
        public void Express(string character, AudioClip uttID, string uttNum, string text)
        {
            MecanimManager.Get().SBExpress(character, uttID.name, uttNum, text);
        }

        public void Express(string character, string uttID, string uttNum, string text)
        {
            MecanimManager.Get().SBExpress(character, uttID, uttNum, text);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "target")
            {
                param.stringData = "user";
            }
        }

        public override string GetLengthParameterName() { return "uttID"; }
        public override bool NeedsToBeFired (CutsceneEvent ce) { return false; }

        public override float CalculateEventLength(CutsceneEvent ce)
        {
            float length = -1;
            if ((ce.FunctionOverloadIndex == 0) && !IsParamNull(ce, 1))
            {
                length = Cast<AudioClip>(ce, 1).length;
            }
            return length;
        }
        #endregion
    }

    public class MecanimEvent_SetFloat : MecanimEvent_Base
    {
        #region Functions
        public void SetFloat(string character, string paramName, float paramData)
        {
            MecanimManager.Get().SetCharacterFloatParam(character, paramName, paramData);
        }

        public void SetFloat(string character, string paramName, float paramData, float blendInTime)
        {
            MecanimManager.Get().SetCharacterFloatParam(character, paramName, paramData, blendInTime);
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "blendInTime")
            {
                param.floatData = 0.5f;
            }
        }

        /*public override string GetLengthParameterName() { return "duration"; }

        public override float CalculateEventLength (CutsceneEvent ce)
        {
            return ce.FunctionOverloadIndex == 0 ? -1 : ce.FindParameter("duration").floatData;
        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "viseme")
            {
                param.stringData = "open";
            }
            else if (param.Name == "weight")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "duration")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "blendTime")
            {
                param.floatData = 0f;
            }
        }*/
        #endregion
    }

    public class MecanimEvent_SetBool : MecanimEvent_Base
    {
        #region Functions
        public void SetBool(string character, string paramName, bool paramData)
        {
            MecanimManager.Get().SetCharacterBoolParam(character, paramName, paramData);
        }
        #endregion
    }

    public class MecanimEvent_SetInt : MecanimEvent_Base
    {
        #region Functions
        public void SetInt(string character, string paramName, int paramData)
        {
            MecanimManager.Get().SetCharacterIntParam(character, paramName, paramData);
        }
        #endregion
    }

    public class MecanimEvent_SetSaccadeBehaviour : MecanimEvent_Base
    {
        #region Functions
        public void SetSaccadeBehaviour(string character, CharacterDefines.SaccadeType behaviour)
        {
            MecanimManager.Get().SetSaccadeBehaviour(character, behaviour);
        }
        #endregion
    }

    public class MecanimEvent_PlaySaccade : MecanimEvent_Base
    {
        #region Functions
        public void PlaySaccade(string character, float direction, float magnitude, float duration)
        {
            MecanimManager.Get().PlaySaccade(character, direction, magnitude, duration);
        }

        public override string GetLengthParameterName() { return "duration"; }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "direction")
            {
                param.floatData = 90;
            }
            if (param.Name == "magnitude")
            {
                param.floatData = 8;
            }
            if (param.Name == "duration")
            {
                param.floatData = 1;
            }
        }
        #endregion
    }

    public class MecanimEvent_SetFaceWeightMultiplier : MecanimEvent_Base
    {
        #region Functions
        public void SetVisemeWeightMultiplier(string character, float startWeight, float endWeight)
        {
            if (Application.isPlaying)
            {
                MecanimManager.Get().SetVisemeWeightMultiplier(character, Mathf.Lerp(startWeight, endWeight, m_InterpolationTime));
            }

        }

        public override void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
        {
            if (param.Name == "startWeight")
            {
                param.floatData = 1.0f;
            }
            else if (param.Name == "endWeight")
            {
                param.floatData = 2.0f;
            }
        }

        public override bool IsFireAndForget ()
        {
            return false;
        }
        #endregion
    }
    #endregion
}
}
