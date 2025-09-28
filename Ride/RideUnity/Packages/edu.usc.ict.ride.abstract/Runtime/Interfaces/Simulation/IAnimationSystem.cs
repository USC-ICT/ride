namespace Ride.Animations
{
    public interface IAnimationSystem  : IRideSystem
    {
        IAnimationController GetAnimationController(RideID id);
    }
}
