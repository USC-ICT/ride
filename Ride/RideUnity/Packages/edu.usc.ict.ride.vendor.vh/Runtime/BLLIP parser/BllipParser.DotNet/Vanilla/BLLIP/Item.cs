using System;

using BstMap = BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>;  //typedef map<CntxArray, Bst, less<CntxArray> > BstMap;
using EdgeSet = BllipParser.DotNet.Vanilla.set<BllipParser.DotNet.Vanilla.Edge>;  //typedef set<Edge*, less<Edge*> > EdgeSet;
using HeadMap = BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.Wrd, BllipParser.DotNet.Vanilla.pair<BllipParser.DotNet.Vanilla.set<BllipParser.DotNet.Vanilla.Edge>, BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>>>;  //typedef map<Wrd, ItmGHeadInfo, less<Wrd> > HeadMap;
using ItmGHeadInfo = BllipParser.DotNet.Vanilla.pair<BllipParser.DotNet.Vanilla.set<BllipParser.DotNet.Vanilla.Edge>, BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>>;  //typedef pair<EdgeSet,BstMap> ItmGHeadInfo;
using PosMap = BllipParser.DotNet.Vanilla.map<int, BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.Wrd, BllipParser.DotNet.Vanilla.pair<BllipParser.DotNet.Vanilla.set<BllipParser.DotNet.Vanilla.Edge>, BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.CntxArray, BllipParser.DotNet.Vanilla.Bst>>>>;  //typedef map<int,HeadMap, less<int> > PosMap;
using Items = BllipParser.DotNet.Vanilla.list<BllipParser.DotNet.Vanilla.Item>;  //typedef list<Item*> Items;

using static BllipParser.DotNet.Vanilla.Bst_global;


namespace BllipParser.DotNet.Vanilla
{
    //class Term;
    //class Word;

    //typedef set<Edge*, less<Edge*> > EdgeSet;
    //typedef EdgeSet::iterator EdgeSetIter;
    //typedef pair<EdgeSet,BstMap> ItmGHeadInfo;
    //typedef map<Wrd, ItmGHeadInfo, less<Wrd> > HeadMap;
    //typedef map<int,HeadMap, less<int> > PosMap;
    //typedef HeadMap::iterator HeadIter;
    //typedef PosMap::iterator PosIter;


    /* Item is an item in the chart.
     * These include a span [start, finish), a terminal (part of speech
     * or phrase type), the word itself (can be null), inside and outside
     * probabilities, and other bookkeeping information (e.g., which Edges
     * are involved with the item.
     */
    class Item
    {
        int start_;
        int finish_;
        Term term_;
        Wrd word_;
        list<Edge> needme_;	/* A list of rules requiring a term starting at start */

        list<Edge> ineed_;	// needme = rules predicted by this (art) item
                    // ineed = rules that predict this (art) item
        double prob_;
        double poutside_;
        double storeP_;	
        BstMap stored_ = new BstMap();
        PosMap posAndheads_ = new PosMap();


        public Item( //const Wrd* hd,
         in Term _term, int _start, int _finish)
        {
            start_ = _start;
            finish_ = _finish;
            term_ = _term;
            word_ = null;
            needme_ = new list<Edge>();
            ineed_ = new list<Edge>();
            prob_ = 1.0;
            poutside_ = 0.0;
            storeP_ = 0.0;
        }

        Item() { }
        //Item( const Item& );

        //~Item();


        //int		    operator== (const Item& item) const;


        public override string ToString()  //friend ostream& operator<< ( ostream& os, const Item& item );
        {
            string os = "";
            os += term() + "(" + start();
            os += ", " + finish();
            //os << ", " << item.head()->lexeme();
            os += ")";
            return os;
        }


        public Term term() { return term_; }
        public ref Wrd word() { return ref word_; }
        public ref int start() { return ref start_; }
        public ref int finish() { return ref finish_; }
        public list<Edge> needme() { return needme_; }
        public list<Edge> ineed() { return ineed_; }
        //void            check();
        public ref double prob() { return ref prob_; }
        /* storeP can be used as beta for rParse */
        //double            beta() const {return storeP_;}
        //double&           beta() {return storeP_;}
        //double &          prob() {return prob_;}
        public ref double poutside() { return ref poutside_; }
        public ref double storeP() { return ref storeP_; }
        public Bst stored(CntxArray ca) { return bstFind(ca, stored_); }
        public PosMap posAndheads() { return posAndheads_; }


        public void set(in Term _term, int _start)
        {
            term_ = _term;
            start_ = _start;
            needme_.clear();
            ineed_.clear();
            word_ = null;
            storeP_ = 0.0;
            stored_.clear();
            posAndheads_.clear();
        }


        //void	    operator= (const Item& itm);
    }


    //typedef list<Item*> Items;
    //typedef Item *	Item_star;
}
