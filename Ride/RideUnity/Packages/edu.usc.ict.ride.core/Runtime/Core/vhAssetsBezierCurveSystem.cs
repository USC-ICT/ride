using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VHAssets;

namespace Ride
{
    /// <summary>
    /// Uses the vhAssets Curve class to move objects along a curve
    /// </summary>
    /// <inheritdoc cref="ITweenMovementBehaviour"/>
    public class vhAssetsBezierCurveSystem : RideSystemMonoBehaviour, ITweenMovementBehaviour
    {
        Dictionary<RideID, Curve> m_curves = new Dictionary<RideID, Curve>();

        public override void SystemAwake()
        {
            base.SystemAwake();

            var curves = RideUtils.FindObjectsByType<Curve>(FindObjectsInactive.Include);
            foreach (var curve in curves)
            {
                RideID id = Globals.api.gameObjectSystem.GetObject(RideUtils.EntityIdToULong(curve.gameObject));
                if (id == RideID.Null)
                {
                    // this curve isn't represented in the gameobject system
                    id = Globals.api.gameObjectSystem.InsertObject(RideUtils.EntityIdToULong(curve.gameObject));
                }

                if (!m_curves.ContainsKey(id))
                {
                    m_curves.Add(id, curve);
                }
                else
                {
                    RideLog.LogError("vhAssetsBezierCurveSystem already has: " + id);
                }
            }
        }

        public RideID[] GetPathes()
        {
            return m_curves.Keys.ToArray();
        }

        public RideID GetCurveID(Curve curve)
        {
            foreach(RideID id in m_curves.Keys)
            {
                if(m_curves[id] == curve)
                {
                    return id;
                }
            }
            return RideID.Null;
        }

        public void MoveAlong(RideID spline, RideID transform, float duration)
        {
            var goSystem = (GameObjectSystemUnity)Globals.api.gameObjectSystem;
            GameObject mover = goSystem.GetGameObject(transform);
            m_curves[spline].FollowCurve(mover, duration);
        }

        public void MoveAlong(RideID spline, RideID transform, float duration, RideID lookAtTarget)
        {
            var goSystem = (GameObjectSystemUnity)Globals.api.gameObjectSystem;
            GameObject mover = goSystem.GetGameObject(transform);
            GameObject target = goSystem.GetGameObject(lookAtTarget);
            var lookTarget = (target != null) ? target.transform : null;
            m_curves[spline].FollowCurve(mover, duration, lookTarget);
        }

        public void MoveAlong(RideID spline, RideID transform, float duration, RideVector3 lookAtTarget)
        {
            var goSystem = (GameObjectSystemUnity)Globals.api.gameObjectSystem;
            GameObject mover = goSystem.GetGameObject(transform);
            m_curves[spline].FollowCurve(mover, duration, lookAtTarget);
        }
    }
}
