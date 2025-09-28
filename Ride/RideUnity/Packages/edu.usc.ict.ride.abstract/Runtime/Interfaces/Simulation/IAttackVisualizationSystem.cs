using System;
using Ride.WorldState;

namespace Ride
{
    /// <summary>
    /// Response from <see cref="DrawTrajectoryPath"/>, containing the LineRenderer used for visualization.
    /// </summary>
    public class TrajectoryPathResponse : SystemResponse
    {
        public UnityEngine.LineRenderer lineRenderer;
    }

    /// <summary>
    /// Response from validity update requests, indicating whether the trajectory is valid.
    /// </summary>
    public class TrajectoryValidityResponse : SystemResponse
    {
        public bool isValid;
    }

    /// <summary>
    /// Interface for drawing and managing agent-based attack trajectories using LineRenderers.
    /// 
    /// This system is typically used during targeting or aiming previews and is responsible
    /// for displaying throw or fire arcs, validating trajectories, and managing their visibility.
    /// </summary>
    public interface IAttackVisualizationSystem : IRideSystem
    {
        /// <summary>
        /// Draws a trajectory path using a LineRenderer attached to the agent.
        /// </summary>
        /// <param name="trajectory">Trajectory event data that defines the arc to be drawn.</param>
        /// <param name="onComplete">Callback that returns the LineRenderer used to draw the path.</param>
        void DrawTrajectoryPath(AgentTrajectoryEvent trajectory, Action<TrajectoryPathResponse> onComplete);

        /// <summary>
        /// Removes the LineRenderer associated with the given agent.
        /// Called when the trajectory visualization is no longer needed.
        /// </summary>
        /// <param name="agent">The RideID of the agent whose trajectory path should be removed.</param>
        void RemoveTrajectoryPath(RideID agent);

        /// <summary>
        /// Marks the agent's trajectory as invalid, typically by changing the color or style of the LineRenderer.
        /// </summary>
        /// <param name="attacker">The RideID of the agent attempting the attack.</param>
        /// <param name="onComplete">Callback that returns the result of the validity update.</param>
        void MarkAsInvalidTrajectoryPath(RideID attacker, Action<TrajectoryValidityResponse> onComplete);

        /// <summary>
        /// Marks the agent's trajectory as valid, restoring its normal appearance.
        /// </summary>
        /// <param name="attacker">The RideID of the agent attempting the attack.</param>
        /// <param name="onComplete">Callback that returns the result of the validity update.</param>
        void MarkAsValidTrajectoryPath(RideID attacker, Action<TrajectoryValidityResponse> onComplete);
    }
}
