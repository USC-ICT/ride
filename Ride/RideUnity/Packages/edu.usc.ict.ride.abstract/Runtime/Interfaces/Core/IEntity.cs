using System;

namespace Ride.Entities
{
    /// <summary>
    /// Defines a simulation entity that can be uniquely identified and labeled with attribute flags.
    /// Entities may represent agents, vehicles, structures, or other interactive elements.
    /// </summary>
    public interface IEntity : IIdentity
    {
        /// <summary>
        /// Gets or sets the attribute flags associated with this entity.
        /// See <see cref="EntityAttributes"/> for common values.
        /// </summary>
        EntityAttributes attributes { get; set; }

        /// <summary>
        /// Checks whether this entity has all of the specified attribute flags.
        /// Equivalent to: <c>(Attributes & att) == att</c>
        /// </summary>
        /// <param name="att">One or more <see cref="EntityAttributes"/> flags to test.</param>
        /// <returns>True if all specified flags are present; otherwise false.</returns>
        bool HasAttributes(EntityAttributes att);
    }
}
