using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents a ray through 3D space.
    /// This is a parallel class to <a href="https://docs.unity3d.com/ScriptReference/Ray.html">UnityEngine.Ray</a>.
    /// Implemented separately to abstract RIDE classes away from UnityEngine specific implementations.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RideRay
    {
        public RideVector3 origin;
        public RideVector3 direction;

        public RideRay(RideVector3 origin, RideVector3 direction)
        {
            this.origin = origin;
            this.direction = direction;
        }

        public RideRay(UnityEngine.Ray ray) : this(ray.origin, ray.direction) { }

        public UnityEngine.Ray ToRay() => new UnityEngine.Ray(origin, direction);

        static public implicit operator RideRay(UnityEngine.Ray ray) => new RideRay(ray.origin, ray.direction);
        static public implicit operator UnityEngine.Ray(RideRay ray) => ray.ToRay();

        public override string ToString() => ToRay().ToString();
    }
}
