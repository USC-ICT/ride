using System;
using System.Diagnostics;

using size_t = System.UInt64;
using WordAndPresence = BllipParser.DotNet.Vanilla.pair<int, bool>;  //typedef pair<int, bool> WordAndPresence;

using static BllipParser.DotNet.Vanilla.edgeSubFns;
using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.MeChart_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    //#define Termstar const Term*

    struct Wwegt  
    {
        public int t;
        public ECString e;
        public float p;
    }


    /* WordAndPresence stores the integer associated with a word and whether
       the word is a "hole" or not (false = hole).  ("hole"s are in the
       vocabulary for the purposes of unified vocabulary indexing. */
    //typedef pair<int, bool> WordAndPresence;


    partial class Bchart : ChartBase
    {
        //static  Item*    dummyItem;
        public static float timeFactor = 21;
        float [] denomProbs = new float[MAXSENTLEN];

        public static map<ECString, WordAndPresence> wordMap = new map<ECString, WordAndPresence>();  //static map<ECString, WordAndPresence, less<ECString> > wordMap;
        static ECString [] invWordMap = new ECString[MAXNUMWORDS];
        protected static int lastKnownWord = 0;
        static int [] lastWord = new int[MAXNUMTHREADS];
        static map<ECString, int> [] newWordMap = new map<ECString, int>[MAXNUMTHREADS];
        static vector<ECString> [] newWords = new vector<ECString>[MAXNUMTHREADS];
        public static UnitRules unitRules = null;
        public static bool caseInsensitive = false;
        public static bool tokenize = true;
        public static size_t Nth = 1;
        public static bool prettyPrint = false;
        public static bool silent = false;
        public static bool smallCorpus = false;
        public static float smoothPosAmount = 0;
        public static string HEADWORD_S1 = "^^";


        static Item [] stops = new Item[MAXSENTLEN];
        EdgeHeap heap;
        int alreadyPoppedNum;
        //Edge [] alreadyPopped = new Edge[450000]; //was 350000;


        static ref int posStarts(int i, int j)
        {
            AssertInternal(i < MAXNUMNTTS);
            AssertInternal(j < MAXNUMNTS);
            return ref posStarts_[i, j];
        }


        static int [,] posStarts_ = new int[MAXNUMNTTS, MAXNUMNTS];
        int [,] curDemerits_ = new int[MAXSENTLEN, MAXSENTLEN];

        static int egtSize_ = 0;
        static float [] bucketLims = new float[14] { 0, 0.003f, 0.01f, 0.033f, 0.09f, 0.33f, 1.01f, 2.01f, 5.1f, 12, 30, 80, 200, 600 };
        static float [] pT_ = new float[MAXNUMNTTS];
        static float [] pHcapgt_ = new float[MAXNUMTS];
        static float [] pHhypgt_ = new float[MAXNUMTS];
        static float [] pHugt_ = new float[MAXNUMTS];

        static Wwegt [] pHegt_;
        list<float> [] wordPlists = new list<float>[MAXSENTLEN];


        // Scratch Buffers

        public class meFHProb_ScratchBuffer
        {
            public FeatureTree[] GInfo;
            public float[] SmoothedPs;

            public meFHProb_ScratchBuffer()
            {
                GInfo = new FeatureTree[MAXNUMFS];
                SmoothedPs = new float[MAXNUMFS];
            }

            public void Clear() { Array.Clear(GInfo, 0, GInfo.Length);  Array.Clear(SmoothedPs, 0, SmoothedPs.Length); }
        }

        public static meFHProb_ScratchBuffer [] m_meFHProb_ScratchBuffers = new meFHProb_ScratchBuffer[MAXNUMTHREADS];
        private meFHProb_ScratchBuffer Get_meFHProb_ScratchBuffer(int thrdid)
        {
            var s = m_meFHProb_ScratchBuffers[thrdid];
            if (s == null)
                m_meFHProb_ScratchBuffers[thrdid] = s = new();
            return s;
        }

        private readonly Item[][] m_lrGotIter_ScratchBuffers = new Item[MAXNUMTHREADS][];
        public Item[] Get_lrGotIter_ScratchBuffer(int thrdid)
        {
            Item[] arr = m_lrGotIter_ScratchBuffers[thrdid];
            if (arr == null)
                m_lrGotIter_ScratchBuffers[thrdid] = arr = new Item[400];

            return arr;
        }

        private readonly Item[][][] m_midGotIter_ScratchBuffers = new Item[MAXNUMTHREADS][][];
        public Item[] Get_midGotIter_ScratchBuffer(int thrdid, int depth)
        {
            // Clamp depth defensively (your parsing depth shouldn't exceed wrd_count_ anyway)
            if (depth < 0) depth = 0;
            if (depth >= MAXSENTLEN) depth = MAXSENTLEN - 1;

            Item[][] byDepth = m_midGotIter_ScratchBuffers[thrdid];
            if (byDepth == null)
                m_midGotIter_ScratchBuffers[thrdid] = byDepth = new Item[MAXSENTLEN][];

            Item[] arr = byDepth[depth];
            if (arr == null)
                byDepth[depth] = arr = new Item[400];

            return arr;
        }

        private readonly FullHist[] m_meEdgeProb_FullHistScratch = new FullHist[MAXNUMTHREADS];
        private FullHist Get_meEdgeProb_FullHistScratch(int thrdid)
        {
            FullHist fh = m_meEdgeProb_FullHistScratch[thrdid];
            if (fh == null)
                m_meEdgeProb_FullHistScratch[thrdid] = fh = new FullHist();
            return fh;
        }

        ////////////////////////////////////////////


        protected Bchart(SentRep sentence, int id)
            : base(sentence, id)
        {
            throw new NotImplementedException();
        }

        protected Bchart(SentRep sentence, ExtPos extPos, int id)
            : base(sentence, id)
        {
            for (int i = 0; i < newWordMap.Length; i++)
                if (newWordMap[i] == null) newWordMap[i] = new map<ECString, int>();  // newWordMap[i] = new map<ECString, int>();

            for (int i = 0; i < newWords.Length; i++)
                if (newWords[i] == null) newWords[i] = new vector<ECString>();  // newWords[i] = new vector<ECString>();

            for (int i = 0; i < wordPlists.Length; i++)
            {
                // wordPlists[i] = new list<float>();
                if (wordPlists[i] == null) wordPlists[i] = new list<float>();
                else wordPlists[i].clear();
            }



            depth = 0;
            curDir = -1;
            gcurVal = null;
            extraPos = extPos;
            alreadyPoppedNum = 0;


            pretermNum = 0;
            heap = new EdgeHeap();
            int len = sentence.length();
            lastWord[id] = lastKnownWord;
            AssertInternal(len <= MAXSENTLEN);
            for (int i = 0; i < len; i++)
            {
                ECString wl = langAwareToLower(sentence.op(i).lexeme());
                int val = wtoInt(wl);
                sentence_.op(i).toInt() = val;
            }

            for (int i = 0; i < MAXSENTLEN; i++)
                for (int j = 0; j < MAXSENTLEN; j++)
                    curDemerits_[i, j] = 0;
        }

        //virtual ~Bchart();


        protected void ResetBchart(SentRep sentence, ExtPos extPosOrNull)
        {
            // Reset base
            ResetChartBase(sentence);

            // The ctor clears wordPlists; do the same here (cheap, only MAXSENTLEN lists).
            for (int i = 0; i < wordPlists.Length; i++)
                wordPlists[i]?.clear();

            // Sentence-specific / parse-specific state
            extraPos = extPosOrNull;
            depth = 0;
            curDir = -1;
            gcurVal = null;

            alreadyPoppedNum = 0;
            pretermNum = 0;

            // Heap: ideally Clear() without realloc 
            heap?.Clear();

            // Recompute sentence_.toInt mapping (this is in ctor)
            int len = sentence.length();
            lastWord[thrdid] = lastKnownWord;
            Debug.Assert(len <= MAXSENTLEN);

            for (int i = 0; i < len; i++)
            {
                ECString wl = langAwareToLower(sentence.op(i).lexeme());
                int val = wtoInt(wl);
                sentence_.op(i).toInt() = val;
            }

            // Clear curDemerits
            for (int i = 0; i < MAXSENTLEN; i++)
                for (int j = 0; j < MAXSENTLEN; j++)
                    curDemerits_[i, j] = 0;
        }


        public override double parse()
        {
            initDenom();
            alreadyPoppedNum = 0;

            bool haveS = false;
            int locTimeout = ruleiCountTimeout_;
            for (;;)
            {
                //check();
                if (ruleiCounts_ > locTimeout || poppedEdgeCount_ > poppedTimeout_)
                {
                    if (printDebug(5))
                        Console.WriteLine("Ran out of time");
                    break;
                }

                if (get_S() != null && !haveS)
                {
                    // once we have found a parse, the total edes is set to edges * 3.5;
                    haveS = true;
                    if (printDebug(10))
                        Console.WriteLine("Found S " + poppedEdgeCount_);

                    poppedEdgeCountAtS_ = poppedEdgeCount_;
                    totEdgeCountAtS_ = ruleiCounts_;
                    int newTime = (int)(ruleiCounts_ * timeFactor);  
                    if (newTime < ruleiCountTimeout_)
                        locTimeout = newTime;
                }

                // We keep track of number of ruleis to decide when time out on parsing.;
                /* get best thing off of keylist */
                Edge edge = heap.pop(); 
                if (edge == null)
                {
                    if (printDebug(5))
                        Console.WriteLine("Nonthing on agenda");
                    break;
                }

                int stus = edge.status();
                int cD = curDemerits_[edge.start(), edge.loc()];
                if (edge.demerits() < cD - 5 && !haveS)
                {
                    edge.demerits() = cD;
                    edge.setmerit();
                    heap.insert(edge);
                    continue;
                }

                if (double.IsInfinity(edge.prob()) || double.IsNaN(edge.prob()) || double.IsInfinity(edge.merit()) || double.IsNaN(edge.merit()))
                {
                    if (printDebug(5))
                        Console.WriteLine("Over or underflow");
                    break;
                }

                if (alreadyPoppedNum >= 400000)
                {
                    if (printDebug(5))
                        Console.WriteLine("alreadyPopped got too large");
                    break;
                }

                if (printDebug() > 10)
                {
                    Console.Write(poppedEdgeCount_ + "\tPop");
                    if (stus == 0)
                        Console.Write("< ");
                    else if (stus == 2)
                        Console.Write(". ");
                    else
                        Console.Write("> ");
                    Console.Write(edge + "\t" + edge.prob().ToString("0.00000") + "\t" + edge.merit().ToString("0.00000"));
                    Console.Write("\t" + ruleiCounts_);
                    Console.WriteLine("");
                }

                poppedEdgeCount_++;
                alreadyPoppedNum++;  //alreadyPopped[alreadyPoppedNum++] = edge;
                if (!haveS)
                    addToDemerits(edge);

                /* and add it to chart */
                //heap->check();
                switch (stus)
                {
                    case 0: add_edge(edge, 0); break; //0 => continuing left;
                    case 1: add_edge(edge, 1); break; //1 => continung right;
                    case 2: addFinishedEdge(edge); break;
                }
            }

            /* at this point we are done looking for edges etc. */
            Item snode = get_S();
            /* No "S" node means the sentence was unparsable. */
            if (snode == null)
            {
                return badParse;
            }

            double ans = snode.prob();

            if (ans <= 0.0)
                error("zero probability parse?");

            /*
            ans = -log2(ans);
            if (ans == quiet_nan(0L))
            error("log returned quiet_nan()");
            */

            double nat_log_2 = Math.Log(2.0);
            ans = -Math.Log( ans ) / nat_log_2;
            crossEntropy_ = ans;
            return ans;
        }


        public static ref int printDebug() { return ref printDebug_; }
        static bool printDebug(int val) { return val < printDebug_; }


        // BchartSm.cs
        //public static void readTermProbs(ECString path)


        //static void     makepUgT(ECString path);


        // BchartSm.cs
        //static void readpUgT(ECString path) { throw new NotImplementedException(); }
        //int      wtoInt(ECString& str);


        //int     extraTime; //if no parse is found on regular time;
        //void            check();


        public static void setPosStarts()
        {
            int i;
            int j;
            int k;
            int l;
            for (i = 0; i < MAXNUMNTTS; i++)
                for (j = 0; j < MAXNUMNTS; j++)
                    posStarts(i, j) = -1;
  
            int [] numFor = new int[MAXNUMNTTS];
            for (i = 0; i < MAXNUMNTTS; i++)
                numFor[i] = 0;

            FeatureTree ft = FeatureTree.roots(MCALC);
            for (k = 0; ft.subtree != null && k < ft.subtree.size(); k++)
            {
                FeatureTree ft2 = ft.subtree.index(k);
                i = ft2.ind(); // i = rule term
                for (l = 0; l < ft2.feats_size(); l++)
                {
                    ref readonly Feat f = ref ft2.feats_index_ref_readonly(l);
                    j = f.ind(); //j = rule head term;
                    AssertInternal(numFor[j] < MAXNUMNTTS);
                    //cerr << "For posstart " << j << " headphrase = " << i << endl;
                    posStarts(j, numFor[j]) = i;
                    numFor[j]++;
                }
            }
        }


        //ECString intToW(int n);
        //bool prned();
        //bool issprn(Edge* e);


        //Item*   edgesFromTree(InputTree* tree);
        //void  set_Betas();


        // BchartSm.cs
        // 06/01/06 ML: made these methods public for access by parseIt.C
        // in getting at least POS tags when parsing fails.
        //list<float>& wordPlist(Wrd* word, int word_num);


        static ref float pT(int val)
        {
            if (val < 0 || val >= MAXNUMNTTS)
            {
                Console.WriteLine("Bad val = " + val);
                AssertInternal(val >= 0 && val < MAXNUMTS);
            }

            return ref pT_[val];
        }


        protected int depth;
        protected Val curVal;
        protected int curDir;
        protected Val gcurVal;
        ExtPos extraPos;


        /* this block of functions are only used/defined in rParse */
        //Wrd*  add_word(const Term* trm, int st, ECString wrdStr);
        //Item* add_item(int b, const Term* trmNm, int wrd);
        //Item* add_item2(int b, const Term* trm, int wInt, ECString wrdstr);
        //Item* addToChart(const Term* trm);;
        //Item* in_chart(int b, const Term* trm, bool& wasThere);
        //Item* in_chartT(int b, const Term* trm);
        //Edge* add_edge(Item* lhs, Items& rhs);
        //void   computeEdgeBeta(Item* itm, Edge* edge);
        //void   propagateItemBeta(Item* itm, double quant);
        //int   headPosFromItems(Item* lhs, Items& rhs);
        //void  readItem(istream& str, int& b, const Term*& trm);
        //void  store_word(Wrd* wrd);
        //Wrd*  find_word(int wint, int st);
        //void  assignRProb(Edge* edge);
        //double compute_Betas();
        //bool  compute_Beta(Item* itm);
        //double compute_EdgeBeta(Edge* edge);
        //InputTree* lookUpPhrase(Item* lhs, ECString phrase);
        //void  newWord(ECString wrdstr, int wInt, Item* ans);
        //Edge*  procPhrasal(Item* lhs, ECString phrase);
        //bool   procPhrase(Item* lhs, InputTree* tree);
        //void   rPendFactor();
        /* end of block */


        void add_reg_item(Item itm)
        {
            if (printDebug() > 250)
                Console.WriteLine("add_reg_item " + itm);

            put_in_reg(itm);
            add_starter_edges(itm); 

            //
            // Look at the art for this item (i.e., it has the same Term and start). For
            // each of the dotted rules which hope to have this item following the 'dot', 
            // extend the rule.  
            //
    
            for (int right = 0; right < 2; right++)
            {
                int pos = right != 0 ? itm.start() : itm.finish();
                //cerr<< "Look for " << *itm << " " << pos << " " << right << endl;
                Edge edgeIter = waitingEdges_Head[right, pos];  //var edgeIter = waitingEdges[right, pos].First;  //Edges::iterator edgeIter = waitingEdges[right][pos].begin();
                for (; edgeIter != null; edgeIter = GetWaitNext(edgeIter, right))  //for ( ; edgeIter != null; edgeIter = edgeIter.Next)  //for( ; edgeIter != waitingEdges[right][pos].end() ; edgeIter++ )
                {
                    //Edge edge = edgeIter.Value;  //Edge* edge = *edgeIter;
                    extend_rule(edgeIter, itm, right);  //extend_rule(edge, itm, right);
                }
            }
        }


        void addFinishedEdge(Edge newEdge)
        {
            if (guided && !inGuide(newEdge))
                return;

            if (printDebug() > 250)
                Console.WriteLine("addFinishedEdge " + newEdge);

            if (newEdge.finishedParent() != null && newEdge.finishedParent().term().terminal_p() != 0)
            {
                add_reg_item(newEdge.finishedParent());
                return;
            }

            Item regi;
            regi = in_chart(null, newEdge.lhs(), newEdge.start(), newEdge.loc());
            if (regi != null)
            {
                /* redoP is a crucial function.  It uses the probability of the edge
                to see what the new prob of regi should be, and if it is over
                the threshold for propogating probs, it will recursively
                do this up the chart. */
                redoP(regi, newEdge.prob());
            }
            else
            {
                //regi = new Item(newEdge->lhs(),
                //	      newEdge->start(), newEdge->loc());
                regi = addtochart(newEdge.lhs());
                regi.start() = newEdge.start();
                regi.finish() = newEdge.loc();
                regi.prob() = newEdge.prob();  
                //regi->headp() = newEdge->headp();
                add_reg_item(regi);
            }
  
            if (newEdge.finishedParent() != null)
            {
                AssertInternal(newEdge.finishedParent() == regi);
            }
            else
            {
                newEdge.setFinishedParent(regi);
            }

            regi.ineed().push_back(newEdge);

            /* setFinishedParent tells newEdge that the consitutent that it
            build is regi */
        }


        //also need to make lhs of edge a term*, and give edge a start member.
        void add_starter_edges(Item itm)
        {
            if (printDebug() > 140)
                Console.WriteLine("add_starter_edges " + itm);

            Term poslhs;
            int ht = itm.term().toInt();
            int i;
            for (i = 0; ; i++)
            {
                int rt = posStarts(ht, i);
                //cerr << "PS " << ht << " " << i << " " << rt << endl;
                if (rt < 0)
                    break;

                poslhs = Term.fromInt(rt);
                Edge nedge = new Edge(poslhs);//???;
                extend_rule(nedge, itm, 0);  //adding head is like extending left;
            }
        }


        float meEdgeProb(in Term trm, Edge edge, int whichInt)
        {
            //FullHist fh = new FullHist(edge);
            //fh.cb = this;
            FullHist fh = Get_meEdgeProb_FullHistScratch(thrdid);
            fh.InitForEdge(edge, this);

            AssertInternal(fh.cb != null);
            float ans = meFHProb(trm, fh, whichInt);
            return ans;
        }


        float meFHProb(in Term trm, FullHist fh, int whichInt)
        {
            AssertInternal(fh.cb != null);
            Edge edge = fh.e;
            int pos = 0;
            /* the left to right position we are working on is either the far left (0)
            or the far right */
            if (globalGi[thrdid] == null) { }
            //else if(edge->item() != globalGi[thrdid]->index(0)) ;
            else if (whichInt == RUCALC || whichInt == RMCALC || whichInt == RCALC)
                pos = globalGi[thrdid].size() - 1;

            fh.pos = pos;

            int cVal = trm.toInt();
            if (printDebug() > 138)
            {
                Console.Write("meP " + trm + " " + cVal + " " + whichInt + " ");
                if (edge != null)
                    Console.WriteLine(edge);
                else
                    Console.WriteLine(fh.preTerm);
            }

            var meFHProbScratch = Get_meFHProb_ScratchBuffer(thrdid);
            FeatureTree [] ginfo = meFHProbScratch.GInfo;  //FeatureTree [] ginfo = new FeatureTree[MAXNUMFS];
            ginfo[0] = FeatureTree.roots(whichInt);
            AssertInternal(ginfo[0] != null);
            float [] smoothedPs = meFHProbScratch.SmoothedPs;  //float [] smoothedPs = new float[MAXNUMFS];

            float ans = 1;

            bool useNewBucketing = Feature.isLM || Feature.useExtraConditioning;
            int featureTotal = Feature.total[whichInt];
            for (int i = 1; i <= featureTotal; i++)
            {
                ginfo[i] = null;
                Feature feat = Feature.fromInt(i, whichInt); 
                /* e.g., g(rtlu) starts from where g(rtl) left off (after tl)*/
                int searchStartInd = feat.startPos;

                FeatureTree strt = ginfo[searchStartInd];
                if (strt == null)
                    continue;

                SubFeature sf = SubFeature.fromInt(feat.subFeat, whichInt);
                int usf = sf.usf;
                int nfeatV = edgeFnsArray[usf](fh);
                FeatureTree histPt = strt.follow(nfeatV, feat.auxCnt); 
                ginfo[i] = histPt;
                if (i == 1)
                {
                    smoothedPs[0] = 1;
                    AssertInternal(histPt != null);
                    //Feat* f =histPt->feats.find(cVal);
                    //if(!f)
                    if (!histPt.try_feats_find_index(cVal, out int fIndex))
                        return 0.0f;
                    ref readonly Feat f = ref histPt.feats_index_ref_readonly(fIndex);

                    smoothedPs[1] = f.g();
                    if (printDebug() > 238)
                        Console.WriteLine(i + " " + nfeatV + " " + smoothedPs[1]);

                    //for (int j = 2; j <= featureTotal; j++)
                    //    smoothedPs[j] = 0;

                    ans = smoothedPs[1];
                    continue;
                }

                if (nfeatV < -1)
                {
                    if (printDebug() > 128)
                    {
                        Console.Write("p" + whichInt + "(" + cVal + "|");
                        if (edge != null)
                            Console.Write(edge);
                        else
                            Console.Write(fh.preTerm);

                        Console.WriteLine(") = " + ans);
                    }

                    return ans;
                }

                if (histPt == null)
                    continue;

                int b;

                if (useNewBucketing)  //if (Feature.isLM || Feature.useExtraConditioning)
                {
                    /*new bucketing */
                    float sz = (float)histPt.feats_size();
                    float estm = (float)histPt.count / sz;
                    AssertInternal(i >= 2);
                    b = bucket(estm, whichInt, i);
                }
                else
                {
                    /* old bucketing*/
                    float estm;
                    //estm = histPt->count * smoothedPs[1];
                    estm = (float)(histPt.count * 0.1);
                    b = bucket(estm);
                }

                //Feat* ft = histPt->feats.find(cVal);
                float unsmoothedVal;
                if (!histPt.try_feats_find_index(cVal, out int ftIndex))
                    unsmoothedVal = 0;
                else
                    unsmoothedVal = histPt.feats_index_ref_readonly(ftIndex).g();

                float lam = Feature.getLambda(whichInt, i, b);
                float uspathprob = lam*unsmoothedVal;
                float osmoothedVal = smoothedPs[searchStartInd];
                //float osmoothedVal = smoothedPs[i-1]; //for deleted interp.
                float smpathprob = (1 - lam) * osmoothedVal;
                float nsmoothedVal = uspathprob+smpathprob;
                if (printDebug() > 238)
                    Console.WriteLine(i + " " + nfeatV + " " + usf + " " + b + " " + unsmoothedVal + " " + lam + " " + nsmoothedVal);

                smoothedPs[i] = nsmoothedVal;
                ans *= nsmoothedVal / osmoothedVal;
            }

            //meFHProbScratch.Clear();

            if (printDebug() > 128)
            {
                Console.Write("p" + whichInt + "(" + cVal + "|");
                if (edge != null)
                    Console.Write(edge);
                else
                    Console.Write(fh.preTerm);

                Console.WriteLine(") = " + ans);
            }

            return ans;
        }


        static int printDebug_ = 0;


        void extend_rule(Edge edge, Item item, int right)
        {
            Edge newEdge = new Edge(edge, item, right);
            if (printDebug() > 140)
                Console.WriteLine("extend_rule " + edge + " " + item);

            Term itemTerm = item.term();

            //LeftRightGotIter lrgi = new LeftRightGotIter(newEdge);
            Item [] lrGotIterScratch = Get_lrGotIter_ScratchBuffer(thrdid);
            LeftRightGotIter lrgi = new LeftRightGotIter(newEdge, lrGotIterScratch);
            globalGi[thrdid] = lrgi;

            if (edge.loc() == edge.start())
            {
                newEdge.prob() *= meEdgeProb(item.term(), newEdge, MCALC); 
                /*stoprightp is p of stopping after seeing what currently
                passes for the rhs of the edge */
                newEdge.rightMerit() = computeMerit(newEdge, RUCALC);
                edge = null;  //delete edge; // just created;
            }
            else if (right != 0)
            {
                newEdge.prob() *= meEdgeProb(item.term(), newEdge, RCALC);
            }
            else
            {
                newEdge.prob() *= meEdgeProb(item.term(), newEdge, LCALC);
            }

            if (right != 0)
            {
                newEdge.rightMerit()  = computeMerit(newEdge, RMCALC);
            }
            else
            {
                /* this is the left boundary stat for constituents that are
                continuing left,  given the label and
                whatever currently appears on the left boundary of the constit.
                we only need this when going left */
                newEdge.leftMerit() = computeMerit(newEdge, LMCALC);
            }

            if (itemTerm == Term.stopTerm)
                newEdge.status() = right != 0 ? (short)2 : (short)1;

            if (newEdge.status() == 2)
                newEdge.prob() *= endFactorComp(newEdge);

            if (printDebug() > 250 )
                Console.WriteLine("Constructed " + newEdge + "\t" + newEdge.leftMerit() + "\t" + newEdge.prob() + "\t" + newEdge.rightMerit());

            int tmp = curDemerits_[newEdge.start(), newEdge.loc()];
            newEdge.demerits() = tmp;
            if (repeatRule(newEdge))
            {
                newEdge.rightMerit() = 0;
            }

            newEdge.setmerit(); 
            //cerr << "DEM " << tmp << " " << newEdge->merit() << endl;
            globalGi[thrdid] = null;
            if (newEdge.merit() == 0)
            {
                AssertInternal(alreadyPoppedNum < 450000);
                alreadyPoppedNum++;  //alreadyPopped[alreadyPoppedNum++] = newEdge;
                Edge prd = newEdge.pred();
                prd?.PopFirstSuccessor();  //prd?.sucs().pop_front();

                return;
            }

            ++ruleiCounts_;
            heap.insert(newEdge);

            if (itemTerm != Term.stopTerm)
                item.needme().push_back(newEdge);
        }


        void already_there_extention(int i, int start, int right, Edge edge)
        {
            AssertInternal(i >= 0 && i < MAXSENTLEN && start >= 0 && start < MAXSENTLEN);
            //var regsiter = regs[i, start].First;  //Items::iterator regsiter = regs[i][start].begin();
            var cell = regs[i, start];
            int count = (int)cell.size();
            for (int k = 0; k < count; k++)  //for( ; regsiter != null; regsiter = regsiter.Next)  //for( ; regsiter != regs[i][start].end() ; regsiter++)
            {
                Item item = cell.at(k);  //Item item = regsiter.Value;  //Item* item = *regsiter;
                extend_rule(edge, item, right);
            }
        }


        /* add_edge does 3 things.  Basic bookkeeping on ineeds and needmes, 
        (2) looks to see if the edge can be immediagely extended, and
        (3) adds a new art, for the position it is now looking at if there
        is not already one in the chart.
        */   
        void add_edge(Edge edge, int right)
        {
            if (printDebug() > 250)
                Console.WriteLine("add_edge " + edge);

            int loc = right != 0 ? edge.loc() : edge.start();
            int i;

            extend_rule(edge, stops[loc], right);
            // Iterate over i = the length of the constituent -1.;
            // looking for a reg item of length i and starting position start;
            if (right != 0)
            {
                for (i = 0; i < wrd_count_ - loc; i++)
                    already_there_extention(i, loc, right, edge);
            }
            else
            {
                for (i = 0; i < loc; i++)
                    already_there_extention(loc - i - 1, i, right, edge);
            }

            AssertInternal(loc >= 0 && loc < MAXSENTLEN);
            WaitListAddTail(right, loc, edge);  //waitingEdges[right, loc].push_back( edge ); 
        }


        void put_in_reg(Item itm)
        {
            int st = itm.start();
            int diff = itm.finish() - st - 1;

            if (diff < 0 || st < 0 || diff > MAXSENTLEN || st > MAXSENTLEN)
                error("illegal indices in put_in_reg");

            if (!regsTouched[diff, st])
            {
                regsTouched[diff, st] = true;
                regsTouchedList.push_back(diff * MAXSENTLEN + st);
            }

            regs[diff, st].push_back(itm);
        }


        //void            addWordsToKeylist( );


        Item in_chart(in Wrd hd, in Term trm, int start, int finish)
        {
            if (finish <= 0 || start < 0 || finish - start - 1 < 0)
            {
                Console.WriteLine("For " + trm + "(" + start + ", " + finish + ")");
                error( "bogus boundary params in in_chart" );
            }

            //var regsIter = regs[finish - start - 1, start].First;  //Items::iterator regsIter = regs[finish - start - 1][start].begin();
            var cell = regs[finish - start - 1, start];
            int count = (int)cell.size();
            for (int k = 0; k < count; k++)  //for ( ; regsIter != null; regsIter = regsIter.Next)  //for( ; regsIter != regs[finish - start - 1][start].end(); ++regsIter )
            {
                Item itm = cell.at(k);  //itm = regsIter.Value;  //itm = *regsIter;
                if (itm.term() == trm &&
                    //itm->head() == hd &&
                    itm.start() == start &&
                    itm.finish() == finish)
                    return itm;
            }

            return null;
        }


        /*
        * To extend a rule we either make a new rule inst and add it to the chart,
        * or, if the rule becomes finished, we add a reg item corresponding to the
        * lhs of the rule to the keylist (we can also do both, because really
        * edges correspond to clusters of rules, one of which might be completed
        * and the others not.
        */
        bool repeatRule(Edge edge)
        {
            if (globalGi[thrdid].size() != 3)
                return false;

            if (globalGi[thrdid].index(0).term() != Term.stopTerm)
                return false;

            if (globalGi[thrdid].index(2).term() != Term.stopTerm)
                return false;

            Term chTrm = globalGi[thrdid].index(1).term();
            if (chTrm.terminal_p() != 0)
                return false;

            int parI = edge.lhs().toInt();
            int chI = chTrm.toInt();
            bool ans = unitRules.badPair(parI, chI);
            return ans;
        }


        void redoP(Edge edge, double probRatio)
        {
            //cerr << "rpEdge " << *edge << endl;
            double oldEdgeP = edge.prob();
            //if(oldEdgeP == 0) cerr << "Zprob " << *edge << endl;
            if (edge.heapPos() >= 0)
                heap.del(edge);

            edge.prob() *= probRatio;
            edge.setmerit();
            if (edge.heapPos() >= 0)
                heap.insert(edge);

            //heap->check();
            if (edge.finishedParent() != null)
            {
                redoP(edge.finishedParent(), edge.prob() - oldEdgeP);
            }
        }



        const double storeCutoff = 0.01;

        /* probDiff is the new probability which should be added to the prob of
        item (which for an initially created item will be zero.  If probDiff
        plus whatever previously unused prob in item->storeP is over threshold
        then recurse */
        void redoP(Item item, double probDiff)
        {
            double oldItemP = item.prob();

            double itemStoreP = item.storeP() + probDiff;
            item.storeP() = itemStoreP;
            if (oldItemP != 0.0)
            {
                if (itemStoreP / oldItemP < storeCutoff)
                {
                    return;
                }
            }

            item.prob() += itemStoreP;
            //cerr << "P( " << *item << " ) goes from  " << oldItemP
            //<< " -> " << item->prob() << endl;
            item.storeP() = 0.0;
            if (oldItemP == 0.0)
            {
                return;
            }

            double pRatio = item.prob() / oldItemP;

            NeedmeIter nmi = new NeedmeIter(item);
            while (nmi.next(out Edge edge))
            {
                redoP(edge, pRatio);
            }
        }


        // BchartSm.cs
        //float           computeMerit(Edge* edge, int whichCalc);
        //void initDenom();
        //double  psktt(Wrd* shU, int t);
        //double  pCapgt(const Wrd* shU, int t);
        //float   pHst(int w, int t);
        //double  psutt(const Wrd* shU, int t);
        //float   pegt(ECString& sh, int t);


        //void    getpHst(const ECString& hd, int t);


        // BchartSm.cs
        //double pHypgt(const ECString& shU, int t);


        static ref float pHcapgt(int i) { return ref pHcapgt_[i]; }
        static ref float pHhypgt(int i) { return ref pHhypgt_[i]; }
        protected static ref float pHugt(int i) { return ref pHugt_[i]; }


        // BchartSm.cs
        //int     bucket(float val, int whichInt, int whichFt);
        //int     bucket(float val);
        //int    greaterThan(Wwegt& wwegt, ECString e, int t);
        //float  pHegt(ECString& es, int t);
        //float  computepTgT(int t1,int t2);


        void addToDemerits(Edge edge)
        {
            int st = edge.start();
            int fn = edge.loc();
            for (int i = st; i < fn; i++)
            {
                // e.g., for st = 3, fn = 5, we store at 3,4 3,5 and 4,5
                for (int j = i + 1; j <= fn; j++)
                    curDemerits_[i, j]++;
            }
        }
    }
}
