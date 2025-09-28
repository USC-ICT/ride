using System;


namespace BllipParser.DotNet.Vanilla
{
    class FBinaryArray
    {
        int size_;
        public Feat [] array_;


        public FBinaryArray()
        {
            size_ = 0;
        }


        public void set(int sz)
        {
            size_ = sz;
            array_ = new Feat[sz];
            for (int i = 0; i < array_.Length; i++)
                array_[i] = new Feat();
        }


        public Feat find(in int id)
        {
            int top = size_;
            int bot = -1;
            int midInd;
            for ( ; ; )
            {
                if (top <= bot + 1)
                {
                    return null;
                }

                int mid = (top + bot) / 2;
                Feat midH = array_[mid];
                midInd = midH.ind();
                if (id == midInd)
                    return midH;
                else if (id < midInd)
                    top = mid;
                else
                    bot = mid;
            }
        }


        public int size() { return size_; }
        public Feat index(int i) { return array_[i]; }
    }


    class FTreeBinaryArray
    {
        int size_;
        public FeatureTree [] array_;


        public FTreeBinaryArray()
        {
            size_ = 0;
        }


        public void set(int sz)
        {
            size_ = sz;
            array_ = new FeatureTree[sz];
            for (int i = 0; i < array_.Length; i++)
                array_[i] = new FeatureTree();
        }


        public FeatureTree find(in int id)
        {
            int top = size_;
            int bot = -1;
            int midInd;
            for ( ; ; )
            {
                if (top <= bot + 1)
                {
                    return null;
                }

                int mid = (top + bot) / 2;
                FeatureTree midH = array_[mid];
                midInd = midH.ind();
                if (id == midInd)
                    return midH;
                else if (id < midInd)
                    top = mid;
                else
                    bot = mid;
            }
        }


        public int size() { return size_; }
        public FeatureTree index(int i) { return array_[i]; }
    }
}
