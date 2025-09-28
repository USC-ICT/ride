using System;


namespace BllipParser.DotNet.Vanilla
{
    class Wrd : IComparable<Wrd>
    {
        //friend class SentRep;


        ECString lexeme_;
        int loc_;
        int wInt_;


        public Wrd()
        {
            throw new NotImplementedException();
            //: lexeme_(""), loc_(-1)
        }

        public Wrd(in Wrd wrd)
        {
            throw new NotImplementedException();
            //: lexeme_(wrd.lexeme()),loc_(wrd.loc()),wInt_(wrd.toInt())
        }

        public Wrd(ECString lx, int ps)
        {
            lexeme_ = lx;
            loc_ = ps;
        }


        public int CompareTo(object obj)
        {
            if (obj is Wrd wrd) return CompareTo(wrd);
            else throw new ArgumentException();
        }

        public int CompareTo(Wrd other)  //friend int operator<(const Wrd& w1, const Wrd& w2)
        {
            return lexeme_.CompareTo(other.lexeme_);  //{ return w1.lexeme_ < w2.lexeme_; }
        }


        public ECString lexeme() { return lexeme_; }

        //friend ewDciTokStrm& operator>>(ewDciTokStrm& is, Wrd& w)
        //{
        //    w.lexeme_ = is.read();
        //    return is;
        //}
        //friend int operator<(const Wrd& w1, const Wrd& w2)
        //{ return w1.lexeme_ < w2.lexeme_; }
        //friend istream& operator>>(istream& is, Wrd& w)
        //{
        //    ECString lx;
        //    is >> lx;
        //    escapeParens(w.lexeme_);
        //    return is;
        //}

        public override string ToString()  //friend ostream& operator<<(ostream& os, const Wrd& w)
        {
            string os = "";
            os += lexeme_;
            return os;
        }

        //void operator=(const Wrd& wr)
        //{
        //    lexeme_ = wr.lexeme();
        //    loc_ = wr.loc();
        //}

        //void setLoc(int l) { loc_ = l; }
        public int loc() { return loc_; }
        public ref int toInt() { return ref wInt_; }
        //int& toInt() { return wInt_; }
    }
}
