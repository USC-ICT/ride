using UnityEngine;

namespace Ride
{
    /// <summary>
    /// Unity-specific implementation of <see cref="IMaterial"/> that wraps a UnityEngine.Material.
    /// Used internally to bridge Unity's material and shader APIs with RIDE's rendering abstraction.
    ///
    /// For more information on Unity's material system, see:
    /// https://docs.unity3d.com/ScriptReference/Material.html
    /// </summary>
    public class MaterialUnity : IMaterial
    {
        Material m_material;


        public MaterialUnity(Material unityMaterial)
        {
            id = IdentityFactory.CreateId();
            m_material = unityMaterial;
        }

        public string name { get => m_material.name; set => m_material.name = value; }
        public RideID id { get; }

        public void SetColor(string key, RideColor color) => m_material.SetColor(key, color);
        public void SetVector(string key, RideVector4 vector) => m_material.SetVector(key, vector);
        public void SetFloat(string key, float value) => m_material.SetFloat(key, value);
        public void SetTexture(string key, RideTexture texture) => m_material.SetTexture(key, texture.texture);
        public RideColor GetColor(string key) => m_material.GetColor(key);
        public RideVector4 GetVector(string key) => m_material.GetVector(key);
        public float GetFloat(string key) => m_material.GetFloat(key);
        public RideTexture GetTexture(string key) => new RideTexture(id, m_material.GetTexture(key));
    }
}
