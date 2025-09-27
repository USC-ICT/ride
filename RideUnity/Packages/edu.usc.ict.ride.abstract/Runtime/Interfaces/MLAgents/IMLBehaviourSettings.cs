namespace Ride.AI.ML
{
    public interface IMLBehaviourSettings
    {
        string behaviourName { get; }
        void Init(RideID entity);
        void SetActive(bool active);
        T GetMLAgent<T>() where T : IMLAgent;
        T GetRLAgent<T>() where T : IRLAgent;
    }
}
