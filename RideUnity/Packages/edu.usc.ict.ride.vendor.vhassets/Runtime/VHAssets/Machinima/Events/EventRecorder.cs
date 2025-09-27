using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
public class EventRecorder
{
    #region Variables
    SortedDictionary<int, List<RewindData>> m_TimeData = new SortedDictionary<int, List<RewindData>>();
    #endregion

    #region Functions
    /// <summary>
    /// Saves the data of all specified events at the specified time. The data that is saved
    /// is specific to each ICutsceneEventInterface
    /// </summary>
    /// <param name="timeOfRecording"></param>
    /// <param name="events"></param>
    public void RecordEvents(float timeOfRecording, List<CutsceneEvent> events)
    {
        string lastEventRead = string.Empty;
        try
        {
            int recordedTime = ConvertTimeToKey(timeOfRecording);
            List<RewindData> rewindDatas;
            if (m_TimeData.ContainsKey(recordedTime))
            {
                rewindDatas = m_TimeData[recordedTime];
            }
            else
            {
                rewindDatas = new List<RewindData>();
                m_TimeData.Add(recordedTime, rewindDatas);
            }

            //foreach (CutsceneEvent ce in events)
            // we want the events to be in opposite order so that, when multiple events of the same type
            // are rewound, the one closest to the start is rewound last (this assumes the event list is already sorted by time)
            for (int i = events.Count - 1; i >= 0; i--)
            {
                CutsceneEvent ce = events[i];
                lastEventRead = ce.Name;
                if (ce.Enabled && ce.HasDataToRecord())
                {
                    // make sure it isn't already there
                    if (rewindDatas.FindIndex(rd => rd.Event == ce) == -1)
                    {
                        rewindDatas.Add(new RewindData(ce, ce.SaveRewindData()));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Failed to record event {0}. Error: {1}", lastEventRead, e.Message));
        }
        //int datapoint = 10;
        //m_TimeData.Keys.Where(w => w > start && w < end);
    }

    /// <summary>
    /// Loads and sets the state of each event at the specified time
    /// </summary>
    /// <param name="timeOfRecording"></param>
    public void LoadEventStatesAtTime(float timeOfRecording)
    {
        string lastEventRead = string.Empty;
        try
        {
            // find the closest key based on the timeOfRecording
            int keyToTest = ConvertTimeToKey(timeOfRecording);
            if (!m_TimeData.TryGetValue(keyToTest, out List<RewindData> rewindDataList))
            {
                return;
            }

            /*m_TimeData.Keys.Zip(m_TimeData.Keys.Skip(1),
                  (a, b) => new { a, b })
             .Where(x => x.a <= keyToTest && x.b >= keyToTest)
             .FirstOrDefault();*/

            // this needs to be optimized
            /*int[] keys = m_TimeData.Keys.ToArray();
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (keys[i] < keyToTest && keys[i + 1] > keyToTest)
                {
                    // found the boundaries
                    if ()
                    {

                    }
                    break;
                }
            }*/

            // this needs to be optimized
            foreach (RewindData rewindData in rewindDataList)
            {
                lastEventRead = rewindData.Event.Name;
                rewindData.Event.LoadRewindData(rewindData.Data);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Failed to record event {0}. Error: {1}", lastEventRead, e.Message));
        }
    }

    int ConvertTimeToKey(float time)
    {
        return (int)(time * 100);
    }

    public void ClearRecordings()
    {
        m_TimeData.Clear();
    }

    public void RemoveEventRecording(CutsceneEvent ce)
    {
        foreach (KeyValuePair<int, List<RewindData>> kvp in m_TimeData)
        {
            int index = kvp.Value.FindIndex(m => m.Event == ce);
            if (index != -1)
            {
                kvp.Value[index].Event.LoadRewindData(kvp.Value[index].Data);
                kvp.Value.RemoveAt(index);
            }
        }
    }

    #endregion
}
}
