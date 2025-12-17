using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using size_t = System.UInt64;
using WordAndPresence = BllipParser.DotNet.Vanilla.pair<int, bool>;  //typedef pair<int, bool> WordAndPresence;

using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.Term_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    partial class Bchart : ChartBase
    {
        //static void readpUgT(ECString path)
        static void readpUgT(ECString path, Dictionary<string, Stream> streams)
        {
            ECString pUstring = new ECString(path);
            pUstring += "pUgT.txt";

            //string pUstream = File.ReadAllText(pUstring);  //ifstream pUstream(pUstring.c_str());
            string pUstream = "";
            using (var streamReader = new StreamReader(streams[pUstring]))
                pUstream = streamReader.ReadToEnd();

            string [] pUstreamSplit = pUstream.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);
            int pUstreamIdx = 0;
            //assert(pUstream);

            for (int i = 0; i <= Term.lastNTInt(); i++)
            {
                if (pUstreamIdx >= pUstreamSplit.Length)
                    break;

                int t;

                //pUstream >> t;
                t = Convert.ToInt32(pUstreamSplit[pUstreamIdx++]);

                float p;

                //pUstream >> p;
                p = Convert.ToSingle(pUstreamSplit[pUstreamIdx++]);

                pHugt(t) = p;
                //cerr << "set pHugt " << t << " = " << p << endl;

                //pUstream >> p;
                p = Convert.ToSingle(pUstreamSplit[pUstreamIdx++]);

                if (p == 0)
                    p = 0.00001f;  //Anything might be capitalized;

                pHcapgt(t) = p;

                //pUstream >> p;
                p = Convert.ToSingle(pUstreamSplit[pUstreamIdx++]);

                pHhypgt(t) = p;
            }
        }


        //public static void readTermProbs(ECString path)
        public static void readTermProbs(ECString path, Dictionary<string, Stream> streams)
        {
            int i;
            for (i = 0 ; i < MAXSENTLEN; i++)
                stops[i] = new Item(Term.stopTerm, i, i);

            //makepUgT(path); //???;
            readpUgT(path, streams);
    
            ECString pTstring = new ECString(path);
            pTstring += "endings.txt";
            ECString ppTstring = new ECString(path);
            ppTstring += "nttCounts.txt";
            //string pTstream = File.ReadAllText(pTstring);  //ifstream pTstream(pTstring.c_str());
            string pTstream = "";
            using (var streamReader = new StreamReader(streams[pTstring]))
                pTstream = streamReader.ReadToEnd();

            string [] pTstreamSplit = pTstream.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);
            //assert(pTstream);

            //string ppTstream = File.ReadAllText(ppTstring);  //ifstream ppTstream(ppTstring.c_str());
            string ppTstream = "";
            using (var streamReader = new StreamReader(streams[ppTstring]))
                ppTstream = streamReader.ReadToEnd();

            string [] ppTstreamSplit = ppTstream.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);
            //assert(ppTstream);


            int numpT;

            //pTstream >> numpT;
            numpT = Convert.ToInt32(pTstreamSplit[0]);

            pHegt_ = new Wwegt[numpT];
            egtSize_ = numpT;
            i = 0;
            for (int pTstreamIdx = 1; pTstreamIdx < pTstreamSplit.Length; )  //while (pTstream)
            {
                int t;
                ECString e;
                float p;
                //pTstream >> t;
                t = Convert.ToInt32(pTstreamSplit[pTstreamIdx++]);
                if (pTstreamIdx >= pTstreamSplit.Length)  //if (!pTstream)
                    break;

                Debug.Assert(i < numpT);

                //pTstream >> e;
                e = pTstreamSplit[pTstreamIdx++];

                //pTstream >> p;
                p = Convert.ToSingle(pTstreamSplit[pTstreamIdx++]);

                pHegt_[i] = new Wwegt();
                pHegt_[i].t = t;
                pHegt_[i].e = e;
                pHegt_[i].p = p;
                i++;
            }

            /* read in counts of each non-term to get p(non-term) in pT */
            int sumTag = 0;
            int sumNT = 0;
            double [] nums = new double[MAXNUMNTTS];
            for (i = 0 ; i < MAXNUMNTTS ; i++)
                nums[i] = 0;

            for (int ppTstreamIdx = 0; ppTstreamIdx < ppTstreamSplit.Length; )  //while (ppTstream)
            {
                int t;

                //ppTstream >> t;
                t = Convert.ToInt32(ppTstreamSplit[ppTstreamIdx++]);

                if (ppTstreamIdx >= ppTstreamSplit.Length)  //if (!ppTstream)
                    break;
      
                Debug.Assert(t < MAXNUMNTTS);

                //ppTstream >> nums[t];
                nums[t] = Convert.ToDouble(ppTstreamSplit[ppTstreamIdx++]);

                if (t > Term.lastTagInt())
                    sumNT += (int)nums[t];
                else
                    sumTag += (int)nums[t];
            }

            float sumTagf = (float)sumTag;
            float sumNTf = (float)sumNT;
            for (i = 0; i < MAXNUMNTTS; i++)
            {
                if (nums[i] == 0)
                    continue;

                float divisor = (i < Term.lastTagInt() + 1) ? sumTagf : sumNTf;
                pT(i) = (float)(nums[i] / divisor);
            }

            ECString wlistString = new ECString(path);
            //wlistString += "probSum.txt";
            wlistString += "pSgT.txt";

            int wnum = 0;
            //ECString w;

            //ifstream wlistStream(wlistString.c_str());
            using (var streamReader = new StreamReader(streams[wlistString]))
            {
                var tokenEnumerator = new TextReaderTokenStream(streamReader);

                //if (!wlistStream) break;

                //wlistStream >> w;  //first entry is number of entries
                if (!tokenEnumerator.TryRead(out string w))
                    throw new InvalidDataException("pSgT.txt is empty or malformed.");

                //lastKnownWord = atoi(w.c_str())-1;
                int wInt = int.Parse(w, NumberStyles.Integer, CultureInfo.InvariantCulture);
                lastKnownWord = wInt - 1;

                // Remaining tokens
                while (tokenEnumerator.HasMore)  // while (wlistStream)
                {
                    //wlistStream >> w;
                    if (!tokenEnumerator.TryRead(out w))
                        throw new InvalidDataException("Unexpected end of pSgT.txt while reading vocab hole filler.");

                    bool isRealWord = true; // assume real word unless we detect a hole

                    string dummy;  //ECString dummy;
                    //WordAndPresence wap;

                    // if we see a vocabulary hole, we increment the word counter and move on
                    if (w == "**VocabHole**")
                    {
                        //wlistStream >> dummy; // the word that would fill this hole
                        if (!tokenEnumerator.TryRead(out dummy))
                            throw new InvalidDataException("Unexpected end of pSgT.txt while reading vocab hole filler.");

                        //wap.second = false; // this is a hole
                        isRealWord = false;  // this is a hole
                    }
                    else
                    {
                        //wap.second = true; // real word (not a hole)
                        for ( ; ; )
                        {
                            //wlistStream >> dummy;
                            if (!tokenEnumerator.TryRead(out dummy))
                                throw new InvalidDataException("Unexpected end of pSgT.txt while reading tag/prob list.");

                            if (dummy == "|")
                                break;

                            int trmInt = int.Parse(dummy, NumberStyles.Integer, CultureInfo.InvariantCulture);

                            float prb;

                            //wlistStream >> prb;
                            if (!tokenEnumerator.TryRead(out string prbToken))
                                throw new InvalidDataException("Unexpected end of pSgT.txt while reading probability.");
                            prb = float.Parse(prbToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);

                            if (prb < 0.001f)
                                continue;

                            Term trm = Term.fromInt(trmInt);
                            Debug.Assert(trm != null);
                            if (trm.terminal_p() == COLON)
                                Term.Colons.push_back(w);
                            else if (trm.terminal_p() == FINAL)
                                Term.Finals.push_back(w);
                        }

                        //int cnt;

                        //wlistStream >> cnt;
                        if (!tokenEnumerator.TryRead(out string cntToken))
                            throw new InvalidDataException("Unexpected end of pSgT.txt while reading count.");
                    }

                    // convert to ECString
                    ECString wECStr = w;

                    /* TODO confirm that invWordMap is okay for holes */
                    invWordMap[wnum] = wECStr;

                    //wap.first = wnum;
                    //wordMap[w] = wap;
                    WordAndPresence wap = new WordAndPresence(wnum, isRealWord);  //wap.first = wnum;
                    wordMap[wECStr] = wap;

                    wnum++;
                }
            }
        }


        static int startState = -1;

        void initDenom()
        {
            int eosInt = Term.stopTerm.toInt();
            /* we compute p(w_0,i t^j) in parray[j][1],
            then move it to parray[j][0].
            each time we also compute the component of p(w_0,n) due to p(w_i)
            = p(w_0,i)/p(w_0,i-1) and put this in denomProbs[i] */
            float [,] parray = new float[MAXNUMNTS, 2];
            int i;
            int j;
            for (i = 0; i < MAXNUMNTS; i++)
                for (j = 0; j < 2; j++)
                    parray[i, j] = 0;

            for (i = 0; i < MAXSENTLEN; i++)
                denomProbs[i] = 0;
  
            if (startState < 0)
                startState = eosInt; 

            parray[startState, 0] = 1;
            Debug.Assert(wrd_count_ < 1000);
            /* compute p(w_0,n t) for all n */
            for (i = 0; i < wrd_count_; i++)
            {
                float pw0n = 0; // = p(w_{0,i}) = sum_j p(w_{0,i}, j)
                /* for n = i, compute p(w_0,n t) for all t */

                list<float> wpl = wordPlist(sentence_.op(i), i);
                var wpli = wpl.First;  //list<float>::iterator wpli = wpl.begin();
                for ( ; wpli != null; wpli = wpli.Next)  //for ( ; wpli != wpl.end(); wpli++)
                {
                    float pw0nt = 0;
                    int trmInt = (int)wpli.Value;  //int trmInt = (int)(*wpli);
                    wpli = wpli.Next;  //wpli++;
                    float prb = wpli.Value;  //float prb = *wpli;
                    if (prb == 0)
                        Console.WriteLine("Zero prob from wordPlist, " + sentence_.op(i) + ", " + trmInt);

                    Debug.Assert(prb >= 0);
                    for (int k = 0; k < MAXNUMNTS; k++)
                    {
                        float pk = parray[k, 0];
                        if (pk == 0)
                            continue;

                        float smb = computepTgT(k,trmInt);

                        // allow labels in terms.txt which do not appear in the training corpus
                        //assert(smb > 0);
                        if (smb == 0)
                            continue;

                        Debug.Assert(pk > 0);
                        pw0nt += pk * prb * smb;
                    }

                    parray[trmInt, 1] = pw0nt;
                    if (printDebug(1000))
                        Console.WriteLine("initD " + i + "\t" + trmInt + "\t" + pw0nt);

                    pw0n += pw0nt;
                }

                if (pw0n == 0)
                {
                    Console.WriteLine("Zero at pos " + i + " word = " + sentence_.op(i));
                    /*If we get here it means that all of the beta values are zero.
                    So assign them .00001 * P(word|tag) as a default to avoid
                    all zeros */
                    wpli = wpl.First;  //wpli = wpl.begin();
                    for ( ; wpli != null; wpli = wpli.Next)  //for ( ; wpli != wpl.end(); wpli++)
                    {
                        int trmInt = (int)wpli.Value;  //int trmInt = (int)(*wpli);
                        wpli = wpli.Next;  //wpli++;
                        float prb = wpli.Value;  //float prb = *wpli;
                        float pw0nt = prb * .00001f;
                        Console.WriteLine("Assigning " + trmInt + " prob = " + pw0nt);
                        parray[trmInt, 1] = pw0nt;
                        pw0n += pw0nt;
                    }
                }

                Debug.Assert(pw0n > 0);
                /* now compute the pwarray value we care about */
                denomProbs[i] = pw0n;
                if (printDebug(1000))
                    Console.WriteLine("denomProb " + i + " = " + pw0n);

                /* and now transfer the values from the [1] column to the [0] */
                for (j = 0; j < MAXNUMNTS; j++)
                {
                    /* each time through we devide probs by p(wi-1|w_0,i-1), and thus
                    all of the figures are p(...|w_o,i) */
                    parray[j, 0] = parray[j, 1] / pw0n;
                    /* I hope this is something like the relative prob of the tag
                    being t^j at this point in the sent */
                    parray[j, 1] = 0;
                }

                wpli = wpl.First;  //wpli = wpl.begin();
                for ( ; wpli != null; wpli = wpli.Next)  //for ( ; wpli != wpl.end(); wpli++) 
                {
                    int trmInt = (int)wpli.Value;  //int trmInt = (int)(*wpli);
                    wpli = wpli.Next;  //wpli++;
                    float prb = wpli.Value;  //float prb = *wpli;
                    prb /= denomProbs[i];

                    Term possTerm = Term.fromInt(trmInt);
                    //Item *item =  new Item(possTerm, i, i + 1);
                    Item item = addtochart(possTerm);
                    item.start() = i;
                    item.finish() = i + 1;
                    item.word() = sentence_.op(i);
                    item.prob() = prb;
                    item.prob() *= 1.2;  // 1.1 factor to overcome bigram superiority;
                    Edge nEdge = new Edge(item);
                    // this next is a hack so that that the merit of nEdge will come
                    // out right/
                    nEdge.leftMerit() = parray[trmInt, 0] / item.prob(); 
                    //cerr << "plstop for " << *item << " = "
                    //<< parray[trmInt][0] << " / " << item->prob()
                    //<< " = " << nEdge->plstopGt() << endl;
                    nEdge.setmerit();
                    heap.insert(nEdge); 
                    ++ruleiCounts_;
                }
            }

            /* finally, compute the dummy p(dummy eos | prev)
            = sum_i p(w,t^i|prev) * p(eos | t^i) */
            float ans = 0;
            for (i = 0; i <= Term.lastTagInt(); i++)
            {
                float pwti = parray[i, 0];
                if (pwti == 0)
                    continue;

                float sbg = computepTgT(i, eosInt);
                ans += sbg * pwti;
            }

            if (printDebug(1000))
                Console.WriteLine("initD " + wrd_count_ + "\tSTOP(" + eosInt + ")\t" + ans);

            denomProbs[wrd_count_] = ans;
        }


        int wtoInt(ECString w)
        {
            var wordMapIter = wordMap.find(w);  //map<ECString, WordAndPresence, less<ECString> >::iterator wordMapIter = wordMap.find(w);
            if (wordMapIter != default)
            {
                /* word is in wordMap, now check to see if it is a hole */
                WordAndPresence wap = wordMapIter;
                if (wap.second)
                    return wap.first;
            }

            var newWordMapIter = newWordMap[thrdid].find(w);  //map<ECString, int, less<ECString> >::iterator newWordMapIter = newWordMap[thrdid].find(w);
            if (newWordMapIter != default)
                return newWordMapIter;

            lastWord[thrdid]++;
            newWordMap[thrdid][w] = lastWord[thrdid];
            newWords[thrdid].push_back(w);
            return lastWord[thrdid];
        }


        list<float> wordPlist(Wrd word, int word_num)
        {
            list<float> ans = wordPlists[word_num];
            if (!ans.empty())
                return ans;

            if (printDebug(500))
                Console.WriteLine("wordPlist " + word);

            ECString head = new ECString(word.lexeme());
            ECString headL = new ECString(langAwareToLower(head));
            int wint = wtoInt(headL); 
            //cerr << "WTI " << headL << " " << wint << endl;
            word.toInt() = wint;
            if (word.lexeme() == Bchart.HEADWORD_S1)
            {
                ans.push_back(Term.stopTerm.toInt());
                ans.push_back(1.0f);
                return ans;
            }

            if (!extraPos.empty())
            {
                Debug.Assert(word_num < (int)extraPos.size());
                vector<Term> vct = extraPos[word_num];
                float prb = 1.0f;
                if (!vct.empty())
                {
                    int vctIIdx = 0;  //vector<const Term*>::iterator vctI=vct.begin();
                    for (; vctIIdx < vct.Count; vctIIdx++)  //for (; vctI != vct.end(); vctI++)
                    {
                        Term trm = vct[vctIIdx];  //const Term* trm = *vctI;
                        int trmint = trm.toInt();
                        prb /= 2.0f;
                        ans.push_back((float)trmint);
                        ans.push_back(prb);
                    }

                    return ans;
                }
            }
      
            bool smoothPos = Bchart.smoothPosAmount > 0;
            if (wint <= lastKnownWord)
            {
                int i;
                for (i = 0; i <= Term.lastTagInt(); i++)
                {
                    if (guided && !inGuide(word_num, word_num + 1, i))
                        continue;

                    float pwgt = pHst(wint,i);
                    //cerr << "pwgt " << i << " " << pwgt << endl;
                    if (pwgt == 0 && !smoothPos)
                        continue;

                    float prob = (float)psktt(word,i); 
                    if (smoothPos && prob == 0 && Term.fromInt(i).openClass())
                    {
                        prob = smoothPosAmount;
                    }

                    if (prob == 0)
                        continue;

                    Debug.Assert(prob > 0);
                    ans.push_back((float)i);
                    ans.push_back(prob);
                    //if(printDebug(7777))
                }

                if (!ans.empty())
                {
                    return ans;
                }
            }

            // in the case of a word that is only known as an NNPS, but we see it
            // uncapitalized, the above will assign 0 prob and ans will be empty.       
            // if this happens, we treat it like an unknown word.;
            for (int i = 0; i <= Term.lastTagInt(); i++)
            {
                if (i == Term.stopTerm.toInt())
                    continue;

                float phut = pHugt(i);
                if (phut == 0)
                    continue;

                float prob = (float)psutt(word,i);
                if (prob == 0)
                    continue;

                Debug.Assert(prob > 0);

                if (printDebug(7777))
                    Console.WriteLine("Uk\t" + i + "\t" + prob);

                ans.push_back((float)i);
                ans.push_back(prob);
                //cerr <<word_num<< "\t" << i << "\t" << prob << endl;
            }

            return ans;
        }


        double psktt(Wrd shU, int t)
        {
            double ans = pHst(shU.toInt(), t); 
            double phcp = 1;
            phcp = pCapgt(shU,t);
            ans *= phcp;
            double put = pHugt(t);
            ans *= (1 - put);
            if (ans < 0)
            {
                Console.WriteLine(phcp + " " + put);
                Console.WriteLine("psktt( " + shU + " | " + t + " ) = " + ans);
                Debug.Assert(ans >= 0);
            }

            return ans;
        }


        protected double pCapgt(Wrd shU, int t)
        {
            if (Bchart.caseInsensitive)
                return 1.0;

            if (Term.Language == "Ch" || Term.Language == "Ar") 
                return 1.0;

            int word_num = shU.loc();
            ECString lex0 = sentence_.op(0).lexeme();
            if (word_num == 0)
                return 1;
            else if (word_num == 1 && (lex0 == "``" || lex0 == "-LCB-" || lex0 == "-LRB-"))
                return 1;

            //cerr << "pCapgt = " << pcap << endl;

            if (shU.lexeme().length() < 2)
                return 1;  //ignore words of length 1;

            ECString sh = new ECString(langAwareToLower(shU.lexeme()));
            bool cap = false;
            /* if all caps, ignore capitalization evidence */
            if (shU.lexeme()[0] != sh[0] && shU.lexeme()[1] != sh[1])
                return 1;

            if (shU.lexeme()[0] != sh[0] && shU.lexeme()[1] == sh[1])
                cap = true;

            double pcap = pHcapgt(t);  
            return cap ? pcap : (1 - pcap);
        }


        float pHst(int wordInt, int t)
        {
            Debug.Assert(wordInt >= 0);
            FeatureTree strt = FeatureTree.roots(HCALC);
            Debug.Assert(strt != null);
            FeatureTree histPt = strt.follow(t, 0);
            if (histPt == null)
                return 0;

            Feat ft = histPt.feats.find(wordInt);
            if (ft == null)
                return 0;
            else
                return ft.g();
        }


        protected double psutt(in Wrd shU, int t)
        {
            //cerr << "Unknown word: " << shU << " for tag: " << t << endl; 
            double ans = pHugt(t);
            //cerr << "pHugt = " << ans << endl;
            Debug.Assert(ans >= 0);
            if (ans == 0)
                return 0;

            double phyp = 1;
            if (Term.Language != "Ch" && Term.Language != "Ar") 
                phyp = pHypgt(shU.lexeme(), t);

            ans *= phyp;
            //cerr << "pHypgt = " << phyp << endl;
            double phcp = 1;
            if (Term.Language != "Ch" && Term.Language != "Ar") 
                phcp = pCapgt(shU,t);

            ans *= phcp;
            ans *= .0001;
            Debug.Assert(ans >= 0);
            if (Term.fromInt(t).openClass())
            {
                ECString sh = new ECString(langAwareToLower(shU.lexeme()));
                float phegt = pegt(sh,t);
                if (phegt == 0)
                    phegt = 0.00001f;

                ans *= phegt;
            }
            else
            {
                ans *= .0001;
            }

            ans *= 600;
            Debug.Assert(ans >= 0);
            //cerr << "psutt( " << shU << " | " << t << " ) = " << ans << endl;
            return ans;
        }


        protected int bucket(float val)
        {
            for (int i = 0; i < 14; i++)
                if (val <= bucketLims[i])
                    return i;

            return 14;
        }


        protected int bucket(float val, int whichInt, int whichFt)
        {
            Debug.Assert(whichInt < Feature.numCalcs);
            Debug.Assert(whichFt < MAXNUMFS);
            float logFac = Feature.logFacs[whichInt, whichFt];
            float lval = logFac * (float)Math.Log(val);
            int lvi = (int)lval;
            lvi++;
            if (lvi <= 14)
                return lvi;

            return 14;
        }


        double pHypgt(in ECString shU, int t)
        {
            //return 1.0  //ADD to IGNORE hypenization for unknown words
            bool hyp = false;
            bool hyppos = shU.Contains("-");  //const char* hyppos = strpbrk(shU.c_str(), "-");
            if (hyppos)
                hyp = true;

            double phyp = pHhypgt(t);
            return hyp ? phyp : (1 - phyp);
        }


        float pegt(ECString sh, int t)
        {
            //return 1.0  //ADD to IGNORE endings for unknown words
            int len = (int)sh.length();
            if (len < 3)
                return 0.01f;

            ECString e = new ECString(sh, (size_t)(len - 2), 2);
            float phegt = pHegt(e,t);
            //cerr << "pegt( " << sh <<", " << e << " | " << t << " ) = " << phegt << endl;
            return phegt;
        }


        float pHegt(ECString es, int t)
        {
            int top = egtSize_;
            int bot = -1;
            for ( ; ; )
            {
                if (top <= bot + 1)
                    return 0.0f;

                int mid = (top + bot) / 2;
                Wwegt midH = pHegt_[mid];
      
                int gt = greaterThan(midH, es, t);
                if (gt == 0)
                    return midH.p;
                else if (gt == 1)
                    top = mid;
                else
                    bot = mid;
            }
        }


        int greaterThan(Wwegt wwegt, ECString e, int t)
        {
            int ans = 0;
            if (wwegt.t < t)
                ans = -1;
            else if (wwegt.t > t)
                ans = 1;
            else if (((string)wwegt.e).CompareTo(e) < 0)  //else if(wwegt.e < e) ans = -1;
                ans = -1;
            else if (((string)wwegt.e).CompareTo(e) > 0)  //else if(wwegt.e > e) ans = 1;
                ans = 1;

            return ans;
        }


        float computepTgT(int t1, int t2)
        {
            FullHist fh = new FullHist();
            fh.preTerm = t1;
            fh.term = t2;
            fh.cb = this;
            return meFHProb(Term.fromInt(t2), fh, TTCALC);
        }


        float computeMerit(Edge edge, int whichDist)
        {
            float ans = 0;  //accumulate the sum here;
            FullHist fh = new FullHist(edge);
            fh.cb = this;
            int denomPos = edge.loc();
            if (whichDist == LMCALC)
                denomPos = edge.start() - 1;

            float denom = 0.1f;  // ??? should be 1, but merit hs a problem with start.
            if (denomPos >= 0)
                denom = denomProbs[denomPos];

            if (denom <= 0)
            {
                /* attempted fix by dmcc */
                return 0.000001f;
                //Console.WriteLine(denomPos + " " + edge);
                //Debug.Assert(denom > 0);
            }

            /* p(w|nt) = sum_t p(w|t)p(t|nt) where w is the word following item */
            if (denomPos == wrd_count_ || denomPos < 0)
            {
                /* if w is the word preceeding or
                following the sentence, it is a pretend word
                with only one part of speech, STOP */
                fh.term = Term.stopTerm.toInt();
                ans = meFHProb(Term.stopTerm, fh, whichDist);
            }
            else
            {
                Wrd w = sentence_.op(denomPos);
                list<float> wpl = wordPlist(w, denomPos);
                var wpli = wpl.First;  //list<float>::iterator wpli = wpl.begin();
                for ( ; wpli != null; wpli = wpli.Next)  //for( ; wpli != wpl.end() ; wpli++)
                {
                    int termInt = (int)wpli.Value;  //int termInt = (int)(*wpli);
                    Term nxtTerm = Term.fromInt(termInt);
                    wpli = wpli.Next;  //wpli++;
                    float pwgt = wpli.Value;  //float  pwgt = *wpli;
                    float phtgnt = meFHProb(nxtTerm, fh, whichDist);
                    ans += pwgt * phtgnt;
                }
            }

            ans *= 1.5f;
            ans /= denom;
            if (whichDist == LMCALC)
                ans *= pT(edge.lhs().toInt());

            if (printDebug(150))
                Console.WriteLine("cM" + whichDist + " = " + ans);

            if (smallCorpus && ans == 0)
                ans = 0.000001f;

            return ans;
        }
    }
}
