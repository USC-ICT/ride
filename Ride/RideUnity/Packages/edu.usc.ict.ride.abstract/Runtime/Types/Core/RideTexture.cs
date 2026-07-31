using System;

namespace Ride
{
    /// <summary>
    /// Represents a wrapper around UnityEngine.Texture, providing an associated <see cref="RideID"/> for use in RIDE's ID-based architecture.
    /// This class is intended to decouple higher-level RIDE systems from direct Unity texture references, enabling identity-based lookup,
    /// abstraction, and potential future extension (e.g., metadata, lifecycle handling).
    /// This is a lightweight container - it does not perform memory management or asset loading.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/Texture.html">UnityEngine.Texture</a>.
    /// </summary>
    [Serializable]
    public class RideTexture : IIdentity
    {
        public RideID id { get; set; }
        public string name => texture.name;

        public UnityEngine.Texture texture;

        public RideTexture() { }
        public RideTexture(RideID _id, UnityEngine.Texture _texture)
        {
            id = _id;
            texture = _texture;
        }
    }
}
