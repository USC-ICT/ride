using System;
using System.Diagnostics;

using CRuleBundle = BllipParser.DotNet.Vanilla.vector<BllipParser.DotNet.Vanilla.ClassRule>;  //typedef vector<ClassRule> CRuleBundle;

using static BllipParser.DotNet.Vanilla.MeChart_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    static class fhSubFns
    {
        static int nullWordInt;


        static int fh_term(FullHist fh)
        {
            return fh.term;
        }

        static int fh_parent_term(FullHist fh) { throw new NotImplementedException(); }

        static int toBe(in string parw)
        {
            if (parw == "am" || parw == "was" || parw == "is" || parw == "be" || parw == "been" || parw == "are" || parw == "were" || parw == "being")
                return 1;
            else
                return 0;
        }

        static int fh_parent_pos_stopint = 0;
        static int fh_parent_pos(FullHist fh)
        {
            if (fh_parent_pos_stopint == 0)
            {
                ECString stopnm = new ECString("STOP");
                fh_parent_pos_stopint = Term.get(stopnm).toInt();
            }

            FullHist par = fh.back;
            if (par == null)
                return fh_parent_pos_stopint;

            int ans = par.preTerm;
            if (ans < 2 && toBe(par.hd.lexeme()) != 0)
                return 48;

            return ans;
        }

        static int fh_term_before_stopint = 0;
        static int fh_term_before(FullHist fh)
        {
            if (fh_term_before_stopint == 0)
            {
                ECString stopnm = new ECString("STOP");
                fh_term_before_stopint = Term.get(stopnm).toInt();
            }

            FullHist par = fh.back;
            if (par == null)
                return fh_term_before_stopint;

            int i = 0;
            for ( ; i < par.size; i++)
            {
                FullHist st = par.fharray[i];
                if (st != fh)
                    continue;

                if (i == 0)
                {
                    return fh_term_before_stopint;
                }

                st = par.fharray[i - 1];
                AssertInternal(st != null);
                return st.term;
            }

            error("Should never get here fh_term_b");
            return -1;
        }


        static int fh_term_after(FullHist fh) { throw new NotImplementedException(); }

        static int fh_pos(FullHist fh)
        {
            return fh.preTerm;
        }

        static int fh_head(FullHist tree)
        {
            int ans = tree.hd.toInt();
            AssertInternal(ans >= -1);
            return ans;
        }

        static int fh_parent_head(FullHist tree)
        {
            Val spectree = null;
            if (Feature.isLM || Feature.useExtraConditioning) 
                spectree = tree_ruleTree(tree, 2);

            if (spectree != null)
            {
                //cerr << "found  st " << endl;
                return spectree.wrd();
            }

            FullHist pt = tree.back;
            if (pt == null)
                return nullWordInt;

            int ans = pt.hd.toInt();
            AssertInternal(ans >= -1);
            return ans;
        }

        static int fh_grandparent_head(FullHist tree) { throw new NotImplementedException(); }

        static int fh_grandparent_term(FullHist fh)
        {
            int s1int = Term.rootTerm.toInt();
            FullHist par = fh.back;
            if (par == null)
                return s1int;

            FullHist gpar = par.back;
            if (gpar == null)
                return s1int;
            else
                return gpar.term;
        }

        static int fh_grandparent_pos(FullHist fh) { throw new NotImplementedException(); }

        static int fh_ccparent_term(FullHist h)
        {
            int s1int = Term.rootTerm.toInt();
            FullHist par = h.back;
            if (par == null)
                return s1int;

            int trmInt = par.term;
            if (trmInt != h.term)
                return trmInt;

            int ccedtrmInt = par.e.ccInd();
            return ccedtrmInt;
        }

        static int fh_size(FullHist fh) { throw new NotImplementedException(); }
        static int fh_effEnd(FullHist h) { throw new NotImplementedException(); }
        //int is_effEnd(FullHist* tree, FullHist* child)
        static int fh_vE(FullHist treeh) { throw new NotImplementedException(); }
        static int fh_mE(FullHist treeh) { throw new NotImplementedException(); }
        static int fh_true(FullHist h) { return 1; }

        static int fh_ngram_stopTermInt = -1;
        static int fh_ngram(FullHist fh, int n, int l)
        {
            //cerr << "fhng " << n << " " << l << " "
            //   << fh->pos << " " << *fh->e << endl;
            if (fh_ngram_stopTermInt < 0)
                fh_ngram_stopTermInt = Term.stopTerm.toInt();

            int pos = fh.pos;
            int hpos = fh.hpos; //???;
            int m = pos + (n * l);
            if (m < 0)
                return fh_ngram_stopTermInt;

            if (m > hpos && l > 0)
                return fh_ngram_stopTermInt;

            AssertInternal(fh.cb != null);
            LeftRightGotIter lrgi = globalGi[fh.cb.thrdid];
            AssertInternal(lrgi != null);
            if (m >= lrgi.size())
                return fh_ngram_stopTermInt;

            Item got = lrgi.index(m);
            AssertInternal(got != null);
            int ans = got.term().toInt();
            return ans;
        }

        static int fh_left0(FullHist fhh) { return fh_ngram(fhh, 0, 0); }
        static int fh_left1(FullHist fhh) { return fh_ngram(fhh, 1, 1); }
        static int fh_left2(FullHist fhh) { return fh_ngram(fhh, 2, 1); }
        static int fh_left3(FullHist fhh) { return fh_ngram(fhh, 3, 1); }
        static int fh_right1(FullHist fhh) { return fh_ngram(fhh, 1, -1); }
        static int fh_right2(FullHist fhh) { return fh_ngram(fhh, 2, -1); }
        static int fh_right3(FullHist fhh) { return fh_ngram(fhh, 3, -1); }

        static int fh_noopenQr(FullHist fh)
        {
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

        static int fh_noopenQl(FullHist fh)
        {
            int pos = fh.pos;
            int hpos = fh.hpos;
            AssertInternal(fh.cb != null);
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

        static int fh_Bl(FullHist treeh) { throw new NotImplementedException(); }
        static int fh_Br(FullHist treeh) { throw new NotImplementedException(); }

        static Val tree_ruleTree(FullHist treeh, int ind)
        {
            CRuleBundle crules = ClassRule.getCRules(treeh, ind);
            //cerr << "TR " << crules.size() << endl;
            for (int i = 0; i < (int)crules.size(); i++)
            {
                Val trdTree = crules[i].apply(treeh);
                if (trdTree != null)
                    return trdTree;
            }

            return null;
        }

        static int tree_ruleHead_third(FullHist treeh) { throw new NotImplementedException(); }
        //int tree_watpos(int pos,FullHist* treeh)
        static int fh_w1(FullHist treeh) { throw new NotImplementedException(); }
        static int fh_w2(FullHist treeh) { throw new NotImplementedException(); }


        public static void addSubFeatureFns()
        {
          /*
            0 t  fh_term
            1 l  fh_parent_term
            2 u  fh_pos
            3 h  fh_head
            4 i  fh_parent_head
            5 T  fh_true
            6 v  fh_parent_pos
            7 b  fh_term_before
            //8 a  fh_term_after
            8 mE fh_mE  
            9 m  fh_grandparent_term
            10 w fh_grandparent_pos
            11 j tree_ruleHead_third    
            12 c fh_ccparent_term
            13 L1 fh_left1
            14 L1 fh_left2
            15 R1 fh_right1
            16 R1 fh_right2
            17 Qr fh_noopenQr
            18 L0 fh_left0;
            19 L3 fh_left3
            20 R3 fh_right3
            21 Qr fh_noopenQl
            22 Bl fh_Bl
            23 Br fh_Br
            24 vE fh_vE
            25 w1 fh_w1
            26 w2 fh_w2
            */
            Func<FullHist, int> [] funs  //int (*funs[27])(FullHist*)
            = { fh_term, fh_parent_term, fh_pos, fh_head,
                fh_parent_head, fh_true, fh_parent_pos, fh_term_before, fh_mE,
                fh_grandparent_term,fh_grandparent_pos,tree_ruleHead_third,
                fh_ccparent_term, fh_left1, fh_left2, fh_right1, fh_right2,
                fh_noopenQr, fh_left0,fh_left3,fh_right3,fh_noopenQl,fh_Bl,
                fh_Br,fh_vE, fh_w1,fh_w2 };
            int i;
            for (i = 0; i < 27; i++)
                SubFeature.Funs[i] = funs[i];

            ECString temp = new ECString(Bchart.HEADWORD_S1);
            nullWordInt = Bchart.wordMap[temp].first;
        }
    }
}
