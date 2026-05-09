using System;
using System.Diagnostics;

using Items = BllipParser.DotNet.Vanilla.vector<BllipParser.DotNet.Vanilla.Item>;  //typedef list<Item*> Items;

using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    //class InputTree;


    abstract class ChartBase
    {
        public int thrdid;
        protected const double badParse = -1;	// error return value for parse(), crossEntropy
        protected SentRep sentence_;
        //vector<vector<int> > extPos;
        public static float endFactor = 1.2f;
        public static float midFactor = 0.88334f;
        static int [] numItemsToDelete = new int[MAXNUMTHREADS];
        static int [] itemsToDeletesize = new int[MAXNUMTHREADS];
        static vector<Item> [] itemsToDelete = new vector<Item>[MAXNUMTHREADS];
        protected static bool guided = false;
        protected Items [,] regs = new Items[MAXSENTLEN, MAXSENTLEN];

        // keep records of what regs were touched to improve reset perf
        protected bool [,] regsTouched = new bool[MAXSENTLEN, MAXSENTLEN];
        protected vector<int> regsTouchedList = new vector<int>();

        //vector<short>   guide[MAXSENTLEN][MAXSENTLEN];
        //protected list<Edge> [,] waitingEdges = new list<Edge>[2, MAXSENTLEN];
        protected Edge[,] waitingEdges_Head = new Edge[2, MAXSENTLEN];
        private readonly Edge[,] waitingEdges_Tail = new Edge[2, MAXSENTLEN];

        protected static Edge GetWaitNext(Edge e, int right) => right != 0 ? e.waitingNext1_ : e.waitingNext0_;
        private static void SetWaitNext(Edge e, int right, Edge next)
        {
            if (right != 0) e.waitingNext1_ = next;
            else e.waitingNext0_ = next;
        }

        protected void WaitListAddTail(int right, int loc, Edge e)
        {
            SetWaitNext(e, right, null);

            Edge tail = waitingEdges_Tail[right, loc];
            if (tail == null)
            {
                waitingEdges_Head[right, loc] = e;
                waitingEdges_Tail[right, loc] = e;
                return;
            }

            SetWaitNext(tail, right, e);
            waitingEdges_Tail[right, loc] = e;
        }

        protected double crossEntropy_;
        protected int wrd_count_;
        protected int poppedEdgeCount_;
        protected int totEdgeCountAtS_;
        protected int poppedEdgeCountAtS_;
        protected int ruleiCounts_; // keeps track of how many edges have been
                                    // created --- used to time out the parse
        //Item*           pretermItems[4000];
        protected int pretermNum;
        int endPos;
        protected static int ruleiCountTimeout_ = 360000; //how many rulei's before we time out.
        protected static int poppedTimeout_ = 50000;


        // Scratch Buffers

        public static double [][] s_setAlphas_ScratchBuffer = new double[MAXNUMTHREADS][];
        private double [] Get_setAlphas_ScratchBuffer(int thrdid)
        {
            var s = s_setAlphas_ScratchBuffer[thrdid];
            if (s == null)
                s_setAlphas_ScratchBuffer[thrdid] = s = new double[400];
            return s;
        }


        static ChartBase()
        {
            for (int i = 0; i < itemsToDelete.Length; i++)
                itemsToDelete[i] = new vector<Item>();
        }


        protected ChartBase(SentRep sentence, int id)
        {
            for (int i = 0; i < regs.GetLength(0); i++)
                for (int j = 0; j < regs.GetLength(1); j++)
                    regs[i, j] = new Items();

            //for (int i = 0; i < waitingEdges.GetLength(0); i++)
            //    for (int j = 0; j < waitingEdges.GetLength(1); j++)
            //        waitingEdges[i, j] = new list<Edge>();


            thrdid = id;
            sentence_ = sentence;
            crossEntropy_ = 0.0;
            wrd_count_ = 0;
            poppedEdgeCount_ = 0;
            ruleiCounts_ = 0;


#if BLLIP_DEBUG
            extern int	rulei_high_water;
            rulei_high_water = 0;
#endif // DEBUG

            numItemsToDelete[id] = 0;
            wrd_count_ = sentence.length();
            endPos = wrd_count_;
            string endwrd = null;
            if (wrd_count_ > 0)
                endwrd = sentence_.op(wrd_count_ - 1).lexeme();

            if (endwrd != null && finalPunc(endwrd))
            {
                endPos = wrd_count_-1;
            }
            else if (wrd_count_ > 2)
            {
                endwrd = sentence.op(wrd_count_ - 2).lexeme();
                if (finalPunc(endwrd))
                {
                    endPos = wrd_count_-2;
                }
                else
                {
                    endwrd = sentence.op(wrd_count_ - 3).lexeme();
                    if (finalPunc(endwrd))
                        endPos = wrd_count_-3;
                }
            }
        }

        //virtual ~ChartBase();


        protected void ResetChartBase(SentRep sentence)
        {
            sentence_ = sentence;

            crossEntropy_ = 0.0;
            poppedEdgeCount_ = 0;
            ruleiCounts_ = 0;
            totEdgeCountAtS_ = 0;
            poppedEdgeCountAtS_ = 0;

            wrd_count_ = sentence.length();
            numItemsToDelete[thrdid] = 0;

            // recompute endPos like ctor
            endPos = wrd_count_;
            string endwrd = null;
            if (wrd_count_ > 0)
                endwrd = sentence_.op(wrd_count_ - 1).lexeme();

            if (endwrd != null && finalPunc(endwrd))
            {
                endPos = wrd_count_ - 1;
            }
            else if (wrd_count_ > 2)
            {
                endwrd = sentence.op(wrd_count_ - 2).lexeme();
                if (finalPunc(endwrd))
                {
                    endPos = wrd_count_ - 2;
                }
                else
                {
                    endwrd = sentence.op(wrd_count_ - 3).lexeme();
                    if (finalPunc(endwrd))
                        endPos = wrd_count_ - 3;
                }
            }

            // clear only touched
            for (int t = 0; t < (int)regsTouchedList.size(); t++)
            {
                int packed = regsTouchedList.at(t);
                int diff = packed / MAXSENTLEN;
                int st = packed - (diff * MAXSENTLEN);

                regs[diff, st].clear();
                regsTouched[diff, st] = false;
            }

            regsTouchedList.clear();

            // Clear intrusive waiting lists (heads/tails) for the next parse.
            // We clear all slots because it's only 2 * MAXSENTLEN, which is cheap and avoids tracking touched indices.
            for (int right = 0; right < 2; right++)
            {
                for (int loc = 0; loc < MAXSENTLEN; loc++)
                {
                    waitingEdges_Head[right, loc] = null;
                    waitingEdges_Tail[right, loc] = null;
                }
            }
        }


        //enum Err { OK, OVERFLW, FAILURE };

        // parsing functions, what the class is all about.
        public abstract double parse();


        // extracting information about the parse.
        public void set_Alphas()
        {
            Item snode = get_S();
            double [] tempAlpha = Get_setAlphas_ScratchBuffer(thrdid);  //double [] tempAlpha = new double[400]; //400 has no particular meaning, just large enough.
  
            if (snode == null || snode.prob() == 0.0)
            {
                WARN("estimating the counts on a zero-probability sentence");
                return;
            }

            double sAlpha = 1.0 / snode.prob();
            snode.poutside() = sAlpha;
  
            /* for each position in the 2D chart, starting at top*/
            /* look at every bucket of length j */
            for (int j = wrd_count_ - 1; j >= 0; j--)
            {
                for (int i = 0; i <= wrd_count_ - j; i++)
                {
                    Items il = regs[j, i];
                    //var ili = il.First;  //list<Item*>::iterator ili = il.begin();
                    int ilCount = (int)il.size();
                    for (int k = 0; k < ilCount; k++)  //for (; ili != null; ili = ili.Next)  //for(; ili != il.end(); ili++ )
                    {
                        Item itmInit = il.at(k);  //itm = ili.Value;  //itm = *ili;
                        if (itmInit != snode)
                            itmInit.poutside() = 0; //init outside probs to 0;
                    }

                    bool valuesChanging = true;
                    /* do alpha calulcations until values settle down */
                    //ili = il.First;  //ili = il.begin();
                    while (valuesChanging)
                    {
                        valuesChanging = false;
                        int tempPos = 0;  //position in tempAlpha;
                        //ili = il.First;  //ili = il.begin();
                        for (int k = 0; k < ilCount; k++)  //for (; ili != null; ili = ili.Next)  //for(; ili != il.end(); ili++ )
                        {
                            Item itm = il.at(k);  //itm = ili.Value;  //itm = *ili;
                            if (itm == snode)
                                continue;

                            double itmalpha = 0;

                            NeedmeIter nmi = new NeedmeIter(itm);
                            while (nmi.next(out Edge e))
                            {
                                Item lhsItem = e.finishedParent();
                                if (lhsItem != null)
                                    itmalpha += lhsItem.poutside() * e.prob();
                            }

                            AssertInternal(tempPos < 400);
                            double val = itmalpha / itm.prob();
                            tempAlpha[tempPos++] = val;
                        }

                        /* at this point the new alpha values are stored in tempAlpha */
                        int temppos = 0;
                        //ili = il.First;  //ili = il.begin();
                        for (int k = 0; k < ilCount; k++)  //for (; ili != null; ili = ili.Next)  //for(; ili != il.end(); ili++ )
                        {
                            Item itm = il.at(k);  //itm = ili.Value;  //itm = *ili;
                            if (itm == snode)
                                continue;

                            /* the start symbol for the entire sentence has poutside =1*/
                            if (i == 0 && j == wrd_count_ - 1 && itm.term().isRoot())
                            {
                                itm.poutside() = sAlpha;
                            }
                            else
                            {
                                double oOutside = itm.poutside();
                                double nOutside = tempAlpha[temppos];
                                if (nOutside == 0)
                                {
                                    if (oOutside != 0)
                                        error("Alpha went down");
                                }
                                else if (oOutside / nOutside < 0.95)
                                {
                                    itm.poutside() = nOutside;
                                    valuesChanging = true;
                                    //cerr << "alpha*beta " << *itm << " = "
                                    //<< (itm->poutside() * itm->prob()) << endl;
                                }
                            }

                            temppos++;
                        }

                        if (temppos != tempPos)
                        {
                            Console.Write("temppos = " + temppos + " and tempPos = " + tempPos + " ");
                            error("Funnly situation in setAlphas");
                        }
                    }
                }
            }
        }


        //const Items&    items( int i, int j ) const
        //        {   return regs[ i ][ j ];   }
        //int             edgeCount() const    { return ruleiCounts_; }
        //int             poppedEdgeCount() const    { return poppedEdgeCount_; }
        //int             poppedEdgeCountAtS() const    { return poppedEdgeCountAtS_; }
        //int             totEdgeCountAtS() const    { return totEdgeCountAtS_; }


        protected Item addtochart(Term trm)
        {
            if (numItemsToDelete[thrdid] >= itemsToDeletesize[thrdid])
            {
                Item dummy = new Item(trm, 0, 0);
                itemsToDelete[thrdid].push_back(dummy);
                itemsToDeletesize[thrdid]++;
            }

            Item ans = itemsToDelete[thrdid][numItemsToDelete[thrdid]++];
            ans.set(trm, 0);
            return ans;
        }


        //// printing information about the parse.
        //const Item*     mapProbs();


        static bool finalPunc(string wrd)
        {
            ECString wd = new ECString(wrd);
            int eiIdx = 0;  //ECStringsIter ei = Term::Colons.begin();
            for ( ; eiIdx < Term.Colons.Count; eiIdx++)  //for( ; ei!= Term::Colons.end() ; ei++)
                if (wrd == Term.Colons[eiIdx])
                    return true;

            eiIdx = 0;  //ei = Term::Finals.begin();
            for ( ; eiIdx < Term.Finals.Count; eiIdx++)  //for( ; ei!= Term::Finals.end() ; ei++)
                if (wrd == Term.Finals[eiIdx])
                    return true;

            return false;
        }


        public Item topS() { return get_S(); }


        //static int&	    ruleCountTimeout()  {   return ruleiCountTimeout_;   }


        protected int effEnd(int pos)
        {
            bool ans;
            if (pos > endPos)
                return 0;

            if (pos == endPos)
                return 1;  //in case no final punc;

            string wrd = sentence_.op(pos).lexeme();
            if (finalPunc(wrd))
                ans = true;
            else if (pos > wrd_count_ -3)
                ans = false;
            else if (wrd == ",")
            {
                if (sentence_.op(pos + 1).lexeme() == "''")
                    ans = true; // ,'' acts like end of sentence;
                else
                    ans = false;  //ans = 2 for alt version???
            }
            else
                ans = false;

            return ans ? 1 : 0;
        }


        //void            setGuide(InputTree* tree);
        //void            addConstraint(int start, int end, int term);


        protected Item get_S()
        {
            Term sterm = Term.rootTerm;
            Items il = regs[wrd_count_ - 1, 0];
            //var ili = il.First;  //Items::iterator ili = il.begin();
            int count = (int)il.size();
            for (int i = 0; i < count; i++)  //for (; ili != null; ili = ili.Next)  //for (; ili != il.end(); ili++ )
            {
                Item itm = il.at(i);  //itm = ili.Value;  //itm = *ili;
                if (itm.term() == sterm)
                    return itm;
            }

            return null;
        }


        protected bool inGuide(int st, int ed, int trm) { throw new NotImplementedException(); }
        protected bool inGuide(Edge e) { throw new NotImplementedException(); }


        protected float endFactorComp(Edge dnrl)
        {
            int start = dnrl.start();
            int finish = dnrl.loc();
            int effVal = effEnd(finish);
            ECString trmNm = new ECString(dnrl.lhs().name());
            Term trm = Term.get(trmNm);
            if ((trm.isRoot() || trm.isS()) && finish == wrd_count_ && start == 0)
                return endFactor;
            else if (effVal == 1)
                return endFactor;
            else if (effVal == 0)
                return midFactor;
            else
                return 0.95f;  //if effVal == 2, currently not used;
        }


        //void            free_chart_items(Items& itms);
        //void            free_chart_itm(Item * itm);
        //void            free_edges(list<Edge*>& edges);
    }
}
