using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Stores and retrieves runtime-defined variables by name or RideID.
    /// Useful for tool systems, configuration, or component-level metadata.
    /// </summary>
    public interface IVariableStore
    {
        /// <summary>
        /// Sets the value of a named variable. Creates the variable if it does not already exist.
        /// </summary>
        /// <typeparam name="T">The data type of the variable.</typeparam>
        /// <param name="name">The name of the variable.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>The RideID associated with the variable, created or reused.</returns>
        RideID Set<T>(string name, T value);

        /// <summary>
        /// Sets the value of a variable using a RideID key. Creates the variable if it does not already exist.
        /// </summary>
        /// <typeparam name="T">The data type of the variable.</typeparam>
        /// <param name="v">The RideID key of the variable.</param>
        /// <param name="value">The value to assign.</param>
        /// <returns>The RideID associated with the variable (same as <paramref name="v"/>).</returns>
        RideID Set<T>(RideID v, T value);

        /// <summary>
        /// Retrieves the value of a named variable. Returns default(T) if the variable is missing or type mismatched.
        /// </summary>
        /// <typeparam name="T">The expected type of the variable.</typeparam>
        /// <param name="name">The name of the variable.</param>
        /// <returns>The value of the variable, or default(T) if missing.</returns>
        T Get<T>(string name);

        /// <summary>
        /// Retrieves the value of a variable using a RideID key. Returns default(T) if the variable is missing or type mismatched.
        /// </summary>
        /// <typeparam name="T">The expected type of the variable.</typeparam>
        /// <param name="v">The RideID key of the variable.</param>
        /// <returns>The value of the variable, or default(T) if missing.</returns>
        T Get<T>(RideID v);

        /// <summary>
        /// Returns true if a variable with the given RideID key exists.
        /// </summary>
        /// <param name="v">The RideID key to check.</param>
        bool Contains(RideID v);

        /// <summary>
        /// Returns true if a variable with the given name exists.
        /// </summary>
        /// <param name="name">The name of the variable to check.</param>
        bool Contains(string name);

        /// <summary>
        /// Removes the variable associated with the given RideID key.
        /// </summary>
        /// <param name="v">The RideID key of the variable to remove.</param>
        void Remove(RideID v);

        /// <summary>
        /// Removes the variable with the specified name.
        /// </summary>
        /// <param name="name">The name of the variable to remove.</param>
        void Remove(string name);

        /// <summary>
        /// Returns all variable names currently stored in this variable store.
        /// </summary>
        IEnumerable<string> GetVariableNames();

        /// <summary>
        /// Removes all variables from the store.
        /// </summary>
        void Clear();
    }
}
