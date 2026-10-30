using UnityEngine;

namespace Ride.Entities
{
    /// <summary>Defines the appearance, dimensions, and initial state of an entity's line-of-sight display.</summary>
    [System.Serializable]
    public struct RideLOSData
    {
        /// <summary>The color used to visualize line of sight.</summary>
        public Color losColor;

        /// <summary>The maximum line-of-sight visualization range.</summary>
        public float losRange;

        /// <summary>The vertical offset used for the line-of-sight origin.</summary>
        public float losHeight;

        /// <summary>Whether line-of-sight visualization should be active when the entity starts.</summary>
        public bool activateOnStart;

        /// <summary>Initializes line-of-sight visualization data.</summary>
        /// <param name="color">The visualization color.</param>
        /// <param name="range">The maximum visualization range.</param>
        /// <param name="height">The vertical offset of the line-of-sight origin.</param>
        /// <param name="activateAtStart">Whether visualization starts active.</param>
        public RideLOSData(Color color, float range, float height, bool activateAtStart)
        {
            losColor = color;
            losRange = range;
            losHeight = height;
            activateOnStart = activateAtStart;
        }
    }

    /// <summary>Exposes serialized <see cref="RideLOSData"/> to RIDE's Unity conversion pipeline.</summary>
    public class RideLOSDataMono : RideDataUnityBootstrap
    {
        /// <summary>The line-of-sight configuration returned during entity conversion.</summary>
        public RideLOSData data;

        /// <inheritdoc/>
        public override object GetData()
        {
            return data;
        }
    }

}
