namespace Ride.Networking
{
    /// <summary>
    /// Describes how a raised network event should be cached or otherwise treated by the networking backend.
    /// </summary>
    /// <remarks>
    /// These values mirror Photon-style event caching semantics, where an event can be sent immediately,
    /// stored for later replay to room participants, or used to mutate the cached event history itself.
    /// Relevant docs:
    /// https://doc.photonengine.com/realtime/current/gameplay/cached-events
    /// </remarks>
    public enum EventCaching : byte
    {
        /// <summary>Default value (not sent).</summary>
        DoNotCache = 0,

        /// <summary>Adds an event to the room's cache</summary>
        AddToRoomCache = 4,

        /// <summary>Adds this event to the cache for actor 0 (becoming a "globally owned" event in the cache).</summary>
        AddToRoomCacheGlobal = 5,

        /// <summary>Remove fitting event from the room's cache.</summary>
        RemoveFromRoomCache = 6,

        /// <summary>Removes events of players who already left the room (cleaning up).</summary>
        RemoveFromRoomCacheForActorsLeft = 7,

        /// <summary>Increase the index of the sliced cache.</summary>
        SliceIncreaseIndex = 10,

        /// <summary>Set the index of the sliced cache. You must set RaiseEventOptions.CacheSliceIndex for this.</summary>
        SliceSetIndex = 11,

        /// <summary>Purge cache slice with index. Exactly one slice is removed from cache. You must set RaiseEventOptions.CacheSliceIndex for this.</summary>
        SlicePurgeIndex = 12,

        /// <summary>Purge cache slices with specified index and anything lower than that. You must set RaiseEventOptions.CacheSliceIndex for this.</summary>
        SlicePurgeUpToIndex = 13,
    }

    /// <summary>
    /// Collects optional delivery settings for a raised network event, such as caching behavior,
    /// interest-group routing, explicit target actors, and receiver-group selection.
    /// </summary>
    /// <remarks>
    /// Ride surfaces this as a backend-agnostic event-delivery options container on its networking APIs.
    /// In practice, callers use it to control who receives an event and whether that event should persist
    /// in room cache for late joiners or other cache-management workflows.
    /// Relevant docs:
    /// https://doc.photonengine.com/realtime/current/reference/dotnet-api/class_photon_1_1_realtime_1_1_raise_event_options.html
    /// https://doc.photonengine.com/realtime/current/gameplay/interestgroups
    /// </remarks>
    public class RaiseEventOptions
    {
        /// <summary>Default options: CachingOption: DoNotCache, InterestGroup: 0, targetActors: null, receivers: Others, sequenceChannel: 0.</summary>
        public readonly static RaiseEventOptions Default = new RaiseEventOptions();

        /// <summary>Defines if the server should simply send the event, put it in the cache or remove events that are like this one.</summary>
        /// <remarks>
        /// When using option: SliceSetIndex, SlicePurgeIndex or SlicePurgeUpToIndex, set a CacheSliceIndex. All other options except SequenceChannel get ignored.
        /// </remarks>
        public EventCaching CachingOption;

        /// <summary>The number of the Interest Group to send this to. 0 goes to all users but to get 1 and up, clients must subscribe to the group first.</summary>
        public byte InterestGroup;

        /// <summary>A list of Player.ActorNumbers to send this event to. You can implement events that just go to specific users this way.</summary>
        public int[] TargetActors;

        /// <summary>Sends the event to All, MasterClient or Others (default). Be careful with MasterClient, as the client might disconnect before it got the event and it gets lost.</summary>
        public ReceiverGroup Receivers;

        /// <summary> Optional flags to be used in Photon client SDKs with Op RaiseEvent and Op SetProperties.</summary>
        /// <remarks>Introduced mainly for webhooks 1.2 to control behavior of forwarded HTTP requests.</remarks>
        //public WebFlags Flags = WebFlags.Default;

        ///// <summary>Used along with CachingOption SliceSetIndex, SlicePurgeIndex or SlicePurgeUpToIndex if you want to set or purge a specific cache-slice.</summary>
        //public int CacheSliceIndex;
    }
}
