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
    struct MH2O
    {
        public MH2OInfo info;
        public MH2OHeader header;
        public float[,] heights;
        public long RenderMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MH2OInfo
    {
        public short type;
        public short flags;
        public float heightlevel1;
        public float heightlevel2;
        public byte x_offset;
        public byte y_offset;
        public byte width;
        public byte height;
        public int ofsMask2;
        public int ofsHeightMap;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MH2OBlock
    {
        MH2OHeader header;
        MH2OInfo info;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MH2OEntry
    {
        public int layeroffset;
        public int layercount;
        public int render;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MH2OHeader
    {
        public int layeroffset;
        public int layercount;
        public int render;
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
        public int Id;
        public string File;
        public Vector3 Position;
        public Vector3 Rotation;
        public float Scale;
    }

    public struct WMO
    {
        public int Id;
        public string File;
        public Vector3 Position;
        public Vector3 Rotation;
        public BoundingBox Bounds;
        public short DoodadSet;
    }
}
