using System.Collections.Generic;
using System.Linq;

namespace Ride
{
    /// <summary>
    /// A standalone variable store for runtime-defined name/value pairs.
    /// Variables can be accessed by string name or RideID.
    /// </summary>
    public class VariableStore : IVariableStore
    {
        public class Attribute
        {
            public string name;
            public object value;
        }

        Dictionary<RideID, Attribute> m_idToAttribute = new Dictionary<RideID, Attribute>();
        Dictionary<string, RideID> m_nameToId = new Dictionary<string, RideID>();

        /// <inheritdoc />
        public RideID Set<T>(string name, T value) => SetInternal(name, value);

        /// <inheritdoc />
        public RideID Set<T>(RideID att, T value) => SetInternal(att, value);

        /// <inheritdoc />
        public T Get<T>(string name)
        {
            T value = default;
            if (!Contains(name))
                CreateMapping(name, default(T));

            value = (T)m_idToAttribute[m_nameToId[name]].value;
            return value;
        }

        /// <inheritdoc />
        public T Get<T>(RideID att)
        {
            T value = default;
            if (!Contains(att))
                RideLog.LogError($"Attribute with RideID {att} does not exist. Did you remove it?");
            else
                value = (T)m_idToAttribute[att].value;

            return value;
        }

        /// <inheritdoc />
        public bool Contains(string name) => m_nameToId.ContainsKey(name);

        /// <inheritdoc />
        public bool Contains(RideID att) => m_idToAttribute.ContainsKey(att);

        /// <inheritdoc />
        public void Remove(RideID att)
        {
            if (Contains(att))
            {
                m_nameToId.Remove(m_idToAttribute[att].name);
                m_idToAttribute.Remove(att);
            }
        }

        /// <inheritdoc />
        public void Remove(string name)
        {
            if (Contains(name))
            {
                RideID att = m_nameToId[name];
                m_nameToId.Remove(name);
                m_idToAttribute.Remove(att);
            }
        }

        /// <inheritdoc />
        public IEnumerable<string> GetVariableNames() => m_nameToId.Keys;

        /// <inheritdoc />
        public void Clear()
        {
            m_idToAttribute.Clear();
            m_nameToId.Clear();
        }

        private RideID SetInternal<T>(string name, T value)
        {
            RideID att = RideID.Null;
            if (Contains(name))
            {
                att = m_nameToId[name];
                SetInternal<T>(att, value);
            }
            else
            {
                att = CreateMapping(name, value);
            }

            return att;
        }

        private RideID SetInternal<T>(RideID att, T value)
        {
            if (Contains(att))
                m_idToAttribute[att].value = value;
            else
                RideLog.LogError($"Attribute with RideID {att} does not exist. Did you remove it?");

            return att;
        }

        private RideID CreateMapping(string name, object value)
        {
            RideID id = RideID.Null;
            if (Contains(name))
            {
                // variable already exists
                id = m_nameToId[name];
                m_idToAttribute[id].name = name;
                m_idToAttribute[id].value = value;
            }
            else
            {
                // new variable
                id = IdentityFactory.CreateId();
                m_nameToId.Add(name, id);
                m_idToAttribute.Add(id, new Attribute() { name = name, value = value });
            }

            return id;
        }
    }
}
