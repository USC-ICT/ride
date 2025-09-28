using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Manages shader variable state.
    /// </summary>
    public interface IShaderSystem : IRideSystem
    {
        /// <summary>
        /// Adds material so that it becomes known and tracked by the system.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <param name="family">Optional parameter. Groups materials with the same family together in a dictionary for quick access.</param>
        /// <returns></returns>
        RideID AddMaterial(IMaterial material, string family = null);

        /// <summary>
        /// Gets material.
        /// </summary>
        /// <param name="materialId">RideID of the material.</param>
        /// <returns></returns>
        IMaterial GetMaterial(RideID materialId);

        /// <summary>
        /// Gets a list of materials within a family.
        /// </summary>
        /// <param name="family">The family name.</param>
        /// <returns></returns>
        HashSet<RideID> GetMaterialsByFamily(string family);

        /// <summary>
        /// Gets a color associated with a material.
        /// </summary>
        /// <param name="materialId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        RideColor GetMaterialColor(RideID materialId, string key);

        /// <summary>
        /// Gets a vector associated with a material.
        /// </summary>
        /// <param name="materialId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        RideVector4 GetMaterialVector(RideID materialId, string key);

        /// <summary>
        /// Gets a float associated with a material.
        /// </summary>
        /// <param name="materialId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        float GetMaterialFloat(RideID materialId, string key);

        /// <summary>
        /// Gets a texture associated with a material.
        /// </summary>
        /// <param name="materialId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        RideTexture GetMaterialTexture(RideID materialId, string key);

        /// <summary>
        ///
        /// </summary>
        /// <param name="materialId">The material id.</param>
        /// <param name="key">The shader variable to modify.</param>
        /// <param name="color">The value the shader variable will be modified with.</param>
        void SetMaterialColor(RideID materialId, string key, RideColor color);

        /// <summary>
        ///
        /// </summary>
        /// <param name="materialId">The material id.</param>
        /// <param name="key">The shader variable to modify.</param>
        /// <param name="vector">The value the shader variable will be modified with.</param>
        void SetMaterialVector(RideID materialId, string key, RideVector4 vector);

        /// <summary>
        ///
        /// </summary>
        /// <param name="materialId"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void SetMaterialFloat(RideID materialId, string key, float value);

        /// <summary>
        ///
        /// </summary>
        /// <param name="materialId"></param>
        /// <param name="key"></param>
        /// <param name="texture"></param>
        void SetMaterialTexture(RideID materialId, string key, RideTexture texture);
    }
}
