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
        /// <inheritdoc/>
        public RideID id { get; set; }
        /// <inheritdoc/>
        public string name => texture.name;

        /// <summary>The wrapped Unity texture.</summary>
        public UnityEngine.Texture texture;

        /// <summary>Initializes an empty texture wrapper for serialization.</summary>
        public RideTexture() { }
        /// <summary>Initializes an identified Unity texture wrapper.</summary>
        /// <param name="_id">The RIDE identifier.</param>
        /// <param name="_texture">The Unity texture.</param>
        public RideTexture(RideID _id, UnityEngine.Texture _texture)
        {
            id = _id;
            texture = _texture;
        }
    }
}
