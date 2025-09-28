using System;

using InputTrees = BllipParser.DotNet.Vanilla.list<BllipParser.DotNet.Vanilla.InputTree>;  //typedef list<InputTree*> InputTrees;

using static BllipParser.DotNet.Vanilla.Feature_global;


namespace BllipParser.DotNet.Vanilla
{
    //class InputTree;
    //typedef list<InputTree*> InputTrees;
    //typedef InputTrees::iterator InputTreesIter;
    //typedef InputTrees::const_iterator ConstInputTreesIter;
    //typedef pair<ECString,ECString> EcSPair;
    //typedef list<EcSPair> EcSPairs;
    //typedef EcSPairs::iterator EcSPairsIter;
    //bool scorePunctuation( const ECString trmString );


    class InputTree
    {
        short start_;
        short finish_;
        ECString word_;
        ECString term_;
        ECString ntInfo_;
        short num_;
        InputTrees subTrees_;
        InputTree parent_;
        InputTree headTree_;


        //InputTree(InputTree* p);

        //InputTree(istream& is);

        public InputTree()
        {
            throw new NotImplementedException();
            //: start_(0), finish_(0), word_(""), term_(""), parent_(NULL)
        }

        public InputTree(int s, int f, in ECString w, in ECString t, in ECString n, InputTrees subT, InputTree par, InputTree headTr)
        {
            start_ = (short)s;
            finish_ = (short)f;
            word_ = w;
            term_ = t;
            ntInfo_ = n;
            num_ = -1;
            subTrees_ = subT;
            parent_ = par;
            headTree_ = headTr;
        }

        //InputTree(const ECString w, int i)
        //: start_(i), finish_(i+1), word_(w), term_(w), ntInfo_(""),num_(-1),
        //parent_(NULL), headTree_(NULL){}


        //~InputTree();

        //friend istream& operator >>( istream& is, InputTree& parse );
        //friend ostream& operator <<( ostream& os, const InputTree& parse );

        public void printproper(ref string os)  //void        printproper( ostream& os ) const;
        {
            if (word_.length() != 0)
            {
                os += "(" + term_ + " " + word_ + ")";
            }
            else
            {
                os += "(";
                os += term_ + ntInfo_;
                var subTreeIter = subTrees_.First;  //ConstInputTreesIter  subTreeIter= subTrees_.begin();
                InputTree subTree;
                for ( ; subTreeIter != null; subTreeIter = subTreeIter.Next)  //for( ; subTreeIter != subTrees_.end() ; subTreeIter++ )
                {
                    subTree = subTreeIter.Value;  //subTree = *subTreeIter;
                    os += " ";
                    subTree.printproper(ref os);
                }

                os += ")";
            }
        }


        //short       num() const { return num_; }
        //short&      num() { return num_; }
        //short       start() const { return start_; }
        //short       length() const { return (finish() - start()); }
        //short       finish() const { return finish_; }
        //const ECString word() const { return word_; }  
        //ECString& word() { return word_; }
        //const ECString term() const { return term_; }
        public ECString term() { return term_; }
        //const ECString ntInfo() const { return ntInfo_; }
        //ECString& ntInfo() { return ntInfo_; }
        //const ECString head() { return headTree_->word(); }
        //const ECString hTag() { return headTree_->term(); }
        public InputTrees subTrees() { return subTrees_; }
        //InputTree*& headTree() { return headTree_; }
        //InputTree*  parent() { return parent_; }
        public ref InputTree parentSet() { return ref parent_; }
        //void   recordGold( ParseStats& parseStats);
        //void   precisionRecall( ParseStats& parseStats );
        //bool   lexact2();

        //void        make(list<ECString>& str);
        //void        makePosList(vector<ECString>& str);
        //static int  pageWidth;     
        static ECString [] tempword = new ECString[400];
        static int tempwordnum;


        public static void init()
        {
            for (int i = 0; i < MAXSENTLEN; i++)
            {
                tempword[i] = "";
            }

            tempwordnum = 0;
        }


        //bool   ccChild();
        //bool   ccTree();


        //void        readParse(istream& is);
        //InputTree*     newParse(istream& is, int& strt, InputTree* par);
        //ECString&  readNext( istream& is );
        //void        parseTerm(istream& is, ECString& a, ECString& b,int& n);
        //void        prettyPrint(ostream& os, int start, bool startLine) const;
        //int         spaceNeeded() const;
    }

    //InputTree* ithInputTree(int i, const list<InputTree*> l);
}
