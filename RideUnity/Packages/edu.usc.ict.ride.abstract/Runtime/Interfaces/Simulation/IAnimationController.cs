namespace Ride.Animations
{
    public struct RideAnimationState
    {
        public string name;
        public bool active;
    }

    public struct RideAnimationTrigger
    {
        public string name;
    }

    public struct RideAnimationFloat
    {
        public string name;
        public float value;
    }

    public interface IAnimationController : IIdentity
    {
        void SetTrigger(string name);

        void SetBool(string name, bool value);

        bool GetBool(string name);

        void SetInteger(string name, int value);

        int GetInteger(string name);

        void SetFloat(string name, float value);

        float GetFloat(string name);
    }
}
