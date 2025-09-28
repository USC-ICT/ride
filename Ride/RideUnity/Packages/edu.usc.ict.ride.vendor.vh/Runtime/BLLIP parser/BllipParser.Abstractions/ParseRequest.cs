#nullable enable

namespace BllipParser
{
    public sealed class ParseRequest
    {

        public string? Name { get; }
        public string Sentence { get; }

        public ParseRequest(string? name, string sentence)
        {
            Name = name;
            Sentence = sentence.Trim();
        }

        public ParseRequest(string sentence) : this(null, sentence) { }
    }
}
