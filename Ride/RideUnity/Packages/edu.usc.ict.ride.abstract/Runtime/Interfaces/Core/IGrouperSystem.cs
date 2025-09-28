using System.Collections.Generic;

namespace Ride.Entities
{
    /// <summary>
    /// Allows for the grouping of objects
    /// </summary>
    /// <typeparam name="T">The type you want to group</typeparam>
    public interface IGrouperSystem<T> : IRideSystem
    {
        /// <summary>
        /// Set the group to the given list of objects
        /// </summary>
        /// <param name="objs">The group</param>
        void SetGroup(IEnumerable<T> objs);

        /// <summary>
        /// Set the group to a saved group with the given groupId
        /// </summary>
        /// <param name="groupId">The saved group id</param>
        void SetGroup(int groupId);

        /// <summary>
        /// Add the obj to the group
        /// </summary>
        /// <param name="objs"></param>
        void Add(T objs);

        /// <summary>
        /// Add the objects to the group
        /// </summary>
        /// <param name="objs"></param>
        void Add(IEnumerable<T> objs);

        /// <summary>
        /// Remove the obj from the group
        /// </summary>
        /// <param name="obj"></param>
        void Remove(T obj);

        /// <summary>
        /// Clears the current group of objects
        /// </summary>
        void ClearCurrentGroup();

        /// <summary>
        /// Saves the current group of objects using the given groupId
        /// </summary>
        /// <param name="groupId">The id that represents the current group</param>
        void SaveGroup(int groupId);

        /// <summary>
        /// Saves the given group of objects using the given group id
        /// </summary>
        /// <param name="groupId">The id that represents the current group</param>
        /// <param name="objs">The objects that will be grouped by the groupId</param>
        void SaveGroup(int groupId, IEnumerable<T> objs);

        /// <summary>
        /// Returns the currently grouped objects
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> GetCurrentGroup();

        /// <summary>
        /// Returns the groups of objects that share the same groupId
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        IEnumerable<T> GetGroup(int groupId);
    }
}
