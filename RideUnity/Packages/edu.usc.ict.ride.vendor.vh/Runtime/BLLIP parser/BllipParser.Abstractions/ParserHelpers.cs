using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
namespace BllipParser
{
    public static class ParserHelpers {

        public static string CreateNativeRequestString(ParseRequest request) {
            var prefix = string.IsNullOrEmpty(request.Name) ? "<s> " : $"<s {request.Name} > ";
            var suffix = " </s>";
            var result = prefix + request.Sentence + suffix;
            return result;
        }


        private static readonly Regex Pattern1 = new Regex(@"\[(.*?)\](.+?)(?=<)([^\[]+)", RegexOptions.Singleline);

        private static readonly Regex Pattern2 = new Regex(@"<(.+?)>([^<]+)", RegexOptions.Singleline);

        public static ParseResponse CreateResponseFromNativeResponseString(ParseRequest request, string nativeResponse) {
            var matches = Pattern1.Matches(nativeResponse);
            Trace.Assert(matches.Count == 1);//The native implementation can handle multiple sentences at the same time, but that feature is not needed by us.
            var match = matches[0];
            var nameString = match.Groups[1].Value;
            var name = string.IsNullOrEmpty(nameString) ? null : nameString;
            var lexemString = match.Groups[2].Value.Trim();
            var lexemes = lexemString.Split(' ');
            var candidateString = match.Groups[3].Value;
            matches = Pattern2.Matches(candidateString);
            var candidates = new List<ParseCandidate>();
            for (var i = 0; i < matches.Count; i++)
            {
                var probString = matches[i].Groups[1].Value;
                var prob = double.Parse(probString);
                var treeString = matches[i].Groups[2].Value.Trim();
                var tree = new ParseTree(treeString);
                var candidate = new ParseCandidate(prob, tree);
                candidates.Add(candidate);
            }
            var result = new ParseResponse(name, request.Sentence, lexemes, candidates);
            return result;
        }
    }
}
