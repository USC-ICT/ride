using System;

namespace Ride
{
    /// <summary>
    /// Represents a base class for all session events recorded and replayed in a Ride session.
    /// Events contain a timestamp relative to the session and a reference to a session object via its ID.
    /// Derived classes should implement the <see cref="Replay"/> method to define how the event is applied.
    /// </summary>
    [Serializable]
    public abstract class SessionEvent
    {
        /// <summary>
        /// The time the event took place relative to the session record this event is from.
        /// The units are determined by the session record file.
        /// </summary>
        public uint Timestamp { get; set; } = uint.MaxValue;

        /// <summary>
        /// The ID of the SessionObject this event is refferencing
        /// </summary>
        public readonly string id;

        public SessionEvent(string _id)
        {
            id = _id;
        }

        public abstract void Replay(uint playbackTime, int timeUnitPrefix);
        //{
        //    // TODO throw some exceptions if session object can't be found
        //    var sessionObjectManager = GameObject.FindFirstObjectByType<SessionObjectManager>();
        //    SessionObject sessionObject = sessionObjectManager.GetSessionObject(this.id);
        //
        //    ReplayEvent(playbackTime, timeUnitPrefix, sessionObject);
        //}

        //protected abstract void ReplayEvent(uint playbackTime, int timeUnitPrefix, SessionObject sessionObject);

        #region Overridden object Methods

        public override int GetHashCode() => HashCode.Combine(Timestamp, id);
        public override bool Equals(object obj) => Equals(obj as SessionEvent);
        public bool Equals(SessionEvent other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null)
                return false;

            return this.Timestamp == other.Timestamp &&
                string.Equals(this.id, other.id, StringComparison.Ordinal);
        }
        public static bool operator ==(SessionEvent left, SessionEvent right) => Equals(left, right);
        public static bool operator !=(SessionEvent left, SessionEvent right) => !Equals(left, right);

        #endregion

        /// <summary>
        /// This is a class attribute used to tag events as non-state altering.
        /// A non-state altering event can safely be skipped without needing to be applied.
        /// An example of a non-state altering event would be playing a sound.
        /// Playing a sound could be skipped because it is really only relevant to the recorded subject.
        /// An example of a state altering event would be scaling an object.
        /// Scaling an object affects the state of world beyond the event itself.
        /// </summary>
        public class NonStateAltering : Attribute
        {
        }
    }
}
