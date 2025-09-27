using System;
using System.Diagnostics;

using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    class EdgeHeap
    {
        const int HeapSize = 370000;


        bool print;

        int unusedPos_;
        Edge [] array = new Edge[HeapSize];


        public EdgeHeap()
        {
            int i;
            for (i = 0; i < HeapSize; i++)
                array[i] = null;
            print = false;
            unusedPos_ = 0;
        }

        //~EdgeHeap();


        public void insert(Edge edge)
        {
            if (print)
                Console.WriteLine("heap insertion of " + edge + " at " + unusedPos_);

            Debug.Assert(unusedPos_ < HeapSize);
            array[unusedPos_] = edge;
            edge.heapPos() = unusedPos_;
            upheap(unusedPos_);
            unusedPos_++;
            Debug.Assert(unusedPos_ < HeapSize);
        }


        public Edge pop()
        {
            if (print)
                Console.WriteLine("popping");

            if (unusedPos_ == 0)
                return null;

            Edge retVal = array[0];
            Debug.Assert(retVal.heapPos() == 0);
            del_(0);
            retVal.heapPos() = -1;
            return retVal;
        }


        public void del(Edge edge)
        {
            if (print)
                Console.WriteLine("del " + edge);

            int pos = edge.heapPos();
            Debug.Assert(pos < unusedPos_ && pos >= 0);
            del_( pos );
        }

        //Edge**  ar() { return array; }
        //int     size() { return unusedPos_; }
        //void    check();


        void del_(int pos)
        {
            if (print)
                Console.WriteLine("del_ " + pos);

            Debug.Assert(unusedPos_ != 0);
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

            array[pos].heapPos() = pos;
            array[unusedPos_ - 1] = null;
            unusedPos_--;
            if (upheap(pos))
                return;

            downHeap(pos);
        }


        void downHeap(int pos)
        {
            if (print)
                Console.WriteLine("downHeap " + pos);

            if (pos >= unusedPos_ - 1)
                return;

            Debug.Assert(pos < HeapSize);
            Edge par = array[pos];
            Debug.Assert(par.heapPos() == pos);
            double merit = par.merit();
            int lc = left_child(pos);
            int rc = right_child(pos);
            int largec;
            int lcthere = 0;
            Edge lct = null;
            if (lc < unusedPos_)
            {
                Debug.Assert(lc < HeapSize);
                lct = array[lc];
                if (lct != null)
                {
                    lcthere = 1;
                    Debug.Assert(lct.heapPos() == lc);
                }
            }

            int rcthere = 0;
            Edge rct = null;
            if (rc < unusedPos_)
            {
                rct = array[rc];
                if (rct != null)
                {
                    rcthere = 1;
                    Debug.Assert(rct.heapPos() == rc);
                }
            }

            if (lcthere == 0 && rcthere == 0)
                return;

            Debug.Assert(lcthere != 0);
            if (rcthere == 0 || (lct.merit() > rct.merit()))
                largec = lc;
            else
                largec = rc;

            Edge largeEdg = array[largec];
            if (merit >= largeEdg.merit())
            {
                if (print)
                    Console.WriteLine("downheap of " + merit + " stopped by " + largeEdg + " " + largeEdg.merit());

                return;
            }

            array[pos] = largeEdg;
            largeEdg.heapPos() = pos;
            array[largec] = par;
            par.heapPos() = largec;
            downHeap(largec);
        }


        bool upheap(int pos)
        {
            if (print)
                Console.WriteLine("in Upheap " + pos);

            if (pos == 0)
                return false;

            Debug.Assert(pos < HeapSize);
            Edge edge = array[pos];
            Debug.Assert(edge.heapPos() == pos);
            double merit = edge.merit();
            int parPos = parent(pos);
            Debug.Assert(parPos < HeapSize);
            Edge par = array[parPos];
            Debug.Assert(par.heapPos() == parPos);

            if (merit > par.merit())
            {
                Debug.Assert(parPos < HeapSize);
                array[parPos] = edge;
                edge.heapPos() = parPos;
                Debug.Assert(pos < HeapSize);
                array[pos] = par;
                par.heapPos() = pos;
                if (print)
                    Console.WriteLine("Put " + edge + " in " + parPos);

                upheap(parPos);
                return true;
            }
            else if (print)
            {
                Console.WriteLine("upheap of " + merit + "stopped by " + par + " " + par.merit());
            }

            return false;
        }


        int left_child(int par) { return (par * 2) + 1; }
        int right_child(int par) { return ((par * 2) + 2); }
        int parent(int child) { return (child - 1) / 2; }
    }
}
