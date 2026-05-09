using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using BstMap = BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>;  //typedef map<CntxArray, Bst, less<CntxArray> > BstMap;
using EdgeSet = BllipParser.DotNet.Vanilla.set<BllipParser.DotNet.Vanilla.Edge>;  //typedef set<Edge*, less<Edge*> > EdgeSet;
using HeadMap = BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.Wrd, BllipParser.DotNet.Vanilla.pair<BllipParser.DotNet.Vanilla.set<BllipParser.DotNet.Vanilla.Edge>, BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>>>;  //typedef map<Wrd, ItmGHeadInfo, less<Wrd> > HeadMap;
using ItmGHeadInfo = BllipParser.DotNet.Vanilla.pair<BllipParser.DotNet.Vanilla.set<BllipParser.DotNet.Vanilla.Edge>, BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>>;  //typedef pair<EdgeSet,BstMap> ItmGHeadInfo;
using PosMap = BllipParser.DotNet.Vanilla.map<int, BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.Wrd, BllipParser.DotNet.Vanilla.pair<BllipParser.DotNet.Vanilla.set<BllipParser.DotNet.Vanilla.Edge>, BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>>>>;  //typedef map<int,HeadMap, less<int> > PosMap;

using static BllipParser.DotNet.Vanilla.Bst_global;
using static BllipParser.DotNet.Vanilla.edgeSubFns;
using static BllipParser.DotNet.Vanilla.Feat_global;
using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.fhSubFns;
using static BllipParser.DotNet.Vanilla.MeChart_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    static class MeChart_global
    {
        public static LeftRightGotIter [] globalGi = new LeftRightGotIter[MAXNUMTHREADS];
    }


    class MeChart : Bchart
    {
        public MeChart(SentRep sentence, int id)
            : base(sentence, id)
        { }

        public MeChart(SentRep sentence, ExtPos extpos, int id)
            : base(sentence, extpos, id)
        { }


        public void ResetForSentence(SentRep sentence, ExtPos extPosOrNull)
        {
            ResetBchart(sentence, extPosOrNull);
        }


        bool sufficiently_likely(in Item itm)
        {
            double pout = itm.poutside();
            double pin = itm.prob();
            double factor = 0.0008;
            //double factor = .00008; //exp;
            if ((pout * pin) > factor)
                return true;

            return false;
        }


        bool sufficiently_likely(Edge edge)
        {
            Item fp = edge.finishedParent();
            if (fp == null)
                return false;

            if (!sufficiently_likely(fp))
                return false;

            GotIter gotIter = new GotIter(edge);
            while (gotIter.next(out Item got))
            {
                if (got.term() == Term.stopTerm)
                    continue;

                if (!sufficiently_likely(got))
                    return false;
            }

            //return true;
            double factorE = 0.00001;
            //double factorE = .000001; //exp;
            double pout = fp.poutside();
            double pin = edge.prob();
            if (pout * pin > factorE)
                return true;

            //cerr << "Edge filtered " << *edge << endl;
            return false;
        }


        public double triGram() { throw new NotImplementedException(); }


        bool useKn(int i, int whichInt)
        {
            if (whichInt == WWCALC)
                return true;
            else
                return false;
        }


        //public static void init(ECString path)
        public static void init(ECString path, Dictionary<string, Stream> streams)
        {
            Feat.Usage = PARSE;
            addEdgeSubFeatureFns();
            addSubFeatureFns();

            ECString [] tmpA = new ECString[MAXNUMCALCS] { "r","h","u","m","l","lm","ru","rm","tt",
                                                           "s","t","ww","dummy","dummy","dummy" };

            for (int which = 0; which < Feature.numCalcs; which++)
            {
                ECString tmp = tmpA[which];
                Feature.init(path, tmp, streams);
                if (tmp == "s" || tmp == "t")
                    continue;
                ECString ftstr = new ECString(path);
                ftstr += tmp;
                ftstr += ".g";
                //ifstream fts(ftstr.c_str());
                using (var streamReader = new StreamReader(streams[ftstr]))
                {
                    var tokenEnumerator = new TextReaderTokenStream(streamReader);

                    //if(!fts) cerr << "could not find " << ftstr << endl;
                    //assert(fts);

                    var ft = new FeatureTree(tokenEnumerator); // puts it in root
                }

                //(void)ft; // stop compiler warning of unused var; want side-effect of ctor

                if (tmp == "ww")
                    continue;

                Feature.readLam(which, tmp, path, streams);
            }

            int cntxSzReq = Feature.total[TCALC];
            int scSz = Feature.total[SCALC];
            if (scSz > cntxSzReq)
                cntxSzReq = scSz;

            ////assert(CntxArray::sz == cntxSzReq);
            ////assert(CntxArray::sz == (Feature::total[UCALC] -1));
        } 


        public Bst findMapParse()
        {
            if (printDebug() > 8)
            {
                prDp();
                Console.WriteLine("In findMapParse");
            }

            Item s = topS();
            AssertInternal(s != null);
            fillInHeads();
            int s1Int = s.term().toInt();
            FullHist s1Fh = new FullHist(s1Int, this);
            Bst bst = bestParse(s, s1Fh, null, null, 0);
            return bst;
        }


        Bst bestParse(Item itm, FullHist h, Val cval, Val gcval, int cdir)
        {
            curVal = cval;
            gcurVal = gcval;
            curDir = cdir;
            Bst bst = recordedBP(itm, h);
            curVal = gcurVal = null;
            curDir = -1;

            if (bst.explored())
            {
                if (printDebug() > 19)
                {
                    prDp();
                    Console.WriteLine("already known bestParse(" + itm + ", ...) has p = " + bst.prob());
                }

                return bst;
            }

            if (printDebug() > 10)
            {
                prDp();
                Console.WriteLine("bestParse(" + itm + ", ...)");
            }

            bst.explored() = true;  //David McClosky bug;
            int itermInt = itm.term().toInt();
            PosMap pm = itm.posAndheads();
            var pi = pm.GetDictionary().GetEnumerator();  //PosIter pi = pm.begin();
            //ECString bestW;

            for (var piFound = pi.MoveNext(); piFound; piFound = pi.MoveNext())  //for ( ; pi != pm.end(); pi++)
            {
                int posInt = pi.Current.Key;  //int posInt = (*pi).first;
                if (printDebug() > 16)
                {
                    prDp();
                    Console.WriteLine("consider Pos(" + itm + ") = " + posInt);
                }

                HeadMap hm = pi.Current.Value;  //HeadMap hm = (*pi).second;
                /* we are using collected counts for p(u|t) */
                float hposprob = 1;
                /* if we have reached a preterminal, then termInt == posInt
                and p(posInt|termInt) == 1 */
                if (itermInt != posInt)
                {
                    curVal = cval;
                    gcurVal = gcval;
                    curDir = cdir;
                    hposprob = meProb(posInt, h, UCALC); 
                    if (hposprob == 0)
                        hposprob = 0.00001f; //??? this can happen;

                    curVal = gcurVal = null;
                    curDir = -1;
                    if (printDebug() > 16)
                    {
                        prDp();
                        Console.WriteLine("p(pos) = " + hposprob);
                    }
                }

                h.preTerm = posInt;
                var hi = hm.GetDictionary().GetEnumerator();  //HeadIter hi = hm.begin(); 
                for (var hiFound = hi.MoveNext(); hiFound; hiFound = hi.MoveNext())  //for ( ; hi != hm.end(); hi++)
                {
                    Wrd subhw = hi.Current.Key;  //Wrd subhw = (*hi).first;
                    int wrdInt = subhw.toInt();
                    ECString subh = subhw.lexeme();

                    if (printDebug() > 16)
                    {
                        prDp();
                        Console.WriteLine("consider head(" + itm + ") = " + subh);
                    }

                    float hprob = 0;

                    if (wrdInt >= 0 && wrdInt <= lastKnownWord)
                    {
                        hprob = (float)pCapgt(subhw, posInt); 
                        hprob *= (1 - pHugt(posInt)); 
                        curVal = cval;
                        gcurVal = gcval;
                        curDir = cdir;
                        float hprob2 = meHeadProb(wrdInt, h);
                        curVal = gcurVal = null;
                        curDir = -1;
                        hprob *= hprob2;

                        if (hprob < 0)
                        {
                            Console.WriteLine(posInt + " " + pHugt(posInt) + " " + hprob2);
                            AssertInternal(hprob >= 0);
                        }
                    }

                    //hprob can be zero if lower case NNPS.
                    if (wrdInt > lastKnownWord || hprob == 0)
                    {
                        hprob = (float)psutt(subhw, posInt);
                    }

                    if (printDebug() > 16)
                    {
                        prDp();
                        Console.WriteLine("p(hd) = " + hprob);
                    }

                    float hhprob = hposprob * hprob;
                    if (hhprob < 0)
                    {
                        Console.WriteLine(hposprob + " " + hprob);
                        AssertInternal(hhprob >= 0);
                    }

                    h.hd = subhw;
                    Bst bst2 = bestParseGivenHead(posInt, subhw, itm, h, hi.Current.Value, cval, gcval);
                    if (bst2.empty())
                        continue;

                    Val nval = new Val();
                    Val oldval0 = bst2.nth(0);
                    nval.prob() = oldval0.prob() * hhprob;
                    nval.bsts().push_back(bst2);
                    nval.status = EXTRAVAL;
                    bst.push(nval);
                    bst.sum() += bst2.sum() * hhprob;
                }

                hi.Dispose();
            }

            pi.Dispose();

            Val nbest = bst.pop();
            if (nbest != null)
                bst.addnth(nbest);

            if (printDebug() > 10)
            {
                prDp();
                Console.WriteLine("Bestp for " + itm + " = " + bst.prob());
            }

            return bst;
        }


        Bst bestParseGivenHead(int posInt, Wrd wd, Item itm, FullHist h, ItmGHeadInfo ighInfo, Val cval, Val gcval)
        {
            EdgeSet es = ighInfo.first;
            BstMap atm = ighInfo.second;
            curVal = cval;
            gcurVal = gcval;
            Bst bst = recordedBPGH(itm, atm, h);
            if (bst.explored())
            {
                if (printDebug() > 19)
                {
                    int [] subfv = new int[MAXNUMFS];
                    getHt(h, subfv);
                    CntxArray ca = new CntxArray(subfv);
                    prDp();
                    Console.WriteLine("bpknown for " + posInt + ", " + wd + ", " + itm + ") : " + bst.prob() + " " + ca);
                }

                curVal = gcurVal = null;
                return bst;
            }

            bst.explored() = true;
            curVal = gcurVal = null;
            Term trm = itm.term();
            if (trm.terminal_p() != 0)
            {
                Val nval = new Val();
                nval.prob() = 1;
                nval.trm1() = (short)itm.term().toInt();
                nval.wrd1() = itm.word().toInt();
                nval.status = TERMINALVAL;
                bst.addnth(nval);
                bst.sum() = nval.prob();
                return bst;
            }

            if (printDebug() > 10)
            {
                prDp();
                Console.WriteLine("bestParseGivenHead(" + posInt + ", " + wd + ", " + itm  + ")");
            }

            double bestP = 0;
            double sumP = 0;
            var ei = es.GetHashSet().GetEnumerator();  //EdgeSetIter ei = es.begin();
            for (bool eiFound = ei.MoveNext(); eiFound; eiFound = ei.MoveNext())  //for( ; ei != es.end() ; ei++)
            {
                Edge e = ei.Current;  //Edge* e = *ei;
                if (!sufficiently_likely(e))
                    continue;

                int finish = e.loc();
                int effVal = effEnd(finish);

                float edgePg = 1;

                /* 08/28/06 ML: these don't change init value and so compiler warns
                if(itm->term()->isRoot()) edgePg = 1;
                else if(Feature::isLM) edgePg == 1;
                */

                if (effVal == 1)
                    edgePg = endFactor;
                else if (effVal == 0)
                    edgePg = midFactor;

                h.e = e;
                if (printDebug() > 20)
                {
                    prDp();
                    Console.WriteLine("consid " + e);
                }

                Item [] lrGotIterScratch = Get_lrGotIter_ScratchBuffer(thrdid);

                gcurVal = gcval;
                float prob = meRuleProb(e, h, lrGotIterScratch);
                gcurVal = null;
 
                double nextP = prob * edgePg;
                double nextPs = nextP;
                //LeftRightGotIter gi(e); 
                //MiddleOutGotIter gi = new MiddleOutGotIter(e);
                Val val = new Val(e, nextPs);
                val.trm1() = (short)itm.term().toInt();
                val.wrd1() = wd.toInt();
                int pos = 0;
                depth++;

                Item [] midGotIterScratch = Get_midGotIter_ScratchBuffer(thrdid, depth);
                MiddleOutGotIter gi = new MiddleOutGotIter(e, midGotIterScratch);

                h = h.extendByEdge(e, lrGotIterScratch);
                bool zeroProb = false;

                while (gi.next(out Item sitm, out pos))
                {
                    //cerr << "Looking at " << *sitm << endl;
                    if (zeroProb)
                    {
                        h = h.extendBySubConstit();
                        continue;
                    }

                    if (sitm.term() == Term.stopTerm)
                    {
                        h = h.extendBySubConstit(); 
                        continue;
                    }

                    if (pos == 0)
                    {
                        h.preTerm = posInt; 
                        h.hd = wd;
                        ItmGHeadInfo ighi = sitm.posAndheads()[posInt][wd]; 
                        Bst bst2 = bestParseGivenHead(posInt, wd, sitm, h, ighi, val, cval);
                        curVal = gcurVal = null;
                        curDir = -1;

                        if (bst2.empty())
                            zeroProb = true;

                        val.extendTrees(bst2, pos); 
                        nextPs *= bst2.sum();
                    }
                    else
                    {
                        Bst bst2 = bestParse(sitm, h, val, cval, pos);
                        if (bst2.empty())
                            zeroProb = true;

                        val.extendTrees(bst2, pos); 
                        nextPs *= bst2.sum();
                    }

                    if (printDebug() > 39)
                    {
                        prDp();
                        Console.Write("FullHist from " + h);
                    }

                    h = h.extendBySubConstit(); 
                    if (printDebug() > 39)
                        Console.WriteLine(" -> " + h);
                }

                if (!zeroProb)
                    bst.push(val);

                if (printDebug() > 20)
                {
                    prDp();
                    Console.Write("P(" + e + " | " + wd + " ) = ");
                    Console.Write(bestP);
                    Console.WriteLine();
                }

                depth--;
                sumP += nextPs;
                h.retractByEdge(); 
                if (printDebug() > 20)
                {
                    prDp();
                    Console.WriteLine("Val: " + val);
                }
            }
            ei.Dispose();

            Val vbest = bst.pop();
            if (vbest != null)
                bst.addnth(vbest);

            bst.sum() = sumP;
            if (printDebug() > 10)
            {
                prDp();
                Console.WriteLine("Bestpgh for " + itm + ", " + wd + " = " + bst.prob());
            }

            return bst;
        }


        void fillInHeads()
        {
            for (int j = 0; j < wrd_count_; j++)
            {
                // now look at every bucket of length j 
                for (int i = 0; i < wrd_count_ - j; i++)
                {
                    var cell = regs[j, i];  //var itmitr = regs[j, i].First;  //list<Item*>::iterator itmitr =regs[j][i].begin();

                    list<Item> doover = new list<Item>();
                    //for ( ; itmitr != null; itmitr = itmitr.Next)  //for ( ; itmitr != regs[j][i].end() ; itmitr++)
                    int cellCount = (int)cell.size();
                    for (int k = 0; k < cellCount; k++)
                    {
                        Item itm = cell.at(k);  //itm = itmitr.Value;  //itm = *itmitr;
                        if (!sufficiently_likely(itm))
                            continue;

                        Term trm = itm.term();
                        int trmInt = trm.toInt();

                        if (trm.terminal_p() != 0)
                        {
                            //HeadMap& hm = itm->posAndheads()[trmInt]; 
                            HeadMap hm;
                            if (itm.posAndheads().GetDictionary().ContainsKey(trmInt))
                            {
                                hm = itm.posAndheads()[trmInt];
                            }
                            else
                            {
                                hm = new HeadMap();
                                itm.posAndheads().GetDictionary().Add(trmInt, hm);
                            }

                            hm[itm.word()] = new ItmGHeadInfo(new EdgeSet(), new map<CntxArray, Bst>());  //hm[*itm->word()];
                            continue;
                        }
                        else
                        {
                            doover.push_back(itm);
                        }

                        headsFromEdges(itm);
                    }

                    bool cont = true;
                    int timesAgain = 0;

                    //;while(cont && timesAgain++ < 2)
                    while (cont && timesAgain++ < 4)
                    {
                        cont = false;
                        var lii = doover.First;  //list<Item*>::iterator lii = doover.begin();
                        for ( ; lii != null; lii = lii.Next)  //for( ; lii != doover.end() ; lii++)
                        {
                            bool tmp = headsFromEdges(lii.Value);  //bool tmp = headsFromEdges(*lii);
                            if (tmp)
                                cont = tmp;
                        }

                        timesAgain++;
                    }
                }
            }
        }


        bool headsFromEdges(Item itm)
        {
            bool ans = false;
            var ineed = itm.ineed();  //var eli = itm.ineed().First;  //list<Edge*>::iterator eli = itm->ineed().begin();

            // for each edge we look for all of its possible head preterms, and all
            // of the possible heads, and file this edge for that case 
            //for ( ; eli != null; eli = eli.Next)  //for( ; eli != itm->ineed().end() ; eli++)
            int count = (int)ineed.size();
            for (int idx = 0; idx < count; idx++)
            {
                Edge e = ineed.at(idx);  //e = eli.Value;  //e = *eli;
                if (!sufficiently_likely(e))
                    continue;

                Item ehd = e.headItem();
                var epi = ehd.posAndheads().GetDictionary().GetEnumerator();  //PosIter epi = ehd->posAndheads().begin();
                if (!epi.MoveNext())  //if (epi == ehd->posAndheads().end())
                    continue;

                for (bool epiFound = true; epiFound; epiFound = epi.MoveNext())  //for ( ; epi != ehd->posAndheads().end() ; epi++ )
                {
                    int posInt = epi.Current.Key;  //int posInt = (*epi).first;

                    if (itm.posAndheads().find(posInt) == default)  //if (itm->posAndheads().find(posInt) == itm->posAndheads().end())
                        ans = true;

                    //HeadMap& ihm = itm->posAndheads()[posInt];
                    HeadMap ihm;
                    if (itm.posAndheads().GetDictionary().ContainsKey(posInt))
                    {
                        ihm = itm.posAndheads().GetDictionary()[posInt];
                    }
                    else
                    {
                        ihm = new HeadMap();
                        itm.posAndheads().GetDictionary().Add(posInt, ihm);
                    }

                    HeadMap ehm = epi.Current.Value;  //HeadMap& ehm = (*epi).second;
                    var ehi = ehm.GetDictionary().GetEnumerator();  //HeadIter ehi = ehm.begin();
                    for (bool ehiFound = ehi.MoveNext(); ehiFound; ehiFound = ehi.MoveNext())  //for( ; ehi != ehm.end() ; ehi++ )
                    {
                        Wrd hd = ehi.Current.Key;  //const Wrd& hd = (*ehi).first;
                        if (ihm.find(hd) == default)  //if(ihm.find(hd) == ihm.end())
                        {
                            if (printDebug() > 16)
                            {
                                prDp();
                                Console.WriteLine("attach hd " + itm + " " + hd);
                            }

                            ans = true;
                        }

                        //EdgeSet& se = ihm[hd].first;
                        EdgeSet se;
                        if (ihm.GetDictionary().ContainsKey(hd))
                        {
                            se = ihm[hd].first;
                        }
                        else
                        {
                            var head = new ItmGHeadInfo(new EdgeSet(), new map<CntxArray, Bst>());
                            se = head.first;
                            ihm.GetDictionary().Add(hd, head);
                        }

                        se.insert(e);
                    }
                    ehi.Dispose();
                }

                epi.Dispose();
            }

            return ans;
        }


        //bool  headsFromEdge(Edge* e);
        //Item * headItem(Edge* edge);


        void getHt(FullHist h, int [] subfVals, int whichTree = SCALC)
        {
            int i;
            for (i = 1; i < MAXNUMFS; i++)
                subfVals[i] = -1;

            for (i = 1; i <= Feature.total[whichTree]; i++)
            {
                Feature ft = Feature.fromInt(i, whichTree); 
                int sfInt = ft.subFeat;
                SubFeature sf = SubFeature.fromInt(sfInt, whichTree);
                int val = sf.fun(h);
                subfVals[sfInt] = val;
            }

            //cerr << "done getHt" << endl;
        }


        //float getpHst(const ECString& hd, int t);


        Bst recordedBP(Item itm, FullHist h)
        {
            int [] subfv = new int[MAXNUMFS];
            getHt(h, subfv, TCALC);
            CntxArray ca = new CntxArray(subfv);
            return itm.stored(ca); 
        }


        Bst recordedBPGH(Item itm, BstMap atm, FullHist h)
        {
            int [] subfv = new int[MAXNUMFS];
            int i;
            for (i = 0; i < MAXNUMFS; i++)
                subfv[i] = -1;

            if (itm.term().terminal_p() == 0)
            {
                getHt(h, subfv);
            }

            CntxArray ca = new CntxArray(subfv);
            return bstFind(ca, atm);
        }


        float meHeadProb(int wInt, FullHist h)
        {
            float ans = meProb(wInt, h, HCALC);
            return ans;
        }


        float meProb(int cVal, FullHist h, int whichInt)
        {
            if (printDebug() > 68)
            {
                prDp();
                Console.WriteLine("meP" + whichInt + "(" + cVal + " | " + h + ")");
            }

            FeatureTree [] ginfo = new FeatureTree[MAXNUMFS];
            ginfo[0] = FeatureTree.roots(whichInt);
            float [] smoothedPs = new float[MAXNUMFS];
            Feature.whichInt = whichInt;

            float ans = 1;
 
            for (int i = 1; i <= Feature.total[whichInt]; i++)
            {
                int knp = useKn(i,whichInt) ? 1 : 0;
                ginfo[i] = null;
                Feature feat = Feature.fromInt(i, whichInt); 
                /* e.g., g(rtlu) starts from where g(rtl) left off (after tl)*/
                int searchStartInd = feat.startPos;

                if (i > 1)
                    smoothedPs[i] = smoothedPs[i - 1];

                FeatureTree strt = ginfo[searchStartInd];
                if (strt == null)
                    continue;

                SubFeature sf = SubFeature.fromInt(feat.subFeat, whichInt);
                int nfeatV = sf.fun(h);
                FeatureTree histPt = strt.follow(nfeatV, feat.auxCnt); 
                ginfo[i] = histPt;
                if (i == 1)
                {
                    smoothedPs[0] = 1;
                    if (histPt == null)
                    {
                        Console.WriteLine(cVal + " " + whichInt + " " + nfeatV + " " + searchStartInd + " " + feat.auxCnt);
                        AssertInternal(histPt != null);
                    }

                    //Feat f = histPt.feats.find(cVal);
                    //if (f == null)
                    if (!histPt.try_feats_find_index(cVal, out int fIndex))
                    {
                        if(printDebug() > 60)
                        {
                            prDp();
                            Console.WriteLine("Zero p" + feat.name + " " + nfeatV);
                        }

                        if (whichInt == HCALC)
                            return 0.001f;

                        return 0.0f;
                    }
                    ref readonly Feat f = ref histPt.feats_index_ref_readonly(fIndex);

                    smoothedPs[1] = f.g();
                    if (printDebug() > 68)
                    {
                        prDp();
                        Console.WriteLine(i + " " + nfeatV + " " + smoothedPs[1]);
                    }

                    for (int j = 2; j <= Feature.total[whichInt]; j++)
                        smoothedPs[j] = 0;

                    ans = smoothedPs[1];
                    continue;
                }

                if (histPt == null)
                    continue;

                int b;
                if (Feature.isLM || Feature.useExtraConditioning)
                {
                    /* this section for new bucketing */
                    float sz = (float)histPt.feats_size();
                    float estm = (float)histPt.count / sz;
                    b = bucket(estm, whichInt, i);
                }
                else
                {
                    /* this section for old bucketing */
                    float estm = (float)(histPt.count * smoothedPs[1]);
                    b = bucket(estm);
                }

                float unsmoothedVal;
                if (!histPt.try_feats_find_index(cVal, out int ftIndex))
                    unsmoothedVal = 0;
                else
                    unsmoothedVal = histPt.feats_index_ref_readonly(ftIndex).g();

                float lam = 1;
                if (knp == 0)
                    lam = Feature.getLambda(whichInt, i, b);

                float uspathprob = lam*unsmoothedVal;

                float osmoothedVal;
                /* First version is for parsing, second for language modeling */
                if (Feature.isLM || Feature.useExtraConditioning)
                    osmoothedVal = smoothedPs[i - 1]; //for deleted interp.
                else
                    osmoothedVal = smoothedPs[searchStartInd];

                float oneMlam = 1 - lam;
                if (knp != 0)
                {
                    oneMlam = (float)(histPt.count / 1000.0);
                }

                float smpathprob = oneMlam * osmoothedVal;
                float nsmoothedVal = uspathprob + smpathprob;
                smoothedPs[i] = nsmoothedVal;
                ans *= nsmoothedVal / osmoothedVal;

                if (printDebug() > 68)
                {
                    prDp();
                    Console.WriteLine(i + " " + nfeatV + " " + b + " " + unsmoothedVal + " " + lam + " " + nsmoothedVal);
                }
            }

            if (whichInt == HCALC)
                ans *= 600;

            if (printDebug() > 30)
            {
                prDp();
                Console.WriteLine("p" + whichInt + "(" + cVal + "|" + h + ") = " + ans);
            }

            return ans;
        }


        float meRuleProb(Edge edge, FullHist h, Item [] lrScratchBuffer)
        {
            if(printDebug() > 30)
            {
                prDp();
                Console.WriteLine("In meruleprob " + h + " " + edge + " " + edge.headPos());
            }

            int i;
            int hpos = edge.headPos(); 
            h.hpos = hpos;
            LeftRightGotIter gi = new LeftRightGotIter(edge, lrScratchBuffer);
            globalGi[thrdid] = gi;
            Item got;
            float ans = 1;
            for (i = 0; ; i++)
            {
                if (i >= gi.size())
                    break;

                got = gi.index(i);
                h.pos = i;
                int cVal = got.term().toInt();
                int whichInt = LCALC;
                if (h.pos == hpos)
                    whichInt = MCALC;
                else if (h.pos > hpos)
                    whichInt = RCALC;

                ans *= meProb(cVal, h, whichInt);
                if (ans == 0)
                    break;
            }

            if (printDebug() > 30)
            {
                prDp();
                Console.WriteLine("merp = " + ans);
            }

            globalGi[thrdid] = null;
            return ans;
        }


        //void  getRelFeats(int c, int c2, int which, Feat* relFeat[],
        //FeatureTree* fts[], FullHist* h, int facPos);

        //int     ccbucket(float val, float* buckets , int sz);
        //static void    initCCArrays(ECString path);
        //static void    initccarray(ifstream& is, float lenArray[6][8]);
        //float   ccLenProb(Edge* edge, int effend);


        void prDp()
        {
            for (int i = 0; i < depth; i++)
                Console.Write(" ");
        }
    }
}
