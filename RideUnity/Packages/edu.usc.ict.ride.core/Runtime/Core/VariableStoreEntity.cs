using System.Collections.Generic;

namespace Ride
{
    /// <summary>
    /// Manages a mapping of entities to variable stores. Each RideID maps to its own VariableStore.
    /// </summary>
    public class VariableStoreEntity : IVariableStoreEntity
    {
        Dictionary<RideID, VariableStore> m_entityVars = new Dictionary<RideID, VariableStore>();

        /// <inheritdoc />
        public RideID Set<T>(RideID entity, string name, T value)
        {
            Create(entity);
            return m_entityVars[entity].Set(name, value);
        }

        /// <inheritdoc />
        public RideID Set<T>(RideID entity, RideID v, T value)
        {
            Create(entity);
            return m_entityVars[entity].Set(v, value);
        }

        /// <inheritdoc />
        public T Get<T>(RideID entity, string name)
        {
            Create(entity);
            return m_entityVars[entity].Get<T>(name);
        }

        /// <inheritdoc />
        public T Get<T>(RideID entity, RideID v)
        {
            Create(entity);
            return m_entityVars[entity].Get<T>(v);
        }

        /// <inheritdoc />
        public bool ContainsEntity(RideID entity)
        {
            Create(entity);
            return m_entityVars.ContainsKey(entity);
        }

        /// <inheritdoc />
        public bool ContainsVariable(RideID entity, RideID v)
        {
            Create(entity);
            return m_entityVars[entity].Contains(v);
        }

        /// <inheritdoc />
        public bool ContainsVariable(RideID entity, string v)
        {
            Create(entity);
            return m_entityVars[entity].Contains(v);
        }

        /// <inheritdoc />
        public void Remove(RideID entity, RideID v)
        {
            if (Exists(entity))
                m_entityVars[entity].Remove(v);
        }

        /// <inheritdoc />
        public void Remove(RideID entity, string name)
        {
            if (Exists(entity))
                m_entityVars[entity].Remove(name);
        }

        /// <inheritdoc />
        public IEnumerable<string> GetVariableNames(RideID entity)
        {
            Create(entity);
            return m_entityVars[entity].GetVariableNames();
        }

        /// <inheritdoc />
        public void Clear() => m_entityVars.Clear();

        private bool Exists(RideID entity) => m_entityVars.ContainsKey(entity);

        /// <summary>
        /// Adds the entity to the dictionary if not already there
        /// </summary>
        /// <param name="entity"></param>
        private void Create(RideID entity)
        {
            if (!Exists(entity))
                m_entityVars.Add(entity, new VariableStore());
        }
    }
}
