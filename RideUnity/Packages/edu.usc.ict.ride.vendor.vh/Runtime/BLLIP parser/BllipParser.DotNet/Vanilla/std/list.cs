using System;
using System.Collections;
using System.Collections.Generic;

using size_t = System.UInt64;


namespace BllipParser.DotNet.Vanilla
{
    // c++ list
    public class list<T>
    {
        LinkedList<T> m_list = new LinkedList<T>();


        public list() { }


        public LinkedList<T> GetList() { return m_list; }


        public int Count { get { return m_list.Count; } }
        public LinkedListNode<T> Find(T item) { return m_list.Find(item); }
        public LinkedListNode<T> First { get { return m_list.First; } }
        public LinkedListNode<T> Last { get { return m_list.Last; } }


        // std::list functions
        public void clear() { m_list.Clear(); }
        public bool empty() { return m_list.Count == 0; }
        public bool find(T item) { return m_list.Contains(item); }
        public LinkedListNode<T> front() { return m_list.First; }
        public LinkedListNode<T> insert(LinkedListNode<T> position, T value) { return m_list.AddBefore(position, value); }
        public void pop_front() { m_list.RemoveFirst(); }
        public void push_back(T item) { m_list.AddLast(item); }
        public void push_front(T item) { m_list.AddFirst(item); }
        public size_t size() { return (size_t)m_list.Count; }
    }
}
