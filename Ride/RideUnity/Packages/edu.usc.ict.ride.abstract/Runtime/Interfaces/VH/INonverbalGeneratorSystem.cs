namespace Ride
{
    public interface INonverbalGeneratorSystem : IRideSystem, IExternalProcess/*TODO: remove this interface*/
    {
        public delegate void NonverbalBehaviorResult(string result);

        void GetNonverbalBehavior(string characterName, string text, NonverbalBehaviorResult resultCallback);

        void StartProcess(string characterName);
    }
}
