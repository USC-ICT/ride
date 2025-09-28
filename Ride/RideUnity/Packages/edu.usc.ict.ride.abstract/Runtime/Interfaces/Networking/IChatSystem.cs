using System.Collections;
using System.Collections.Generic;

namespace Ride.Networking
{
    public interface IChatSystem : IRideSystem
    {
        bool IsConnected { get; }

        void Connect(string username);
        void Disconnect();

        void SendChatMessage(string message);
        string GetChatMessages();
    }
}
