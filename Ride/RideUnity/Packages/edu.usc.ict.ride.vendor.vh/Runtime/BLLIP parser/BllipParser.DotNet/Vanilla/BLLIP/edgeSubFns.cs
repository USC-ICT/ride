using System;
using System.Diagnostics;

using static BllipParser.DotNet.Vanilla.MeChart_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    static class edgeSubFns
    {
        public static Func<FullHist, int> [] edgeFnsArray = new Func<FullHist, int> [24];  //int (*edgeFnsArray[24])(FullHist*);


        static int edge_term(FullHist fh) { Edge edge = fh.e; return edge.lhs().toInt(); }
        static int edge_noInfo(FullHist fh) { return -2; }

        static int stopTermInt = -1;
        static int edge_ngram(FullHist fh, int n, int l)
        {
            Edge edge = fh.e;
            if (stopTermInt < 0)
                stopTermInt = Term.stopTerm.toInt();

            AssertInternal(fh.cb != null);
            LeftRightGotIter lrgi = globalGi[fh.cb.thrdid];
            AssertInternal(lrgi != null);

            int pos = fh.pos;
            /* the left to right position we are working on is either the far left (0)
            or the far right */
            int hpos = edge.headPos();
            int m = pos + (n * l);
            //cerr << "eng " << n << " " << l << " " << fh->pos 
            //   << " " << m << " " << hpos << " " << *edge << endl;
            if (m < 0)
                return stopTermInt;

            if (m > hpos && l > 0)
            {
                return stopTermInt;
            }

            if (m >= lrgi.size())
                return stopTermInt;

            Item got = lrgi.index(m);
            AssertInternal(got != null);
            int ans = got.term().toInt();
            return ans;
        }

        static int edge_left0(FullHist fh) { return edge_ngram(fh, 0, 0); }
        static int edge_left1(FullHist fh) { return edge_ngram(fh, 1, 1); }
        static int edge_left2(FullHist fh) { return edge_ngram(fh, 2, 1); }
        static int edge_left3(FullHist fh) { return edge_ngram(fh, 3, 1); }
        static int edge_right1(FullHist fh) { return edge_ngram(fh, 1, -1); }
        static int edge_right2(FullHist fh) { return edge_ngram(fh, 2, -1); }
        static int edge_right3(FullHist fh) { return edge_ngram(fh, 3, -1); }

        static int edge_noopenQr(FullHist fh)
        {
            //Edge* edge = fh->e;
            int pos = fh.pos;
            AssertInternal(fh.cb != null);
            LeftRightGotIter lrgi = globalGi[fh.cb.thrdid];
            Item got;
            int i;
            bool sawOpen = false;
            for (i = 0; i < lrgi.size(); i++)
            {
                if (i == pos)
                    break;

                //if(i >= pos-3) break; //??? -3 because we already know about last3;
                got = lrgi.index(i);
                Term trm = got.term();
                if (trm.isOpen())
                    sawOpen = true;
                else if (trm.isClosed())
                    sawOpen = false;
            }

            if (sawOpen)
                return 0;
            else
                return 1;
        }


        static int edge_noopenQl(FullHist fh)
        {
            Edge edge = fh.e;
            int pos = fh.pos;
            int hpos = edge.headPos();
            LeftRightGotIter lrgi = globalGi[fh.cb.thrdid];
            Item got;
            int i;
            bool sawOpen = false;
            for (i = hpos; i >= 0; i--)
            {
                if (i == pos)
                    break;

                //if(i <= (pos+3)) break; //??? +3 because we already know about next 3;
                got = lrgi.index(i);
                Term trm = got.term();
                if (trm.isClosed())
                    sawOpen = true;
                else if (trm.isOpen())
                    sawOpen = false;
            }

            if (sawOpen)
                return 0;
            else
                return 1;
        }

        static int edge_Bl(FullHist fh) { return fh.preTerm; }
        static int edge_Br(FullHist fh) { throw new NotImplementedException(); }
        static int edge_true(FullHist fh) { return 1; }


        public static void addEdgeSubFeatureFns()
        {
            /*
            0 t  edge_term
            1 l  edge_noInfo
            2 u  edge_noInfo
            3 h  edge_noInfo
            4 i  edge_noInfo
            5 T  edge_true
            6 v  edge_noInfo
            7 b  edge_noInfo
            8 a  edge_noInfo
            9 m  edge_noInfo
            10 w edge_noInfo
            11 j edge_noInfo
            12 c edge_noInfo
            13 L1 edge_left1
            14 L2 edge_left2
            15 R1 edge_right1
            16 R2 edge_right2
            17 Qr edge_noopenQr
            18 L0 edge_left0;
            19 L3 edge_left3
            20 R3 edge_right3
            21 Qr edge_noopenQl
            22 Bl edge_Bl
            23 Br edge_Br
            */
            Func<FullHist, int> [] funs  //int (*funs[24])(FullHist*)
            = { edge_term, edge_noInfo, edge_noInfo, edge_noInfo,
                edge_noInfo, edge_true, edge_noInfo, edge_noInfo, edge_noInfo,
                edge_noInfo,edge_noInfo,edge_noInfo,
                edge_noInfo, edge_left1, edge_left2, edge_right1, edge_right2,
                edge_noopenQr, edge_left0,edge_left3,edge_right3,edge_noopenQl,
                edge_Bl,edge_Br };

            int i;
            for (i = 0; i < 24; i++)
                edgeFnsArray[i] = funs[i];
        }
    }
}
