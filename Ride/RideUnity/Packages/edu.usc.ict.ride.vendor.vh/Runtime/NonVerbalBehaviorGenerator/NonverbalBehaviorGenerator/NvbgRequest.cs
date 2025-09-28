#nullable enable

namespace NonverbalBehaviorGenerator
{
    public sealed class NvbgRequest
    {

        public NvbgRequestKind Kind { get; }

        public string MessageId { get; }

        public string SourceId { get; }

        public string TargetId { get; }

        /// <summary>
        /// BML or FML input
        /// </summary>
        public string? Xml { get; }

        /// <summary>
        /// plain text input
        /// </summary>
        public string? PlainText { get; }

        public NvbgRequest(NvbgRequestKind kind, string messageId, string sourceId, string targetId, string? xml = null, string? plainText = null)
        {
            Kind = kind;
            MessageId = messageId.Trim('"').Trim();
            SourceId = sourceId.Trim('"').Trim();
            TargetId = targetId.Trim('"').Trim();
            Xml = xml?.Trim('"').Trim();
            PlainText = plainText?.Trim();
        }
    }

}

