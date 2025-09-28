namespace Ride
{
    public enum RideCameraView
    {
        Locked,
        FreeLook,
        RotateAround,
        LookAt
    }

    /// <summary>
    /// Provides a system-level interface for managing and interacting with cameras
    /// in the RIDE environment. Supports creation, activation, user control,
    /// coordinate conversions, view switching, targeting, and spatial queries.
    /// </summary>
    public interface ICameraSystem : IRideSystem
    {
        // ------------------------------------------------------------
        // General Camera Management
        // ------------------------------------------------------------

        /// <summary>
        /// Gets the current main camera's RideID.
        /// </summary>
        RideID GetMainCamera();

        /// <summary>
        /// Sets the specified camera as the new main camera.
        /// </summary>
        /// <param name="camera">RideID of the camera to set as main.</param>
        void SetMainCamera(RideID camera);

        /// <summary>
        /// Creates a new camera in the scene based on the given parameters.
        /// </summary>
        /// <param name="parameters">Parameters for the new camera</param>
        /// <returns>RideID of the created camera</returns>
        RideID CreateCamera(ICameraCreationParameters parameters);

        /// <summary>
        /// Removes a camera from the scene and internal system tracking.
        /// </summary>
        /// <param name="cameraId">The ID of the camera</param>
        void RemoveCamera(RideID cameraId);

        /// <summary>
        /// Returns the ICamera component associated with the specified camera ID.
        /// </summary>
        ICamera GetCamera(RideID cameraId);

        /// <summary>
        /// Returns true if the object has a camera controller or input-controllable camera registered.
        /// </summary>
        bool ContainsRideCameraController(RideID id);


        // ------------------------------------------------------------
        // Activation and Input Control
        // ------------------------------------------------------------

        /// <summary>
        /// Sets the specified camera's active state.
        /// </summary>
        /// <param name="camera">The camera to activate</param>
        /// <param name="activate">true if you want the camera activated, false if you want it deactivated</param>
        void SetActive(RideID camera, bool activate);

        /// <summary>
        /// Sets all cameras associated with the given owner active or inactive.
        /// </summary>
        void SetActiveCameraOwner(RideID owner, bool activate);

        /// <summary>
        /// Activates the specified camera, optionally deactivating all others.
        /// </summary>
        /// <param name="camera">The camera to activate</param>
        /// <param name="deactivateAllOtherCameras">true if you want all other cameras deactivated,
        /// otherwise any camera that is currently active will stay active</param>
        void ActivateCamera(RideID camera, bool deactivateAllOtherCameras);

        /// <summary>
        /// Returns true if the specified camera is currently active in the scene.
        /// </summary>
        bool IsCameraActive(RideID camera);

        /// <summary>
        /// Returns true if the camera is controllable by user input.
        /// </summary>
        bool IsUserControllable(RideID camera);

        /// <summary>
        /// Returns whether user input is currently enabled for the camera.
        /// </summary>
        bool IsInputEnabled(RideID camera);

        /// <summary>
        /// Enables or disables user input for the camera.
        /// </summary>
        void EnableInput(RideID camera, bool enabled);


        // ------------------------------------------------------------
        // Camera View and Targeting
        // ------------------------------------------------------------

        /// <summary>
        /// Switches to a different view index on the camera, or cycles to the next view if no index is specified.
        /// </summary>
        void SwitchView(RideID cameraId, int idx = -1);

        /// <summary>
        /// Gets the current camera view mode.
        /// </summary>
        RideCameraView GetCurrentView(RideID camera);

        /// <summary>
        /// Returns the name of the currently active view for the specified camera.
        /// Returns null if the view is not set or the camera is invalid.
        /// </summary>
        string GetCurrentViewName(RideID camera);

        /// <summary>
        /// Returns the number of view modes available on the specified camera.
        /// </summary>
        int GetViewCount(RideID camera);

        /// <summary>
        /// Returns the name or label of the specified view index for the camera.
        /// Returns null if the index is invalid.
        /// </summary>
        string GetViewName(RideID camera, int index);

        /// <summary>
        /// Assigns a new target for the camera to look at.
        /// </summary>
        void SetLookAt(RideID camera, RideID targetEntity);

        /// <summary>
        /// Clears the current look-at target, reverting to the camera's own transform.
        /// </summary>
        void ClearLookAt(RideID camera);

        /// <summary>
        /// Gets the current LookAt target for the camera, if any.
        /// Returns RideID.Null if no LookAt target is assigned.
        /// </summary>
        RideID GetLookAtTarget(RideID camera);


        // ------------------------------------------------------------
        // Camera Controls and Orientation
        // ------------------------------------------------------------

        /// <summary>
        /// Rotates the camera using a specified input delta and sensitivity factor.
        /// </summary>
        void RotateCamera(RideID cameraId, RideVector2 rotationDelta, float lookSensitivity = 10.0f);


        // ------------------------------------------------------------
        // Coordinate and Raycasting Conversions
        // ------------------------------------------------------------

        /// <summary>
        /// Converts a world-space position into normalized viewport coordinates (0 to 1 range).
        /// This is useful for determining whether a point is within the camera's visible area.
        /// </summary>
        /// <param name="camera">The camera performing the conversion.</param>
        /// <param name="worldPos">The position in world space.</param>
        /// <returns>Normalized viewport coordinates (x, y, z), where z is the depth from the camera.</returns>
        RideVector3 ConvertToCoordinates(RideID camera, RideVector3 worldPos);

        /// <summary>
        /// Converts a world-space position to screen coordinates.
        /// </summary>
        /// <param name="pos">World-space position to convert.</param>
        /// <returns>Screen-space position in pixels relative to screen dimensions.</returns>
        RideVector3 WorldToScreenPoint(RideVector3 pos);

        /// <summary>
        /// Converts a world-space position to screen coordinates using the specified camera.
        /// </summary>
        /// <param name="camera">The camera performing the conversion.</param>
        /// <param name="pos">World-space position to convert.</param>
        /// <returns>Screen-space position in pixels relative to screen dimensions.</returns>
        RideVector3 WorldToScreenPoint(RideID camera, RideVector3 pos);

        /// <summary>
        /// Converts a screen-space position to world-space using the specified camera.
        /// The Z component of the input specifies the depth from the camera.
        /// </summary>
        /// <param name="camera">The camera to use for the conversion.</param>
        /// <param name="pos">Screen-space position (X, Y in pixels, Z as depth).</param>
        /// <returns>World-space position corresponding to the screen-space input.</returns>
        RideVector3 ScreenToWorldPoint(RideID camera, RideVector3 pos);

        /// <summary>
        /// Creates a ray from the main camera through the specified screen-space point.
        /// The point should be in pixel coordinates relative to the screen.
        /// </summary>
        /// <param name="pos">Screen-space position in pixels.</param>
        /// <returns>A ray from the camera through the specified screen point into the world.</returns>
        RideRay ScreenPointToRay(RideVector3 pos);

        /// <summary>
        /// Creates a ray from the specified camera through the specified screen-space point.
        /// The point should be in pixel coordinates relative to the screen.
        /// </summary>
        /// <param name="camera">The camera generating the ray.</param>
        /// <param name="pos">Screen-space position in pixels.</param>
        /// <returns>A ray from the camera through the specified screen point into the world.</returns>
        RideRay ScreenPointToRay(RideID camera, RideVector3 pos);

        /// <summary>
        /// Returns a ray pointing in the direction of the camera's view, centered on screen.
        /// </summary>
        RideRay GetRideCameraControllerViewRay(RideID id);


        // ------------------------------------------------------------
        // Camera Field of View and Clipping
        // ------------------------------------------------------------

        /// <summary>
        /// Gets the field of view in degrees for the specified camera.
        /// </summary>
        float GetFieldOfView(RideID camera);

        /// <summary>
        /// Sets the field of view in degrees for the specified camera.
        /// </summary>
        void SetFieldOfView(RideID camera, float fov);

        /// <summary>
        /// Gets the near clipping plane distance of the specified camera.
        /// </summary>
        float GetNearClipPlane(RideID camera);

        /// <summary>
        /// Gets the far clipping plane distance of the specified camera.
        /// </summary>
        float GetFarClipPlane(RideID camera);


        // ------------------------------------------------------------
        // Geometry and Visibility Queries
        // ------------------------------------------------------------

        /// <summary>
        /// Returns the view frustum (set of planes) representing the camera's visible volume.
        /// </summary>
        RideFrustum GetViewFrustum(RideID camera);

        /// <summary>
        /// Returns a bounding box representing the 3D viewport of the camera between the specified near and far planes.
        /// </summary>
        RideBounds GetViewportBounds(RideID camera, float near, float far);
    }
}
