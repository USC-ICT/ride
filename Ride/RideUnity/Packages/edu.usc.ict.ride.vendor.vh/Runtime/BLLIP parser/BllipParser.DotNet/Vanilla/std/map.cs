using System;
using System.Collections;
using System.Collections.Generic;

using size_t = System.UInt64;


namespace BllipParser.DotNet.Vanilla
{
    // c++ map
    public class map<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        SortedDictionary<K, V> m_dictionary = new SortedDictionary<K, V>();


        public map() { }


        // IEnumerable
        IEnumerator IEnumerable.GetEnumerator() { return m_dictionary.GetEnumerator(); }
        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator() { return m_dictionary.GetEnumerator(); }


        public SortedDictionary<K, V> GetDictionary() { return m_dictionary; }

        public V this[K key] { get { return m_dictionary[key]; } set { m_dictionary[key] = value; } }


        // std::map functions
        public void clear() { m_dictionary.Clear(); }
        public V find(K key) { if (m_dictionary.TryGetValue(key, out V value)) return value; else return default; }
        public size_t size() { return (size_t)m_dictionary.Count; }
    }
}
