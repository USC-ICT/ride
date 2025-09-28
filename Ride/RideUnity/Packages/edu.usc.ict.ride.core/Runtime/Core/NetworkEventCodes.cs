namespace Ride.Networking
{
    public enum NetworkEventCode : byte
    {
        START = 1,

        /// <summary>
        /// Common TSS networking events
        /// </summary>
        TSS_START = START,
        DestructTerrain,

        TSS_END = 100,

        /// <summary>
        /// Start from this value to create events that are custom to your scenario
        /// </summary>
        CUSTOM_START = TSS_END + 1,

        END = 199,
        CUSTOM_END = END
    }
}
