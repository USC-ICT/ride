using System;
using UnityEngine;
using UnityEngine.Playables;

namespace VHAssets
{
/// <summary>
/// Represents a word on the timeline since markers can't have text
/// </summary>
[Serializable]
public class WordAsset : PlayableAsset
{
    // Factory method that generates a playable based on this asset
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        var behaviour = ScriptPlayable<WordBehaviour>.Create(graph);
        return behaviour;
    }
}
}
