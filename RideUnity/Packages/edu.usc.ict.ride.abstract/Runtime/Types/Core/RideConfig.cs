using System;

namespace Ride
{
    /// <summary>
    /// Configuration structure for settings specific to the application that can be configured outside of the simulation engine.
    /// See tss_config.json in the streaming assets folder
    /// </summary>
    [Serializable]
    public struct RideConfig
    {
        public static readonly Version DefaultVersion = new Version("1.0.5.5");


        /// <summary>Application specific settings for Anthropic</summary>
        [Serializable]
        public struct AnthropicSettings
        {
            /// <summary>URL for OpenAI</summary>
            public string endpoint;

            /// <summary>Authorization key for OpenAI</summary>
            public string endpointKey;

            public static AnthropicSettings Default => new AnthropicSettings
            {
                endpoint = "https://api.anthropic.com/v1/messages",
                endpointKey = "sk-ant-api03-k0yD-XXXXXXXXXXXXXXXXXXXXXXXXXXXXX_XXXXXXXXXXXXXXXXXXXX-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXX-XXXXXXXX"
            };
        }

        /// <summary>Application specific settings for Ask Sage</summary>
        [Serializable]
        public struct AskSageSettings
        {
            /// <summary>URL for Ask Sage</summary>
            public string endpoint;

            /// <summary>API key for Ask Sage</summary>
            public string apiKey;

            /// <summary>Authorization Token for Ask Sage</summary>
            public string authorizationToken;

            public static AskSageSettings Default => new AskSageSettings
            {
                apiKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                authorizationToken = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX.XXXXXXXXXXXXXXXXXXXXXXXXXXXX_XXXXXXXX-XXXXXXXXXX_XXXXXX-XXXXXXXXXXXXXXXXXXXXXX-XXXXXXX",
                endpoint = "https://api.asksage.ai/server/query",
            };
        }

        /// <summary>Application specific settings for AWS Lex</summary>
        [Serializable]
        public struct AWSLexSettings
        {
            public string botName;
            public string botId;
            public string botAlias;
            public string botAliasId;
            public string host_runtime;
            public string host_model_building;
            public string host_runtime_v2;
            public string host_model_building_v2;
            public string region;
            public string localeId;
            public string accessKey;
            public string secretKey;

            public static AWSLexSettings Default => new AWSLexSettings
            {
                botName = "LexBot",
                botId = "XXXXXXXXXX",
                botAlias = "dev",
                botAliasId = "XXXXXXXXXX",
                host_runtime = "runtime.lex.us-west-2.amazonaws.com",
                host_model_building = "models.lex.us-west-2.amazonaws.com",
                host_runtime_v2 = "runtime-v2-lex.us-west-2.amazonaws.com",
                host_model_building_v2 = "models-v2-lex.us-west-2.amazonaws.com",
                region = "us-west-2",
                localeId = "en_US",
                accessKey = "XXXXXXXXXXXXXXXXXXXX",
                secretKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
        }

        /// <summary>Application specific settings for AWS Polly</summary>
        [Serializable]
        public struct AWSPollySettings
        {
            public string accessKey;
            public string secretKey;

            public static AWSPollySettings Default => new AWSPollySettings
            {
                accessKey = "XXXXXXXXXXXXXXXXXXXX",
                secretKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
        }

        /// <summary>Application specific settings for AWS Rekognition</summary>
        [Serializable]
        public struct AWSRekognitionSettings
        {
            public string accessKey;
            public string secretKey;

            public static AWSRekognitionSettings Default => new AWSRekognitionSettings
            {
                accessKey = "XXXXXXXXXXXXXXXXXXXX",
                secretKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
        }

        /// <summary>Application specific settings for Amazon Web Services</summary>
        [Serializable]
        public struct AWSSettings
        {
            public string region;
            public string s3bucket;
            public string cognitoIdentityPoolId;

            public static AWSSettings Default => new AWSSettings
            {
                region = "us-west-2",
                s3bucket = "test-bucket",
                cognitoIdentityPoolId = "us-west-2:0000xx0x-000x-000x-0000-x0xx0x0x00xx",
            };
        }

        /// <summary>Application specific settings for AWS Terrain Key</summary>
        [Serializable]
        public struct AWSTerrain
        {
            public string cognitoIdentityPoolId;

            public static AWSTerrain Default => new AWSTerrain
            {
                cognitoIdentityPoolId = "us-west-2:0000xx0x-000x-000x-0000-x0xx0x0x00xx",
            };
        }

        /// <summary>Application specific settings for Microsoft Azure</summary>
        [Serializable]
        public struct AzureBlobSettings
        {
            public string connectionString;

            /// <summary>for storing data in file storage or blob</summary>
            public string storageKey;

            public static AzureBlobSettings Default => new AzureBlobSettings
            {
                connectionString = "DefaultEndpointsProtocol=https;AccountName=myazureaccount;AccountKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX==;EndpointSuffix=core.windows.net",
                storageKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX==",
            };
        }

        /// <summary>Application specific settings for Microsoft Azure Custom QnA</summary>
        [Serializable]
        public struct AzureCustomQnASettings
        {
            /// <summary>URL for Custom QnA</summary>
            public string endpoint;

            /// <summary>Ocp-Apim-Subscription-Key for Custom QnA</summary>
            public string ocpApimSubscriptionKey;

            /// <summary>project name of Custom QnA</summary>
            public string projectName;

            /// <summary>api version of Custom QnA</summary>
            public string apiVersion;

            /// <summary>api version of Custom QnA</summary>
            public string deploymentName;

            public static AzureCustomQnASettings Default => new AzureCustomQnASettings
            {
                endpoint = @"https://myqna.cognitiveservices.azure.com",
                ocpApimSubscriptionKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                projectName = "qnamaker-kb-qna",
                apiVersion = "2021-10-01",
                deploymentName = "test",
            };
        }

        /// <summary>Application specific settings for Microsoft Azure Face</summary>
        [Serializable]
        public struct AzureFaceSettings
        {
            /// <summary>URL for Azure Face</summary>
            public string endpoint;

            /// <summary>Subscription key for Azure Face</summary>
            public string endpointKey;

            public static AzureFaceSettings Default => new AzureFaceSettings
            {
                endpoint = @"https://azure-face.cognitiveservices.azure.com",
                endpointKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
        }

        /// <summary>Application specific settings for Microsoft Azure QnA Maker</summary>
        [Serializable]
        public struct AzureQnAMakerSettings
        {
            /// <summary>URL for QnAMaker</summary>
            public string endpoint;

            /// <summary>Authorization key for QnAMaker</summary>
            public string endpointKey;

            /// <summary>ID of the QnAMaker knowledge base</summary>
            public string kbId;

            public static AzureQnAMakerSettings Default => new AzureQnAMakerSettings
            {
                endpoint = @"https://myqna.azurewebsites.net",
                endpointKey = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
                kbId = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX",
            };
        }

        /// <summary>Application specific settings for Microsoft Azure Speech Recognition</summary>
        [Serializable]
        public struct AzureSpeechRecognitionSettings
        {
            /// <summary>API Key</summary>
            public string apiKey;

            /// <summary>Region</summary>
            public string region;

            public static AzureSpeechRecognitionSettings Default => new AzureSpeechRecognitionSettings
            {
                apiKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                region = "westus",
            };
        }

        /// <summary>Application specific settings for Microsoft Azure Text Analytics (TA)</summary>
        [Serializable]
        public struct AzureTASettings
        {
            /// <summary>URL for Azure Text Analytics (TA)</summary>
            public string endpoint;

            /// <summary>Subscription key for Azure Text Analytics (TA)</summary>
            public string endpointKey;

            public static AzureTASettings Default => new AzureTASettings
            {
                endpoint = "https://my-text-analytics.cognitiveservices.azure.com",
                endpointKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
        }

        /// <summary>Application specific settings for ElevenLabs Text to Speech</summary>
        [Serializable]
        public struct ElevenLabsSettings
        {
            /// <summary>API key for ElevenLabs</summary>
            public string apiKey;

            public static ElevenLabsSettings Default => new ElevenLabsSettings
            {
                apiKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
        }

        /// <summary>Application specific settings for Google DialogFlow</summary>
        [Serializable]
        public struct GoogleDialogflowSettings
        {
            /// <summary>The service account associated</summary>
            public string googleServiceAccount;

            /// <summary>the google project id associated</summary>
            public string projectId;

            /// <summary>the access token request URL (or for any kind of authentication)</summary>
            public string oath2TokenUrl;

            /// <summary>a space-delimited list of URL of the permissions that the application requests</summary>
            public string accessRequestScopeUrl;

            /// <summary>convert the defined string to URL before using it</summary>
            public string grantType;

            /// <summary>the type of assertion to get the required signed JSON Web Token for proper authentication</summary>
            public string assertionType;

            public static GoogleDialogflowSettings Default => new GoogleDialogflowSettings
            {
                googleServiceAccount = "dialogflow-XXXXXX@mydialogflow.iam.gserviceaccount.com",
                projectId = "mydialogflow",
                oath2TokenUrl = "https://oauth2.googleapis.com/token",
                accessRequestScopeUrl = "https://www.googleapis.com/auth/cloud-platform",
                grantType = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                assertionType = "http://oauth.net/grant_type/jwt/1.0/bearer",
            };
        }

        /// <summary>Application specific settings for HuggingFace</summary>
        [Serializable]
        public struct HuggingFaceSettings
        {
            /// <summary>API key for HuggingFace</summary>
            public string apiKey;

            /// <summary>API key for Stability API in HuggingFace, see InferenceEndpointTextGenerationSystem</summary>
            public string stabilityApiKey;

            public static HuggingFaceSettings Default => new HuggingFaceSettings
            {
                apiKey = "XX_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
                stabilityApiKey = "XX_XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
        }

        /// <summary>Settings for Learning Records System</summary>
        [Serializable]
        public struct LRSSettings
        {
            public string url;
            public string key;
            public string secret;
            public bool writeToFile;

            public static LRSSettings Default => new LRSSettings
            {
                url = "https://mylrs.lrs.io/xapi/", 
                key = "XXXXXX", 
                secret = "XXXXXX", 
                writeToFile = false,
            };
        }

        /// <summary>Application specific settings for OpenAI</summary>
        [Serializable]
        public struct OpenAISettings
        {
            /// <summary>URL for OpenAI</summary>
            public string endpoint;

            /// <summary>Authorization key for OpenAI</summary>
            public string endpointKey;

            public static OpenAISettings Default => DefaultChatGPT;
            public static OpenAISettings DefaultChatGPT => new OpenAISettings
            {
                endpoint = @"https://api.openai.com/v1/chat/completions",
                endpointKey = "XX-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
            public static OpenAISettings DefaultDalle => new OpenAISettings
            {
                endpoint = @"https://api.openai.com/v1/images/generations",
                endpointKey = "XX-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
            public static OpenAISettings DefaultGPT => new OpenAISettings
            {
                endpoint = @"https://api.openai.com/v1/completions",
                endpointKey = "XX-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
            public static OpenAISettings DefaultNews => new OpenAISettings
            {
                endpointKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",  // newsapi.org
            };
            public static OpenAISettings DefaultWeather => new OpenAISettings
            {
                endpointKey = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",  // openweathermap.org
            };
        }

        /// <summary>Backend server that hosts RIDE REST services</summary>
        [Serializable]
        public struct RestServerApiSettings
        {
            /// <summary>The server's address</summary>
            public string url;

            /// <summary>The stage of the backend rest api to invoke dev, test, production, etc</summary>
            public string stage;

            public static RestServerApiSettings Default => new RestServerApiSettings
            {
                url = "https://xxxxxxxxxx.execute-api.us-west-2.amazonaws.com",
                stage = "Prod",
            };
        }

        /// <summary>These settings are used when a RIDE powered application is created its own REST services</summary>
        [Serializable]
        public struct RESTSettings
        {
            /// <summary>the address of the device that is hosting the RESTful services</summary>
            public string address;

            /// <summary>the communication port</summary>
            public ushort port;

            public static RESTSettings Default => new RESTSettings
            {
                address = "127.0.0.1",
                port = 9157,
            };
        }

        /// <summary>Settings for Slack</summary>
        [Serializable]
        public struct SlackSettings
        {
            /// <summary>The key required to gain access to the slack api</summary>
            public string token;

            /// <summary>The id of the slack channel to connect with</summary>
            public string channel;

            public static SlackSettings Default => new SlackSettings
            {
                token = "XXXX-XXXXXXXXXXX-XXXXXXXXXXXXX-XXXXXXXXXXXXXXXXXXXXXXXX",
                channel = "XXXXXXXXXXX",
            };
        }

        /// <summary>Application specific settings for Stability AI</summary>
        [Serializable]
        public struct StabilityAISettings
        {
            /// <summary>URL for Stability AI</summary>
            public string endpoint;

            /// <summary>Authorization key for Stability AI</summary>
            public string endpointKey;

            public static StabilityAISettings Default => new StabilityAISettings
            {
                endpoint = @"https://api.stability.ai/v1/generation/stable-diffusion-xl-beta-v2-2-2/text-to-image",
                endpointKey = @"XX-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX",
            };
        }


        public Version version;
        public AnthropicSettings anthropic;
        public AskSageSettings askSage;
        public AWSLexSettings awsLex;
        public AWSPollySettings awsPolly;
        public AWSRekognitionSettings awsRekognition;
        public AWSSettings aws;
        public AWSTerrain awsTerrain;
        public AzureBlobSettings azureBlob;
        public AzureCustomQnASettings azureCustomQnA;
        public AzureFaceSettings azureFace;
        public AzureQnAMakerSettings azureQnA;
        public AzureSpeechRecognitionSettings azureSpeech;
        public AzureTASettings azureTA;
        public ElevenLabsSettings elevenLabs;
        public GoogleDialogflowSettings googleDflow;
        public HuggingFaceSettings huggingFace;
        public LRSSettings lrs;
        public OpenAISettings openAIChatGPT;
        public OpenAISettings openAIDalle;
        public OpenAISettings openAIGPT;
        public OpenAISettings openAINews;
        public OpenAISettings openAIWeather;
        public RestServerApiSettings restApi;
        public RESTSettings rest;
        public SlackSettings slack;
        public StabilityAISettings stableDiffusion;


        public static readonly RideConfig Default = new RideConfig
        {
            version = DefaultVersion,
            anthropic = AnthropicSettings.Default,
            askSage = AskSageSettings.Default,
            awsLex = AWSLexSettings.Default,
            awsPolly = AWSPollySettings.Default,
            awsRekognition = AWSRekognitionSettings.Default,
            aws = AWSSettings.Default,
            awsTerrain = AWSTerrain.Default,
            azureBlob = AzureBlobSettings.Default,
            azureCustomQnA = AzureCustomQnASettings.Default,
            azureFace = AzureFaceSettings.Default,
            azureQnA = AzureQnAMakerSettings.Default,
            azureSpeech = AzureSpeechRecognitionSettings.Default,
            azureTA = AzureTASettings.Default,
            elevenLabs = ElevenLabsSettings.Default,
            googleDflow = GoogleDialogflowSettings.Default,
            huggingFace = HuggingFaceSettings.Default,
            lrs = LRSSettings.Default,
            openAIChatGPT = OpenAISettings.DefaultChatGPT,
            openAIDalle = OpenAISettings.DefaultDalle,
            openAIGPT = OpenAISettings.DefaultGPT,
            openAINews = OpenAISettings.DefaultNews,
            openAIWeather = OpenAISettings.DefaultWeather,
            restApi = RestServerApiSettings.Default,
            rest = RESTSettings.Default,
            slack = SlackSettings.Default,
            stableDiffusion = StabilityAISettings.Default,
        };
    }
}
