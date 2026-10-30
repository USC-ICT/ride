namespace Ride.Networking
{
    /// <summary>Provides topic-based message publishing and subscription, primarily through the VHMsg adapter.</summary>
    public interface IPublishSubscribe : IRideSystem
    {
        /// <summary>Represents a callback invoked when a subscribed message is received.</summary>
        /// <param name="sender">The implementation that received the message.</param>
        /// <param name="message">The received message text.</param>
        public delegate void MessageEventCallback(object sender, string message);

        /// <summary>
        /// Publishes a message through the configured messaging transport.
        /// </summary>
        /// <param name="message">The message text to publish.</param>
        void Publish(string message);

        /// <summary>
        /// Subscribes to messages matching the specified transport topic.
        /// </summary>
        /// <param name="topic">The topic or message pattern recognized by the transport.</param>
        void Subscribe(string topic);

        /// <summary>
        /// Registers a callback for received subscribed messages.
        /// </summary>
        /// <param name="callback">The callback to register.</param>
        void AddMessageEventHandler(MessageEventCallback callback);
    }
}
