using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace adt4
{
    [StructLayout(LayoutKind.Explicit)]
    struct MCNK
    {
        [FieldOffset(4)]
        public int x;
        [FieldOffset(8)]
        public int y;
        [FieldOffset(16)]
        public int nDoodadRefs;
        [FieldOffset(20)]
        public int ofsHeight;
        [FieldOffset(32)]
        public int ofsRefs;
        [FieldOffset(56)]
        public int nMapObjRefs;
        [FieldOffset(60)]
        public int holes;
        [FieldOffset(104)]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] position;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MCIN
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public MCINEntry[] entries;
    };

    [StructLayout(LayoutKind.Sequential)]
    struct MHDR
    {
        public int flags;
        public int mcin;
        public int mtex;
        public int mmdx;
        public int mmid;
        public int mwmo;
        public int mwid;
        public int mddf;
        public int modf;
        public int mfbo;                     // this is only set if flags & mhdr_MFBO.
        public int mh2o;
        public int mtxf;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MVER
    {
        int version;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MCVT
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 145)]
        public float[] z;
    }

    struct Model
    {
        public int id;
        public string file;
        public Vector3 position;
        public Vector3 rotation;
        public float scale;
    }

    public struct WMO
    {
        public int id;
        public string file;
        public Vector3 position;
        public Vector3 rotation;
        public BoundingBox bounds;
        public short doodadSet;
    }
}
