using System;
using System.Diagnostics;

namespace Ride
{
    /// <summary>
    /// Represents a 4x4 vector
    /// This is a parallel class to <a href="https://docs.unity3d.com/ScriptReference/Matrix4x4.html">UnityEngine.Matrix4x4</a>.
    /// Implemented separately to abstract Ride classes away from UnityEngine specific implementations.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public struct RideMatrix4x4
    {
        public float m00;
        public float m10;
        public float m20;
        public float m30;
        public float m01;
        public float m11;
        public float m21;
        public float m31;
        public float m02;
        public float m12;
        public float m22;
        public float m32;
        public float m03;
        public float m13;
        public float m23;
        public float m33;

        public RideMatrix4x4(RideVector4 column0, RideVector4 column1, RideVector4 column2, RideVector4 column3)
        {
            m00 = column0.x;
            m10 = column0.y;
            m20 = column0.z;
            m30 = column0.w;

            m01 = column1.x;
            m11 = column1.y;
            m21 = column1.z;
            m31 = column1.w;

            m02 = column2.x;
            m12 = column2.y;
            m22 = column2.z;
            m32 = column2.w;

            m03 = column3.x;
            m13 = column3.y;
            m23 = column3.z;
            m33 = column3.w;
        }

        public RideMatrix4x4(UnityEngine.Matrix4x4 mat) : this(mat.GetColumn(0), mat.GetColumn(1), mat.GetColumn(2), mat.GetColumn(3)) { }

        public float this[int index]
        {
            get => index switch
            {
                0 => m00,
                1 => m10,
                2 => m20,
                3 => m30,
                4 => m01,
                5 => m11,
                6 => m21,
                7 => m31,
                8 => m02,
                9 => m12,
                10 => m22,
                11 => m32,
                12 => m03,
                13 => m13,
                14 => m23,
                15 => m33,
                _ => throw new IndexOutOfRangeException("Invalid matrix index!"),
            };
            set
            {
                switch (index)
                {
                    case 0: m00 = value; break;
                    case 1: m10 = value; break;
                    case 2: m20 = value; break;
                    case 3: m30 = value; break;
                    case 4: m01 = value; break;
                    case 5: m11 = value; break;
                    case 6: m21 = value; break;
                    case 7: m31 = value; break;
                    case 8: m02 = value; break;
                    case 9: m12 = value; break;
                    case 10: m22 = value; break;
                    case 11: m32 = value; break;
                    case 12: m03 = value; break;
                    case 13: m13 = value; break;
                    case 14: m23 = value; break;
                    case 15: m33 = value; break;
                    default: throw new IndexOutOfRangeException("Invalid matrix index!");
                }
            }
        }

        public float this[int row, int column] { get { return this[row + column * 4]; } set { this[row + column * 4] = value; } }

        public float determinant => UnityEngine.Matrix4x4.Determinant(this);

        public UnityEngine.Matrix4x4 ToMatrix4x4() => new UnityEngine.Matrix4x4(GetColumn(0), GetColumn(1), GetColumn(2), GetColumn(3));

        public static implicit operator UnityEngine.Matrix4x4(RideMatrix4x4 mat) => mat.ToMatrix4x4();
        public static implicit operator RideMatrix4x4(UnityEngine.Matrix4x4 mat) => new RideMatrix4x4(mat);

        public static RideMatrix4x4 operator *(RideMatrix4x4 lhs, RideMatrix4x4 rhs) => lhs.ToMatrix4x4() * rhs.ToMatrix4x4();

        public RideVector4 GetColumn(int index)
        {
            return index switch
            {
                0 => new RideVector4(m00, m10, m20, m30),
                1 => new RideVector4(m01, m11, m21, m31),
                2 => new RideVector4(m02, m12, m22, m32),
                3 => new RideVector4(m03, m13, m23, m33),
                _ => throw new IndexOutOfRangeException("Invalid column index!"),
            };
        }

        public static RideMatrix4x4 Inverse(RideMatrix4x4 m) => UnityEngine.Matrix4x4.Inverse(m);
        public static RideMatrix4x4 TRS(RideVector3 pos, RideQuaternion q, RideVector3 s) => UnityEngine.Matrix4x4.TRS(pos, q, s);
        public static RideMatrix4x4 Perspective(float fov, float aspect, float zNear, float zFar) => UnityEngine.Matrix4x4.Perspective(fov, aspect, zNear, zFar);
    }
}
