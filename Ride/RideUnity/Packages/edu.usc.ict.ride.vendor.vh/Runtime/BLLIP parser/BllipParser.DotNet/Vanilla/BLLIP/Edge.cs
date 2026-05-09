using System;


namespace BllipParser.DotNet.Vanilla
{
    class Edge
    {
        const double DEMFAC = 0.999;
        //#define EDGE_CHUNKSIZE		1000


        //friend class GotIter;
        //friend class SuccessorIter;
        //friend class LeftRightGotIter;


        //static int      numEdges;
        //static float    DemFac;


        Term lhs_;
        short loc_;
        Item finishedParent_;
        Edge pred_;
        short start_;
        //short num_;
        short status_; 
        Item item_;
        int heapPos_;
        int demerits_;

        double leftMerit_;
        double rightMerit_;
        double prob_;
        double merit_;
        //list<Edge> sucs_ = new list<Edge>();
        // Intrusive successor chain (stack-like). This replaces list<Edge> sucs_.
        // Successors are pushed as head nodes, matching the original push_front behavior.
        Edge sucsHead_;
        Edge sucsNext_;

        // One next pointer per side (right=0 and right=1).
        public Edge waitingNext0_;
        public Edge waitingNext1_;


        public Edge(Term trm)  //Edge(ConstTerm* trm);
        {
            lhs_ = trm;
            loc_ = -1;
            finishedParent_ = null;
            pred_ = null;
            start_ = -1;
            //num_ = -1;
            status_ = 0;
            item_ = null;
            heapPos_ = -1;
            demerits_ = 0;
            prob_ = 1.2; // encourage constits???


            if (lhs_.isVP())
                prob_ *= 1.4; //???;

            // VPs are badly modeled by system;  This is only called during first parse.
        }

        public Edge(Edge src, Item itm, int right)
        {
            lhs_ = src.lhs_;
            loc_ = src.loc_;
            finishedParent_ = null;
            start_ = src.start_;
            //num_ = -1;
            status_ = (short)right;
            item_ = itm;
            heapPos_ = -1;
            demerits_ = src.demerits_;
            leftMerit_ = src.leftMerit();
            rightMerit_ = src.rightMerit();
            prob_ = src.prob();


            //numEdges++;
            if (start_ == -1)
                start_ = (short)itm.start();

            if (loc_ == -1)
                loc_ = (short)itm.finish();

            if (right != 0)
                loc_ = (short)itm.finish();
            else
                start_ = (short)itm.start();

            if (src.item_ == null) //it has no item
            {
                pred_ = null;
            }
            else
            {
                pred_ = src;
                //pred_.sucs_.push_front(this);
                // Intrusive push_front: this becomes the new head successor of pred_.
                sucsNext_ = pred_.sucsHead_;
                pred_.sucsHead_ = this;
                //cerr << *pred_ << " has suc " << *this << endl;
            }

            prob_ *= itm.prob();
        }

        public Edge(Item itm)
        {
            lhs_ = itm.term();
            loc_ = (short)itm.finish();
            finishedParent_ = itm;
            pred_ = null;
            start_ = (short)itm.start();
            //num_ = -1;
            status_ = 2;
            item_ = null;
            heapPos_ = -1;
            demerits_ = 0;
            leftMerit_ = 1;
            rightMerit_ = 1;
            prob_ = itm.prob();


            ////numEdges++;
        }

        //Edge( const Edge& src ) { error("edge copying no longer exists"); }

        public Edge()
        {
            throw new NotImplementedException();
            //: num_(-1)
        }


        //~Edge();


        //bool            check(); 

        //int 	    operator== (const Edge& rhs) { return this == &rhs; }
        //bool	    finished() const;
        public Term lhs() { return lhs_; }
        public ref int heapPos() { return ref heapPos_; }
        public short start() { return start_; }
        //short&	    start() {   return start_;   }
        //public list<Edge> sucs() { return sucs_; }
        //list<Edge*>&    sucs() { return sucs_; }
        public Edge sucsHead() { return sucsHead_; }
        public Edge sucsNext() { return sucsNext_; }

        // Called on a predecessor when we want to undo the last push_front().
        public void PopFirstSuccessor()
        {
            if (sucsHead_ != null)
                sucsHead_ = sucsHead_.sucsNext_;
        }

        public short loc() { return loc_; }
        //short&	    loc() {   return loc_;   }
        public Item item() { return item_; }
        public Edge pred() { return pred_; }
        public ref double prob() { return ref prob_; }


        public int headPos(int i = 0)
        {
            if (pred() == null)
                return i;

            Edge prd = pred();
            //cerr << "H " << *item() << endl;
            if (prd.start() > start())
            {
                return prd.headPos(i + 1);
            }
            else if (prd.start() == start() && item().term() == Term.stopTerm && item().start() == start())
            {
                //cerr << "HPST " << *(prd->item()) << " " << i << endl;
                return prd.headPos(i + 1);
            }
            else
            {
                return pred().headPos(i);
            }
        }


        public Item headItem()
        {
            GotIter gotiter = new GotIter(this);
            Item ans = null;
            Item next = null;
            while (gotiter.next(out next))  //the head will be the the last thing in gotiter;
                ans = next;

            return ans;  
        }


        /* only used in rParse, when merits are not used */
        //double           beta() const { return leftMerit_; }
        //double&          beta() { return leftMerit_; }

        public ref double leftMerit() { return ref leftMerit_; }
        public ref double rightMerit() { return ref rightMerit_; }
        public ref int demerits() { return ref demerits_; }
        //short           num() const { return num_; }
        //short&          num() { return num_; }

        public double merit() { return merit_; }
        public void setmerit() { merit_ = prob_ * leftMerit_ * rightMerit_ * Math.Pow(DEMFAC, demerits_); }
        public ref short status() { return ref status_; }

        string print()  //void print( ostream& os );
        {
            string os = "";
            if (item_ == null) //dummy rule
            {
                if (finishedParent_ != null)
                {
                    os += finishedParent_ + " -> ";
                }
                else
                {
                    os += lhs_ + " -> ";
                }
            }
            else
            {
                os += lhs_ + "(" + start() + ", " + loc_ + ") -> ";
                //LeftRightGotIter gi = new LeftRightGotIter(this);
                Item [] lrGotIterScratch = LeftRightGotIterThreadLocal.Get_lrGotIter_ScratchBuffer();
                LeftRightGotIter gi = new LeftRightGotIter(this, lrGotIterScratch);
                while (gi.next(out Item itm))
                {
                    if (itm.term() == Term.stopTerm)
                        continue;

                    os += itm + " ";
                }
            }

            return os;
        }

        public override string ToString()  //friend ostream& operator<< (ostream& os, Edge& edge )
        {
            return print();  //{ edge.print( os ); return os;}
        }

        public void setFinishedParent(Item par) { finishedParent_ = par; }
        public Item finishedParent() { return finishedParent_; }


        public int ccInd()
        {
            Term trm = lhs();
            int tint = trm.toInt();
            ECString tNm = trm.name();
            bool sawComma = false;
            bool sawColen = false;
            bool sawCC = false;
            int numTrm = 0;
            //LeftRightGotIter gi = new LeftRightGotIter(this);  
            Item [] lrGotIterScratch = LeftRightGotIterThreadLocal.Get_lrGotIter_ScratchBuffer();
            LeftRightGotIter gi = new LeftRightGotIter(this, lrGotIterScratch);
            int pos = 0;
            /*Change next line to indicate which non-terminals get specially
            marked to indicate that they are conjoined together */
            if (!trm.isNP() && !trm.isS() && !trm.isVP())
                return tint;

            while (gi.next(out Item itm))
            {
                Term subtrm = itm.term();
                if (subtrm == Term.stopTerm)
                    continue;

                if (subtrm == trm)
                    numTrm++;

                if (pos != 0 && subtrm.isCC())
                    sawCC = true;

                if (pos != 0 && subtrm.isComma())
                    sawComma = true;

                if (pos != 0 && subtrm.isColon())
                    sawColen = true;
      
                pos++;
            }

            if (trm.isNP() && numTrm == 2 && !sawCC)
                return Term.lastNTInt() + 1;

            if ((sawComma || sawColen || sawCC) && numTrm >= 2)
                return tint + Term.lastNTInt();

            return tint;
        }
    }


    //typedef list<Edge*> Edges;
}
