using System;
using System.Diagnostics;

using BstMap = BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>;  //typedef map<CntxArray, Bst, less<CntxArray> > BstMap;
using Bsts = BllipParser.DotNet.Vanilla.list<BllipParser.DotNet.Vanilla.Bst>;  //typedef list<Bst*> Bsts;
using Vals = BllipParser.DotNet.Vanilla.vector<BllipParser.DotNet.Vanilla.Val>;  //typedef vector<Val*> Vals;

using static BllipParser.DotNet.Vanilla.Bst_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    //typedef vector<Val*> Vals;
    //typedef Vals::iterator ValsIter;


    class ValHeap
    {
        static bool print;

        int unusedPos_;
        Vals array = new Vals();


        public ValHeap()
        {
            unusedPos_ = 0;
        }

        //~ValHeap();


        public void push(Val atp)
        {
            AssertInternal(atp != null);
            if (print)
                Console.WriteLine("heap insertion of atp at " + unusedPos_);

            AssertInternal((int)array.size() >= unusedPos_);
            if ((int)array.size() == unusedPos_)
                array.push_back(atp);
            else
                array[unusedPos_] = atp;

            upheap(unusedPos_);
            unusedPos_++;
        }


        public Val pop()
        {
            if (print)
                Console.WriteLine("popping");

            if (unusedPos_ == 0)
                return null;

            Val retVal = array[0];
            del_(0);
            return retVal;
        }


        public int size() { return unusedPos_; }

        //Val*   index(int i) { return array[i]; }


        void del_(int pos)
        {
            if (print)
                Console.WriteLine("del_ " + pos);

            AssertInternal(unusedPos_ != 0);
            if (pos == (unusedPos_ - 1))
            {
                unusedPos_--;
                array[unusedPos_] = null;
                return;
            }

            /* move the final edge in heap to empty position */
            array[pos] = array[unusedPos_ - 1];
            if (array[pos] == null)
            {
                error("Never get here");
                return;
            }

            unusedPos_--;
            array[unusedPos_] = null;
            downHeap(pos);
        }


        void downHeap(int pos)
        {
            if (print)
                Console.WriteLine("downHeap " + pos);

            if (pos >= unusedPos_ - 1)
                return;

            Val par = array[pos];
            double merit = par.fom();
            int lc = left_child(pos);
            int rc = right_child(pos);
            int largec;
            int lcthere = 0;
            Val lct = null;
            if (lc < unusedPos_)
            {
                lct = array[lc];
                if (lct != null)
                    lcthere = 1;
            }

            int rcthere = 0;
            Val rct = null;
            if (rc < unusedPos_)
            {
                rct = array[rc];
                if (rct != null)
                    rcthere = 1;
            }

            if (lcthere == 0 && rcthere == 0)
                return;

            AssertInternal(lcthere != 0);

            if (rcthere == 0 || (lct.fom() > rct.fom()))
                largec = lc;
            else
                largec = rc;

            Val largeatp = array[largec];
            if (merit >= largeatp.fom()) 
            {
                if (print)
                    Console.WriteLine("downheap of " + merit + " stopped by " + " " + largeatp.fom());

                return;
            }

            array[pos] = largeatp;
            array[largec] = par;
            downHeap(largec);
        }


        bool upheap(int pos)
        {
            if (print)
                Console.WriteLine("in Upheap " + pos + " " + array.size());

            if (pos == 0)
                return false;

            Val atp = array[pos];
            AssertInternal(atp != null);
            double merit = atp.fom();
            int parPos = parent(pos);
            Val par = array[parPos];
            double pmerit = par.fom();

            if (print)
                Console.WriteLine("merits " + merit + " " + pmerit);

            if (merit > pmerit)
            {
                array[parPos] = atp;
                array[pos] = par;
                if (print)
                    Console.WriteLine("Put " + pos + " in " + parPos);

                upheap(parPos);
                return true;
            }
            else if (print)
            {
                Console.WriteLine("upheap of " + merit + "stopped by " + parPos + " " + pmerit);
            }

            return false;
        }


        int left_child(int par) { return (par * 2) + 1; }
        int right_child(int par) { return (par * 2) + 2; }
        int parent(int child) { return (child - 1) / 2; }
    }
}
