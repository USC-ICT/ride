using System;

using static BllipParser.DotNet.Vanilla.utils;


namespace BllipParser.DotNet.Vanilla
{
    class Field
    {
        int fragmentation_;
        byte [] mask_;


        public Field(int length, byte [] mask)
        {
            fragmentation_ = length;
            mask_ = mask;


            if (fragmentation_ <= 0)
                error("fragmentation <= 0");
        }


        public bool in_(int integer)
        {
            if (integer < fragmentation_)
            {
                if (integer < 0)
                    error("Field given integer < 0");

                return mask_[integer] != 0;
            }
            else
            {
                int	index = integer % fragmentation_;
                return mask_[index] != 0;
            }
        }
    }
}
