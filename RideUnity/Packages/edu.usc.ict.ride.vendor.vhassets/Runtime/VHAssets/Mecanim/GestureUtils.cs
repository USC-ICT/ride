using UnityEngine;

namespace VHAssets
{
public static class GestureUtils
{
    #region Constants
    public enum Handedness
    {
        LEFT_HAND,
        RIGHT_HAND,
        BOTH_HANDS
    }

    public enum Emotion
    {
        angry,
        neutral,
        sad
    }

    public enum Style
    {
        energetic,

    }

    public enum Lexeme
    {
        DEICTIC,
        METAPHORIC,
        EMBLEM,
        ADAPTOR,
    }

    public enum Type
    {
        YOU,
        ME,
        LEFT,
        RIGHT,
        NEGATION,
        CONTRAST,
        ASSUMPTION,
        RHETORICAL,
        INCLUSIVITY,
        QUESTION,
        OBLIGATION,
        GREETING,
        CONTEMPLATE,
    }
    #endregion
}
}
