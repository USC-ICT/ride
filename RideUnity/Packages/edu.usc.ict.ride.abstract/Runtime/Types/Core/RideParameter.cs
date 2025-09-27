using System;

namespace Ride
{
    public enum DataType { None, Boolean, Integer, Float, String }

    [Serializable]
    public struct RideParameter
    {
        public string tag;
        public DataType valueType;
        public string value;
    }
}
