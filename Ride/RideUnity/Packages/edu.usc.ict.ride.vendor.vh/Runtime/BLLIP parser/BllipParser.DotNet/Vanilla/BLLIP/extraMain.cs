using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using static BllipParser.DotNet.Vanilla.Bst_global;
using static BllipParser.DotNet.Vanilla.headFinder;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    static class extraMain
    {
        public static void generalInit(ECString path, Dictionary<string, Stream> streams)
        {
            /*
            struct rlimit 	core_limits;
            core_limits.rlim_cur = 0;
            core_limits.rlim_max = 0;
            setrlimit( RLIMIT_CORE, &core_limits );

            struct rlimit stack_limits;
            stack_limits.rlim_cur = 0;
            stack_limits.rlim_max = 0;
            getrlimit( RLIMIT_STACK, &stack_limits );
            if (stack_limits.rlim_cur < stack_limits.rlim_max)
            {
            stack_limits.rlim_cur = stack_limits.rlim_max;
            setrlimit( RLIMIT_STACK, &stack_limits );
            }
            */

            // load locale settings from the environment
            //setlocale(LC_ALL, "");

            path = sanitizePath(path);

            Term.init(path, streams);
            readHeadInfo(path, streams);
            InputTree.init();
            UnitRules ur = new UnitRules();
            ur.readData(path, streams);
            Bchart.unitRules = ur;
            Bchart.readTermProbs(path, streams);
            MeChart.init(path, streams);
            Bchart.setPosStarts();
            ChartBase.midFactor = (float)((1.0 - (.3684 * ChartBase.endFactor)) / (1.0 - .3684));
            if (Feature.isLM || Feature.useExtraConditioning) 
                ClassRule.readCRules(path);
        }


        //InputTree* inputTreeFromAnsTree(AnsTree* at, short& pos, SentRep& sr); //???;


        public static InputTree inputTreeFromBsts(Val at, ref short pos, SentRep sr)
        {
            //cerr << "itfat " << at->trm() << " " << at->bsts().size() << endl;
            short trmInt = at.trm();
            if (trmInt >= 400)
            {
                Console.WriteLine("Bad trm int: " + trmInt);
                Debug.Assert(trmInt < 400);
            }

            Term trm = null;
            ECString trmString = "";
            if (trmInt >= 0)
            {
                trm = Term.fromInt(trmInt);
                trmString = trm.name();
            }

            ECString wrdString = "";
            ECString ntString = "";
            list<InputTree> subtrs = new list<InputTree>();
            InputTree ans;
            short begn = pos;
            if (trm != null && trm.terminal_p() != 0 && at.status == TERMINALVAL)
            {
                wrdString = sr.op(pos).lexeme();
                pos++;
            }
            else
            {
                Debug.Assert(at != null);
                var bi = at.bsts().First;  //Bsts::iterator bi = at->bsts().begin();
                int vpos = 0;
                for ( ; bi != null; bi = bi.Next)
                {
                    Bst sb = bi.Value;  //Bst& sb = **bi;
                    int vval = at.vec()[vpos];
                    if (vval >= sb.num())
                    {
                        Console.WriteLine(vpos + " " + vval + " " + sb.num() + " " + at);
                        Debug.Assert(vval < sb.num());
                    }

                    InputTree sit = inputTreeFromBsts(sb.nth(vval), ref pos, sr);
                    Debug.Assert(sit != null);
                    subtrs.push_back(sit);
                    vpos++;
                }
            }

            /* bestParse in MeChart creates a ficticious level of structure that
            this removes*/

            //if(!trm && !at->edge() && at->status == EXTRAVAL) return subtrs.front();
            if (at.edge() == null && at.status == EXTRAVAL)
                return subtrs.front().Value;

            ans = new InputTree(begn, pos, wrdString, trmString, ntString, subtrs, null, null);


            /* This code inserts the position of the head word after the
            non-terminal */
            /*
            if(!trm->terminal_p())
            {
            int hp = headPosFromTree(ans);
            assert(hp >= 0);
            ans->ntInfo() = intToString(hp);
            }
            */
            var iti = subtrs.First;  //InputTreesIter iti = subtrs.begin();
            for ( ; iti != null; iti = iti.Next)  //for( ; iti != subtrs.end() ; iti++)
                iti.Value.parentSet() = ans;  //(*iti)->parentSet() = ans;


            //cerr << "ITF " << *ans << endl;
            return ans;
        }
    }
}
