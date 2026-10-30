using UnityEngine;

namespace Ride.UI
{
    /// <summary>Associates an entity with the visual indicator used to show its selection state.</summary>
    /// <remarks><c>RideEntitySelectorSystem</c> creates and toggles these components during pointer selection.</remarks>
    public class RideEntitySelector : RideSpatialObject
    {
        /// <summary>The child or auxiliary object displayed while the entity is selected.</summary>
        public GameObject selectorDisplay;

        /// <summary>Gets or sets whether the configured selection indicator is active.</summary>
        /// <remarks>Returns <c>false</c> and ignores assignments when <see cref="selectorDisplay"/> is not configured.</remarks>
        public bool SelectorEnable
        {
            get { return selectorDisplay != null ? selectorDisplay.activeSelf : false; }
            set { if (selectorDisplay != null) selectorDisplay.SetActive(value); }
        }
    }
}
