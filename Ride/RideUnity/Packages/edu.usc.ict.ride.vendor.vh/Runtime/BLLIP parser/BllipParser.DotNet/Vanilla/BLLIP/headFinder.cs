using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using static BllipParser.DotNet.Vanilla.headFinderCh;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    static class headFinder
    {
        static set<ECString> head1s = new set<ECString>();
        static set<ECString> head2s = new set<ECString>();


        //static void readHeadInfoEn(ECString path)
        static void readHeadInfoEn(ECString path, Dictionary<string, Stream> streams)
        {
            ECString headStrg = new ECString(path);
            headStrg += "headInfo.txt";
            //string headStrm = File.ReadAllText(headStrg);  //ifstream headStrm(headStrg.c_str());
            string headStrm = "";
            using (var streamReader = new StreamReader(streams[headStrg]))
                headStrm = streamReader.ReadToEnd();

            string [] headStrmSplit = headStrm.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);
            //assert(headStrm);

            ECString next;
            ECString next2;

            //headStrm >> next;
            next = new ECString(headStrmSplit[0]);

            AssertInternal(next == "1");
            int whichHeads = 1;
            for (int headStrmIdx = 1; headStrmIdx < headStrmSplit.Length; headStrmIdx++)  //while (headStrm)
            {
                //headStrm >> next;
                next = headStrmSplit[headStrmIdx++];

                if (headStrmIdx >= headStrmSplit.Length)  //if (!headStrm)
                    break;

                if (next == "2")
                {
                    whichHeads = 2;
                    continue;
                }
      
                //headStrm >> next2;
                next2 = headStrmSplit[headStrmIdx];

                ////cerr << "NN " << next << " " << next2 << endl;
                if (headStrmIdx >= headStrmSplit.Length)  //if (!headStrm)
                    error("Bad format for headInfo.txt");

                next += next2;

                if (whichHeads == 1)
                    head1s.insert(next);
                else
                    head2s.insert(next);
            }
        }


        //public static void readHeadInfo(ECString path)
        public static void readHeadInfo(ECString path, Dictionary<string, Stream> streams)
        {
            if (Term.Language == "Ch" || Term.Language == "Ar") 
                { readHeadInfoCh(path, streams); return; }
            else
                { readHeadInfoEn(path, streams); return; }
        }


        //int headPosFromTree(InputTree* tree);

        //int headPriority(ECString lhsString, ECString rhsString, int ansPriority);
    }
}
