using System;

using CRuleBundle = BllipParser.DotNet.Vanilla.vector<BllipParser.DotNet.Vanilla.ClassRule>;  //typedef vector<ClassRule> CRuleBundle;


namespace BllipParser.DotNet.Vanilla
{
    //#define MCALCRULES 10

    class ClassRule
    {
        //int t_;
        //int m_;
        //int rel_;
        //int d_;
        //static vector<ClassRule>  rBundles2_[MAXNUMNTTS][MAXNUMNTS];
        //static vector<ClassRule>  rBundles3_[MAXNUMNTTS][MAXNUMNTS];
        //static vector<ClassRule>  rBundlesm_[MAXNUMNTTS][MAXNUMNTS];


        //ClassRule(int dd, int mm, int rr, int tt)
        //: t_(tt), m_(mm), rel_(rr), d_(dd) {}
        //ClassRule(const ClassRule& cr)
        //: t_(cr.t()), m_(cr.m()), rel_(cr.rel()), d_(cr.d()) {}


        public Val apply(FullHist treeh) { throw new NotImplementedException(); }
        public static void readCRules(ECString str) { throw new NotImplementedException(); }
        public static vector<ClassRule> getCRules(FullHist treeh, int wh) { throw new NotImplementedException(); }
        //friend ostream& operator<<(ostream& os, const ClassRule& cr)
        //{
        //    os << "{"<< cr.d() << "," << cr.m() << "," << cr.rel() << "," << cr.t() << "}";
        //    return os;
        //}
        //int d() const { return d_; }
        //int m() const { return m_; }
        //int t() const { return t_; }
        //int rel() const { return rel_; }
    }

    //typedef vector<ClassRule> CRuleBundle;
}
