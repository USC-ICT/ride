using Ride.Combat;

namespace Ride.Entities
{
    public enum AgentPosture
    {
        Stand = 0,
        Crouch = 1,
        Prone = 2,
        Jumping = 3
    }

    /// <summary>
    /// An agent is any soldier or civilian or other human
    /// and this is all of the data for that human
    /// </summary>
    public interface IAgent
    {
        EntityStatus status { get; set; }
        string agentName { get; set; }
        Unit agentData { get; }
        Team Team { get; set; }
        void SetAgentData(Unit unit);
        AgentPosture posture { get; set; }
        float suppression { get; set; }
        float suppressionReductionRate { get; set; }
        float skillLevel { get; set; }
        RideVector3 viewportOffset { get; set; }
        float range { get; set; }

        IAttackResult CalculateAttack(RideID weaponId);

        IAttackResult CalculateAttack(RideID weaponId, RideVector3 position);
    }
}
