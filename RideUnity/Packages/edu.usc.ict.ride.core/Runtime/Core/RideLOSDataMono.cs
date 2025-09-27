using UnityEngine;

namespace Ride.Entities
{
    [System.Serializable]
    public struct RideLOSData
    {
        public Color losColor;
        public float losRange;
        public float losHeight;
        public bool activateOnStart;

        public RideLOSData(Color color, float range, float height, bool activateAtStart)
        {
            losColor = color;
            losRange = range;
            losHeight = height;
            activateOnStart = activateAtStart;
        }
    }

    public class RideLOSDataMono : RideDataUnityBootstrap
    {
        public RideLOSData data;

        public override object GetData()
        {
            return data;
        }
    }

}