using System;
using System.Diagnostics;

using BstMap = BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>;  //typedef map<CntxArray, Bst, less<CntxArray> > BstMap;
using Bsts = BllipParser.DotNet.Vanilla.list<BllipParser.DotNet.Vanilla.Bst>;  //typedef list<Bst*> Bsts;

using static BllipParser.DotNet.Vanilla.Bst_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    static partial class Bst_global
    {
        public const int NORMALVAL = 0;
        public const int TERMINALVAL = 1;
        public const int EXTRAVAL = 2;
    }


    //typedef list<Bst*> Bsts;
    //typedef vector<short> shorts;
    //typedef shorts::iterator shortIter;


    class Val
    {
        public short status;
        short len_;
        double prob_;
        Edge edge_;
        short trm_;
        int wrd_;
        Bsts bsts_ = new Bsts();
        vector<short> vec_ = new vector<short>();


        public Val()
        {
            status = NORMALVAL;
            len_ = 1;
            prob_ = 0;
            edge_ = null;
            trm_ = -1;
            wrd_ = -1;


            vec_.push_back(0);
        }

        public Val(Edge e, double prb)
        {
            status = NORMALVAL;
            len_ = 0;
            prob_ = prb;
            edge_ = e;
            wrd_ = -2;


            trm_ = (short)e.lhs().toInt();
            AssertInternal(trm_ >= 0 && trm_ < 400);
        }

        public Val(Val oval)
        {
            throw new NotImplementedException();
        }

        //~Val();


        public static Val newIth(int ith, Val oval, ref bool stop) { throw new NotImplementedException(); }


        public Edge edge() { return edge_; }
        public Bsts bsts() { return bsts_; }
        public short len() { return len_; }
        //short& len() { return len_; }

        public short trm()
        {
            if (status == EXTRAVAL)
            {
                if (len() != 1)
                    return trm_;
                else
                    return bsts_.front().Value.nth(0).trm();
            }
            else
            {
                return trm_;
            }
        }

        public ref short trm1() { return ref trm_; }
        public int wrd() { throw new NotImplementedException(); }
        public ref int wrd1() { return ref wrd_; }
        public vector<short> vec() { return vec_; }
        //vector<short> vec() const { return vec_; }
        //short&  vec(int i) { return vec_[i]; }
        public ref double prob() { return ref prob_; }
        public double fom() { return prob_; }

        public void extendTrees(Bst bst2, int pos)
        {
            len_++;
            vec_.push_back(0);
            if (bsts_.size() == 0)
                bsts_.push_back(bst2);
            else if (pos < 2)
                bsts_.push_front(bst2);
            else
                bsts_.push_back(bst2);

            prob_ *= bst2.prob();
        }


        //friend ostream& operator<<(ostream& os, const Val& v);
        //friend bool operator==(Val& v1, Val& v2);
        //bool check();
    }


    class Bst
    {
        ValHeap heap = new ValHeap();

        bool explored_;
        bool done_;
        int num_;
        double sum_;
        vector<Val> nbest = new vector<Val>();


        public Bst()
        {
            explored_ = false;
            done_ = false;
            num_ = 0;
            sum_ = 0;
        }

        //~Bst();


        public Val next(int n)
        {
            //int hsz = heap.size();
            //cerr << "Need " << n << "th variation out of " << num()
            // << " with " << hsz << " on heap. " << done_<< endl;
            AssertInternal(n <= num());

            if (num() > n)
                return nbest[n];

            if (done_)
                return null;

            double oprob = 1;
            if (n > 0)
            {
                AssertInternal(nbest[n - 1] != null);
                oprob = nbest[n - 1].prob();
            }

            Val val = nbest[n - 1];
            //cerr << "   For " << *val << endl;
  
            for (int i = 0; i < val.len(); i++)
            {
                bool stop = false;
                Val nv = Val.newIth(i, val, ref stop);
                if (nv != null)
                {
                    //cerr << "Got the possible variation " 
                    //   << *nv  << " "<< heap.size()<< " "
                    //   << i << " " << n << " " << num() << endl;
                    //assert(ptst(nv));
                    heap.push(nv);
                }

                if (stop)
                    break;
            }

            if (heap.size() == 0)
            {
                done_ = true;
                return null;
            }

            Val ans = heap.pop();

            //assert(ans->check());
            //cerr << "The desired variation is " << *ans << " " << num_ << " "
            //   << hsz << " " << heap.size() << endl;
            /*
            if(!ans->edge())
            {
            if(!ans->len() == 1)
            {
            cerr << "Odd situation" << endl;
            }
            else
            {
            int wh2 = ans->vec(0);
            Bst& b2 = ithBst(0,ans->bsts());
            Val* subans = b2.nth(wh2);
            if(!subans->edge())
            {
            cerr << "Odd 2" << endl;
            }
            cerr << "Subedge = " << *subans->edge() << endl;
            }
            }
            */

            nbest.push_back(ans);
            num_++;
            return ans;
        }


        public ref bool explored() { return ref explored_; }
        public Val nth(int i) { return nbest[i]; }
        public int num() { return num_; }
        //int& num() { return num_; }
        public bool empty() { return num_ == 0; }
        public double prob() { return num_ == 0 ? 0 : nbest[0].prob(); }
        public ref double sum() { return ref sum_; }
        public void push(Val val) { heap.push(val); }
        public Val pop() { return heap.pop(); }
        public void addnth(Val val)
        {
            num_++;
            nbest.push_back(val);
        }
        //static void tester(Val* val);
        //bool ptst(Val* val);
    }


    //typedef map<CntxArray, Bst, less<CntxArray> > BstMap;


    static partial class Bst_global
    {
        public static Bst bstFind(CntxArray ca, BstMap bm)
        {
            var bi = bm.find(ca);
            if (bi == default)
            {
                //return bm[ca];
                if (bm.GetDictionary().ContainsKey(ca))
                {
                    return bm[ca];
                }
                else
                {
                    var bst = new Bst();
                    bm.GetDictionary().Add(ca, bst);
                    return bst;
                }
            }
            else
            {
                return bi;
            }
        }


        //Bst&  ithBst(int i, Bsts& bsts);
    }
}
