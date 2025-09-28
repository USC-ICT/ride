namespace BllipParser
{

    public sealed class ParseTree
    {

        //TODO: Real Tree structure

        private readonly string _string;

        public ParseTree(string str)
        {
            _string = str;
        }

        public override string ToString()
        {
            return _string;
        }
    }
}
