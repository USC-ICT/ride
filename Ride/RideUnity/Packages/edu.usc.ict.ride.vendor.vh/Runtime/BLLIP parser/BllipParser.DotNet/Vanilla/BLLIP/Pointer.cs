using System;


namespace BllipParser.DotNet.Vanilla
{
    // this attempts to act as a C++ pointer.  It has an array and an offset into that container.
    public class Pointer<T>
    {
        protected T [] m_memory;
        protected int m_offset;


        public Pointer() { }
        public Pointer(T [] memory, int offset = 0) { m_memory = memory; m_offset = offset; }
        public Pointer(Pointer<T> pointer, int offset = 0) : this(pointer.m_memory, pointer.m_offset + offset) { }


        //public virtual MemoryContainer<T> Buffer { get { return m_memory; } }
        //public virtual int Offset { get { return m_offset; } }
        //public virtual int Count { get { return m_memory.Count; } }


        public virtual T this[int i] { get { return m_memory[m_offset + i]; } set { m_memory[m_offset + i] = value; } }
        public virtual T this[UInt64 i] { get { return m_memory[m_offset + (int)i]; } set { m_memory[m_offset + (int)i] = value; } }


        //public static Pointer<T> operator +(Pointer<T> left, int right) { return new Pointer<T>(left, right); }
        //public static Pointer<T> operator +(Pointer<T> left, UInt32 right) { return new Pointer<T>(left, (int)right); }
        //public static Pointer<T> operator +(Pointer<T> left, Pointer<T> right) { if (!left.Buffer.MemoryEquals(right.Buffer)) return null; return new Pointer<T>(left.Buffer, left.Offset + right.Offset); }
        //public static Pointer<T> operator ++(Pointer<T> left) { left.m_offset++; return left; }
        //public static Pointer<T> operator -(Pointer<T> left, int right) { return new Pointer<T>(left, -right); }
        //public static Pointer<T> operator -(Pointer<T> left, UInt32 right) { return new Pointer<T>(left, -(int)right); }
        //public static Pointer<T> operator -(Pointer<T> left, Pointer<T> right) { if (!left.Buffer.MemoryEquals(right.Buffer)) return null; return new Pointer<T>(left.Buffer, left.Offset - right.Offset); }
        //public static Pointer<T> operator --(Pointer<T> left) { left.m_offset--; return left; }


        public virtual T op { get { return this[0]; } set { this[0] = value; } }


        //public virtual bool CompareTo(Pointer<T> right, int count)
        //{
        //    for (int i = 0; i < count; i++)
        //    {
        //        if (!this[i].Equals(right[i]))
        //            return false;
        //    }
        //    return true;
        //}

        //public virtual void CopyTo(int srcStart, Pointer<T> dest, int destStart, int count) { m_memory.CopyTo(m_offset + srcStart, dest.Buffer, dest.m_offset + destStart, count); }
        //public virtual void CopyTo(int srcStart, Span<T> span, int count) { m_memory.CopyTo(srcStart, span, count); }

        //public virtual void Fill(T value, int count) { Fill(value, 0, count); }
        //public virtual void Fill(T value, int start, int count) { m_memory.Fill(value, m_offset + start, count); }
    }


    // this class holds a Pointer reference so that if the pointer changes, this class will track it.
    public class PointerRef<T>
    {
        public Pointer<T> m_pointer;

        public PointerRef() { }
        public PointerRef(Pointer<T> pointer) { m_pointer = pointer; }
    }
}
