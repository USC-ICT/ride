using System;
using System.Diagnostics;


namespace BllipParser.DotNet.Vanilla
{
    class GotIter
    {
        Edge whereIam;


        public GotIter(Edge edge)
        {
            whereIam = edge;
        }


        public bool next(out Item itm)
        {
            if (whereIam == null || whereIam.item() == null)
            {
                itm = null;
                return false;
            }
            else
            {
                itm = whereIam.item();
                whereIam = whereIam.pred();
                return true;
            }
        }
    }


    class LeftRightGotIter
    {
        Item [] lrarray = new Item[400];
        int pos_;
        int size_;


        public LeftRightGotIter(Edge edge)
        {
            makelrgi(edge);
        }


        public bool next(out Item itm)
        {
            itm = null;
            if (pos_ >= size_)
                return false;

            Debug.Assert(pos_ < 400);
            itm = lrarray[pos_];
            pos_++;
            return true;
        }

        public Item index(int i) { Debug.Assert(i < 400); return lrarray[i]; }
        public int size() { return size_; }
        //int&    pos() { return pos_; }


        void makelrgi(Edge ri)
        {
            GotIter gi = new GotIter(ri);
            bool finishedRight = false;
            int spos = ri.start();
            /* gotiter return a b head c d in the order d c a b head */
            //list<Item*>::iterator lri;
            list<Item> lrlist = new list<Item>();
            var lri = lrlist.First;
            while (gi.next(out Item itm))
            {
                //cerr << "lrgi " << *itm << endl;
                if (finishedRight || itm.start() == spos)
                {
                    // if 1st consits is STOP(3, 3) then next can have same start pos.
                    if (itm.start() == spos && !finishedRight)
                    {
                        finishedRight = true;
                        lri = lrlist.First;  //lri = lrlist.begin();
                    }

                    //lri = lrlist.insert(lri, itm);
                    if (lri == null)
                    {
                        lrlist.push_back(itm);
                        lri = lrlist.Last;
                    }
                    else
                    {
                        lri = lrlist.insert(lri, itm);
                    }
                    lri = lri.Next;  //lri++;
                }
                else
                {
                    lrlist.push_front(itm);
                }
            }

            lri = lrlist.First;  //lri = lrlist.begin();
            int i = 0;
            for ( ; lri != null; lri = lri.Next)  //for ( ; lri != lrlist.end() ; lri++)
            {
                Debug.Assert(i < 400);
                lrarray[i] = lri.Value;  //lrarray[i] = (*lri);
                i++;
            }

            size_ = i;
            pos_ = 0;
        }
    }


    class MiddleOutGotIter
    {
        Item [] lrarray = new Item[400];
        int pos_;
        int size_;
        int dir_;
        Item firstRight_;


        public MiddleOutGotIter(Edge e)
        {
            GotIter gi = new GotIter(e);
            bool startRight = false;
            int spos = e.start();
            /* gotiter return a b head c d in the order d c a b head */
            int i = 0;
            while (gi.next(out Item itm))
            {
                Debug.Assert(i < 400);
                lrarray[i] = itm;
                //cerr << "lrgi " << *itm << endl;
                if (itm.start() == spos && !startRight)
                {
                    startRight = true;
                    Debug.Assert(i > 0);
                    firstRight_ = lrarray[i - 1];
                }

                i++;
            }

            size_ = i;
            pos_ = i-1;
            //if(i > 20) cerr << "MOGII " << size_ << " " << *firstRight_ << endl;
        }


        public bool next(out Item itm, out int dir)
        {
            itm = null;
            dir = -1;

            //if(pos_ > 20) cerr << "MOGI pos " << pos_ << " " << size_ << " " << *firstRight_<<endl;
            if (pos_ < 0)
                return false;

            Debug.Assert(pos_ < 400);
            itm = lrarray[pos_];
            //if(pos_ > 20) cerr << "MOGI itm " << *itm << endl;
            dir = dir_;
            if (pos_ == size_ - 1)
            {
                dir = 0;
                dir_ = 1;
            }

            if (itm == firstRight_)
            {
                dir = 2;
                dir_ = 2;
            }

            pos_--;
            //if(pos_ > 20) cerr << "AA" << endl;
            return true;
        }


        //int     size() const { return size_; }
        //int     dir() { return dir_; }

        //void         makelrgi(Edge* edge);
    }


    //class           SuccessorIter
    //{
    //    public:
    //    SuccessorIter(Edge* edge) : edge_(edge), edgeIter( edge->sucs().begin() ) {}
    //    bool    next(Edge*& itm);
    //    private:
    //    Edge*  edge_;
    //    list<Edge*>::iterator edgeIter;
    //}


    class NeedmeIter
    {
        ////int          stackptr;
        ////  Edge*        stack[64000]; //??? was 32;
        vector<Edge> stack = new vector<Edge>();


        public NeedmeIter(Item itm)
        {
            //stack.reserve(64000);
            stack.assign(itm.needme().GetList());
        }


        public bool next(out Edge e)
        {
            e = null;

            if (stack.size() == 0)
                return false;

            e = stack.back();
            stack.pop_back();
            stack.AddRange(e.sucs().GetList());
            return true;
        }
    }
}
