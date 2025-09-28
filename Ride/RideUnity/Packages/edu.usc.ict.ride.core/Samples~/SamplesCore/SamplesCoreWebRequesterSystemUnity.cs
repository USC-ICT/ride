using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ride;
using Ride.IO;
using Ride.Networking;

namespace Ride.Samples
{
    public class SamplesCoreWebRequesterSystemUnity : RideMonoBehaviour
    {
        DebugMenu m_debugMenu;
        WebRequesterSystemUnity m_webRequester;

        string m_responseRaw;
        List<string> m_response = new();

        protected override void Start()
        {
            base.Start();

            m_debugMenu = Globals.api.GetSystem<DebugMenu>();
            m_webRequester = Globals.api.GetSystem<WebRequesterSystemUnity>();
        }

        public void OnGUIWebRequester()
        {
            if (m_debugMenu.Button("Send Request"))
            {
                string url = "https://httpbin.org/put";
                m_webRequester.Put(url, null, "", (result, error, response) =>
                {
                    ParseResponse(response);
                });
            }

            if (m_debugMenu.Button("Send Advanced Request"))
            {
                string url = "https://httpbin.org/put";

                var headers = new Dictionary<string, string>()
                {
                    { "User-Agent", "UnityWebRequest-Test" },
                    { "Authorization", "Bearer dummy_token" }
                };

                m_webRequester.Put(url, headers, "", (result, error, response) =>
                {
                    ParseResponse(response);
                });
            }

            if (!string.IsNullOrEmpty(m_responseRaw))
            {
                m_debugMenu.Label("<b>Response (raw):</b>");
                m_debugMenu.Label(m_responseRaw);
            }

            if (m_response.Count > 0)
                m_debugMenu.Label("<b>Response (text):</b>");
            foreach (var line in m_response)
                m_debugMenu.Label(line);
        }

        void ParseResponse(string response)
        {
            m_responseRaw = response;
            m_response.Clear();

            var data = RideIO.JsonDeserialize<Dictionary<string, object>>(response);
            if (data.ContainsKey("headers"))
            {
                var headers = RideIO.JsonDeserialize<Dictionary<string, object>>(data["headers"].ToString());
                foreach (var entry in headers)
                {
                    if (entry.Value is string)
                        m_response.Add($"H: {entry.Key}: {entry.Value}");
                }
            }

            foreach (var entry in data)
            {
                if (entry.Value is string)
                    m_response.Add($"{entry.Key}: {entry.Value}");
            }
        }
    }
}
