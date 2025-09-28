namespace Ride
{
    /// <summary>
    /// Represents a platform-agnostic wrapper for Unity materials.
    /// Provides typed access to common shader properties used in rendering systems.
    ///
    /// This abstraction allows materials to be queried and modified without directly referencing UnityEngine.
    /// See Unity's native Material API: https://docs.unity3d.com/ScriptReference/Material.html
    /// </summary>
    public interface IMaterial : IIdentity
    {
        /// <summary>
        /// Retrieves a color property from the material.
        /// </summary>
        /// <param name="key">Shader property name.</param>
        /// <returns>Color value associated with the key.</returns>
        RideColor GetColor(string key);

        /// <summary>
        /// Retrieves a vector property from the material.
        /// </summary>
        /// <param name="key">Shader property name.</param>
        /// <returns>Vector value associated with the key.</returns>
        RideVector4 GetVector(string key);

        /// <summary>
        /// Retrieves a float property from the material.
        /// </summary>
        /// <param name="key">Shader property name.</param>
        /// <returns>Float value associated with the key.</returns>
        float GetFloat(string key);

        /// <summary>
        /// Retrieves a texture property from the material.
        /// </summary>
        /// <param name="key">Shader property name.</param>
        /// <returns>Texture wrapper containing the Unity texture.</returns>
        RideTexture GetTexture(string key);

        /// <summary>
        /// Assigns a color to the material.
        /// </summary>
        /// <param name="key">Shader property name.</param>
        /// <param name="color">Color value to assign.</param>
        void SetColor(string key, RideColor color);

        /// <summary>
        /// Assigns a vector to the material.
        /// </summary>
        /// <param name="key">Shader property name.</param>
        /// <param name="vector">Vector value to assign.</param>
        void SetVector(string key, RideVector4 vector);

        /// <summary>
        /// Assigns a float to the material.
        /// </summary>
        /// <param name="key">Shader property name.</param>
        /// <param name="value">Float value to assign.</param>
        void SetFloat(string key, float value);

        /// <summary>
        /// Assigns a texture to the material.
        /// </summary>
        /// <param name="key">Shader property name.</param>
        /// <param name="texture">Texture wrapper to assign.</param>
        void SetTexture(string key, RideTexture texture);
    }
}
