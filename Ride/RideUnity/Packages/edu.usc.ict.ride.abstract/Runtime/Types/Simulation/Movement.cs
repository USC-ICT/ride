using System;

namespace Ride.Movement
{
    /// <summary>Stores the behavior, speed, and optional path used to move an entity.</summary>
    /// <remarks>
    /// <see cref="Unit.moveBehaviour"/> carries this data into movement-system implementations.
    /// The commented destination and enabled fields are legacy placeholders; current movement
    /// systems receive destinations and enabled state through their operational APIs instead.
    /// </remarks>
    [Serializable]
    public struct Movement
    {
        /// <summary>Movement constraint or behavior applied to the entity.</summary>
        public MovementBehaviour movementBehaviour;

        /// <summary>Behavior used when traversing the assigned path.</summary>
        public PathingBehaviour pathingBehaviour;

        //public bool isEnabled;

        /// <summary>Requested movement speed.</summary>
        public float speed;

        /// <summary>Maximum permitted movement speed.</summary>
        public float maxSpeed;

        //public Position destination;

        /// <summary>Path assigned to the movement operation, or <see langword="null"/> when none is assigned.</summary>
        public IPath path;

        /// <summary>Initializes movement data.</summary>
        /// <param name="movementBehaviour">Movement constraint or behavior to apply.</param>
        /// <param name="pathingBehaviour">Behavior to use while traversing the path.</param>
        /// <param name="destination">Legacy parameter retained for compatibility; the value is not stored.</param>
        /// <param name="speed">Requested movement speed.</param>
        /// <param name="maxSpeed">Maximum permitted movement speed.</param>
        /// <param name="isEnabled">Legacy parameter retained for compatibility; the value is not stored.</param>
        /// <param name="path">Path to assign, or <see langword="null"/>.</param>
        public Movement(MovementBehaviour movementBehaviour, PathingBehaviour pathingBehaviour, RideVector3 destination, float speed, float maxSpeed, bool isEnabled, IPath path)
        {
            this.movementBehaviour = movementBehaviour;
            this.pathingBehaviour = pathingBehaviour;
            this.speed = speed;
            this.maxSpeed = maxSpeed;
            //this.destination = destination;
            //this.isEnabled = isEnabled;
            this.path = path;
        }
    }
}
