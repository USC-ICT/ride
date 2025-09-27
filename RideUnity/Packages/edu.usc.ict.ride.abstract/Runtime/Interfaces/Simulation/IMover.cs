using UnityEngine;
using Ride.Entities;

namespace Ride.Movement
{
    /// <summary>
    /// Interface for any entity that can move to a different location
    /// </summary>
    public interface IMover : IEntity, ISpatialObject
    {
        /// <summary>
        /// Turns on/off the IMover
        /// </summary>
        bool enabled { get; set; }

        /// <summary>
        /// The current speed of the mover
        /// </summary>
        float speed { get; set; }

        /// <summary>
        /// The maximum speed of the mover
        /// </summary>
        float maxSpeed { get; set; }

        /// <summary>
        /// The behaviour of a mover upon reaching a waypoint along it's path
        /// </summary>
        PathingBehaviour pathingBehaviour { get; set; }

        /// <summary>
        /// The manner which the mover moves along its path
        /// </summary>
        MovementBehaviour movementBehaviour { get; set; }

        /// <summary>
        /// The path the mover is current onl
        /// </summary>
        IPath path { get; set; }

        /// <summary>
        /// The component that allows the mover to move
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T GetMotor<T>() where T : Component;

        /// <summary>
        /// Returns true if the mover is moving along a path
        /// </summary>
        bool hasPath { get; }

        /// <summary>
        /// Returns true if the mover is calculating a path
        /// </summary>
        bool isPathPending { get; }

        /// <summary>
        /// The distance remaining to the destination
        /// </summary>
        float remainingDistance { get; }

        /// <summary>
        /// How far the mover should be from the destination before slowing down
        /// </summary>
        float stoppingDistance { get; }

        /// <summary>
        /// Current velocity of the mover
        /// </summary>
        RideVector3 velocity { get; set; }

        /// <summary>
        /// The velocity that will be used when the mover is locomoting instead of pathing
        /// </summary>
        RideVector3 desiredLocomotionVelocity { get; set; }

        /// <summary>
        /// Current forward direction of the mover
        /// </summary>
        RideVector3 forwardDir { get; }

        /// <summary>
        /// Current rotation velocity of the mover
        /// </summary>
        float rotationSpeed { get; set; }

        /// <summary>
        /// Max rotation velocity of the mover
        /// </summary>
        float maxRotationSpeed { get; }

        /// <summary>
        /// Returns true if the entity is carrying additional weight
        /// </summary>
        bool isLoaded { get; set; }

        MovementLeg Leg { get; set; }
    }
}
