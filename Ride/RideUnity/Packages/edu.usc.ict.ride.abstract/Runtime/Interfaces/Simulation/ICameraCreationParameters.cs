namespace Ride
{
    /// <summary>
    /// For <see cref="ICameraSystem.CreateCamera"/>.
    /// </summary>
    /// <remarks>
    /// Inherit this interface to add supported platform specific camera settings
    /// </remarks>
    public interface ICameraCreationParameters {

        /// <summary>
        /// <para>The ID of the object that the created camera should become a child of.</para>
        /// <para>If set to <see cref="RideID.Null"/>, the camera will become an standalone object. </para>
        /// </summary>
        RideID parentObjectId { get; }

        /// <summary>
        /// The position of the camera object.
        /// If a parent object is given, this will be a local position, otherwise a world position.
        /// </summary>
        RideVector3 position { get; }

        /// <summary>
        /// The rotation of the camera object.
        /// If a parent object is given, this will be a local rotation, otherwise a world rotation.
        /// </summary>
        RideQuaternion rotation { get; }

        /// <summary>
        /// Indicate whether the created camera is actived after creation.
        /// </summary>
        bool active { get; }
    }
}
