using System;
using System.Diagnostics;

using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    class Params
    {
        const int DEFAULT_SENT_LEN = 100;


        //string file;
        //const ECString&   fileString()
        //{  return fileString_;  }
        //const ECString&   numString()
        //{   return numString_;  }
        //const int	    whichSent()
        //{   return whichSent_;   }
        //const int	    ofTotal()
        //{   return ofTotal_;   }
        public Field field() { return field_; }
        //bool&      stdInput() { return stdInput_; }
        //bool&      outputData() { return outputData_; }
        public int maxSentLen;
        public string [] extPosIfstream;  //ifstream*  extPosIfstream;
        //private:
        //bool stdInput_;
        //bool outputData_;
        ECString fileString_;
        ECString numString_;
        int whichSent_;
        int ofTotal_;
        Field field_;


        public Params()
        {
            //file = null;
            maxSentLen = DEFAULT_SENT_LEN;
            //stdInput_ = false;
            //outputData_ = false;
            fileString_ = new ECString();
            numString_ = new ECString();
            whichSent_ = 0;
            ofTotal_ = 1;
            field_ = null;
        }


        public void init(ECArgs args)
        {
            fileString_ = args.arg(0);

            if (args.nargs() > 2 || args.nargs() == 0)	// require path name 
                error( "Needs 1 or 2 args." );

            if (args.isset('M'))
            {
                Feature.setLM();
                CntxArray.sz = 6;
            }

            if (args.isset('X'))
            {
                Feature.setExtraConditioning();
                CntxArray.sz = 6;
            }

            if (args.isset('N'))
            {
                Bchart.Nth = Convert.ToUInt64(args.value('N'));
            }

            if (args.isset('s')) Bchart.smallCorpus = true;
            if (args.isset('S')) Bchart.silent = true;
            if (args.isset('P')) Bchart.prettyPrint = true;
            if (args.isset('C')) Bchart.caseInsensitive = true;
            if (args.isset('K')) Bchart.tokenize = false;
            if (args.isset('E'))
            {
                string nm = args.value('E');

                throw new NotImplementedException();
#if false
                extPosIfstream = new ifstream(nm);
                AssertInternal(extPosIfstream);
#endif
            }

            if (args.isset('p'))
            {
                float smoothPosAmount = Convert.ToSingle(args.value('p'));
                AssertInternal(smoothPosAmount >= 0);
                AssertInternal(smoothPosAmount <= 1);
                Bchart.smoothPosAmount = smoothPosAmount;
            }

            if (args.isset('T'))
            {
                int fac = Convert.ToInt32(args.value('T'));
                float ffac = (float)fac;
                ffac /= 10;
                Bchart.timeFactor = ffac;
            }

            if (args.isset('l'))
            {
                maxSentLen = Convert.ToInt32(args.value('l'));
                if (maxSentLen > MAXSENTLEN)
                {
                    Console.WriteLine("\nMaximum sentence length allowed is " + MAXSENTLEN);
                    Console.WriteLine("; using this value.\n\n");
                    maxSentLen = MAXSENTLEN;
                }
            }

            if( args.isset('d') )
            {
                int lev = Convert.ToInt32(args.value('d'));
                Bchart.printDebug() = lev;
            }

            if (args.isset('L'))
            {
                Term.Language = args.value('L');
                if (!(Term.Language == "En" || Term.Language == "Ch" || Term.Language == "Ar"))
                    error("Language (-L) must be one of En, Ch, or Ar.");
                if (Term.Language == "Ar")
                Bchart.tokenize = false;
            }

            if (args.isset('n'))
            {
                string etemp;
                etemp = args.value('n');
                int tempIdx = etemp.IndexOf('/');  //char *	temp = strchr( etemp, '/' );
                if (tempIdx == -1)
                    error( "No terminal '/' found in '-n' argument" );
                etemp = etemp.Substring(0, tempIdx);  //*temp = '\0';
                ofTotal_ = Convert.ToInt32(etemp.Substring(++tempIdx));  //ofTotal_ = atoi( ++temp );
                byte [] mask = new byte[ofTotal_];
                for (int i = 0; i < ofTotal_; i++)
                    mask[i] = 0;
                // fill in mask with valid numbers;
                ECString tmp2 = etemp;
                numString_ = tmp2;		// meaningful id for this process;
                whichSent_ = Convert.ToInt32(tmp2);
                mask[whichSent_] = 1;
                field_ = new Field(ofTotal_, mask);
            }
            else
            {
                byte [] mask = new byte[] { 1 };
                field_ = new Field(1, mask);
            }
        }
    }
}
