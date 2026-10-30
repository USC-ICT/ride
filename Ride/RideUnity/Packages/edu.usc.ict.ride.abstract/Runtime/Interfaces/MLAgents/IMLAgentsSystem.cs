namespace Ride.AI.ML
{
    /// <summary>
    /// Defines manual advancement of the Unity ML-Agents environment.
    /// The RIDE ML-Agents system uses this contract when automatic Academy stepping is disabled.
    /// </summary>
    public interface IMLAgentsSystem
    {
        /// <summary>Advances the machine-learning environment by one step.</summary>
        public void Step();
    }
}
