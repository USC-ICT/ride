namespace BllipParser
{
    public sealed class ParseCandidate
    {
        public double Probability { get; }

        public ParseTree Tree { get; }

        public ParseCandidate(double probability, ParseTree tree)
        {
            Probability = probability;
            Tree = tree;
        }
    }
}
