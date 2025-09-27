using System;
using System.Collections;
using System.Collections.Generic;


namespace BllipParser.DotNet.Vanilla
{
    // c++ set
    public class set<T> : IEnumerable<T>
    {
        HashSet<T> m_set = new HashSet<T>();


        public set() { }


        // IEnumerable
        IEnumerator IEnumerable.GetEnumerator() { return m_set.GetEnumerator(); }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() { return m_set.GetEnumerator(); }


        public HashSet<T> GetHashSet() { return m_set; }


        // std::set functions
        public bool insert(T item) { return m_set.Add(item); }
    }
}
