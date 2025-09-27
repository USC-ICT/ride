using System;

using size_t = System.UInt64;


namespace BllipParser.DotNet.Vanilla
{
    public class ECString : IEquatable<ECString>, IComparable<ECString>
    {
        string m_string;


        public ECString(string s = "")
        {
            m_string = s;
        }

        public ECString(ECString s)
        {
            m_string = s.m_string;
        }

        public ECString(in ECString str, size_t pos, size_t len = size_t.MaxValue)
        {
            m_string = str.m_string.Substring((int)pos, (int)len);
        }

        public ECString(size_t n, char c)
        {
            m_string = "";
            for (int i = 0; i < (int)n; i++)
            {
                m_string += c;
            }
        }


        public bool Equals(ECString value) { return m_string.Equals(value); }
        public override bool Equals(object obj) { return m_string.Equals(obj); }
        public override int GetHashCode() { return m_string.GetHashCode(); }

        public override string ToString() { return m_string; }

        public int CompareTo(object obj) { return m_string.CompareTo(obj.ToString()); }
        public int CompareTo(ECString other) { return m_string.CompareTo(other.m_string); }


        public static implicit operator string(ECString s) => s.m_string;
        public static implicit operator ECString(string s) => new ECString(s);


        public static bool operator ==(ECString a, ECString b) { return a.m_string == b.m_string; }
        public static bool operator !=(ECString a, ECString b) { return a.m_string != b.m_string; }


        public bool Contains(string value) { return m_string.Contains(value); }
        public ECString ToLower() { return m_string.ToLower(); }


        // std::string functions
        public char this[int index] { get { return m_string[index]; } }
        public size_t length() { return (size_t)m_string.Length; }
        public size_t rfind(ECString str) { return (size_t)m_string.LastIndexOf(str); }
        public size_t size() { return (size_t)m_string.Length; }
        public ECString substr(size_t pos, size_t len) { return m_string.Substring((int)pos, (int)len); }
    }
}
