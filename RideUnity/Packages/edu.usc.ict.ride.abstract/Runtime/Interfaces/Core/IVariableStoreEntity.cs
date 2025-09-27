using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Provides a dynamic runtime mechanism for associating and retrieving variables with Ride entities.
    /// Useful for metadata, tags, or runtime state that cannot be statically defined in the entity class.
    /// </summary>
    public interface IVariableStoreEntity
    {
        /// <summary>
        /// Sets the value of a named variable for the given entity. Creates the variable if it does not already exist.
        /// </summary>
        /// <typeparam name="T">The data type of the variable.</typeparam>
        /// <param name="entity">The RideID of the entity.</param>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>The same RideID used as input, for chaining or tracking.</returns>
        RideID Set<T>(RideID entity, string name, T value);

        /// <summary>
        /// Sets the value of a variable using a RideID-based variable key. Creates it if it does not exist.
        /// </summary>
        /// <typeparam name="T">The data type of the variable.</typeparam>
        /// <param name="entity">The RideID of the entity.</param>
        /// <param name="v">The RideID key of the variable.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>The same RideID used as input, for chaining or tracking.</returns>
        RideID Set<T>(RideID entity, RideID v, T value);

        /// <summary>
        /// Retrieves the value of a named variable for the given entity. Returns default(T) if not found or mismatched.
        /// </summary>
        /// <typeparam name="T">The expected type of the variable.</typeparam>
        /// <param name="entity">The RideID of the entity.</param>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The value of the variable, or default(T) if missing.</returns>
        T Get<T>(RideID entity, string name);

        /// <summary>
        /// Retrieves the value of a variable using a RideID-based key. Returns default(T) if not found or mismatched.
        /// </summary>
        /// <typeparam name="T">The expected type of the variable.</typeparam>
        /// <param name="entity">The RideID of the entity.</param>
        /// <param name="v">The RideID key of the variable.</param>
        /// <returns>The value of the variable, or default(T) if missing.</returns>
        T Get<T>(RideID entity, RideID v);

        /// <summary>
        /// Returns true if the specified entity has any variables associated with it.
        /// </summary>
        /// <param name="entity">The RideID of the entity.</param>
        bool ContainsEntity(RideID entity);

        /// <summary>
        /// Returns true if the specified entity has a variable with the given RideID key.
        /// </summary>
        /// <param name="entity">The RideID of the entity.</param>
        /// <param name="v">The RideID key of the variable.</param>
        bool ContainsVariable(RideID entity, RideID v);

        /// <summary>
        /// Returns true if the specified entity has a variable with the given name.
        /// </summary>
        /// <param name="entity">The RideID of the entity.</param>
        /// <param name="v">The name of the variable.</param>
        bool ContainsVariable(RideID entity, string v);

        /// <summary>
        /// Removes a variable associated with the entity using a RideID key.
        /// </summary>
        /// <param name="entity">The RideID of the entity.</param>
        /// <param name="v">The RideID key of the variable to remove.</param>
        void Remove(RideID entity, RideID v);

        /// <summary>
        /// Removes a variable associated with the entity using a variable name.
        /// </summary>
        /// <param name="entity">The RideID of the entity.</param>
        /// <param name="name">The name of the variable to remove.</param>
        void Remove(RideID entity, string name);

        /// <summary>
        /// Returns a list of all variable names associated with the specified entity.
        /// </summary>
        /// <param name="entity">The RideID of the entity.</param>
        IEnumerable<string> GetVariableNames(RideID entity);

        /// <summary>
        /// Removes all variables associated with all entities.
        /// </summary>
        void Clear();
    }
}
