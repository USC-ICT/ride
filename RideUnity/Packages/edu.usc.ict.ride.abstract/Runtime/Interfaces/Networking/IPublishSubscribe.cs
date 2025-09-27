namespace Ride.Networking
{
    public interface IPublishSubscribe : IRideSystem
    {
        public delegate void MessageEventCallback(object sender, string message);

        void Publish(string message);
        void Subscribe(string topic);
        void AddMessageEventHandler(MessageEventCallback callback);
    }
}
