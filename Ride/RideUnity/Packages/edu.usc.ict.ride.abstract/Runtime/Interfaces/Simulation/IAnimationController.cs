namespace Ride.Animations
{
    /// <summary>Describes the desired active state of a named animation Boolean parameter.</summary>
    public struct RideAnimationState
    {
        /// <summary>The animation parameter name.</summary>
        public string name;

        /// <summary>The desired Boolean value.</summary>
        public bool active;
    }

    /// <summary>Identifies a named animation trigger.</summary>
    public struct RideAnimationTrigger
    {
        /// <summary>The animation trigger name.</summary>
        public string name;
    }

    /// <summary>Describes the desired value of a named animation float parameter.</summary>
    public struct RideAnimationFloat
    {
        /// <summary>The animation parameter name.</summary>
        public string name;

        /// <summary>The desired floating-point value.</summary>
        public float value;
    }

    /// <summary>Provides backend-independent access to animation parameters for an identified RIDE entity.</summary>
    /// <remarks>Implemented by RIDE Core's agent animation controller and managed through <see cref="IAnimationSystem"/>.</remarks>
    public interface IAnimationController : IIdentity
    {
        /// <summary>Activates a named animation trigger.</summary>
        /// <param name="name">The trigger parameter name.</param>
        void SetTrigger(string name);

        /// <summary>Sets a named Boolean animation parameter.</summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        void SetBool(string name, bool value);

        /// <summary>Gets a named Boolean animation parameter.</summary>
        /// <param name="name">The parameter name.</param>
        /// <returns>The current parameter value.</returns>
        bool GetBool(string name);

        /// <summary>Sets a named integer animation parameter.</summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        void SetInteger(string name, int value);

        /// <summary>Gets a named integer animation parameter.</summary>
        /// <param name="name">The parameter name.</param>
        /// <returns>The current parameter value.</returns>
        int GetInteger(string name);

        /// <summary>Sets a named floating-point animation parameter.</summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The parameter value.</param>
        void SetFloat(string name, float value);

        /// <summary>Gets a named floating-point animation parameter.</summary>
        /// <param name="name">The parameter name.</param>
        /// <returns>The current parameter value.</returns>
        float GetFloat(string name);
    }
}
