using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public abstract class GestureMapDefinition : MonoBehaviour
{
    // these parameters are defined by smartbody.  From SBGestureMap.h
    // SBAPI void addGestureMapping(const std::string& name, const std::string& lexeme, const std::string& type, const std::string& hand, const std::string& style, const std::string& posture);
    public class SmartbodyGestureMap
    {
        public string animName;
        public string lexeme;
        public string type;
        public string hand;
        public string style;
        public string parentPosture;

        public SmartbodyGestureMap(string animName, string type, string lexeme, string hand, string style, string parentPosture) { this.animName = animName; this.lexeme = lexeme; this.type = type; this.hand = hand; this.style = style; this.parentPosture = parentPosture; }
    }

    [NonSerialized] public string gestureMapName;
    [NonSerialized] public List<SmartbodyGestureMap> gestureMaps = new List<SmartbodyGestureMap>();


    void Start()
    {
    }

    /// <summary>
    /// Returns a random animation based on the provided lexeme and type. Example DEICTIC and YOU
    /// </summary>
    /// <param name="lexeme">Lexeme.</param>
    /// <param name="type">Type.</param>
    public string GetAnimation(string lexeme, string type)
    {
        List<SmartbodyGestureMap> gestures = gestureMaps.FindAll(gm => gm.lexeme == lexeme && gm.type == type);
        string animName = "";
        if (gestures.Count > 0)
        {
            animName = gestures[UnityEngine.Random.Range(0, gestures.Count)].animName;
        }

        if (string.IsNullOrEmpty(animName))
        {
            Debug.LogError(string.Format("couldn't find an animation for gesture lexeme {0} and type {1}", lexeme, type));
        }

        return animName;
    }
}
}
