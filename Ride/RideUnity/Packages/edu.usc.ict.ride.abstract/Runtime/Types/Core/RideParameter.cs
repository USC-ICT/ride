using System;

namespace Ride
{
    /// <summary>Identifies the serialized value type stored by a <see cref="RideParameter"/>.</summary>
    public enum DataType
    {
        /// <summary>No value type has been assigned.</summary>
        None,
        /// <summary>A Boolean value.</summary>
        Boolean,
        /// <summary>A signed integer value.</summary>
        Integer,
        /// <summary>A floating-point value.</summary>
        Float,
        /// <summary>A string value.</summary>
        String
    }

    /// <summary>Stores a named, string-serialized metadata value with an explicit data type.</summary>
    /// <remarks>Used primarily for scenario entity tags and other portable configuration metadata.</remarks>
    [Serializable]
    public struct RideParameter
    {
        /// <summary>The parameter name or metadata tag.</summary>
        public string tag;
        /// <summary>The type used to interpret <see cref="value"/>.</summary>
        public DataType valueType;
        /// <summary>The serialized parameter value.</summary>
        public string value;
    }
}
