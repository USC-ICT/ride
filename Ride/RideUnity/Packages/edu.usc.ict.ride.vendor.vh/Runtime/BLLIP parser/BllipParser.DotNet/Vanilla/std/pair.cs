using System;


namespace BllipParser.DotNet.Vanilla
{
    // c++ pair
    public class pair<T, V>
    {
        Tuple<T, V> m_pair;


        public pair(T key, V value) { m_pair = Tuple.Create(key, value); }


        public T first { get { return m_pair.Item1; } }
        public V second { get { return m_pair.Item2; } }
    }
}
