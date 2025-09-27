using UnityEngine;

namespace Ride.UI
{
    public class RideEntitySelector : RideSpatialObject
    {
        public GameObject selectorDisplay;

        public bool SelectorEnable
        {
            get { return selectorDisplay != null ? selectorDisplay.activeSelf : false; }
            set { if (selectorDisplay != null) selectorDisplay.SetActive(value); }
        }
    }
}