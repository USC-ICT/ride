using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Declares a 2D region in the Unity scene using local-space polygon points.
    /// These are automatically discovered at runtime by <see cref="RegionSystem"/> using <c>Object.FindObjectsOfType&lt;UnityRegion&gt;()</c>.
    ///
    /// These regions may be used to drive triggers, constraints, or simulation logic that depends on custom spatial boundaries.
    /// A matching visualizer script (typically <c>UnityRegionVisualizer</c>) may render the shape in the Scene view.
    /// </summary>
    public class UnityRegion : RideMonoBehaviour
    {
        /// <summary>Whether this region should be drawn in the Unity Scene view using Gizmos.</summary>
        public bool m_visualize;

        /// <summary>The color used when drawing the region in the Scene view.</summary>
        public Color m_color;

        /// <summary>
        /// The ordered set of 2D local-space points defining the region's polygon shape.
        /// These should form a closed, non-intersecting loop and are typically defined in either clockwise or counter-clockwise order.
        /// </summary>
        public RideVector2[] m_points;

        /// <summary>
        /// The RideID assigned to this region during runtime registration.
        /// This is typically set by <see cref="RegionSystem"/> after discovery and integration.
        /// </summary>
        [HideInInspector]
        public RideID rideID;
    }
}
