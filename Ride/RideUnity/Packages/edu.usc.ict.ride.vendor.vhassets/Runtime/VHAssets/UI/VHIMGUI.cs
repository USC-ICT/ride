using UnityEngine;

namespace VHAssets
{
public static class VHIMGUI
{
    public static Rect ScaleToRes(ref Rect r)
    {
        r.x *= Screen.width;
        r.y *= Screen.height;
        r.width *= Screen.width;
        r.height *= Screen.height;
        return r;
    }
}
}
