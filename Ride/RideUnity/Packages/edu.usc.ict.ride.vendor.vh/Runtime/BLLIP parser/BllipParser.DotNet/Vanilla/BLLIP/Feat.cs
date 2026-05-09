using System;


namespace BllipParser.DotNet.Vanilla
{
    static class Feat_global
    {
        //#define ISCALE 1
        public const int PARSE = 2;
    }


    struct Feat
    {
        public int ind_;
        ////int cnt_;
        ////float* uVals;
        float g_;
        public static int Usage;


        //friend ostream& operator<< ( ostream& os, Feat& t );
        public readonly float g() { return g_; }
        ////float& lambda() { return uVals[1]; }
        ////float& u(int i) { return uVals[i+1]; }
        ////int& cnt() { return cnt_; }
        public readonly int ind() { return ind_; }


        public static ref int ind_ref(ref Feat f) => ref f.ind_;
        public static ref float g_ref(ref Feat f) => ref f.g_;
    }
}
