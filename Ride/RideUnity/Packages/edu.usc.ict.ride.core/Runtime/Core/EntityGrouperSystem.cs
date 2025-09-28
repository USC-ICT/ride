using System.Collections.Generic;

namespace Ride.Entities
{
    public class EntityGrouperSystem : RideSystemMonoBehaviour, IGrouperSystem<RideID>
    {
        protected List<RideID> m_currentGroup = new List<RideID>();
        Dictionary<int, IEnumerable<RideID>> m_savedGroups = new Dictionary<int, IEnumerable<RideID>>();

        /// <summary>
        /// Set the group to the given list of objects
        /// </summary>
        /// <param name="objs">The group</param>
        virtual public void SetGroup(IEnumerable<RideID> entities)
        {
            ClearCurrentGroup();
            Add(entities);
        }

        /// <summary>
        /// Set the group to a saved group with the given groupId
        /// </summary>
        /// <param name="groupId">The saved group id</param>
        public void SetGroup(int groupId)
        {
            if (m_savedGroups.ContainsKey(groupId))
            {
                SetGroup(m_savedGroups[groupId]);
            }
            else
            {
                //Debug.LogErrorFormat("Cannot select group {0} because it doesn't exist", groupId);
            }
        }

        /// <summary>
        /// Add the obj to the group
        /// </summary>
        /// <param name="objs"></param>
        virtual public void Add(RideID entity)
        {
            if (!m_currentGroup.Contains(entity))
            {
                m_currentGroup.Add(entity);
            }
        }

        /// <summary>
        /// Add the objects to the group
        /// </summary>
        /// <param name="objs"></param>
        virtual public void Add(IEnumerable<RideID> entities)
        {
            // TODO: check for dups
            m_currentGroup.AddRange(entities);
        }

        /// <summary>
        /// Remove the obj from the group
        /// </summary>
        /// <param name="obj"></param>
        virtual public void Remove(RideID entity)
        {
            m_currentGroup.Remove(entity);
        }

        /// <summary>
        /// Clears the current group of objects
        /// </summary>
        virtual public void ClearCurrentGroup()
        {
            m_currentGroup.Clear();
        }

        /// <summary>
        /// Saves the current group of objects using the given groupId
        /// </summary>
        /// <param name="groupId">The id that represents the current group</param>
        public virtual void SaveGroup(int groupId)
        {
            SaveGroup(groupId, m_currentGroup);
        }

        /// <summary>
        /// Saves the given group of overs using the given group id
        /// </summary>
        /// <param name="groupId">The id that represents the current group</param>
        /// <param name="objs">The objects that will be grouped by the groupId</param>
        public virtual void SaveGroup(int groupId, IEnumerable<RideID> entities)
        {
            if (!m_savedGroups.ContainsKey(groupId))
            {
                m_savedGroups.Add(groupId, null);
            }

             m_savedGroups[groupId] = new List<RideID>(entities);
        }

        /// <summary>
        /// Returns the currently grouped objects
        /// </summary>
        /// <returns></returns>
        public IEnumerable<RideID> GetCurrentGroup()
        {
            return m_currentGroup;
        }

        /// <summary>
        /// Returns the groups of objects that share the same groupId
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public IEnumerable<RideID> GetGroup(int groupId)
        {
            return m_savedGroups[groupId];
        }
    }
}
