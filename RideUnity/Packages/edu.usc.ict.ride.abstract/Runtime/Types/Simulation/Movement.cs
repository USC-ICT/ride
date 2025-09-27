using System;

namespace Ride.Movement
{
    /// <summary>
    /// Stores data that explains how and where an entity will move around the world
    /// </summary>
    [Serializable]
    public struct Movement
    {
        public MovementBehaviour movementBehaviour;
        public PathingBehaviour pathingBehaviour;
        //public bool isEnabled;
        public float speed;
        public float maxSpeed;
        //public Position destination;
        public IPath path;

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
