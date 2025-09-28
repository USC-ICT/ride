using System;

using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    class CntxArray : IComparable<CntxArray>
    {
        int [] d = new int[7];
        public static int sz;


        public CntxArray()
        {
            throw new NotImplementedException();
        }

        public CntxArray(int [] data)  //CntxArray(int * data);
        {
            int i;
            for (i = 0; i < sz; i++)
                //d[i] = data[i+1];
                d[i] = data[i];
        }


        public int CompareTo(object obj)
        {
            if (obj is CntxArray array) return CompareTo(array);
            else throw new ArgumentException();
        }

        public int CompareTo(CntxArray other)  //friend int operator< (CntxArray a1, CntxArray a2);
        {
            int i;
            for (i = 0; i < sz; i++)
            {
                if (d[i] > other.d[i])
                    return 0;
                else if (d[i] < other.d[i])
                    return 1;
                else if (d[i] < 0)
                    return 0;
            }

            return 0;
        }


        public override string ToString()  //friend ostream& operator<< ( ostream& os, const CntxArray& ca );
        {
            string os = "";

            int i;
            //int sz = ca.sz;
            for (i = 0; i < sz; i++)
            {
                int val = d[i];
                if (val == -1)
                    os += ".";
                else
                    os += val;

                if (i != sz - 1)
                    os += "/";
            }

            return os;
        }
    }
}
