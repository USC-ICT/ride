namespace Ride.Entities
{
    /// <summary>Specifies how an explosive is activated.</summary>
    public enum ExplosiveTriggerType
    {
        /// <summary>Requires an explicit trigger action.</summary>
        Trigger,

        /// <summary>Activates when a target enters the configured proximity.</summary>
        Proximity
    }

    /// <summary>Describes explosive item characteristics consumed by the weapon system.</summary>
    public interface IExplosive : IItem
    {
        /// <summary>Gets the activation mode.</summary>
        ExplosiveTriggerType explosiveType { get; }

        /// <summary>Gets the radius within which the explosion applies effects.</summary>
        float explosiveRadius { get; }

        /// <summary>Gets the base explosion damage.</summary>
        float explosiveDamage { get; }

        /// <summary>Gets the proximity distance that activates a proximity-triggered explosive.</summary>
        float explosiveProximity { get; }

        /// <summary>Gets the delay in seconds between activation and explosion.</summary>
        float explosiveTimer { get; }
    }
}
