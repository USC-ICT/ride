using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents a geometric plane in 3D space, structurally similar to UnityEngine.Plane but decoupled for use in both Unity and non-Unity environments.
    /// This struct provides core plane functionality (e.g., distance, normal, point testing) and conversion to/from Unity types. Used primarily for
    /// frustum clipping, visibility checks, and general 3D geometry operations in RIDE systems.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Plane.html">UnityEngine.Plane</a>
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RidePlane
    {
        public RideVector3 normal;
        public float distance;

        public RidePlane(RideVector3 normal, float distance)
        {
            this.normal = normal;
            this.distance = distance;
        }

        public static RidePlane FromUnityPlane(UnityEngine.Plane unityPlane) => new RidePlane(unityPlane.normal, unityPlane.distance);
        public UnityEngine.Plane ToUnityPlane() => new UnityEngine.Plane(normal, distance);

        public override string ToString() => ToUnityPlane().ToString();
    }
}
