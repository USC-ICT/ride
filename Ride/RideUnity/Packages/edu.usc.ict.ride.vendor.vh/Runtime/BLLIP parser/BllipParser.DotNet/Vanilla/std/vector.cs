using System;
using System.Collections;
using System.Collections.Generic;

using size_t = System.UInt64;


namespace BllipParser.DotNet.Vanilla
{
    // c++ vector
    public class vector<T>
    {
        List<T> m_list = new List<T>();


        public vector() { }


        public List<T> GetList() { return m_list; }


        public void AddRange(IEnumerable<T> collection) { m_list.AddRange(collection); }
        public int Count { get { return m_list.Count; } }


        // std::vector functions
        public T this[int index] { get { return m_list[index]; } set { m_list[index] = value; } }
        public void assign(IEnumerable<T> collection) { clear();  AddRange(collection); }
        public T at(int index) { return m_list[index]; }
        public T back() { return empty() ? default : this[Count - 1]; }
        public void clear() { m_list.Clear(); }
        public bool empty() { return Count == 0; }
        public void pop_back() { if (Count > 0) { m_list.RemoveAt(Count - 1); } }
        public void push_back(T item) { m_list.Add(item); }
        public void reserve(size_t value) { m_list.Capacity = (int)value; }
        public size_t size() { return (size_t)m_list.Count; }
    }
}
