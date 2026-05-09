using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.FeatureTree_global;
using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    public readonly struct TokenSpan
    {
        public readonly string Line;
        public readonly int Start;
        public readonly int Length;

        public TokenSpan(string line, int start, int length)
        {
            Line = line;
            Start = start;
            Length = length;
        }

        public ReadOnlySpan<char> Span => Line.AsSpan(Start, Length);

        public bool EqualsLiteral(string s) => Span.SequenceEqual(s.AsSpan());

        public bool TryParseInt32(out int value) =>
            int.TryParse(Span, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

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

        bool TryReadInt32(out int value);
        bool TryReadSingle(out float value);
        bool TryReadDouble(out double value);

        bool TryReadToken(out TokenSpan token);
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

        private string m_line;
        private int m_pos;
        private bool m_isEndOfStream;

        public TextReaderTokenStream(TextReader reader) => m_reader = reader ?? throw new ArgumentNullException(nameof(reader));

        public bool HasMore
        {
            get
            {
                if (m_isEndOfStream)
                    return false;

                if (TrySkipWhitespaceToToken())
                    return true;

                while (true)
                {
                    m_line = m_reader.ReadLine();
                    if (m_line == null)
                    {
                        m_isEndOfStream = true;
                        return false;
                    }

                    m_pos = 0;

                    if (TrySkipWhitespaceToToken())
                        return true;
                }
            }
        }

        public bool TryRead(out string token)
        {
            token = null;

            if (!TryReadToken(out TokenSpan t))
                return false;

            // Allocates only when you call this (we will avoid it for numeric tokens).
            token = m_line.Substring(t.Start, t.Length);
            return true;
        }

        public bool TryReadInt32(out int value)
        {
            value = 0;

            if (!TryReadToken(out TokenSpan t))
                return false;

            return int.TryParse(t.Span, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        public bool TryReadSingle(out float value)
        {
            value = 0;

            if (!TryReadToken(out TokenSpan t))
                return false;

            return float.TryParse(t.Span, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
        }

        public bool TryReadDouble(out double value)
        {
            value = 0;

            if (!TryReadToken(out TokenSpan t))
                return false;

            return double.TryParse(t.Span, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
        }

        public bool TryReadToken(out TokenSpan token)
        {
            token = default;

            if (!HasMore)
                return false;

            int start = m_pos;

            while (m_pos < m_line.Length && !char.IsWhiteSpace(m_line[m_pos]))
                m_pos++;

            int length = m_pos - start;
            token = new TokenSpan(m_line, start, length);
            return length > 0;
        }

        private bool TrySkipWhitespaceToToken()
        {
            if (m_line == null)
                return false;

            while (m_pos < m_line.Length && char.IsWhiteSpace(m_line[m_pos]))
                m_pos++;

            return m_pos < m_line.Length;
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
        private int m_featCount;
        private Feat m_inlineFeat;     // only used when m_featCount == 1
        private FBinaryArray feats;     // only used when m_featCount > 1
        public FTreeBinaryArray subtree;

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
            subtree = new FTreeBinaryArray();
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


        public void ResetForRead()
        {
            // Clear per-node state so the object can be reused safely during model loading.
            auxNd = null;
            back = null;
            ind_ = FeatureTree_global.NULLIND;
            count = 0;

            // Reset feature storage bookkeeping. The actual buffers (FBinaryArray) can be reused.
            m_featCount = 0;

            // If this node is reused and later ends up with no subtree, we want follow() to behave
            // as if there is no subtree (same as current code path that sets subtree = null).
            // We keep subtree allocated only if the reader sets it up again.
            subtree = null;

            // feats: keep the object (if any) for reuse; its size will be set() when needed.
            // m_inlineFeat is overwritten when m_featCount == 1.
        }


        void read(ITokenStream is_, Pointer<FTypeTree> ftt)  //void read(istream& is, FTypeTree* ftt);
        {
            //ECString indStr;
            int indI;

            //is >> count;
            if (!is_.TryReadDouble(out count))
                throw new InvalidDataException("Unexpected EOF while reading FeatureTree count.");

            int cfeats;
            int ctrees;

            //is >> cfeats;
            if (!is_.TryReadInt32(out cfeats))
                throw new InvalidDataException("Unexpected EOF while reading FeatureTree cfeats.");

            //is >> ctrees;
            if (!is_.TryReadInt32(out ctrees))
                throw new InvalidDataException("Unexpected EOF while reading FeatureTree ctrees.");

            m_featCount = cfeats;

            //cerr << "R " << ftt->n << " " << ind() << " " << count << endl;
            int cf;
            if (cfeats > 0)
            {
                if (cfeats == 1)
                {
                    feats = null; // ensure we don’t keep an object around
                }
                else  // cfeats > 1
                {
                    if (feats == null)
                        feats = new FBinaryArray();

                    feats.set(cfeats);
                }
            }

            for (cf = 0; cf < cfeats; cf++)
            {
                //is >> indI;
                if (!is_.TryReadInt32(out indI))
                    throw new InvalidDataException("Unexpected EOF while reading feature index.");

                ref Feat nf = ref feats_index_ref(cf);
                nf.ind_ = indI;

                float v;

                //is >> v;
                if (!is_.TryReadSingle(out v))
                    throw new InvalidDataException("Unexpected EOF while reading feature value.");

                Feat.g_ref(ref nf) = v;  //nf.g() = v;

                //cerr << indI << "\t" << v << endl;
            }

            if (ctrees > 0)
            {
                if (subtree == null)
                    subtree = new FTreeBinaryArray();

                subtree.set(ctrees);
            }
            else
            {
                subtree = null;
            }

            othReadFeatureTree(is_, ftt, ctrees);
        }


        int readOneLevel0(ITokenStream is_, int c)  //int readOneLevel0(istream& is, int c);
        {
            int nextInd;
            //ECString nextIndStr;

            //is >> nextIndStr;
            //if(!is) return -1;
            if (!is_.TryReadToken(out TokenSpan nextIndToken))
                return -1;

            if (nextIndToken.EqualsLiteral("Selected"))  //if (nextIndStr == "Selected")
                return -1;

            if (!nextIndToken.TryParseInt32(out nextInd))
                throw new InvalidDataException("Expected subtree index or 'Selected'.");

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
                if (subtree == null)
                    return null;

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
                if (!is_.TryReadInt32(out indI))
                    throw new InvalidDataException("Unexpected EOF while reading subtree index.");

                FeatureTree ntr = subtree.array_[c];
                AssertInternal(ftt.op.left != null);
                ntr.ind_ = indI;
                ntr.read(is_, ftt.op.left);
            }

            if (ftt.op.right == null)
            {
                return;
            }

            AssertInternal(auxNd == null);

            //is >> indStr;
            if (!is_.TryReadToken(out TokenSpan indTok))
                throw new InvalidDataException("Unexpected EOF while reading aux indicator.");

            if (!indTok.EqualsLiteral("A"))  //if (indStr != "A")
            {
                Console.WriteLine("fi = " + ftt.op.n + " " + ctrees + " " + indTok.Span.ToString() + " " + ind() + " " + count);
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
                AssertInternal(indTok.EqualsLiteral("A"));  //Debug.Assert(indStr == "A");
            }

            int ac;
            //is >> ac;
            if (!is_.TryReadInt32(out ac))
                throw new InvalidDataException("Unexpected EOF while reading aux count.");

            /* auxNds point back not to the node the are auxes of, but to its pred */
            auxNd = new FeatureTree(AUXIND, this.back);
            if (ac > 0)
            {
                if (auxNd.subtree == null)
                    auxNd.subtree = new FTreeBinaryArray();

                auxNd.subtree.set(ac);
            }

            auxNd.othReadFeatureTree(is_, ftt.op.right, ac);
        }


        //void printFfCounts2(int asVal, int depth, ostream& os);


        // Helpers for Feat access
        public int feats_size() => m_featCount;

        public ref Feat feats_index_ref(int index)
        {
            if (m_featCount == 1)
                return ref m_inlineFeat;

            return ref feats.index_ref(index);
        }

        public ref readonly Feat feats_index_ref_readonly(int index)
        {
            if (m_featCount == 1)
                return ref m_inlineFeat;

            return ref feats.index_ref_readonly(index);
        }

        public bool try_feats_find_index(int id, out int index)
        {
            if (m_featCount == 1)
            {
                if (m_inlineFeat.ind_ == id)
                {
                    index = 0;
                    return true;
                }

                index = -1;
                return false;
            }

            return feats.try_find_index(id, out index);
        }
    }
}
