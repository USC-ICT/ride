namespace Ride.Movement
{
    public interface IMoverBooster : IIdentity
    {
        bool IsBoosting { get; set; }
        float BoostLevel { get; }
        float BoostAmount { get; set; }
        float MaxBoostAmount { get; set; }
        float BoostMultiplier { get; set; }
        float BoostReductionRate { get; set; }
        float BoostReplenishmentRate { get; set; }
        bool LimitedBoost { get; set; }

        void ToggleBoost(bool tog);
    }
}
