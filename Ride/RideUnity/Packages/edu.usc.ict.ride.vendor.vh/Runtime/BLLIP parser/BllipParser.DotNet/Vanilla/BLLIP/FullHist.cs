using System;
using System.Diagnostics;

using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    //typedef list<FullHist*>::iterator FullHistIter;


    class FullHist
    {
        int cpos;
        public Edge e;
        Item itm;
        public int term;
        public FullHist back;
        public int pos;
        public Wrd hd;
        public Bchart cb;
        public int hpos;
        public int preTerm;
        public FullHist [] fharray;  //public FullHist [] fharray = new FullHist[400];
        public int size;


        public FullHist()
        {
            cpos = 0;
            e = null;
            back = null;
            hd = null;
            cb = null;
        }

        public FullHist(Edge e1)
        {
            cpos = 0;
            e = e1;
            back = null;
            cb = null;
        }

        public FullHist(int tint, Bchart cb)
        {
            cpos = 0;
            term = tint;
            back = null;
            pos = -1;
            hd = null;
            this.cb = cb;
        }

        public FullHist(int tint, FullHist fh, Item i)
        {
            cpos = 0;
            itm = i;
            term = tint;
            back = fh;
            pos = -1;
            hd = null;
            cb = fh.cb;
        }


        private void Init(int term, FullHist back, Item itm, Bchart cb)
        {
            cpos = 0;
            e = null;
            this.itm = itm;
            this.term = term;
            this.back = back;
            pos = -1;
            hd = null;
            this.cb = cb;
            // leave hpos/preTerm as-is
        }


        public void InitForEdge(Edge edge, Bchart cb)
        {
            cpos = 0;
            e = edge;

            itm = null;
            term = 0;

            back = null;
            pos = -1;
            hd = null;

            this.cb = cb;

            // Match constructor-default behavior (avoid stale pooled values).
            hpos = 0;
            preTerm = 0;

            // Keep the pooled array, but reset logical state.
            size = 0;
        }


        public FullHist extendByEdge(Edge e1, Item [] lrScratchBuffer)
        {
            //cerr << "ebe " << *e1 << endl;
            if (back != null)
                AssertInternal(back.term != Term.stopTerm.toInt());

            //if(back) assert(back->term != 47);

            if (fharray == null)
                fharray = new FullHist[400];

            e = e1;
            LeftRightGotIter gi = new LeftRightGotIter(e1, lrScratchBuffer);
            int i = 0;
            while (gi.next(out Item itm))
            {
                int termInt = itm.term().toInt();
                //cerr << "ebei " << termInt << endl;
                AssertInternal(i < 400);

                //FullHist st = new FullHist(termInt, this, itm);
                //fharray[i++] = st;
                //st.cpos = 0;
                FullHist st = fharray[i];
                if (st == null)
                    fharray[i] = st = new FullHist();
                st.Init(termInt, this, itm, cb);
                i++;
            }

            //cerr << "ebe ret " << *fharray[hpos] << endl;
            size = i;
            cpos = hpos; //a mess.  hpos was set during meRule Prob;
            return fharray[cpos];
        }


        public FullHist extendBySubConstit()
        {
            int hp = back.hpos;
            ref int bcpos = ref back.cpos;

            if (bcpos > hp)
                bcpos++; //???;
            else if (bcpos == 0)
                bcpos = hp + 1;
            else
                bcpos--;

            ////cerr << "npcpos " << bcpos << " " << back->size << endl;
            if (bcpos < back.size)
                return back.fharray[bcpos];
            else
                return back;
        }


        public FullHist retractByEdge()
        {
            if (fharray != null && size > 0)
                Array.Clear(fharray, 0, size);
            size = 0;

            //assert(cpos == size);
            int i = 0;
            for (; i < size; i++)
            {
                //delete fharray[i];
            }

            return this;
        }


        //FullHist* nth(int n)
        //{
        //    if(n < 0 || n >= size) return NULL;
        //    else return fharray[n];
        //}


        public override string ToString()  //friend ostream& operator<<(ostream& os, const FullHist& fh);
        {
            string os = "";

            FullHist bfh = back;
            if (bfh != null)
            {
                os += bfh.term + "/";
                if (bfh.hd != null)
                    os += bfh.hd;

                os += "--";
            }

            os += term + "/";

            if (hd != null)
                os += hd;

            return os;
        }
    }
}
