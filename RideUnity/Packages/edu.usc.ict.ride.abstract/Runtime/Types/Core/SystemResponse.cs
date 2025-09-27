using System;

namespace Ride
{
    /// <summary>
    /// Represents the result of a system operation.
    /// Used to indicate success or failure of an action,
    /// such as initialization, configuration, or a service request.
    ///
    /// This type is lightweight and serializable, and is suitable for 
    /// general-purpose status reporting across systems.
    /// </summary>
    [Serializable]
    public class SystemResponse
    {
        /// <summary>
        /// An error message indicating the failure reason.
        /// If null or empty, the operation was successful.
        /// </summary>
        public string error;

        /// <summary>
        /// Gets whether the operation completed successfully.
        /// Returns true if <see cref="error"/> is null or empty.
        /// </summary>
        public bool success => string.IsNullOrEmpty(error);
    }
}
