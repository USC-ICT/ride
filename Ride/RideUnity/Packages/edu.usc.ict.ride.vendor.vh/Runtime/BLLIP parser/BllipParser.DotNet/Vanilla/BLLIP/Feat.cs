using System;


namespace BllipParser.DotNet.Vanilla
{
    static class Feat_global
    {
        //#define ISCALE 1
        public const int PARSE = 2;
    }


    class Feat
    {
        public int ind_;
        ////int cnt_;
        ////float* uVals;
        float g_;
        public static int Usage;


        //friend ostream& operator<< ( ostream& os, Feat& t );
        public ref float g() { return ref g_; }
        ////float& lambda() { return uVals[1]; }
        ////float& u(int i) { return uVals[i+1]; }
        ////int& cnt() { return cnt_; }
        public ref int ind() { return ref ind_; }
    }
}
