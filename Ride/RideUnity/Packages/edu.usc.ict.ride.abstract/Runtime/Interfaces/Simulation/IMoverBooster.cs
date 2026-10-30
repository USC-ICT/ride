namespace Ride.Movement
{
    /// <summary>Controls temporary movement boosting for an identified mover.</summary>
    /// <remarks>Used by the movement system and implemented by Simulation's unit booster.</remarks>
    public interface IMoverBooster : IIdentity
    {
        /// <summary>Gets or sets whether boost is currently active.</summary>
        bool IsBoosting { get; set; }
        /// <summary>Gets the movement multiplier currently contributed by boost, or zero when boost is inactive.</summary>
        float BoostLevel { get; }
        /// <summary>Gets or sets the current boost resource amount.</summary>
        float BoostAmount { get; set; }
        /// <summary>Gets or sets the maximum boost resource amount.</summary>
        float MaxBoostAmount { get; set; }
        /// <summary>Gets or sets the movement multiplier applied while boosting.</summary>
        float BoostMultiplier { get; set; }
        /// <summary>Gets or sets the rate at which active boost is consumed.</summary>
        float BoostReductionRate { get; set; }
        /// <summary>Gets or sets the rate at which boost is replenished.</summary>
        float BoostReplenishmentRate { get; set; }
        /// <summary>Gets or sets whether boost is constrained by the available resource.</summary>
        bool LimitedBoost { get; set; }

        /// <summary>Enables or disables movement boost.</summary>
        /// <param name="tog"><see langword="true"/> to enable boost; otherwise, <see langword="false"/>.</param>
        void ToggleBoost(bool tog);
    }
}
