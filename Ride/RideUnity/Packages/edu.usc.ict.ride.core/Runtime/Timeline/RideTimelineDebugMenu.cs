using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Ride;
using Ride.Examples;


public class RideTimelineDebugMenu : RideBaseMinimal
{
    [SerializeField] private List<PlayableDirector> DirectorList;

    private bool isPaused = false;
    
    protected override void Start()
    {
        base.Start();

        AddDebugMenu("Timeline", OnGUITimeline);
        SetDebugMenu(4);
        ShowDebugMenu(true);
    }

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
