using System;
using System.Diagnostics;

using size_t = System.UInt64;

using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    class ECArgs
    {
        int nargs_;
        int nopts_;
        ECString [] argList = new ECString[32];
        list<ECString> optList = new list<ECString>();


        public ECArgs(int argc, string [] argv)
        {
            nargs_ = 0;
            for (int i = 1 ; i < argc ; i++)
            {
                ECString arg = new ECString(argv[i]);
                if (arg[0] != '-')
                {
                    argList[nargs_] = arg;
                    nargs_++;
                }
                else
                {
                    nopts_++;
                    int l = (int)arg.length();
                    AssertInternal(l > 1);
                    ECString opt = new ECString(1, arg[1]);
                    optList.push_back(opt);
                    if (l == 2)
                    {
                        optList.push_back("");
                    }
                    else
                    {
                        ECString v = new ECString(arg, 2, (size_t)l - 2);
                        optList.push_back(v);
                    }
                }
            }
        }


        public int nargs() { return nargs_; }


        public bool isset(char c)
        {
            ECString sig = "";
            sig += c;
            //list<ECString>::iterator oIter = find(optList.begin(), optList.end(), sig);
            //bool found = (oIter != optList.end());
            bool found = optList.find(sig);
            return found;
        }


        public ECString value(char c)
        {
            ECString sig = "";
            sig += c;

            var oIter = optList.Find(sig);  //list<ECString>::iterator oIter = find(optList.begin(), optList.end(), sig);
            bool found = oIter != null;  //bool found = (oIter != optList.end());
            if (!found)
            {
                Console.WriteLine("Looking for value of on-line argument " + sig);
                error("could not find value");
            }

            oIter = oIter.Next;  //++oIter;
            return oIter.Value;  //return *oIter;
        }


        public ECString arg(int n) { return argList[n]; }
    }
}  
