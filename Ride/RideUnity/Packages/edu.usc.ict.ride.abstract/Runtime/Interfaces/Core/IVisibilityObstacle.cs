using UnityEngine;

namespace Ride.Entities
{
    public interface IVisibilityObstacle
    {
        float visibilityHinderance { get; set; }

        float duration { get; set; }
    }
}
