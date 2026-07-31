using System;


namespace BllipParser.DotNet.Vanilla
{
    class ewDciTokStrm
    {
        ECString sentenceName;
        string [] istr_;  //istream& istr_;

        ECString savedWrd_;
        ECString nextWrd_;
        //int parenFlag;
        //int ellipFlag;


        public ewDciTokStrm(string [] stream)  //ewDciTokStrm( istream& );
        {
            sentenceName = "";
            istr_ = stream;
            savedWrd_ = "";             // holds not-yet-processed parts of current Wrd
            nextWrd_ = "";              // holds "on-deck" Wrd
            //parenFlag = 0;                        // ParenFlag = 0 except while words
            //ellipFlag = 0;                   // counts how many dots are in an ellipsis
        }

        //virtual ~ewDciTokStrm() {}


        public ECString read() { throw new NotImplementedException(); }


        //int		operator!()
        //{
        //    return savedWrd_.length() == 0 && nextWrd_.length() == 0 && !istr_;
        //}


        protected virtual ECString nextWrd2() { throw new NotImplementedException(); }


        //ECString	flush_to_sentence();
        //ECString	splitAtPunc( ECString );
        //int         is_stateLike( const ECString& );
    }
}
