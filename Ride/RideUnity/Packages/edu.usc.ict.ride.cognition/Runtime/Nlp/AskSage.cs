namespace Ride.NLP
{
    /// <summary>
    /// Provides support for Ask Sage data structures. Ask Sage leverages 3rd party vender LLMs
    /// and is government focused.
    /// 
    /// API documentation:
    /// https://app.swaggerhub.com/apis-docs/NICOLASCHAILLAN_1/server_ask-sage_api/1.0#/default/
    /// https://app.swaggerhub.com/apis-docs/NICOLASCHAILLAN_1/user-api/1.0#/default/
    /// 
    /// All interactions requires an access token, obtained with the user's email and API key.
    /// </summary>
    public class AskSage
    {
        /// <summary>
        /// User input
        /// </summary>
        [System.Serializable]
        // Sample JSON:
        //{
        //  "message": "hello what can you do",
        //  "persona": "Contracting Officer",
        //  "system_prompt": "",
        //  "dataset": "DoD",
        //  "limit_references": 0,
        //  "temperature": 0.3,
        //  "live": 0,
        //  "model": "GPT Auto"
        //}
        public struct AskSageQuestion
        {
            public string message;
            public string persona;
            public string system_prompt;
            public string dataset;
            public int limit_references;
            public double temperature;
            public int live;
            public string model;
        }

        /// <summary>
        /// System response
        /// </summary>
        [System.Serializable]
        // Sample JSON:
        //{
        //  "added_obj": null,
        //  "embedding_down": false,
        //  "message": "Hello! I'm Ask Sage, an AI chatbot. I can help you with a variety of tasks, such as answering questions, providing information, translating languages, writing essays or articles, generating code, creating diagrams, and more. Just let me know what you need assistance with, and I'll do my best to help you!",
        //  "references": "",
        //  "response": "OK",
        //  "status": 200,
        //  "tool_calls": null,
        //  "type": "completion",
        //  "usage": null,
        //  "uuid": "5527dea0-5a82-4189-b955-6e81e7ff1d36",
        //  "vectors_down": false
        //}
        public struct AskSageAnswer
        {
            public string added_obj;
            public bool embedding_down;
            public string message;
            public string references;
            public string response;
            public int status;
            public int tool_calls;
            public string type;
            public string usage;
            public string uuid;
            public bool vectors_down;
        }
    }
}