using System;

namespace Ride
{
    /// <summary>
    /// A unique identifier type used throughout the RIDE codebase to represent objects, entities, and systems.
    ///
    /// <para>
    /// RIDE uses an ID-based architecture, inspired by ECS (Entity Component System) principles. 
    /// Rather than maintaining direct object references, components and systems communicate using 
    /// RideIDs as keys into internal registries or simulation tables.
    /// </para>
    ///
    /// <para>
    /// The RideID encapsulates an <c>id</c> value and a <c>version</c>. This allows ID reuse over time 
    /// without introducing bugs, because systems can validate whether an ID still refers to the original object. 
    /// This is a common pattern in game engines to prevent stale references from being used.
    /// </para>
    ///
    /// <para>
    /// By default, a RideID with <c>id = 0</c> and <c>version = 0</c> is considered the "null" or "uninitialized" state. 
    /// Future versions may switch this to <c>id = -1</c> for consistency with Unity APIs and general conventions.
    /// </para>
    ///
    /// <remarks>
    /// Typical use cases:
    /// <list type="bullet">
    /// <item>Referencing units, agents, cameras, or terrain tiles</item>
    /// <item>Passing IDs between systems (e.g., input, UI, simulation)</item>
    /// <item>Storing ID lists for selections or group behaviors</item>
    /// </list>
    /// </remarks>
    /// </summary>
    [Serializable]
    public struct RideID : IEquatable<RideID>
    {
        /// <summary>
        /// The version number used for newly created RideIDs.
        /// </summary>
        public const int CurrentVersion = 0;

        /// <summary>
        /// The default "null" or unassigned RideID. May be replaced with 'Invalid' in future revisions.
        /// </summary>
        // TODO - replace 'Null' with 'Invalid'
        //public static readonly RideID Invalid = new RideID(-1, CurrentVersion);
        public static readonly RideID Null = new RideID(0, CurrentVersion);

        /// <summary>
        /// The numeric identifier for the object. This is assigned by the IdentityFactory or internal system.
        /// </summary>
        // TODO - replace with proper casing
        public int id { get; }

        /// <summary>
        /// The version of the ID, used to detect reuse or stale references.
        /// </summary>
        public int version { get; }


        /// <summary>
        /// Constructs a RideID with the specified id and the default version.
        /// </summary>
        /// <param name="_id">The object identifier.</param>
        public RideID(int _id) : this(_id, CurrentVersion) { }

        /// <summary>
        /// Constructs a RideID with a specific id and version.
        /// </summary>
        /// <param name="_id">The object identifier.</param>
        /// <param name="_version">The version number to associate with the ID.</param>
        public RideID(int _id, int _version)
        {
            id = _id;
            version = _version;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(id, version);

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is RideID other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(RideID other) => id == other.id && version == other.version;

        /// <summary>
        /// Checks whether two RideIDs refer to the same object and version.
        /// </summary>
        public static bool operator ==(RideID left, RideID right) => left.Equals(right);

        /// <summary>
        /// Checks whether two RideIDs differ in id or version.
        /// </summary>
        public static bool operator !=(RideID left, RideID right) => !left.Equals(right);

        /// <summary>
        /// Returns a readable string representation of the RideID, including its id and version.
        /// </summary>
        public override string ToString() => $"RideID(Id={id}, Version={version})";

        /// <summary>
        /// Allows explicit conversion from an int to a RideID. Version will be set to CurrentVersion.
        /// </summary>
        /// <param name="id">The raw integer ID to wrap.</param>
        public static explicit operator RideID(int id) => new RideID(id);
    }
}
