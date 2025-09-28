#nullable enable

namespace NonverbalBehaviorGenerator.LegacyInterop
{
    /// <summary>
    /// Since legacy code (and refactor code based on legacy code) will modify requst members, and we want to keep our request object uncontaminated, so we create this class for holding modified values
    /// </summary>
    internal sealed class LegacyRequest
    {
        public NvbgRequestKind Kind { get; set; }

        public string MessageId { get; set; }

        public string SourceId { get; set; }

        public string TargetId { get; set; }

        public string? Xml { get; }

        public string? PlainText { get; }

        public LegacyRequest(NvbgRequest request)
        {
            Kind = request.Kind;
            MessageId = request.MessageId;
            SourceId = request.SourceId;
            TargetId = request.TargetId;
            Xml = request.Xml;
            PlainText = request.PlainText;
        }
    }
}

