using System;

using size_t = System.UInt64;


namespace BllipParser.DotNet.Vanilla
{
    class ExtPos : vector<vector<Term>>
    {
        public void read(string [] ifs, SentRep sr)  //void read(ifstream* ifs,SentRep& sr);
        {
            throw new NotImplementedException();
        }


        public bool hasExtPos()
        {
            for (size_t i = 0; i < size(); i ++)
            {
                vector<Term> terms = this[(int)i];  //operator[](i);
                if (terms.size() > 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
