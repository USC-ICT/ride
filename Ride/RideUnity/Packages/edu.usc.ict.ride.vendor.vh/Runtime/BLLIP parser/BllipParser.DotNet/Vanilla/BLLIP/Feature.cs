using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.FeatureTree_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    static class Feature_global
    {
        public const int MAXNUMFS = 30;
        public const int MAXNUMCALCS = 15;
        public const int MAXNUMTHREADS = 64;

        public const int RCALC  =  0;
        public const int HCALC  =  1;
        public const int UCALC  =  2;
        public const int MCALC  =  3;
        public const int LCALC  =  4;
        public const int LMCALC =  5;
        public const int RUCALC =  6;
        public const int RMCALC =  7;
        public const int TTCALC =  8;
        public const int SCALC  =  9;
        public const int TCALC  = 10;
        public const int WWCALC = 11;

        public const int MAXNUMNTS = 200;
        public const int MAXNUMNTTS = 200;
        public const int MAXNUMTS = 200;

        public const int MAXSENTLEN = 400;
        public const int MAXNUMWORDS = 700000;
    }


    //class FTypeTree;

    class FTypeTree
    {
        public Pointer<FTypeTree> back;
        public Pointer<FTypeTree> left;
        public Pointer<FTypeTree> right;
        public int n;


        public FTypeTree()
        {
            back = null;
            left = null;
            right = null;
            n = -1;
        }

        public FTypeTree(int fi) 
        {
            back = null;
            left = null;
            right = null;
            n = fi;
        }
    }


    //class FullHist;

      /*  Currently what goes in Funs.
        0 t  tree_term 0 |
        1 l  tree_parent_term
        2 u  tree_pos
        3 h  tree_head
        4 ph tree_parent_head
        */

    /*
      num  name  function
      0    t      0
      1    l      1
      2    u      2
      */
    class SubFeature
    {
        int num;
        ECString name;
        public int usf;
        public Func<FullHist, int> fun;  //int (*fun)(FullHist*);
        list<int> featList;
        public static int [] total = new int[MAXNUMCALCS];
        public static Func<FullHist, int> [] Funs = new Func<FullHist, int> [MAXNUMFS];  //static int (*Funs[MAXNUMFS])(FullHist*);
        //static int (*PRFuns[2])(int);
        public static int [,] ufArray = new int[MAXNUMCALCS, MAXNUMFS];
        //static int      splitPts[MAXNUMCALCS][MAXNUMFS];

        static SubFeature [,] array_ = new SubFeature[MAXNUMCALCS, MAXNUMFS];


        public SubFeature(int i, ECString nm, int fnn, list<int> fl)
        {
            num = i;
            name = nm;
            usf = fnn;
            fun = Funs[fnn];
            featList = fl;
        }


        public static ref SubFeature fromInt(int i, int which) { return ref array_[which, i]; }
    }


    /*
      num name ff startpos
      1   rt   0  0
      2   rtl  1  1
      3   rtu  2  1
      */
    class Feature
    {
        int num;
        public ECString name;
        public int subFeat;
        int usubFeat;
        public int startPos;
        public int auxCnt;
        int condPR;


        public static bool isLM = false;
        public static bool useExtraConditioning = false;
        public static int numCalcs = 11;


        public static int whichInt;
        //static int assumedFeatVal;
        //static int (*conditionedEvent)(FullHist*);
        //static int (*assumedSubFeat)(FullHist*);


        Feature(int i, ECString nm, int ff, int pos, int cpr)
        {
            num = i;
            name = nm;
            subFeat = ff;
            startPos = pos;
            auxCnt = 0;
            condPR = cpr;
        }

        public static Feature fromInt(int i, int which)
        {
            AssertInternal(i > 0);
            return array_[which, i - 1];
        }

        public static void setLM() { isLM = true; numCalcs = 12; }
        public static void setExtraConditioning() { useExtraConditioning = true; }


        static void assignCalc(ECString conditioned)
        {
            if (conditioned == "h") whichInt = HCALC;
            else if (conditioned == "u") whichInt = UCALC;
            else if (conditioned == "r") whichInt = RCALC;
            else if (conditioned == "ru") whichInt = RUCALC;
            else if (conditioned == "rm") whichInt = RMCALC;
            else if (conditioned == "tt") whichInt = TTCALC;
            else if (conditioned == "l") whichInt = LCALC;
            else if (conditioned == "lm") whichInt = LMCALC;
            else if (conditioned == "s") whichInt = SCALC;
            else if (conditioned == "t") whichInt = TCALC;
            else if (conditioned == "ww") whichInt = WWCALC;
            else
            {
                AssertInternal(conditioned == "m");
                whichInt = MCALC;
            }
        }


        public static int [] total = new int[MAXNUMCALCS];
        static int [] conditionedFeatureInt = new int[MAXNUMCALCS];


        //public static void init(ECString path, ECString conditioned)
        public static void init(ECString path, ECString conditioned, Dictionary<string, Stream> streams)
        {
            assignCalc(conditioned);

            for (int f = 0; f < MAXNUMFS; f++)
            {
                float [] vec = new float[15];
                lambdas_[whichInt, f] = vec;
                for (int k = 0; k < 15; k++)
                    vec[k] = 0.0f;
            }

            ECString dataECString = new ECString(path);
            dataECString += "featInfo.";
            dataECString += conditioned;
            //string dataStrm = File.ReadAllText(dataECString);  //ifstream dataStrm(dataECString.c_str());
            string dataStrm = "";
            using (var streamReader = new StreamReader(streams[dataECString]))
                dataStrm = streamReader.ReadToEnd();

            string [] dataStrmSplit = dataStrm.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);
            int dataStrmIdx = 0;
            //assert(dataStrm);

            int [] auxCnts = new int [MAXNUMFS];
            for (int i = 0; i < MAXNUMFS; i++)
                auxCnts[i] = 0;


            for (int i = 0; i < MAXNUMCALCS; i++)
                Feature.ftTree[i] = new FTypeTree();

            Feature.ftTreeFromInt[whichInt, 0] = new Pointer<FTypeTree>(Feature.ftTree, whichInt);

            int conditionedInt;
            conditionedInt = Convert.ToInt32(dataStrmSplit[dataStrmIdx++]);  //dataStrm >> conditionedInt;

            conditionedFeatureInt[whichInt] = conditionedInt;
            int num;
            for (num = 0; ; num++)
            {
                int n;
                int subf;
                int pos;
                int cpr;
                ECString nm;
                ECString tmp;
                tmp = dataStrmSplit[dataStrmIdx++];  //dataStrm >> tmp;
                if (tmp == "--")
                    break;

                n = Convert.ToInt32(tmp);
                nm = dataStrmSplit[dataStrmIdx++];  //dataStrm >> nm;
                subf = Convert.ToInt32(dataStrmSplit[dataStrmIdx++]);  //dataStrm >> subf;
                pos = Convert.ToInt32(dataStrmSplit[dataStrmIdx++]);  //dataStrm >> pos;
                tmp = dataStrmSplit[dataStrmIdx++];  //dataStrm >> tmp;
     
                if (tmp == "|")
                {
                    cpr = -1;
                }
                else
                {
                    cpr = Convert.ToInt32(tmp);
                    tmp = dataStrmSplit[dataStrmIdx++];  //dataStrm >> tmp;
                    AssertInternal(tmp == "|");
                }

                array_[whichInt, n - 1] = new Feature(n, nm, subf, pos, cpr);
                array_[whichInt, n - 1].auxCnt = auxCnts[pos];
                auxCnts[pos]++;
                createFTypeTree(Feature.ftTreeFromInt[whichInt, pos], n, whichInt);
            }

            Feature.total[whichInt] = num;
            for (num = 0; ; num++)
            {
                int n;
                int fn;
                ECString nm;
                ECString tmp;
                tmp = dataStrmSplit[dataStrmIdx++];  //dataStrm >> tmp;
                if (tmp == "--")
                    break;

                n = Convert.ToInt32(tmp);
                nm = dataStrmSplit[dataStrmIdx++];  //dataStrm >> nm;
                fn = Convert.ToInt32(dataStrmSplit[dataStrmIdx++]);  //dataStrm >> fn;
                list<int> featList = new list<int>();
                for ( ; ; )
                {
                    tmp = dataStrmSplit[dataStrmIdx++];  //dataStrm >> tmp;
                    if (tmp == "|")
                        break;

                    int f = Convert.ToInt32(tmp);
                    featList.push_back(f);
                }

                SubFeature.fromInt(n, whichInt) = new SubFeature(n, nm, fn, featList);
                AssertInternal(SubFeature.fromInt(n, whichInt) != null);
            }

            SubFeature.total[whichInt] = num;

            /* set the universal function num on feats from their subfeats */
            for (num = 0; num < Feature.total[whichInt]; num++)
            {
                Feature f = array_[whichInt, num];
                f.usubFeat = SubFeature.fromInt(f.subFeat, whichInt).usf;
            }

            /* set up the table from universal subfeat nums to subfeat nums */
            for (num = 0; num < MAXNUMFS; num++)
                SubFeature.ufArray[whichInt, num] = -1;

            for (num = 0; num < SubFeature.total[whichInt]; num++)
            {
                SubFeature sf = SubFeature.fromInt(num, whichInt);
                SubFeature.ufArray[whichInt, sf.usf] = num;
            }
        }


        public static void readLam(int which, ECString tmp, ECString path, Dictionary<string, Stream> streams)
        {
            ECString ftstr = new ECString(path);
            ftstr += tmp;
            ftstr += ".lambdas";
            //string fts = File.ReadAllText(ftstr);  //ifstream fts(ftstr.c_str());
            string fts = "";
            using (var streamReader = new StreamReader(streams[ftstr]))
                fts = streamReader.ReadToEnd();

            string [] ftsSplit = fts.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);
            int ftsIdx = 0;
            //assert(fts);
            int b;
            int f;
            int tot = Feature.total[which];

            if (Feature.isLM || Feature.useExtraConditioning)
            {
                /* This for loop is removed for old bucketing; */
                for (f = 2; f <= tot; f++)
                {
                    float logBase;
                    logBase = Convert.ToSingle(ftsSplit[ftsIdx++]);  //fts >> logBase;
                    logFacs[which, f] = (float)(1.0 / Math.Log(logBase));
                }
            }

            for (b = 1; b < 15; b++)
            {
                int bb;
                if (ftsIdx >= ftsSplit.Length)  //if (!fts)
                {
                    Console.WriteLine("Trouble reading lambs for " + which + " in " + ftstr);
                    //assert(fts);
                }

                bb = Convert.ToInt32(ftsSplit[ftsIdx++]);  //fts >> bb;
                //cerr << bb << endl;
                if (bb != b)
                {
                    Console.WriteLine(tmp + " " + b + " " + bb);
                    //assert(bb == b);
                }

                for (f = 2; f <= tot; f++)
                {
                    float lam;
                    //assert(fts);
                    lam = Convert.ToSingle(ftsSplit[ftsIdx++]);  //fts >> lam;
                    //cerr << which << " " << f << " " << b << " " << lam << endl;
                    Feature.setLambda(which, f, b, lam);
                }
            }
        }


        //static void createLam(int which, ECString tmp, ECString path);
        //static void printLambdas(ostream& res);

        ////e.g., when processing rules for NP, it would be 55;
        public static float getLambda(int wi, int featInt, int bucketInt)
        { return lambdas_[wi, featInt - 1][bucketInt];}
        static void setLambda(int wi, int featInt, int bucketInt, float val)
        { lambdas_[wi, featInt - 1][bucketInt] = val;}
        //static float& lamVal(int wi, int featInt, int bucketInt)
        //{ return lambdas_[wi][featInt-1][bucketInt]; }

        public static FTypeTree [] ftTree = new FTypeTree[MAXNUMCALCS];
        static Pointer<FTypeTree> [,] ftTreeFromInt = new Pointer<FTypeTree>[MAXNUMCALCS, MAXNUMFS];


        static void createFTypeTree(Pointer<FTypeTree> posftTree, int n, int which)
        {
            AssertInternal(posftTree.op != null);
            if (posftTree.op.left == null)
            {
                posftTree.op.left = new Pointer<FTypeTree>(new FTypeTree[] { new FTypeTree(n) });
                Feature.ftTreeFromInt[which, n] = posftTree.op.left;
            }
            else if (posftTree.op.right == null)
            {
                posftTree.op.right = new Pointer<FTypeTree>(new FTypeTree[] { new FTypeTree(AUXIND) });
                createFTypeTree(posftTree.op.right, n, which);
            }
            else
            {
                createFTypeTree(posftTree.op.right, n, which);
            }
        }


        public static float [,] logFacs = new float[MAXNUMCALCS, MAXNUMFS];

        static Feature [,] array_ = new Feature[MAXNUMCALCS, MAXNUMFS];
        static float [,][] lambdas_ = new float[MAXNUMCALCS, MAXNUMFS][];
    }
}
