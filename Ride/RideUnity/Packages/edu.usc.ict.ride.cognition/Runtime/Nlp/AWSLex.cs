using System.Collections.Generic;

namespace Ride.NLP
{
    /// <summary>
    /// Provides support for AWS Lex.
    /// </summary>
    public class AWSLex
    {
        /// <summary>
        /// Response from AWS Lex.
        /// </summary>
        [System.Serializable]
        public struct AWSLexResponse
        {
            public string dialogState;
            public string intentName;
            public string message;
            public string messageFormat;
            public string responseAttributes;
            public string responseCard;
            public AWSSentimentResponse sentimentResponse;
            public string sessionAttributes;
            public string sessionId;
            public string slotToElicit;
            public AWSSlots slots;
        }

        /// <summary>
        /// Response from AWS Lex V2 for RecognizeText
        /// https://docs.aws.amazon.com/lexv2/latest/APIReference/API_runtime_RecognizeText.html 
        /// </summary>
        [System.Serializable]
        public struct AWSLexResponseV2
        {
            public AWSIntent[] interpretations;
            public AWSMessage[] messages;            
            public string sessionId;
            public AWSSessionState sessionState;            
        }

        public struct AWSInterpretation
        {
            public AWSIntent[] intents;
            public AWSNluConfidence nluConfidence;
        }

        public struct AWSIntent
        {
            public string confirmationState;
            public string name;
            public AWSSlots slots;
            public string state;
        }

        public struct AWSNluConfidence
        {
            public long score;
        }

        public struct AWSMessage
        {
            public string content;
            public string contentType;
        }

        public struct AWSSessionState
        {
            public AWSDialogAction dialogAction;
            public AWSIntent intent;
            public string originatingRequestId;
            public AWSSessionAttributes sessionAttributes;
        }

        public struct AWSDialogAction
        {
            public string type;
        }

        public struct AWSSentimentResponse
        {
            public string sentimentLabel;
            public string sentimentScore;
        }

        public struct AWSSessionAttributes
        {

        }
        
        public struct AWSSlots { }

        // add/put intent request (https://docs.aws.amazon.com/lex/latest/dg/API_PutIntent.html)
        [System.Serializable]
        //public class AddIntentRequest
        //{
        //    //public string checksum;
        //    public ConclusionStatement conclusionStatement;
        //    public ConfirmationPrompt confirmationPrompt;
        //    public bool createVersion = true;
        //    public string description = "";
        //    public DialogCodeHook dialogCodeHook;
        //    public FollowUpPrompt followUpPrompt;
        //    public FulfillmentActivity fulfillmentActivity;
        //    public InputContexts[] inputContexts;
        //    public KendraConfiguration kendraConfiguration;
        //    public OutputContexts[] outputContexts;
        //    public string parentIntentSignature = "";
        //    public ConclusionStatement rejectionStatement;
        //    public string[] sampleUtterances;
        //    public Slots[] slots;
        //}

        public class AddIntentRequest
        {
            public FulfillmentActivity fulfillmentActivity;
            public string[] sampleUtterances;
            public ConclusionStatement conclusionStatement;
        }

        [System.Serializable]
        public class ConclusionStatement
        {
            public Messages[] messages;
            //public string responseCard = "";
        }

        [System.Serializable]
        public class Messages
        {
            public string content = "";
            public string contentType = "";
            //public int groupNumber;
        }

        [System.Serializable]
        public class ConfirmationPrompt 
        {
            public int maxAttempts;
            public Messages[] messages;
            //public string responseCard = "";
        }
        
        [System.Serializable]
        public class DialogCodeHook 
        {
            public string messageVersion = "";
            public string uri = "";
        }

        [System.Serializable]
        public struct FollowUpPrompt 
        {
            public ConfirmationPrompt prompt;
            public ConclusionStatement rejectionStatement;
        }

        [System.Serializable]
        public class FulfillmentActivity 
        {
            //public DialogCodeHook codeHook;
            public string type = "";
        }

        [System.Serializable]
        public class InputContexts 
        {
            public string name = "";
        }

        [System.Serializable]
        public class KendraConfiguration 
        {
            public string kendraIndex = "";
            public string queryFilterString = "";
            public string role = "";
        }

        [System.Serializable]
        public class OutputContexts 
        {
            public string name = "";
            public double timeToLiveInSeconds;
            public int turnsToLive;
        }

        [System.Serializable]
        public class Slots 
        {
            public string slotType;
            public string name;
            public string slotConstraint;
            public ConfirmationPrompt valueElicitationPrompt;
            public int priority;
            public string slotTypeVersion;
            public string[] sampleUtterances;
            public string description;
        }

        // Add/Update Bot

        [System.Serializable]
        public class GetBot
        {
            public AbortStatement abortStatement;
            public string checksum;
            public bool childDirected;
            public ClarificationPrompt clarificationPrompt;
            public string description;
            public bool detectSentiment;
            public bool enableModelImprovements;
            public string failureReason;
            public string group;
            public double idleSessionTTLInSeconds;
            public List<Intents> intents;
            public string name;
            public string locale;
            public string processBehaviour;
        }

        [System.Serializable]
        public class AbortStatement
        {
            public Messages[] messages;
        }

        [System.Serializable]
        public class ClarificationPrompt
        {
            public int maxAttempts;
            public Messages[] messages;
        }

        [System.Serializable]
        public class Intents
        {
            public string intentVersion;
            public string intentName;
        }

        [System.Serializable]
        public class CheckSum
        {
            public string checksum;
        }

        [System.Serializable]
        public class BotAliasRequest
        {
            public string botVersion;
            public string checksum;
        }

        /// <summary>
        /// Creates AWS Lex ask query URI specific to bot
        /// </summary>
        /// <param name="host">Host name of AWS Lex server</param>
        /// <param name="botName">Name of the AWS Lex bot</param>
        /// <param name="botAlias">Alias of AWS Lex bot</param>
        /// <returns>URI for AWS Lex to ask user question to</returns>
        public static string GetAWSLexAskUri(string host, string botName, string botAlias)
        {
            // Example: https://runtime.lex.us-west-2.amazonaws.com/bot/ArnoTest/alias/dev/user/test/text
            return "https://" + host + "/bot/" + botName + "/alias/" + botAlias + "/user/ridenlpexampleuser/text";
        }

        /// <summary>
        /// Creates AWS Lex V2 ask query URI specific to bot and session
        /// https://docs.aws.amazon.com/lexv2/latest/APIReference/API_runtime_RecognizeText.html 
        /// </summary>
        /// <param name="host">Host name of AWS Lex server, e.g., runtime-v2-lex.us-west-2.amazonaws.com, https://docs.aws.amazon.com/general/latest/gr/lex.html </param> 
        /// <param name="botId">ID of the bot, e.g., AIHKSCNMXQ, see Details panel in Amazon Lex console</param>
        /// <param name="botAliasId">ID of the alias, e.g., PQAJKLKSMXQ, see Details in Alias panel in Amazon Lex console</param>
        /// <param name="localeId">Location / language, e.g., en_US, https://docs.aws.amazon.com/lexv2/latest/dg/how-languages.html</param>
        /// <param name="sessionId">ID to keep track of a session, user defined</param>
        /// <returns></returns>
        public static string GetAWSLexAskUriV2(string host, string botId, string botAliasId, string localeId, string sessionId)
        {
            // https://runtime-v2-lex.us-west-2.amazonaws.com/bots/botId/botAliases/botAliasId/botLocales/localeId/sessions/sessionId/text            
            return "https://" + host + "/bots/" + botId + "/botAliases/" + botAliasId + "/botLocales/" + localeId + "/sessions/" + sessionId + "/text";
        }
    }
}
