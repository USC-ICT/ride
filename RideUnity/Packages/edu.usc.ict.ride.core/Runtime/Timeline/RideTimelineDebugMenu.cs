using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Ride;
using Ride.Examples;


public class RideTimelineDebugMenu : RideBaseMinimal
{
    [SerializeField] private List<PlayableDirector> DirectorList;

    //private PlayableDirector currentDirector;
    private bool isPaused = false;
    //private bool isFirstStart = true;

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
                //if(currentDirector == null)
                //{
                //    Debug.LogWarning("RideTimelineDebugMenu.cs: Timeline director has not been selected.");
                //}
                //else if(isFirstStart)
                //{
                //    currentDirector.Play();
                //    isFirstStart = false;
                //}
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

        //using(new GUILayout.HorizontalScope())
        //{
        //    for(int i =0; i<DirectorList.Count; ++i)
        //    {
        //        if(DrawGUIButton(DirectorList[i].name, 100f))
        //        {
        //            DirectorList[i].Play();
        //            currentDirector = DirectorList[i];
        //        }
        //        //BeginHorizontal();
        //    }
        //}

    }   //--OnGUITimeline()


}
