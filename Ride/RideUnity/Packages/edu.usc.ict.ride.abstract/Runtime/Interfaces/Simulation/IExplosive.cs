namespace Ride.Entities
{
    public enum ExplosiveTriggerType
    {
        Trigger,
        Proximity
    }

    public interface IExplosive : IItem
    {
        ExplosiveTriggerType explosiveType { get; }
        float explosiveRadius { get; }
        float explosiveDamage { get; }
        float explosiveProximity { get; }
        float explosiveTimer { get; }
    }
}
