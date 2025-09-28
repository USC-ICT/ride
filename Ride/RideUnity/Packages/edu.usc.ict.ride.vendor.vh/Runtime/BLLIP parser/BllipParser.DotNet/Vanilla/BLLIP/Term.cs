using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using ECStrings = BllipParser.DotNet.Vanilla.vector<BllipParser.DotNet.Vanilla.ECString>;  //typedef vector<ECString> ECStrings;
using TermMap = BllipParser.DotNet.Vanilla.map<BllipParser.DotNet.Vanilla.ECString, BllipParser.DotNet.Vanilla.Term>;  //typedef map<ECString, Term*, less<ECString> >  TermMap;

using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    //typedef Term *		Term_p;
    //typedef const Term *	Const_Term_p;
    //typedef const Term     ConstTerm;

    //#define Terms list<ConstTerm*>
    //#define ConstTerms const list<ConstTerm*>
    //#define TermsIter list<Term*>::iterator
    //#define ConstTermsIter list<ConstTerm*>::const_iterator
    //typedef map<ECString, Term*, less<ECString> >  TermMap;


    static class Term_global
    {
        public const int FINAL = 3;
        public const int COLON = 8;
    }


    class Term 
    {
        int terminal_p_;
        int num_;
        ECString name_;
        static Term [] array_ = new Term[MAXNUMNTTS];
        static TermMap termMap_ = new TermMap();
        static int lastTagInt_ = 0;
        static int lastNTInt_ = 0;


        public static Term stopTerm;
        static Term startTerm;
        public static Term rootTerm;
        public static ECStrings Finals = new ECStrings();
        public static ECStrings Colons = new ECStrings();
        public static ECString Language = "En";


        Term()  // provided only for maps.
        {
            throw new NotImplementedException();
        }

        Term(ECString s, int terminal, int num)
        {
            terminal_p_ = terminal;
            num_ = num;
            name_ = s;
        }

        Term(Term src)
        {
            throw new NotImplementedException();
        }


        public int toInt() { return num_; }
        public ECString name() {  return name_;  }


        public override string ToString()  //friend ostream& operator<< ( ostream& os, const Term& t );
        {
            string os = "";
            os += name();
            return os;
        }

        //friend ostream& operator>> ( istream& os, const Term& t );
        //int		operator== (const Term& rhs ) const;

        public int terminal_p() { return terminal_p_; }
        //bool   isPunc() const { return (terminal_p_ > 2) ? true : false ; }
        public bool openClass() { return (terminal_p_ == 2) ? true : false ; }
        public bool isColon() { return vECfind(name(), Colons); }
        //bool   isFinal() const { return vECfind(name(), Finals);}
        public bool isComma() { return terminal_p_ == 4; }
        public bool isCC() { return name() == "CC" || name() == "CONJP"; }
        public bool isRoot() { return name() == "S1"; }

        public bool isS()
        {
            if (Term.Language == "Ch")
                return name_ == "IP";
            else
                return name_ == "S";
        }

        //bool   isParen() const {return terminal_p_ == 7;}
        public bool isNP() {return name() == "NP";}
        public bool isVP() {return name() == "VP";}
        public bool isOpen() { return terminal_p_ == 5; }
        public bool isClosed() { return terminal_p_ == 6; }


        public static Term get(ECString nm)
        {
            var ti = termMap_.find(nm);  //TermMap::iterator ti = termMap_.find(nm);
            if (ti == default)  //if (ti == termMap_.end()) return NULL;
                return null;

            return ti;
        }


        //public static void init(ECString prefix)
        public static void init(ECString prefix, Dictionary<string, Stream> streams)
        {
            ECString fileName = new ECString(prefix);
            fileName += "terms.txt";
            //string stream = File.ReadAllText(fileName);  //ifstream           stream(fileName.c_str(), ios::in);
            string stream = "";
            using (var streamReader = new StreamReader(streams[fileName]))
                stream = streamReader.ReadToEnd();

            string [] streamSplit = stream.Split((char [])null, StringSplitOptions.RemoveEmptyEntries);
            if (stream == null)
            {
                Console.WriteLine("Can't open terms file " + fileName);
                return;
            }
  
            ECString termName;
            int ind = 0;
            int n;
            n = 0;
            bool seenNTs = false;
            for (int streamIdx = 0; streamIdx < streamSplit.Length; )  //while (stream >> termName)
            {
                termName = streamSplit[streamIdx++];
                if (streamIdx >= streamSplit.Length)
                    break;

                ind = Convert.ToInt32(streamSplit[streamIdx++]);  //stream >> ind;

                Term nextTerm = new Term(termName, ind, n);
                termMap_[nextTerm.name()] = nextTerm;
                if (termName == "STOP") Term.stopTerm = nextTerm;
                else if (termName == "G4") Term.startTerm = nextTerm;
                else if (termName == "S1") Term.rootTerm = nextTerm;
                array_[n] = nextTerm;
                if (ind == 0 && !seenNTs)
                {
                    Debug.Assert(n > 0);
                    lastTagInt_ = n-1;
                    seenNTs = true;
                }

                n++;
                Debug.Assert(n < 400);
            }

            Debug.Assert(ind == 0);
            lastNTInt_ = n - 1;
            //lastNTInt_ = n-4;  //??? hack to ignore G1 and G2 and G3;
            //stream.close();
        }


        public static Term fromInt(int i) { Debug.Assert(i < MAXNUMNTTS); return array_[i]; }
        public static int lastTagInt() { return lastTagInt_; }
        public static int lastNTInt() { return lastNTInt_; }

        //ECString* namePtr() { return (ECString*)&name_; }
    }
}
