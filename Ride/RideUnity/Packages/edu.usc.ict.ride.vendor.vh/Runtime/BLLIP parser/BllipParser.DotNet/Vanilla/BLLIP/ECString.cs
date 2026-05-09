using System;

using size_t = System.UInt64;


namespace BllipParser.DotNet.Vanilla
{
    public sealed class ECString : IEquatable<ECString>, IComparable<ECString>
    {
        readonly string m_string;


        public ECString(string s = "") => m_string = s;
        public ECString(ECString s) => m_string = s.m_string;

        public ECString(in ECString str, size_t pos, size_t len = size_t.MaxValue)
        {
            int start = (int)pos;
            int maxLen = str.m_string.Length - start;

            if (start < 0 || start > str.m_string.Length)
                throw new ArgumentOutOfRangeException(nameof(pos));

            int length;
            if (len == size_t.MaxValue)
                length = maxLen;
            else
                length = Math.Min((int)len, maxLen);

            m_string = str.m_string.Substring(start, length);
        }

        public ECString(size_t n, char c) => m_string = new string(c, (int)n);


        public bool Equals(ECString other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(m_string, other.m_string, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is ECString other && Equals(other);

        public override int GetHashCode() => m_string?.GetHashCode() ?? 0;

        public override string ToString() => m_string;

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(this, obj)) return 0;
            if (obj is ECString other) return CompareTo(other);
            if (obj is string s) return string.Compare(m_string, s, StringComparison.Ordinal);
            return string.Compare(m_string, obj?.ToString(), StringComparison.Ordinal);
        }

        public int CompareTo(ECString other)
        {
            if (other is null) return 1; // this > null
            return string.Compare(m_string, other.m_string, StringComparison.Ordinal);
        }


        public static implicit operator string(ECString s) => s.m_string;
        public static implicit operator ECString(string s) => new ECString(s);


        public static bool operator ==(ECString a, ECString b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return string.Equals(a.m_string, b.m_string, StringComparison.Ordinal);
        }
        public static bool operator !=(ECString a, ECString b) => !(a == b);


        public bool Contains(string value) =>m_string.Contains(value);
        public ECString ToLower() => m_string.ToLower();


        // std::string functions
        public char this[int index] => m_string[index];
        public size_t length() => (size_t)m_string.Length;
        public size_t rfind(ECString str)
        {
            if (str is null) return size_t.MaxValue;
            int idx = m_string.LastIndexOf(str.m_string, StringComparison.Ordinal);
            return idx < 0 ? size_t.MaxValue : (size_t)idx;
        }
        public size_t size() => (size_t)m_string.Length;

        public ECString substr(size_t pos, size_t len)
        {
            int start = (int)pos;
            int maxLen = m_string.Length - start;

            if (start < 0 || start > m_string.Length)
                throw new ArgumentOutOfRangeException(nameof(pos));

            int length = Math.Min((int)len, maxLen);
            return new ECString(m_string.Substring(start, length));
        }
    }
}
