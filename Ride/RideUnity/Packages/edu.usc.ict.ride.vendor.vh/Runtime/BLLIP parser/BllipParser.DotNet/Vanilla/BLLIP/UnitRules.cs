using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using static BllipParser.DotNet.Vanilla.Feature_global;


namespace BllipParser.DotNet.Vanilla
{
    class UnitRules
    {
        //int unitRules[MAXNUMNTS];
        ////int treeData_[MAXNUMNTS][MAXNUMNTS];
        bool [,] bef_ = new bool[MAXNUMNTS, MAXNUMNTS];


        //void init();
        //void readTrees(istream& dataStream);
        //void gatherData(InputTree* tree);
        //void setData();


        public bool badPair(int par, int chi)
        {
            int parInt = par - Term.lastTagInt() - 1;
            int chiInt = chi - Term.lastTagInt() - 1;
            //cerr << "BP " << parInt << " " << chiInt << endl;
            return !bef_[parInt, chiInt];
        }


        //bool badPairB(int par, int chi);
        //void printData(ECString path);


        //public void readData(ECString path)
        public void readData(ECString path, Dictionary<string, Stream> streams)
        {
            int p;
            int c;
            for (p = 0 ; p < MAXNUMNTS; p++)
                for (c = 0 ; c < MAXNUMNTS; c++)
                    bef_[p, c] = false;
  
            ECString fl = new ECString(path);
            fl += "unitRules.txt";
            //string data = File.ReadAllText(fl);  //ifstream data(fl.c_str());
            string data = "";
            using (var streamReader = new StreamReader(streams[fl]))
                data = streamReader.ReadToEnd();

            string [] dataSplit = data.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);

            //assert(data);

            for (int dataIdx = 0; dataIdx < dataSplit.Length; )  //for ( ; ; )
            {
                //data >> p;
                p = Convert.ToInt32(dataSplit[dataIdx++]);
                if (dataIdx >= dataSplit.Length)  //if (!data)
                    break;

                //data >> c;
                c = Convert.ToInt32(dataSplit[dataIdx++]);

                bef_[p, c] = true;
                ////cerr << "PCT " << p << " " << c << endl;
            }
        }
    }
}
