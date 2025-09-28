using System;

using ECStrings = BllipParser.DotNet.Vanilla.vector<BllipParser.DotNet.Vanilla.ECString>;  //typedef vector<ECString> ECStrings;
using size_t = System.UInt64;

using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    public static class utils
    {
        public static void WARN(string msg)  //#define WARN( msg ) if (!Bchart::silent) { warn( __FILE__, __LINE__, msg ); }
        {
            if (!Bchart.silent)
            {
                Console.WriteLine(msg);
            }
        }

        public static void ERROR(string msg)  //#define ERROR( msg ) error( __FILE__, __LINE__, msg )
        {
            throw new Exception(msg);
        }

        //void warn(const char *filename, int filelinenum, const char *msg);
        //void warn(const char* filename, int filelinenum, string str);
        //void error(const char *filename, int filelinenum, const char *msg);
        //void error(const char *filename, int filelinenum, string str);
        public static void error(string s) { ERROR(s); }  // backwards compatibility


        public static ECString langAwareToLower(ECString str)
        {
            /* Arabic doesn't get lowercased, all other languages do (for now) */
            if (Term.Language == "Ar")
            {
                return str;
            }
            else
            {
                //string lowercased(str);
                //std::transform(lowercased.begin(), lowercased.end(),
                //lowercased.begin(), ::tolower);
                //return lowercased;
                return str.ToLower();
            }
        }


        public static ECString intToString(int i)
        {
            //char temp[16];
            //sprintf(temp, "%i", i); 
            //ECString ans(temp);
            //return ans;
            return i.ToString();
        }


        //typedef vector<ECString> ECStrings;
        //typedef ECStrings::iterator ECStringsIter;


        public static bool vECfind(ECString st, ECStrings sts)
        {
            return sts.GetList().Contains(st);  //return ( find(sts.begin(),sts.end(),s) != sts.end() );
        }


        static void findAndReplace(ref string text, string oldPattern, string newPattern)
        {
            //size_t pos = 0;
            size_t oldLength = (size_t)oldPattern.Length;
            //size_t newLength = newPattern.length();

            if (oldLength == 0)
            {
                return;
            }

            //for (; (pos = text.find(oldPattern, pos)) != string::npos; ) {
            //    text.replace(pos, oldLength, newPattern);
            //    pos += newLength;
            //}
            text = text.Replace(oldPattern, newPattern);
        }


        public static void escapeParens(ref ECString word)
        {
            string temp = word;
            escapeParens(ref temp);
            word = temp;
        }
        public static void escapeParens(ref string word)
        {
            findAndReplace(ref word, "(", "-LRB-");
            findAndReplace(ref word, ")", "-RRB-");
            findAndReplace(ref word, "{", "-LCB-");
            findAndReplace(ref word, "}", "-RCB-");
            findAndReplace(ref word, "[", "-LSB-");
            findAndReplace(ref word, "]", "-RSB-");
        }


        //void unescapeParens(string& word);


        // returns whether string ends with pattern
        static bool endsWith(ECString str, ECString pattern)
        {
            int index = (int)str.rfind(pattern);
            return index == ((int)str.size() - (int)pattern.size());
        }


        // make sure filesystem path is good for loading
        public static string sanitizePath(string modelPath)
        {
            if (!endsWith(modelPath, "/"))
            {
                modelPath += "/";
            }

            return modelPath;
        }
    }
}
