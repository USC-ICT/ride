using NonverbalBehaviorGenerator.LegacyInterop;
using System.Xml;

namespace NonverbalBehaviorGenerator
{
    public sealed class NvbgResponse
    {
        public NvbgRequest Request { get; }

        internal LegacyRequest UnprotectedRequest { get; }

        public XmlDocument BehaviorMarkupLanguage { get; }

        public NvbgRequestKind Kind => UnprotectedRequest.Kind;

        public string SourceId => UnprotectedRequest.SourceId;

        public string TargetId => UnprotectedRequest.TargetId;

        public string MessageId => UnprotectedRequest.MessageId;

        internal NvbgResponse(NvbgRequest originalRequest, LegacyRequest unprotectedRequest, XmlDocument behaviorMarkupLanguage)
        {
            Request = originalRequest;
            UnprotectedRequest = unprotectedRequest;
            BehaviorMarkupLanguage = behaviorMarkupLanguage;
        }
    }
}

