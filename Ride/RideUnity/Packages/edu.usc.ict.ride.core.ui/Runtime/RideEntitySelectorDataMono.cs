namespace Ride
{
    [System.Serializable]
    public struct RideEntitySelectorData
    {
        public UnityEngine.GameObject selectorDisplayPrefab;
        public RideVector3 colliderCenter;
        public RideVector3 colliderSize;
        public float selectorScale;
        public RideVector3 selectorOffset;
    }

    public class RideEntitySelectorDataMono : RideDataUnityBootstrap
    {
        public RideEntitySelectorData data;

        public override object GetData()
        {
            return data;
        }
    }
}