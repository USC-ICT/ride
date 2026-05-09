using System;


namespace BllipParser.DotNet.Vanilla
{
    class FBinaryArray
    {
        int size_;
        Feat [] array_;


        public FBinaryArray()
        {
            size_ = 0;
        }


        public void set(int sz)
        {
            size_ = sz;
            //array_ = new Feat[sz];
            if (array_ == null || array_.Length < sz)
                array_ = new Feat[sz];
        }


        //Feat* FBinaryArray::find(const int id) const

        public bool try_find_index(int id, out int index)
        {
            int top = size_;
            int bot = -1;

            while (true)
            {
                if (top <= bot + 1)
                {
                    index = -1;
                    return false;
                }

                int mid = (top + bot) / 2;
                int midInd = array_[mid].ind_;

                if (id == midInd)
                {
                    index = mid;
                    return true;
                }
                else if (id < midInd)
                {
                    top = mid;
                }
                else
                {
                    bot = mid;
                }
            }
        }


        public int size() { return size_; }
        public ref Feat index_ref(int i) { return ref array_[i]; }
        public ref readonly Feat index_ref_readonly(int i) { return ref array_[i]; }
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

            // Reuse backing array (avoid per-set allocations).
            if (array_ == null || array_.Length < sz)
            {
                // Grow; keep existing objects.
                int oldLen = array_ != null ? array_.Length : 0;
                Array.Resize(ref array_, sz);

                for (int i = oldLen; i < sz; i++)
                    array_[i] = new FeatureTree();
            }

            // Reset only the active range that will be filled by the reader.
            for (int i = 0; i < sz; i++)
                array_[i].ResetForRead();
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
