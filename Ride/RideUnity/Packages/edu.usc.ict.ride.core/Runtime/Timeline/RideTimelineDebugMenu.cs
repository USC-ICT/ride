using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Ride;
using Ride.Examples;


/// <summary>Adds basic Timeline playback and global pause controls to the RIDE debug menu.</summary>
/// <remarks>
/// The Start action plays the first configured <see cref="PlayableDirector"/>. Pause toggles
/// <see cref="Time.timeScale"/> for the entire simulation, not only the configured Timeline.
/// </remarks>
public class RideTimelineDebugMenu : RideBaseMinimal
{
    /// <summary>The directors exposed to the debug UI; the first entry is started by the Start button.</summary>
    [SerializeField] private List<PlayableDirector> DirectorList;

    /// <summary>Whether this menu has paused the simulation by setting the time scale to zero.</summary>
    private bool isPaused = false;
    
    /// <inheritdoc/>
    protected override void Start()
    {
        base.Start();

        AddDebugMenu("Timeline", OnGUITimeline);
        SetDebugMenu(4);
        ShowDebugMenu(true);
    }

    /// <summary>Draws the Timeline start and pause controls.</summary>
    void OnGUITimeline()
    {
        DrawGUILabel("Timeline Controls: ");

        using (m_debugMenu.Horizontal())
        {
            if(DrawGUIButton("Start"))
            {                
                if(isPaused == false)
                {
                    DirectorList[0].Play();
                }
                else
                {
                    Time.timeScale = 1;
                    isPaused = false;
                }
            }

            if(DrawGUIButton("Pause"))
            {
                if(!isPaused)
                {
                    Time.timeScale = 0;
                    isPaused = true;
                }
                else
                {
                    Time.timeScale = 1;
                    isPaused = false;
                }
            }
        }
    }   
}
