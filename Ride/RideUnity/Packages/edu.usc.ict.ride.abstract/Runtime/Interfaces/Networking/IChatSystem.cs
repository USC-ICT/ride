using System.Collections;
using System.Collections.Generic;

namespace Ride.Networking
{
    /// <summary>Provides the text-chat connection used by RIDE chat clients such as the Photon chat implementation.</summary>
    public interface IChatSystem : IRideSystem
    {
        /// <summary>Gets whether the chat service is currently connected.</summary>
        bool IsConnected { get; }

        /// <summary>
        /// Connects to the chat service as the specified user.
        /// </summary>
        /// <param name="username">The user name presented to the chat service.</param>
        void Connect(string username);

        /// <summary>Disconnects from the chat service.</summary>
        void Disconnect();

        /// <summary>
        /// Sends a message through the active chat connection.
        /// </summary>
        /// <param name="message">The message text to send.</param>
        void SendChatMessage(string message);

        /// <summary>Gets the chat transcript maintained by the implementation.</summary>
        /// <returns>The accumulated chat messages.</returns>
        string GetChatMessages();
    }
}
