#nullable enable

using System.Collections.Generic;

namespace BllipParser
{
    public sealed class ParseResponse
    {
        public string? Name { get; }

        public string Sentence { get; }

        public IReadOnlyList<string> Lexemes { get; }

        public IReadOnlyList<ParseCandidate> Candidates { get; }

        public ParseResponse(string? name, string sentence, IReadOnlyList<string> lexemes, IReadOnlyList<ParseCandidate> candidates)
        {
            Name = name;
            Sentence = sentence;
            Lexemes = lexemes;
            Candidates = candidates;
        }
    }
}
