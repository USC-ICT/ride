using System;

using size_t = System.UInt64;

using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    class SentRep
    {
        vector<Wrd> sent_ = new vector<Wrd>();
        ECString name_;


        public SentRep() { throw new NotImplementedException(); }

        public SentRep(int size)  // initial size for vector to grow from
        {
            sent_.reserve((size_t)size);
        }

        public SentRep(list<ECString> wtList) { throw new NotImplementedException(); }  // used by wwBCTest


        //------------------------------
        // 05/30/06 ML
        // This really belongs in ewDciTokStrm.h/.C, but as it appears it
        // isn't needed elsewhere, I'll just avoid modifying another file.
        static void op(ewDciTokStrm istr, out ECString w)  //static ewDciTokStrm& operator>>( ewDciTokStrm& istr, ECString& w)
        {
            w = istr.read();
            //return istr;
        }


        //------------------------------
        // Avoid scaring anyone by exposing use of template in header file --
        // instead just map operator>> calls to this file local function.
        //template<class T>
        static void readSentence(string [] istr, ref int istrIdx, vector<Wrd> sent, out ECString name)  //static T& readSentence( T& istr, vector<Wrd>& sent, ECString& name)
        {
            name = "";

            sent.clear();
            ECString w;

            while (istrIdx < istr.Length)  //while (!(!istr))
            {
                w = istr[istrIdx++];  //istr >> w;
                if (w == "<s>")
                    break;

                if (w == "<s")
                {
                    name = istr[istrIdx++];  //istr >> name;
                    if (name[(int)name.length() - 1] == '>')
                    {
                        name = name.substr(0, name.length() - 1); // discard trailing '>'
                    }
                    else // "<s LABEL >"
                    {
                        w = istr[istrIdx++];  //istr >> w;
                        if (w != ">")
                            WARN("No closing '>' delimiter found to match opening \"<s\"");
                    }

                    break;
                }
            }

            while (istrIdx < istr.Length)  //while (!(!istr))
            {
                w = istr[istrIdx++];  //istr >> w;

                if (w == "</s>")
                    break;

                escapeParens(ref w);
                int pos = (int)sent.size();
                sent.push_back(new Wrd(w, pos));
            }

            //return istr;
        }


        // SGML layout introduces sentence with <s> and ends it with </s>.
        // <s name> ... </s> also allowed and returned as "name" parameter. 
        public static void op(string [] is_, ref int is_Idx, SentRep sr)  //friend istream& operator>> (istream& is, SentRep& sr);
        {
            readSentence(is_, ref is_Idx, sr.sent_, out sr.name_);
        }

        public static void op(ewDciTokStrm is_, SentRep sr)  //friend ewDciTokStrm& operator>> (ewDciTokStrm& is, SentRep& sr);
        {
            throw new NotImplementedException();
            //return readSentence(is_, sr.sent_, sr.name_);
        }


        public int length() { return (int)sent_.size(); }

        public Wrd op(int index) { return sent_[index]; }  //Wrd&       operator[] ( int index )       { return sent_[ index ]; }

        public ECString getName() { return name_; }


        public override string ToString()  //ostream& operator<< (ostream& os, const SentRep& sr);
        {
            string os = "";
            for (int i = 0; i < length(); i++)
                os += this.op(i) + " ";

            return os;
        }
    }


    //ostream& operator<< (ostream& os, const SentRep& sr);
}
