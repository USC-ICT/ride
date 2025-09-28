using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
[Serializable]
public class CutsceneTrackGroup
{
    #region Constants
    const int MaxChildren = 3;
    #endregion

    #region Variables
    public string GroupName = "";

    [HideInInspector]
    public bool Expanded;

    [HideInInspector]
    public bool EditingName;

    [HideInInspector]
    public bool Hidden;

    [HideInInspector]
    public bool Muted;

    [HideInInspector]
    public Rect GroupNamePosition = new Rect();

    [HideInInspector]
    public bool IsSelected = false;

    [HideInInspector]
    public Rect TrackPosition = new Rect();

    [HideInInspector]
    public string UniqueId = "";

    //[HideInInspector]
    // IMPORTANT: this had to be removed in order to get rid of the unity 4.5 nested class error
    //List<CutsceneTrackGroup> m_Children = new List<CutsceneTrackGroup>();
    //public CutsceneTrackGroup[] m_Children = new CutsceneTrackGroup[MaxChildren];
    //public CutsceneTrackGroup m_Children;

    // the GUID's of the events that are on the track
    [HideInInspector]
    public List<string> m_TrackItems = new List<string>();
    #endregion

    #region Properties
    public bool HasChildren
    {
        get { return NumChildren > 0; }
        //get { return m_Children.Length > 0; }
        //get { return m_Children != null; }
    }

    public int NumChildren
    {
        get { return 0; }
        //get { return m_Children.Count; }
        //get { return m_Children.Length; }
        //get { return m_Children != null ? 1 : 0; }
    }

    public List<CutsceneTrackGroup> Children
    {
        get { return null; }
        //get { return new List<CutsceneTrackGroup>(); }
    }
    #endregion

    #region Functions
    public CutsceneTrackGroup() { }
    public CutsceneTrackGroup(Rect trackPosition, string groupName, bool expanded)
    {
        GroupName = groupName;
        Expanded = expanded;
        TrackPosition = trackPosition;
        UniqueId = Guid.NewGuid().ToString();
    }

    public List<CutsceneTrackGroup> ConvertChildrenToList()
    {
        //return new List<CutsceneTrackGroup>(m_Children);
        List<CutsceneTrackGroup> children = new List<CutsceneTrackGroup>();
        return children;
    }

    public void AddChildGroup(CutsceneTrackGroup child)
    {
        if (!ContainsChild(child))
        {
            AddChild(child);
            //m_Children.Add(child);
            Expanded = true;
            //Debug.Log(string.Format("{0} now has child {1}", GroupName, child.GroupName));
        }
    }

    public bool ContainsChild(CutsceneTrackGroup child)
    {
        return true;
        //return Array.Find<CutsceneTrackGroup>(m_Children, c => c== child) != null;
        //return child == m_Children;
    }

    void AddChild(CutsceneTrackGroup child)
    {
        //m_Children = child;

        //int newSize = 1;
        //if (m_Children == null || m_Children.Length == 0)
        //{
        //    m_Children = new CutsceneTrackGroup[newSize];
        //}
        //else
        //{
        //    // deep copy the array
        //    newSize = m_Children.Length + 1;
        //    CutsceneTrackGroup[] clonedArray = (CutsceneTrackGroup[])m_Children.Clone();
        //    m_Children = new CutsceneTrackGroup[newSize];
        //    for (int i = 0; i < clonedArray.Length; i++)
        //    {
        //        m_Children[i] = clonedArray[i];
        //    }
        //}

        //m_Children[newSize - 1] = child;
    }

    public void RemoveChild(CutsceneTrackGroup child)
    {
        //if (child == m_Children)
        //{
        //    m_Children = null;
        //}


        //int index = Array.FindIndex<CutsceneTrackGroup>(m_Children, c => c == child);
        //if (index != -1)
        //{
        //    m_Children[index] = null;
        //    CutsceneTrackGroup[] clonedArray = (CutsceneTrackGroup[])m_Children.Clone();
        //    m_Children = new CutsceneTrackGroup[clonedArray.Length - 1];
        //    for (int i = 0; i < clonedArray.Length; i++)
        //    {
        //        if (clonedArray[i] != null)
        //        {
        //            m_Children[i] = clonedArray[i];
        //        }
        //    }
        //}
    }

    public void SetStartingPosition(Rect pos)
    {
        SetTrackWidthHeight(pos.width, pos.height);
    }

    public void SetTrackPosition(float x, float y)
    {
        TrackPosition.x = x;
        TrackPosition.y = y;
    }

    public void SetTrackWidthHeight(float w, float h)
    {
        TrackPosition.width = w;
        TrackPosition.height = h;
    }

    /// <summary>
    /// Returns true if you clicked in the track area
    /// </summary>
    /// <param name="selectionPoint"></param>
    /// <returns></returns>
    public bool TrackContainsPosition(Vector2 selectionPoint)
    {
        Rect tempTrackPosition = TrackPosition;
        // this is a hack to make sure that when we drag events off the right side of the screen,
        // the track appears to be infinite in width so that events know what track they are on.
        // Without this, quickly dragging events to the right will cause errors about the event not
        // being on any track
        tempTrackPosition.width = 1000000;
        return tempTrackPosition.Contains(selectionPoint);
    }

    /// <summary>
    /// Returns true if you clicked in the track area
    /// </summary>
    /// <param name="posX"></param>
    /// <param name="posY"></param>
    /// <returns></returns>
    public bool TrackContainsPosition(float posX, float posY)
    {
        return TrackContainsPosition(new Vector2(posX, posY));
    }

    /// <summary>
    /// Returns true if the trackItem is part of this track group
    /// </summary>
    /// <param name="trackItem"></param>
    /// <returns></returns>
    public bool TrackContainsItem(CutsceneTrackItem trackItem)
    {
       // return m_TrackItems.Find(t => t.UniqueId == trackItem.UniqueId) != null;
        return (int)trackItem.GuiPosition.y == (int)TrackPosition.y;
    }

    /// <summary>
    /// Returns true if you click the name of the group
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool GroupNameContainsPosition(Vector2 position)
    {
        return GroupNamePosition.Contains(position);
    }

    /// <summary>
    /// Highlights the track at the specified location
    /// </summary>
    /// <param name="selectionPoint"></param>
    /// <param name="clearPreviousSelections"></param>
    public void SelectTrack(bool selected)
    {
        IsSelected = selected;
    }

    public void Draw(Rect drawPosition)
    {
        Color original = GUI.color;
        TrackPosition.y = drawPosition.y;

        if (IsSelected)
        {
            GUI.color = CutsceneEvent.SelectedColor;
        }

        GUI.Box(drawPosition, "");

        // restore
        GUI.color = original;
    }

    /// <summary>
    /// Returns true if the potentialChild is a child. This checks recursively through child tracks
    /// </summary>
    /// <param name="potentialChild"></param>
    /// <returns></returns>
    public bool IsAncestorOf(CutsceneTrackGroup potentialChild)
    {
        Stack<CutsceneTrackGroup> groupStack = new Stack<CutsceneTrackGroup>();
        foreach (CutsceneTrackGroup group in Children)
        {
            groupStack.Clear();
            groupStack.Push(group);

            while (groupStack.Count > 0)
            {
                CutsceneTrackGroup currGroup = groupStack.Pop();
                if (currGroup == potentialChild)
                {
                    return true;
                }

                foreach (CutsceneTrackGroup child in currGroup.Children)
                {
                    groupStack.Push(child);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if this track has potentialAncestor in it's hierarchy
    /// </summary>
    /// <param name="potentialAncestor"></param>
    /// <returns></returns>
    public bool IsChildOf(CutsceneTrackGroup potentialAncestor)
    {
        return potentialAncestor.IsAncestorOf(this);
    }
    #endregion
}
}
