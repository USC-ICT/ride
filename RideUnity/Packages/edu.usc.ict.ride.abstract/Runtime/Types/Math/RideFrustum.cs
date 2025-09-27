using System;

namespace Ride
{
    /// <summary>
    /// Represents a view frustum defined by six clipping planes, abstracted from UnityEngine.Plane[] to support engine-agnostic visibility tests.
    /// The planes are ordered as follows: Left, Right, Bottom, Top, Near, Far — matching the output of UnityEngine.GeometryUtility.CalculateFrustumPlanes().
    /// 
    /// Provides construction helpers from camera parameters or explicit transforms, bounding volume containment checks, and conversion to/from Unity types.
    /// Used in RIDE for frustum culling, perception modeling, and general 3D spatial queries.
    /// 
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/GeometryUtility.CalculateFrustumPlanes.html">UnityEngine.GeometryUtility</a><br/>
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Plane.html">UnityEngine.Plane</a>
    /// </summary>
    [Serializable]
    public struct RideFrustum
    {
        public RidePlane[] planes;


        public RideFrustum(RidePlane[] planes)
        {
            this.planes = planes;
        }

        public bool IsValid => planes != null && planes.Length == 6;

        public static RideFrustum FromUnityPlanes(UnityEngine.Plane[] unityPlanes)
        {
            var ridePlanes = new RidePlane[unityPlanes.Length];
            for (int i = 0; i < unityPlanes.Length; i++)
                ridePlanes[i] = RidePlane.FromUnityPlane(unityPlanes[i]);

            return new RideFrustum(ridePlanes);
        }

        public static RideFrustum FromCamera(UnityEngine.Camera cam) => FromUnityPlanes(UnityEngine.GeometryUtility.CalculateFrustumPlanes(cam));

        public UnityEngine.Plane[] ToUnityPlanes()
        {
            var unityPlanes = new UnityEngine.Plane[planes.Length];
            for (int i = 0; i < planes.Length; i++)
                unityPlanes[i] = planes[i].ToUnityPlane();

            return unityPlanes;
        }

        public bool Contains(RideBounds bounds) => UnityEngine.GeometryUtility.TestPlanesAABB(ToUnityPlanes(), bounds);

        ///// <summary>Creates a view matrix and returns its frustum planes.</summary>
        ///// <param name="fov">Field of view</param>
        ///// <param name="aspect">Screen Width / Height</param>
        ///// <param name="near">Near Plane Distance</param>
        ///// <param name="far">Far Plane Distance</param>
        ///// <param name="viewer">The position and rotation that will be used for the viewing origin</param>
        ///// <returns></returns>
        //public static Plane[] GetFrustum(float fov, float aspect, float near, float far, Transform viewer) => 
        //    GetFrustum(fov, aspect, near, far, viewer.position, viewer.rotation);

        public static RideFrustum GetFrustum(float fov, float aspect, float near, float far, RideVector3 viewerPos, RideQuaternion viewRot)
        {
            var viewToWorldMatrix = RideMatrix4x4.Inverse(RideMatrix4x4.TRS(
              viewerPos,
              viewRot,
              new RideVector3(1, 1, 1)));

            // do the same thing unity does internally with cameraToWorldMatrix by switching the z to the way opening does it
            viewToWorldMatrix.m20 *= -1f;
            viewToWorldMatrix.m21 *= -1f;
            viewToWorldMatrix.m22 *= -1f;
            viewToWorldMatrix.m23 *= -1f;

            RideFrustum frustum = FromUnityPlanes(UnityEngine.GeometryUtility.CalculateFrustumPlanes(RideMatrix4x4.Perspective(fov, aspect, near, far) * viewToWorldMatrix /*Camera.main.cameraToWorldMatrix.inverse*/));
            return frustum;
        }
    }
}
