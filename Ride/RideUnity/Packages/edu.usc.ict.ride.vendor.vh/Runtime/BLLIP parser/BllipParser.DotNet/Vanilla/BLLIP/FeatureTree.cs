using System;
using System.Diagnostics;

using static BllipParser.DotNet.Vanilla.Feature_global;
using static BllipParser.DotNet.Vanilla.FeatureTree_global;


namespace BllipParser.DotNet.Vanilla
{
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

        public FeatureTree(string [] is_)  //FeatureTree(istream& is);
        {
            auxNd = null;
            back = null;
            ind_ = ROOTIND;


            int done = 0;
            int c = 0;
            subtree.set(MAXNUMNTTS);

            int is_Idx = 0;
            while (is_Idx < is_.Length && done == 0)  //while(is && !done)
            {
                int val = readOneLevel0(is_, ref is_Idx, c);
                if (val == -1)
                    done = 1;
                c++;
            }

            roots_[Feature.whichInt] = this;
        }

        ////FeatureTree(istream& is, istream& idxs, int asVal);


        void read(string [] is_, ref int is_Idx, Pointer<FTypeTree> ftt)  //void read(istream& is, FTypeTree* ftt);
        {
            //ECString indStr;
            int indI;
            count = Convert.ToDouble(is_[is_Idx++]);  //is >> count;
            int cfeats;
            int ctrees;
            cfeats = Convert.ToInt32(is_[is_Idx++]);  //is >> cfeats;
            ctrees = Convert.ToInt32(is_[is_Idx++]);  //is >> ctrees;
            //cerr << "R " << ftt->n << " " << ind() << " " << count << endl;
            int cf;
            if (cfeats > 0)
                feats.set(cfeats);

            for (cf = 0; cf < cfeats; cf++)
            {
                indI = Convert.ToInt32(is_[is_Idx++]);  //is >> indI;
                Feat nf = feats.array_[cf];
                nf.ind_ = indI;
                float v;
                v = Convert.ToSingle(is_[is_Idx++]);  //is >> v;
                nf.g() = v;
                //cerr << indI << "\t" << v << endl;
            }

            if (ctrees > 0)
                subtree.set(ctrees);

            othReadFeatureTree(is_, ref is_Idx, ftt, ctrees);
        }


        int readOneLevel0(string [] is_, ref int is_Idx, int c)  //int readOneLevel0(istream& is, int c);
        {
            int nextInd;
            ECString nextIndStr;
            nextIndStr = is_[is_Idx++];  //is >> nextIndStr;
            if (is_Idx >= is_.Length)  //if (!is)
                return -1;
            if (nextIndStr == "Selected")
                return -1;
            nextInd = Convert.ToInt32(nextIndStr);
            FeatureTree nft = subtree.array_[c];
            nft.ind_ = nextInd;
            nft.read(is_, ref is_Idx, Feature.ftTree[Feature.whichInt].left);
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


        void othReadFeatureTree(string [] is_, ref int is_Idx, Pointer<FTypeTree> ftt, int ctrees)  //void othReadFeatureTree(istream& is, FTypeTree* ftt, int cnt);
        {
            //cerr << "F " << ftt->n << " " << ind() << " " << count
            //   << " " << ctrees << endl;
            ECString indStr;
            int indI;
            int c;
            for (c = 0; c < ctrees; c++)
            {
                indI = Convert.ToInt32(is_[is_Idx++]);  //is >> indI;
                FeatureTree ntr = subtree.array_[c];
                Debug.Assert(ftt.op.left != null);
                ntr.ind_ = indI;
                ntr.read(is_, ref is_Idx, ftt.op.left);
            }

            if (ftt.op.right == null)
            {
                return;
            }

            Debug.Assert(auxNd == null);
            indStr = is_[is_Idx++];  //is >> indStr;
            if (indStr != "A")
            {
                Console.WriteLine("fi = " + ftt.op.n + " " + ctrees + " " + indStr + " " + ind() + " " + count);
                for (int i = 0; i < 5; i++)
                {
                    ECString tmp;
                    tmp = is_[is_Idx++];  //is >> tmp;
                    Console.Write(tmp + " ");
                }

                Console.WriteLine();  //cerr << endl;
                Console.WriteLine(ftt.op.right.op.n);  //cerr << ftt->right->n << endl;
                Debug.Assert(indStr == "A");
            }

            int ac;
            ac = Convert.ToInt32(is_[is_Idx++]);  //is >> ac;
            /* auxNds point back not to the node the are auxes of, but to its pred */
            auxNd = new FeatureTree(AUXIND, this.back);
            if (ac > 0)
                auxNd.subtree.set(ac);

            auxNd.othReadFeatureTree(is_, ref is_Idx, ftt.op.right, ac);
        }


        //void printFfCounts2(int asVal, int depth, ostream& os);
    }
}
