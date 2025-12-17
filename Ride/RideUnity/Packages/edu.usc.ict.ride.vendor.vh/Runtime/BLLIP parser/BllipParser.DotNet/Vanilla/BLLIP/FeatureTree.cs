using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.FeatureTree_global;


namespace BllipParser.DotNet.Vanilla
{
    public interface ITokenStream
    {
        /// <summary>
        /// Returns true if a token exists at the specified index. This will
        /// lazily read more data from the underlying reader as needed.
        /// </summary>
        bool HasMore { get; }

        /// <summary>
        /// Attempts to read the next token and advance the stream.
        /// Returns false if no more tokens are available.
        /// </summary>
        bool TryRead(out string token);
    }

    /// <summary>
    /// Provides a lazily evaluated token stream over a <see cref="TextReader"/>.
    /// </summary>
    /// <remarks>
    /// This class is designed for scenarios where tokens must be consumed,
    /// but where fully materializing all tokens
    /// up front would be unnecessary or too expensive. The implementation reads
    /// whitespace-separated tokens from the underlying <see cref="TextReader"/>
    /// on demand and caches only the tokens that have been requested so far.
    ///
    /// Unlike <see cref="IEnumerator{T}"/> or <see cref="IEnumerable{T}"/>,
    /// this token stream supports HaseMore. This makes
    /// it possible to preserve the original parsing semantics of the C++ BLLIP
    /// and NVBG feature tree loader, which relied on <c>vector&lt;string&gt;</c>
    /// length checks.
    ///
    /// Tokens are produced using the same splitting rules as
    /// <c>string.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)</c>,
    /// which splits on any whitespace sequence. Lines containing no tokens are
    /// skipped automatically. The stream reads as little data as necessary to
    /// satisfy indexing requests, and the remaining input is only processed if
    /// the caller accesses additional indices.
    ///
    /// Because tokens are cached after first access, repeated reads of the same
    /// index incur no additional parsing cost.
    ///
    /// This class is specifically intended to simplify the C# port of the
    /// BLLIP/NVBG FeatureTree parser by removing the need for explicit calls to
    /// <c>MoveNext()</c>, <c>Current</c>, and manual iterator state management,
    /// while avoiding the up-front allocation cost of loading the full file.
    /// </remarks>
    public sealed class TextReaderTokenStream : ITokenStream
    {
        private readonly TextReader m_reader;
        private readonly List<string> m_tokens = new List<string>();

        private string[] m_currentLineTokens;
        private int m_currentLineIndex;
        private bool m_isEndOfStream;

        public TextReaderTokenStream(TextReader reader) => m_reader = reader ?? throw new ArgumentNullException(nameof(reader));

        public bool HasMore
        {
            get
            {
                if (m_currentLineTokens != null && m_currentLineIndex < m_currentLineTokens.Length) return true;

                // Attempt to read ahead without consuming a token.
                if (m_isEndOfStream) return false;

                // Try to load another line containing tokens.
                while (true)
                {
                    string line = m_reader.ReadLine();
                    if (line == null)
                    {
                        m_isEndOfStream = true;
                        return false;
                    }

                    if (line.Length == 0)
                        continue; // skip blank lines

                    m_currentLineTokens = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    m_currentLineIndex = 0;

                    // If this line actually contained tokens, we now have them available.
                    if (m_currentLineTokens.Length > 0)
                        return true;
                }
            }
        }

        public bool TryRead(out string token)
        {
            token = null;

            if (!HasMore)
                return false;

            // We already ensured m_currentLineTokens and m_currentLineIndex are valid
            token = m_currentLineTokens[m_currentLineIndex++];
            return true;
        }
    }


    static class FeatureTree_global
    {
        public const int ROOTIND = -99;
        public const int AUXIND  = -9;
        public const int NULLIND = 9999999;
    }


    class FeatureTree
    {
        FeatureTree auxNd;
        FeatureTree back;
        int ind_;
        public double count;
        ////int specFeatures;
        public FBinaryArray feats = new FBinaryArray();
        public FTreeBinaryArray subtree = new FTreeBinaryArray();

        static FeatureTree [] roots_ = new FeatureTree[20];


        public FeatureTree()
        {
            auxNd = null;
            back = null;
            ind_ = NULLIND;
            count = 0;
        }

        //FeatureTree(int i) :
        //auxNd(NULL),
        //back(NULL),
        //ind_(i),
        //count(0)
        //{}

        FeatureTree(int i, FeatureTree b)
        {
            auxNd = null;
            back = b;
            ind_ = i;
        }

        public FeatureTree(ITokenStream is_)  //FeatureTree(istream& is);
        {
            auxNd = null;
            back = null;
            ind_ = ROOTIND;


            int done = 0;
            int c = 0;
            subtree.set(MAXNUMNTTS);

            while (is_.HasMore && done == 0)  //while(is && !done)
            {
                int val = readOneLevel0(is_, c);
                if (val == -1)
                    done = 1;
                c++;
            }

            roots_[Feature.whichInt] = this;
        }

        ////FeatureTree(istream& is, istream& idxs, int asVal);


        void read(ITokenStream is_, Pointer<FTypeTree> ftt)  //void read(istream& is, FTypeTree* ftt);
        {
            //ECString indStr;
            int indI;

            //is >> count;
            if (!is_.TryRead(out string countToken))
                throw new InvalidDataException("Unexpected EOF while reading FeatureTree count.");
            count = double.Parse(countToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);  //is >> count;

            int cfeats;
            int ctrees;

            //is >> cfeats;
            if (!is_.TryRead(out string cfeatsToken))
                throw new InvalidDataException("Unexpected EOF while reading FeatureTree cfeats.");
            cfeats = int.Parse(cfeatsToken, NumberStyles.Integer, CultureInfo.InvariantCulture);  //is >> cfeats;

            //is >> ctrees;
            if (!is_.TryRead(out string ctreesToken))
                throw new InvalidDataException("Unexpected EOF while reading FeatureTree ctrees.");
            ctrees = int.Parse(ctreesToken, NumberStyles.Integer, CultureInfo.InvariantCulture);  //is >> ctrees;

            //cerr << "R " << ftt->n << " " << ind() << " " << count << endl;
            int cf;
            if (cfeats > 0)
                feats.set(cfeats);

            for (cf = 0; cf < cfeats; cf++)
            {
                //is >> indI;
                if (!is_.TryRead(out string indIToken))
                    throw new InvalidDataException("Unexpected EOF while reading feature index.");
                indI = int.Parse(indIToken, NumberStyles.Integer, CultureInfo.InvariantCulture);

                Feat nf = feats.array_[cf];
                nf.ind_ = indI;

                float v;

                //is >> v;
                if (!is_.TryRead(out string vToken))
                    throw new InvalidDataException("Unexpected EOF while reading feature value.");
                v = float.Parse(vToken, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture);  //is >> v;

                nf.g() = v;
                //cerr << indI << "\t" << v << endl;
            }

            if (ctrees > 0)
                subtree.set(ctrees);

            othReadFeatureTree(is_, ftt, ctrees);
        }


        int readOneLevel0(ITokenStream is_, int c)  //int readOneLevel0(istream& is, int c);
        {
            int nextInd;
            //ECString nextIndStr;

            //is >> nextIndStr;
            //if(!is) return -1;
            if (!is_.TryRead(out string nextIndStr))
                return -1;

            if (nextIndStr == "Selected")
                return -1;

            nextInd = int.Parse(nextIndStr, NumberStyles.Integer, CultureInfo.InvariantCulture);

            FeatureTree nft = subtree.array_[c];
            nft.ind_ = nextInd;
            nft.read(is_, Feature.ftTree[Feature.whichInt].left);
            nft.back = this; 
            return nextInd;
        }


        public FeatureTree follow(int val, int auxCnt)
        {
            if (auxCnt == 0)
            {
                return subtree.find(val);
            }

            if (auxNd == null)
            {
                return null;
                //cerr << "No auxnd " << *this << ", " << val << ", " << auxCnt << endl;
                //assert(auxNd);
            }

            return auxNd.follow(val, auxCnt - 1);
        }


        public static FeatureTree roots(int which) { return roots_[which]; }
        //void         printFfCounts(int asVal, int depth, ostream& os);
        //friend ostream&  operator<<(ostream& os, const FeatureTree& ft);


        public int ind() { return ind_; }


        void othReadFeatureTree(ITokenStream is_, Pointer<FTypeTree> ftt, int ctrees)  //void othReadFeatureTree(istream& is, FTypeTree* ftt, int cnt);
        {
            //cerr << "F " << ftt->n << " " << ind() << " " << count
            //   << " " << ctrees << endl;
            //ECString indStr;
            int indI;
            int c;
            for (c = 0; c < ctrees; c++)
            {
                //is >> indI;
                if (!is_.TryRead(out string indIToken))
                    throw new InvalidDataException("Unexpected EOF while reading subtree index.");
                indI = int.Parse(indIToken, NumberStyles.Integer, CultureInfo.InvariantCulture);  //is >> indI;

                FeatureTree ntr = subtree.array_[c];
                Debug.Assert(ftt.op.left != null);
                ntr.ind_ = indI;
                ntr.read(is_, ftt.op.left);
            }

            if (ftt.op.right == null)
            {
                return;
            }

            Debug.Assert(auxNd == null);

            //is >> indStr;
            if (!is_.TryRead(out string indStr))
                throw new InvalidDataException("Unexpected EOF while reading aux indicator.");

            if (indStr != "A")
            {
                Console.WriteLine("fi = " + ftt.op.n + " " + ctrees + " " + indStr + " " + ind() + " " + count);
                for (int i = 0; i < 5; i++)
                {
                    //ECString tmp;
                    //is >> tmp;
                    if (!is_.TryRead(out string tmp))
                        throw new InvalidDataException("Unexpected EOF while dumping aux debug tokens.");
                    Console.Write(tmp + " ");
                }

                Console.WriteLine();  //cerr << endl;
                Console.WriteLine(ftt.op.right.op.n);  //cerr << ftt->right->n << endl;
                Debug.Assert(indStr == "A");
            }

            int ac;
            //is >> ac;
            if (!is_.TryRead(out string acToken))
                throw new InvalidDataException("Unexpected EOF while reading aux count.");
            ac = int.Parse(acToken, NumberStyles.Integer, CultureInfo.InvariantCulture);  //is >> ac;

            /* auxNds point back not to the node the are auxes of, but to its pred */
            auxNd = new FeatureTree(AUXIND, this.back);
            if (ac > 0)
                auxNd.subtree.set(ac);

            auxNd.othReadFeatureTree(is_, ftt.op.right, ac);
        }


        //void printFfCounts2(int asVal, int depth, ostream& os);
    }
}
