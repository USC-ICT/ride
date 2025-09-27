using System;
using System.Collections;
using System.Collections.Generic;

namespace VHAssets
{
public class CharacterDefines
{
    #region Constants
    public enum FaceSide
    {
        both,
        left,
        right,
    }

    public enum GazeTargetBone
    {
        NONE,
        BASE,
        EYEBALL_LEFT,
        EYEBALL_RIGHT,
        SKULL_BASE,
        SPINE1,
        SPINE2,
        SPINE3,
        SPINE4,
        SPINE5,
        r_wrist
    }

    public enum GazeDirection
    {
        NONE,
        UP,
        DOWN,
        LEFT,
        RIGHT,
        UPLEFT,
        UPRIGHT,
        DOWNLEFT,
        DOWNRIGHT,
        POLAR
    }

    public enum GazeJointRange
    {
        EYES,
        NECK,
        CHEST,
        BACK,
        HEAD_EYES,
        EYES_NECK,
        EYES_CHEST,
        EYES_BACK,
        NECK_CHEST,
        NECK_BACK,
        CHEST_BACK,
    }

    public enum SaccadeType
    {
        Listen,
        Talk,
        Think,
        End,
        Default,
    }

    public enum HeadMovementType
    {
        Nod,
        Shake,
        Tilt,
    }

    public static readonly Dictionary<int, string> AUToFacialLookUp = new Dictionary<int, string>()
    {
        { 1, "InnerBrowRaiser"},
        { 2, "OuterBrowRaiser"},
        { 4, "InnerBrowLowerer"},
        { 5, "UpperLidRaiser"},
        { 6, "EyeSquint"},
        { 7, "LidTightener"},
        { 9, "NoseWrinkle"},
        { 10, "UpperLipRaiser"},
        { 12, "SmileMouth"},
        { 14, "Smile"},
        { 15, "LipCornerDepressor"},
        { 23, "LipTightener"},
        { 25, "LipParser"},
        { 38, "NostrilDilator"},
        { 39, "NostrilCompressor"},
        { 45, "Blink"}
    };

    public static readonly string [] SyncPointNames =
    {
        "emphasis",
        "ready",
        "relax",
        "strokeStart",
        "stroke",
    };


    #endregion

    #region Functions
    public static GazeJointRange ParseGazeJointRange(string value)
    {
        string temp = value.Replace(" ", "_");
        string [] enumValues = Enum.GetNames(typeof(CharacterDefines.GazeJointRange));

        // first attemp to match the string to the enum
        for (int i = 0; i < enumValues.Length; i++)
        {
            if (temp.ToLower() == enumValues[i].ToLower())
            {
                return (CharacterDefines.GazeJointRange)Enum.Parse(typeof(CharacterDefines.GazeJointRange), temp, true);
            }
            else
            {
                string [] words = enumValues[i].Split('_');
                Array.Reverse(words);
                string reverse = string.Join("_", words);
                // there is no match, so reverse the enum string and try that
                if (temp.ToLower() == reverse.ToLower())
                {
                    return (CharacterDefines.GazeJointRange)Enum.Parse(typeof(CharacterDefines.GazeJointRange), enumValues[i], true);
                }
            }
        }

        // if you got this far, an exception will be thrown
        return (CharacterDefines.GazeJointRange)Enum.Parse(typeof(CharacterDefines.GazeJointRange), temp, true);
    }
    #endregion
}
}
