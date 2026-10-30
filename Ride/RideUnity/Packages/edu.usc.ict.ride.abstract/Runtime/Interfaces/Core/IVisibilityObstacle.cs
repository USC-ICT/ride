using UnityEngine;

namespace Ride.Entities
{
    /// <summary>
    /// Represents a collider-backed effect, such as smoke, that reduces visibility along a line of sight.
    /// RIDE Simulation attack calculations query this interface on visibility raycast hits.
    /// </summary>
    public interface IVisibilityObstacle
    {
        /// <summary>Gets or sets the visibility reduction, where <c>0</c> has no effect and <c>1</c> fully obscures the target.</summary>
        float visibilityHinderance { get; set; }

        /// <summary>Gets or sets the lifetime of the obstacle, in seconds.</summary>
        float duration { get; set; }
    }
}
