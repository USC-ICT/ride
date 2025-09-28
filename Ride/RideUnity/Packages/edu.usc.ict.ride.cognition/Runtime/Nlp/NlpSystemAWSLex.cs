using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ride.AWS;

namespace Ride.NLP
{
    /// <summary>
    /// Uses AWS Lex to provide NLP functionality for question answering and sentiment analysis.
    /// Supports both V1 and V2 of AWS Lex.
    /// </summary>
    public class NlpSystemAWSLex : NlpSystemUnity
    {
        public bool useV2 = false;
        public string m_botName { get; set; }               // V1
        public string m_botAlias { get; set; }              // V1
        public string m_host_runtime { get; set; }          // V1
        public string m_host_model_building { get; set; }
        public string m_awsRegion { get; set; }
        public string m_awsAccessKey { get; set; }
        public string m_awsSecretKey { get; set; }
        public string m_host_runtime_v2 { get; set; }       // V2
        public string m_botId { get; set; }                 // V2
        public string m_botAliasId { get; set; }            // V2
        public string m_localeId { get; set; }              // V2
        public string m_sessionId { get; set; }             // V2
        public string m_response { get; set; }

        private string m_awsService = "lex";

        private string AWSDateTime  { get => DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ");  }
        private string AWSDate      { get => DateTimeOffset.UtcNow.ToString("yyyyMMdd");}

        private bool m_processing = false;
        private NlpSentimentResponse m_nlpSentimentResponse = new NlpSentimentResponse("empty");
        private NlpResponse m_nlpQnAAnswer = new NlpResponse("empty");
        private Queue<AWSLexServiceRequest> m_requestQueue = new Queue<AWSLexServiceRequest>();

        private struct AWSLexServiceRequest
        {
            public string action;
            public string content;
            public Action<NlpResponse> onCompleteDelegate;
        };

        /// <inheritdoc/>
        public override void SystemInit()
        {
            var configSystem = Globals.api.GetSystem<ConfigurationSystemUnity>();
            m_botName = configSystem.config.awsLex.botName;
            m_botAlias = configSystem.config.awsLex.botAlias;
            m_host_runtime = configSystem.config.awsLex.host_runtime;
            m_host_model_building = configSystem.config.awsLex.host_model_building;
            m_awsRegion = configSystem.config.awsLex.region;
            m_awsAccessKey = configSystem.config.awsLex.accessKey;
            m_awsSecretKey = configSystem.config.awsLex.secretKey;

            if (useV2)
            {
                m_botId = configSystem.config.awsLex.botId;
                m_botAliasId = configSystem.config.awsLex.botAliasId;
                m_localeId = configSystem.config.awsLex.localeId;
                m_sessionId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                m_host_runtime_v2 = configSystem.config.awsLex.host_runtime_v2;
                m_awsRegion = configSystem.config.awsLex.region;
                m_awsAccessKey = configSystem.config.awsLex.accessKey;
                m_awsSecretKey = configSystem.config.awsLex.secretKey;
            }

            base.SystemInit();
        }

        /// <inheritdoc/>
        override public void SystemUpdate(float dt)
        {
            if (m_requestQueue.Count != 0)  // If items in the queue
            {
                AWSLexServiceRequest req = m_requestQueue.Dequeue();
                if (!m_processing)          // Process current request if not already processing
                {
                    ProcessRequest(req.content, req.onCompleteDelegate);
                }

                CheckProcessing(req);       // Check progress on separate thread
            }
        }

        /// <inheritdoc/>
        public async void ProcessRequest(string question, Action<NlpResponse> onComplete, string data = null)
        {
            System.Diagnostics.Stopwatch stopwatch2 = new System.Diagnostics.Stopwatch();
            m_processing = true; // Locking, since single request provides both QnA Answer and Sentiment analysis

            string uri = AWSLex.GetAWSLexAskUri(m_host_runtime, m_botName, m_botAlias);

            var utcNow = DateTimeOffset.UtcNow;
            string awsDateTime = utcNow.ToString("yyyyMMddTHHmmssZ");
            string awsDate = utcNow.ToString("yyyyMMdd");

            var strHeadersForAuthorization = new Dictionary<string, string>();
            strHeadersForAuthorization.Add("x-amz-date", awsDateTime);
            strHeadersForAuthorization.Add("host", m_host_runtime);

            string questionJSON = "{\"inputText\": \"" + question + "\"}";
            string awsAuthorization = RideAWSUtils.GetAWSAuthorizationHeader(uri, questionJSON, System.Net.Http.HttpMethod.Post, m_awsRegion, m_awsAccessKey, m_awsSecretKey, strHeadersForAuthorization, awsDateTime, awsDate, m_awsService);

            var strHeadersForSending = new Dictionary<string, string>();
            strHeadersForSending.Add("x-amz-date", awsDateTime);
            strHeadersForSending.Add("Authorization", awsAuthorization);

            // Call web service
            stopwatch2.Start();
            string response = await RideIO.Post(uri, questionJSON, strHeadersForSending, "application/json", m_host_runtime);
            TimeSpan ts = stopwatch2.Elapsed;
            m_responseTime = ts.Milliseconds.ToString() + " ms";

            // Process both QnA Answer and Sentiment Analsysis
            AWSLex.AWSLexResponse awsResponse = RideIO.JsonDeserializeIgnoreNullAndMissing<AWSLex.AWSLexResponse>(response);
            m_nlpQnAAnswer = new NlpResponse(awsResponse.message);
            string[] awsSentimentScores = awsResponse.sentimentResponse.sentimentScore.Split(',', '{', '}', ':', ' '); // Example: {Positive: 0.029292157,Negative: 0.10695917,Neutral: 0.86374426,Mixed: 4.3991945E-6}
            m_nlpSentimentResponse = new NlpSentimentResponse(response, awsResponse.sentimentResponse.sentimentLabel, Convert.ToDouble(awsSentimentScores[3]), Convert.ToDouble(awsSentimentScores[9]), Convert.ToDouble(awsSentimentScores[6]));

            m_processing = false; // Unlocking
        }

        /// <summary>
        /// Waits for any ongoing Lex processing to finish and invokes the appropriate callback.
        /// </summary>
        /// <param name="request">The Lex service request to complete.</param>
        private async void CheckProcessing(AWSLexServiceRequest request)
        {
            while (m_processing == true) { await Task.Delay(50); }

            if (request.action.Equals("AskQuestion"))
                request.onCompleteDelegate?.Invoke(m_nlpQnAAnswer);
            else if (request.action.Equals("AnalyzeSentiment"))
                request.onCompleteDelegate?.Invoke(m_nlpSentimentResponse);
        }

        /// <inheritdoc/>
        public override async void Request(NlpRequest request, Action<NlpResponse> onComplete)
        {
            // TODO: 
            // Clean-up; currently mix of V1 Ask Question, Queing system, and Request
            System.Diagnostics.Stopwatch stopwatch2 = new System.Diagnostics.Stopwatch();

            var strHeadersForAuthorization = new Dictionary<string, string>();
            strHeadersForAuthorization.Add("x-amz-date", AWSDateTime);
            strHeadersForAuthorization.Add("host", m_host_runtime_v2);

            string uri = AWSLex.GetAWSLexAskUriV2(m_host_runtime_v2, m_botId, m_botAliasId, m_localeId, m_sessionId);
            string questionJSON = "{\"text\": \"" + request.content + "\"}";
            string awsAuthorization = RideAWSUtils.GetAWSAuthorizationHeader(uri, questionJSON, System.Net.Http.HttpMethod.Post, m_awsRegion, m_awsAccessKey, m_awsSecretKey, strHeadersForAuthorization, AWSDateTime, AWSDate, m_awsService);

            var strHeadersForSending = new Dictionary<string, string>();
            strHeadersForSending.Add("x-amz-date", AWSDateTime);
            strHeadersForSending.Add("Authorization", awsAuthorization);

            // Call web service
            stopwatch2.Start();
            string response = await RideIO.Post(uri, questionJSON, strHeadersForSending, "application/json", m_host_runtime_v2);
            TimeSpan ts = stopwatch2.Elapsed;
            m_responseTime = ts.Milliseconds.ToString() + " ms";

            // Process response
            AWSLex.AWSLexResponseV2 awsResponse = RideIO.JsonDeserializeIgnoreNullAndMissing<AWSLex.AWSLexResponseV2>(response);
            AWSLex.AWSMessage[] awsMessages = awsResponse.messages;
            string[] answers = new string[awsMessages.Length];
            int i = 0;
            foreach (AWSLex.AWSMessage message in awsMessages)
            {
                answers[i++] = message.content.ToString();
            }
            m_nlpQnAAnswer = new NlpResponse(answers);

            onComplete?.Invoke(m_nlpQnAAnswer);
        }

        /// <summary>
        /// Sends a GET request to retrieve data from the specified AWS Lex endpoint.
        /// </summary>
        /// <param name="uri">The endpoint URI.</param>
        /// <returns>Response string from the API.</returns>
        public async Task<string> RequestGet(string uri)
        {
            var strHeadersForAuthorization = new Dictionary<string, string>();
            strHeadersForAuthorization.Add("x-amz-date", AWSDateTime);
            strHeadersForAuthorization.Add("host", m_host_model_building);

            string awsAuthorization = RideAWSUtils.GetAWSAuthorizationHeader(uri, "", System.Net.Http.HttpMethod.Get, m_awsRegion, m_awsAccessKey, m_awsSecretKey, strHeadersForAuthorization, AWSDateTime, AWSDate, m_awsService);

            var strHeadersForSending = new Dictionary<string, string>();
            strHeadersForSending.Add("x-amz-date", AWSDateTime);
            strHeadersForSending.Add("Authorization", awsAuthorization);

            string response = await RideIO.GetAsyncHost(uri, strHeadersForSending, m_host_model_building);
            return response;
        }

        /// <summary>
        /// Sends a request to add a new intent to the bot via the AWS Lex model building API.
        /// </summary>
        /// <param name="uri">The full API endpoint URI.</param>
        /// <param name="questions">Utterances for the intent.</param>
        /// <param name="responses">Responses for the intent.</param>
        public async void RequestAddIntent(string uri, List<string> questions, List<string> responses)
        {
            var utcNow = DateTimeOffset.UtcNow;
            string awsDateTime = utcNow.ToString("yyyyMMddTHHmmssZ");
            string awsDate = utcNow.ToString("yyyyMMdd");

            AWSLex.AddIntentRequest requestBody = new AWSLex.AddIntentRequest();
            requestBody.fulfillmentActivity = new AWSLex.FulfillmentActivity();
            requestBody.fulfillmentActivity.type = "ReturnIntent";

            requestBody.sampleUtterances = new string[questions.Count];
            for (int i = 0; i < questions.Count; i++)
            {
                requestBody.sampleUtterances[i] = questions[i].Replace("?", "");
            }

            requestBody.conclusionStatement = new AWSLex.ConclusionStatement();
            requestBody.conclusionStatement.messages = new AWSLex.Messages[responses.Count];
            for (int i = 0; i < responses.Count; i++)
            {
                requestBody.conclusionStatement.messages[i] = new AWSLex.Messages();
                requestBody.conclusionStatement.messages[i].content = responses[i];
                requestBody.conclusionStatement.messages[i].contentType = "PlainText";
            }

            string jsonRequestBody = RideIO.JsonSerializeNoObjRef(requestBody);

            var strHeadersForAuthorization = new Dictionary<string, string>();
            strHeadersForAuthorization.Add("x-amz-date", awsDateTime);
            strHeadersForAuthorization.Add("host", m_host_model_building);

            string awsAuthorization = RideAWSUtils.GetAWSAuthorizationHeader(uri, jsonRequestBody, System.Net.Http.HttpMethod.Put, m_awsRegion, m_awsAccessKey, m_awsSecretKey, strHeadersForAuthorization, awsDateTime, awsDate, m_awsService);

            var strHeadersForSending = new Dictionary<string, string>();
            strHeadersForSending.Add("x-amz-date", awsDateTime);
            strHeadersForSending.Add("Authorization", awsAuthorization);

            // Call web service
            string result = await RideIO.PutAsync(uri, jsonRequestBody, strHeadersForSending, "application/json", m_host_model_building);

            if (result.Contains("contentType"))
            {
                m_response = "intent added successffully to Lex bot";
            }
            else
            {
                m_response = "intent already present in Lex bot";
            }
        }

        /// <summary>
        /// Sends a request to update a bot alias with new configuration.
        /// </summary>
        /// <param name="uri">The alias endpoint URI.</param>
        /// <param name="jsonRequestBody">Serialized request body JSON.</param>
        /// <returns>Response string from the API.</returns>
        public async Task<string> RequestPutBotAlias(string uri, string jsonRequestBody)
        {
            var strHeadersForAuthorization = new Dictionary<string, string>();
            strHeadersForAuthorization.Add("x-amz-date", AWSDateTime);
            strHeadersForAuthorization.Add("host", m_host_model_building);

            string awsAuthorization = RideAWSUtils.GetAWSAuthorizationHeader(uri, jsonRequestBody, System.Net.Http.HttpMethod.Put, m_awsRegion, m_awsAccessKey, m_awsSecretKey, strHeadersForAuthorization, AWSDateTime, AWSDate, m_awsService);

            var strHeadersForSending = new Dictionary<string, string>();
            strHeadersForSending.Add("x-amz-date", AWSDateTime);
            strHeadersForSending.Add("Authorization", awsAuthorization);

            // Call web service
            string response = await RideIO.PutAsync(uri, jsonRequestBody, strHeadersForSending, "application/json", m_host_model_building);

            return response;
        }

        /// <summary>
        /// Sends a request to create a new bot version in AWS Lex.
        /// </summary>
        /// <param name="uri">The version creation endpoint.</param>
        /// <param name="jsonRequestBody">Serialized request body JSON.</param>
        /// <returns>Response string from the API.</returns>
        public async Task<string> RequestCreateBotVersion(string uri, string jsonRequestBody)
        {
            var strHeadersForAuthorization = new Dictionary<string, string>();
            strHeadersForAuthorization.Add("x-amz-date", AWSDateTime);
            strHeadersForAuthorization.Add("host", m_host_model_building);

            string awsAuthorization = RideAWSUtils.GetAWSAuthorizationHeader(uri, jsonRequestBody, System.Net.Http.HttpMethod.Post, m_awsRegion, m_awsAccessKey, m_awsSecretKey, strHeadersForAuthorization, AWSDateTime, AWSDate, m_awsService);

            var strHeadersForSending = new Dictionary<string, string>();
            strHeadersForSending.Add("x-amz-date", AWSDateTime);
            strHeadersForSending.Add("Authorization", awsAuthorization);

            // Call web service
            string response = await RideIO.Post(uri, jsonRequestBody, strHeadersForSending, "application/json", m_host_model_building);

            return response;
        }

        /// <summary>
        /// Sends a request to update a bot configuration.
        /// </summary>
        /// <param name="uri">The bot endpoint URI.</param>
        /// <param name="jsonRequestBody">Serialized request body JSON.</param>
        /// <returns>Response string from the API.</returns>
        public async Task<string> RequestPutBot(string uri, string jsonRequestBody)
        {
            var strHeadersForAuthorization = new Dictionary<string, string>();
            strHeadersForAuthorization.Add("x-amz-date", AWSDateTime);
            strHeadersForAuthorization.Add("host", m_host_model_building);

            string awsAuthorization = RideAWSUtils.GetAWSAuthorizationHeader(uri, jsonRequestBody, System.Net.Http.HttpMethod.Put, m_awsRegion, m_awsAccessKey, m_awsSecretKey, strHeadersForAuthorization, AWSDateTime, AWSDate, m_awsService);

            var strHeadersForSending = new Dictionary<string, string>();
            strHeadersForSending.Add("x-amz-date", AWSDateTime);
            strHeadersForSending.Add("Authorization", awsAuthorization);

            // Call web service
            string response = await RideIO.PutAsync(uri, jsonRequestBody, strHeadersForSending, "application/json", m_host_model_building);

            return response;
        }

        /// <summary>
        /// Analyzes sentiment of text through AWS Lex.
        /// </summary>
        /// <param name="text">Text to be analyzed</param>
        /// <param name="onComplete">Delegate to execute on successful request</param>
        public void AnalyzeSentiment(string text, Action<NlpResponse> onComplete)
        {
            m_requestQueue.Enqueue(new AWSLexServiceRequest() { action = "AnalyzeSentiment", content = text, onCompleteDelegate = onComplete });
        }

        /// <summary>
        /// Adds a new intent to the configured Lex bot.
        /// </summary>
        /// <param name="intentName">The name of the new intent.</param>
        /// <param name="questions">List of user utterances for training.</param>
        /// <param name="responses">List of responses associated with the intent.</param>
        public void AddIntent(string intentName, List<string> questions, List<string> responses)
        {
            RequestAddIntent("https://" + m_host_model_building + "/intents/" + intentName + "/versions/$LATEST", questions, responses);
        }

        /// <summary>
        /// Sends a request to retrieve a specific intent definition.
        /// </summary>
        /// <param name="intentName">The name of the intent.</param>
        public async void GetIntent(string intentName)
        {
            string response = await RequestGet("https://" + m_host_model_building + "/" + intentName + "/versions/$LATEST");
            print(response);
        }

        /// <summary>
        /// Retrieves the current definition of a bot from AWS Lex.
        /// </summary>
        /// <param name="botName">The name of the bot to retrieve.</param>
        /// <returns>The bot definition JSON as a string.</returns>
        public async Task<string> GetBot(string botName)
        {
            return (await RequestGet("https://" + m_host_model_building + "/bots/" + botName + "/versions/$LATEST"));
        }

        /// <summary>
        /// Updates a bot with new intent definitions and triggers a rebuild.
        /// </summary>
        /// <param name="botName">The name of the bot to update.</param>
        /// <param name="intents">List of intent names to include.</param>
        public async void PutBot(string botName, List<string> intents)
        {
            // GetBot
            string responseGetBot = await GetBot(botName);
            AWSLex.GetBot responseGetBotJson = RideIO.JsonDeserializeIgnoreNullAndMissing<AWSLex.GetBot>(responseGetBot);

            // make getBot response ready for putBot with necessary changes
            for (int i = 0; i < intents.Count; i++)
            {
                AWSLex.Intents temp = new AWSLex.Intents();
                temp.intentName = intents[i];
                temp.intentVersion = "$LATEST";
                responseGetBotJson.intents.Add(temp);
            }

            responseGetBotJson.processBehaviour = "BUILD";
            string serializedPutBotJson = RideIO.JsonSerializeNoObjRef(responseGetBotJson);

            // PutBot
            string responsePutBot = await RequestPutBot("https://" + m_host_model_building + "/bots/" + botName + "/versions/$LATEST", serializedPutBotJson);

            if (responsePutBot.Contains("checksum"))
            {
                m_response = " lex bot is ready with new intents";
            }
            else
            {
                m_response = " lex bot cannot be built";
            }           
        }        
    }
}
