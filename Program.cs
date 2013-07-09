using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace adt4
{
    class Adt
    {
        MCNK[] mcnk = new MCNK[256];
        MCVT[] mcvt = new MCVT[256];
        MH2O[] mh2o = new MH2O[256];
        Obj obj;
        IntPtr file;
        MDDF mddf;
        string[] mmdx;
        MODF modf;
        string[] mwmo;
        //MH2OInfo[] mh2o_i = new MH2OInfo[256];

        public Adt(string adt, Obj obj)
        {
            this.obj = obj;

            file = Mpq.OpenFile(adt);

            if (file == IntPtr.Zero)
                return;

            MVER mver = ReadChunk<MVER>(file);
            MHDR mhdr = ReadChunk<MHDR>(file);

            MCIN mcin = ReadChunk<MCIN>(file);

            for (int i = 0; i < 256; i++)
            {
                int offset = mcin.entries[i].mcnk;
                Mpq.Seek(file, offset);

                mcnk[i] = ReadChunk<MCNK>(file);
                Mpq.Seek(file, offset + mcnk[i].ofsHeight);
                mcvt[i] = ReadChunk<MCVT>(file);
                mcnk[i].ofsRefs += offset;
            }

            //Models

            Mpq.Seek(file, mhdr.mmdx + 24);

            byte[] b;
            int size = Read<int>(file);
            b = Mpq.ReadFile(file, size);
            string hej = Encoding.Default.GetString(b);
            mmdx = hej.Split('\0');
            Array.Resize(ref mmdx, mmdx.Length - 1);

            Mpq.Seek(file, mhdr.mddf + 24);
            size = Read<int>(file);

            
            mddf.Entries = new MDDFEntry[size / 36];

            for (int i = 0; i < size / 36; i++)
            {
                b = Mpq.ReadFile(file, 36);
                GCHandle pinnedArray = GCHandle.Alloc(b, GCHandleType.Pinned);
                IntPtr addr = pinnedArray.AddrOfPinnedObject();
                mddf.Entries[i] = (MDDFEntry)Marshal.PtrToStructure(addr, typeof(MDDFEntry));
                pinnedArray.Free();
            }

            //WMO

            Mpq.Seek(file, mhdr.mwmo + 24);
            size = Read<int>(file);
            b = Mpq.ReadFile(file, size);
            hej = Encoding.Default.GetString(b);
            mwmo = hej.Split('\0');
            Array.Resize(ref mwmo, mwmo.Length - 1);

            Mpq.Seek(file, mhdr.modf + 24);
            size = Read<int>(file);

            modf.Entries = new MODFEntry[size / 64];

            for (int i = 0; i < size / 64; i++)
            {
                b = Mpq.ReadFile(file, 64);
                GCHandle pinnedArray = GCHandle.Alloc(b, GCHandleType.Pinned);
                IntPtr addr = pinnedArray.AddrOfPinnedObject();
                modf.Entries[i] = (MODFEntry)Marshal.PtrToStructure(addr, typeof(MODFEntry));
                pinnedArray.Free();
            }

            //Water

            if (mhdr.mh2o == 0)
                return;
            Mpq.Seek(file, mhdr.mh2o + 28);
            //MH2O mh2o = Mpq.ReadChunk<MH2O>(file);

            for (int i = 0; i < 256; i++)
            {
                mh2o[i].header = Mpq.ReadStruct<MH2OHeader>(file);
            }

            for (int i = 0; i < 256; i++)
            {
                if (mh2o[i].header.layercount > 0)
                {
                    Mpq.Seek(file, mhdr.mh2o + 28 + mh2o[i].header.layeroffset);
                    mh2o[i].info = Mpq.ReadStruct<MH2OInfo>(file);
                    Mpq.Seek(file, mhdr.mh2o + 28 + mh2o[i].info.ofsHeightMap);
                    mh2o[i].heights = new float[8, 8];

                    /*if(mh2o[i].info.type == 2)
                        continue;
                    if(mh2o[i].info.ofsHeightMap == 0)
                        continue;*/

                    for (int y = mh2o[i].info.y_offset; y < mh2o[i].info.height + mh2o[i].info.y_offset; y++)
                    {
                        for (int x = mh2o[i].info.x_offset; x < mh2o[i].info.width + mh2o[i].info.x_offset; x++)
                        {
                            mh2o[i].heights[x, y] = Mpq.Read<float>(file);
                        }
                    }

                    if (mh2o[i].header.render == 0)
                    {
                        mh2o[i].RenderMask = long.MaxValue;
                        continue;
                    }

                    Mpq.Seek(file, mhdr.mh2o + 28 + mh2o[i].header.render);
                    
                    mh2o[i].RenderMask = Mpq.Read<long>(file);
                }
            }
        }

        public Model[] GetModels()
        {
            List<Model> hej = new List<Model>();

            foreach (int i in Models)
            {
                Model balle;
                balle.Id = mddf.Entries[i].uniqueId;
                balle.File = mmdx[mddf.Entries[i].mmidEntry];
                balle.Position = new Vector3(mddf.Entries[i].position[0], mddf.Entries[i].position[1], mddf.Entries[i].position[2]);
                balle.Rotation = new Vector3(mddf.Entries[i].rotation[0], mddf.Entries[i].rotation[1], mddf.Entries[i].rotation[2]);
                balle.Scale = mddf.Entries[i].scale / 1024.0f;
                hej.Add(balle);
            }

            return hej.ToArray();
        }

        static Vector3 FloatToVector(float[] f)
        {
            return new Vector3(f[0], f[1], f[2]);
        }

        public WMO[] GetWObjects()
        {
            List<WMO> hej = new List<WMO>();

            foreach (int i in wmos)
            {
                WMO balle;
                balle.Id = modf.Entries[i].UniqueId;
                balle.File = mwmo[modf.Entries[i].MwidEntry];
                balle.Position = FloatToVector(modf.Entries[i].Position);
                balle.Rotation = FloatToVector(modf.Entries[i].Rotation);
                
                balle.Bounds = new BoundingBox(FloatToVector(modf.Entries[i].LowerBounds), FloatToVector(modf.Entries[i].UpperBounds));
                balle.Bounds.Min.X -= 51200 / 3f;
                balle.Bounds.Min.Z -= 51200 / 3f;
                balle.Bounds.Max.X -= 51200 / 3f;
                balle.Bounds.Max.Z -= 51200 / 3f;

                balle.Position.X -= 51200f / 3;
                balle.Position.Z -= 51200f / 3;

                balle.DoodadSet = modf.Entries[i].DoodadSet;
                hej.Add(balle);
            }
            return hej.ToArray();
        }

        public List<int> Models = new List<int>();

        public void AddChunk(int _x, int _y)
        {
            if (file == IntPtr.Zero)
                return;

            MCNK m = mcnk[_y * 16 + _x];
            MCVT h = mcvt[_y * 16 + _x];
            MH2O o = mh2o[_y * 16 + _x];
            int l = 0;
            Vector3[] v = new Vector3[145];
            List<int> i = new List<int>();
            double step = 1600.0 / 768.0;
            Vector3[] v2 = new Vector3[10];

            const float kuk = 25f / 6;

            Vector3[,] MiddlePositions = new Vector3[8, 8];
            Vector3[,] OuterPositions = new Vector3[9, 9];

            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    OuterPositions[x, y] = new Vector3(x * kuk - m.position[1], h.z[y * 17 + x] + m.position[2], y * kuk - m.position[0]);
                }
            }

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    MiddlePositions[x, y] = new Vector3(x * kuk - m.position[1] + kuk / 2, h.z[y * 17 + 9 + x] + m.position[2], y * kuk - m.position[0] + kuk / 2);
                }
            }

            for (int k = 0; k < 8; k++)
            {
                for (int j = 0; j < 8; j++)
                {
                    l = j + k * 17 + 1;

                    i.AddRange(new int[] { l + 9, l + 1, l });
                    i.AddRange(new int[] { l + 9, l + 18, l + 1 });
                    i.AddRange(new int[] { l + 9, l + 17, l + 18 });
                    i.AddRange(new int[] { l + 9, l, l + 17 });
                }
            }

            for (int j = 0; j < 145; j++)
            {
                int row_idx = j / 17;
                int col_idx = j % 17;

                bool isRow9x9 = col_idx < 9;
                if (isRow9x9)
                {
                    v[j].X = (float)(col_idx * step * 2);
                    v[j].Z = (float)(row_idx * step * 2);
                }
                else
                {
                    v[j].X = (float)((col_idx - 9) * step * 2 + step);
                    v[j].Z = (float)(row_idx * step * 2 + step);
                }

                v[j].X += -m.position[1];
                v[j].Y = h.z[j] + m.position[2];
                v[j].Z += -m.position[0];
            }

            //Water

            List<Vector3> water_v = new List<Vector3>();
            List<int> water_i = new List<int>();

            float[,] neger = new float[,]
            {
                { 0, 2 },
                { 2, 0 }
            };

            if (o.header.layercount > 0)
            {
                for (int y = o.info.y_offset; y < o.info.height + o.info.y_offset; y++)
                {
                    for (int x = o.info.x_offset; x < o.info.width + o.info.x_offset; x++)
                    {
                        /*if ((o.RenderMask >> y * 8 >> x & 1) == 0)
                            continue;*/


                        //o.heights[x, y] = 0;

                        //o.heights[x, y] = neger[_x % 2, _y % 2];

                        water_v.AddRange(new[]
                        {
                            new Vector3(OuterPositions[x, y].X, o.heights[x, y], OuterPositions[x, y].Z),
                            new Vector3(OuterPositions[x + 1, y].X, o.heights[x, y], OuterPositions[x + 1, y].Z),
                            new Vector3(OuterPositions[x, y + 1].X, o.heights[x, y], OuterPositions[x, y + 1].Z),
                            new Vector3(OuterPositions[x + 1, y + 1].X, o.heights[x, y], OuterPositions[x + 1, y + 1].Z)
                        });

                        int c = water_v.Count - 4;

                        //Console.Write("{0} ", o.info.type);
                        //Console.Write("0x{0:X} ", o.info.flags);

                        water_i.AddRange(new[]
                        {
                            3 + c, 2 + c, 1 + c,
                            3 + c, 4 + c, 2 + c
                        });
                    }
                }
            }

            obj.Add(water_v.ToArray(), water_i.ToArray());

            //Holes

            for (int cy = 0; cy < 4; cy++)
            {
                for (int cx = 0; cx < 4; cx++)
                {
                    int shift = cy * 4 + cx;
                    if ((m.holes & (1 << shift)) > 0)
                    {
                        for (int k = 0; k < 24; k++)
                        {
                            i[k + cx * 24 + cy * 192] = 0;
                        }
                        for (int k = 0; k < 24; k++)
                        {
                            i[k + cx * 24 + cy * 192 + 96] = 0;
                        }
                    }
                }
            }

            obj.Add(v, i.ToArray());
            
            Mpq.Seek(file, m.ofsRefs + 8);
            for (int j = 0; j < m.nDoodadRefs; j++)
            {
                int model = Mpq.Read<int>(file);
                int id = mddf.Entries[model].uniqueId;
                if (!Models.Contains(model))
                {
                    Models.Add(model);
                }
            }

            for (int j = 0; j < m.nMapObjRefs; j++)
            {
                int mapobj = Mpq.Read<int>(file);
                int id = modf.Entries[mapobj].UniqueId;
                if(!wmos.Contains(mapobj))
                {
                    wmos.Add(mapobj);
                }

            }
        }

        public List<int> wmos = new List<int>();

        T Read<T>(IntPtr h) where T : new()
        {
            object ret;

            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    ret = BitConverter.ToInt32(Mpq.ReadFile(h, 4), 0);
                    break;
                default:
                    throw new NotSupportedException(typeof(T).FullName + " is not currently supported by Read<T>");
            }
            return (T)ret;
        }

        T ReadChunk<T>(IntPtr h) where T : new()
        {
            byte[] b;
            int size;
            GCHandle pinnedArray;
            IntPtr addr;
            T type = new T();

            b = Mpq.ReadFile(h, 4);

            Array.Reverse(b);

            b = Mpq.ReadFile(h, 4);
            size = BitConverter.ToInt32(b, 0);

            b = Mpq.ReadFile(h, size);

            pinnedArray = GCHandle.Alloc(b, GCHandleType.Pinned);
            addr = pinnedArray.AddrOfPinnedObject();
            type = (T)Marshal.PtrToStructure(addr, typeof(T));
            pinnedArray.Free();

            return type;
        }
    }

    struct MODF
    {
        public MODFEntry[] Entries;
    }

    struct MODFEntry
    {
        public int MwidEntry;           // references an entry in the MWID chunk, specifying the model to use.
        public int UniqueId;            // this ID should be unique for all ADTs currently loaded. Best, they are unique for the whole map.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Position;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Rotation;            // same as in MDDF.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] LowerBounds;         // these two are position plus the wmo bounding box.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] UpperBounds;         // they are used for defining when if they are rendered as well as collision.
        public short Flags;               // values from enum MODFFlags.
        public short DoodadSet;           // which WMO doodad set is used.
        public short NameSet;             // which WMO name set is used. Used for renaming goldshire inn to northshire inn while using the same model.
        short _padding;             // it reads only a WORD into the WMAPOBJDEF structure for name. I don't know about the rest.
    }

    struct MDDF
    {
        public MDDFEntry[] Entries;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct MDDFEntry
    {
        public int mmidEntry;           // references an entry in the MMID chunk, specifying the model to use.
        public int uniqueId;            // this ID should be unique for all ADTs currently loaded. Best, they are unique for the whole map. Blizzard has these unique for the whole game.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] position;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] rotation;            // degrees. This is not the same coordinate system orientation like the ADT itself! (see history.)
        public short scale;               // 1024 is the default size equaling 1.0f.
        public short flags;               // values from enum MDDFFlags.
    }    

    [StructLayout(LayoutKind.Sequential)]
    struct MCINEntry
    {
        public int mcnk;                   // absolute offset.
        public int size;                // the size of the MCNK chunk, this is refering to.
        public int flags;               // these two are always 0. only set in the client.
        public int asyncId;
    };

    struct Settings
    {
        public bool Doodad;
        public bool Wmo;
        public bool Terrain;
        public bool Water;
    }

    class Program
    {
        static Obj obj = new Obj();
        static BoundingBox bbox;// = new BoundingBox(new Vector3(-1600 / 48f, float.MinValue, 8500f), new Vector3(1600 / 3f, float.MaxValue, 9100f));

        static Settings settings = new Settings
        {
            Terrain = true
        };

        static void Main(string[] args)
        {
            /*if(!Mpq.HasFile(args[0]))
            {
                Console.WriteLine("File not found");
            }*/

            Match m = Regex.Match(args[0], "(.*)_(\\d*)_(\\d*).*");
            int x, y;
            int.TryParse(m.Groups[2].Value, out x);
            int.TryParse(m.Groups[3].Value, out y);
            string map = m.Groups[1].Value;

            Vector3 bord = new Vector3(1600 / 48f, 0, 1600 / 48f);

            Vector3 min = new Vector3();
            min.X = (x - 32) * 1600 / 3f;
            min.Y = float.MinValue;
            min.Z = (y - 32) * 1600 / 3f;
            min -= bord;

            Vector3 max = new Vector3();
            max.X = (x + 1 - 32) * 1600 / 3f;
            max.Y = float.MaxValue;
            max.Z = (y + 1 - 32) * 1600 / 3f;
            max += bord;

            bbox = new BoundingBox(min, max);

            Adt adt = new Adt(args[0], obj);
            for (int i = 0; i < 256; i++)
            {
                adt.AddChunk(i % 16, i / 16);
            }

            List<Model> models = new List<Model>();
            models.AddRange(adt.GetModels());

            List<WMO> wmos = new List<WMO>();
            wmos.AddRange(adt.GetWObjects());

            Adt[] borders = 
            {
                new Adt(string.Format("{0}_{1}_{2}.adt", map, x, y - 1), obj),
                new Adt(string.Format("{0}_{1}_{2}.adt", map, x, y + 1), obj),
                new Adt(string.Format("{0}_{1}_{2}.adt", map, x - 1, y), obj),
                new Adt(string.Format("{0}_{1}_{2}.adt", map, x + 1, y), obj)
            };

            for (int i = 0; i < 16; i++)
            {
                borders[0].AddChunk(i, 15);
                borders[1].AddChunk(i, 0);
                borders[2].AddChunk(15, i);
                borders[3].AddChunk(0, i);
            }

            foreach (Adt a in borders)
            {
                models.AddRange(a.GetModels());
                wmos.AddRange(a.GetWObjects());
            }

            Adt[] corners =
            {
                new Adt(string.Format("{0}_{1}_{2}.adt", map, x - 1, y - 1), obj),
                new Adt(string.Format("{0}_{1}_{2}.adt", map, x + 1, y - 1), obj),
                new Adt(string.Format("{0}_{1}_{2}.adt", map, x - 1, y + 1), obj),
                new Adt(string.Format("{0}_{1}_{2}.adt", map, x + 1, y + 1), obj)
            };

            corners[0].AddChunk(15, 15);
            corners[1].AddChunk(0, 15);
            corners[2].AddChunk(15, 0);
            corners[3].AddChunk(0, 0);

            foreach (Adt a in corners)
            {
                models.AddRange(a.GetModels());
                wmos.AddRange(a.GetWObjects());
            }

            AddModels(models.ToArray());
            AddWMOS(wmos.ToArray());

            string[] tmp = map.Split('\\');
            map = tmp.Last();
            Console.WriteLine("Saving {0}_{1}_{2}.obj", map, x, y);
            obj.Save(string.Format("{0}_{1}_{2}.obj", map, x, y));
        }

        static void AddModels(Model[] m)
        {
            m = m.Distinct().ToArray();

            foreach (Model i in m)
            {
                AddModel(i.File, i.Position, i.Rotation, i.Scale);
            }
            Console.WriteLine("Added {0} model{1}", m.Length, m.Length == 1 ? "" : "s");
        }

        static void AddWMOS(WMO[] wmo)
        {
            wmo = wmo.Distinct().ToArray();

            for (int i = 0; i < wmo.Length; i++)
            {
                AddWMO(wmo[i]);
                Console.WriteLine("({0}/{1}) {2}", i + 1, wmo.Length, wmo[i].File);
            }
        }

        static void AddWMO(WMO w)
        {
            IntPtr file = Mpq.OpenFile(w.File);

            //Console.WriteLine(w.file);
            byte[] b;

            Mpq.ReadChunk<MVER>(file);
            MOHD mohd = Mpq.ReadChunk<MOHD>(file);
            Mpq.SkipChunk(file);
            Mpq.SkipChunk(file);
            Mpq.SkipChunk(file);

            MOGI[] mogi = new MOGI[mohd.nGroups];

            Mpq.ReadFile(file, 8);

            for (int i = 0; i < mohd.nGroups; i++)
            {
                b = Mpq.ReadFile(file, 32);
                GCHandle array = GCHandle.Alloc(b, GCHandleType.Pinned);
                IntPtr addr = array.AddrOfPinnedObject();
                mogi[i] = (MOGI)Marshal.PtrToStructure(addr, typeof(MOGI));
            }

            Mpq.SkipChunk(file);
            Mpq.SkipChunk(file);
            Mpq.SkipChunk(file);
            Mpq.SkipChunk(file);
            Mpq.SkipChunk(file);
            Mpq.SkipChunk(file);
            Mpq.SkipChunk(file);

            Mpq.ReadFile(file, 8);

            MODS[] mods = new MODS[mohd.nSets];

            for (int i = 0; i < mohd.nSets; i++)
            {
                b = Mpq.ReadFile(file, 32);
                GCHandle array = GCHandle.Alloc(b, GCHandleType.Pinned);
                IntPtr addr = array.AddrOfPinnedObject();
                mods[i] = (MODS)Marshal.PtrToStructure(addr, typeof(MODS));
            }

            int modn_offset = Mpq.Position(file);

            Mpq.ReadFile(file, 4);
            int size = Mpq.Read<int>(file);
            b = Mpq.ReadFile(file, size);

            string[] tmp = Encoding.Default.GetString(b).Split('\0');
            List<string> models = new List<string>();
            foreach (string s in tmp)
            {
                if (s != "")
                {
                    models.Add(s.Split('.')[0] + ".M2");
                }
            }

            Mpq.ReadFile(file, 8);
            MODD[] modd = new MODD[mohd.nDoodads];
            for (int i = 0; i < mohd.nDoodads; i++)
            {
                b = Mpq.ReadFile(file, 40);
                GCHandle array = GCHandle.Alloc(b, GCHandleType.Pinned);
                IntPtr addr = array.AddrOfPinnedObject();
                modd[i] = (MODD)Marshal.PtrToStructure(addr, typeof(MODD));
            }

            Mpq.Seek(file, modn_offset + 4);
            size = Mpq.Read<int>(file);
            b = Mpq.ReadFile(file, size);

            for (int i = 0; i < mohd.nDoodads; i++)
            {
                GCHandle array = GCHandle.Alloc(b, GCHandleType.Pinned);
                IntPtr addr = array.AddrOfPinnedObject();

                modd[i].model = Marshal.PtrToStringAnsi(addr + modd[i].index);
                modd[i].model = modd[i].model.Split('.')[0] + ".M2";
            }

            for (int i = mods[w.DoodadSet].start; i < mods[w.DoodadSet].count; i++)
            {
                Quaternion q = new Quaternion(modd[i].rotation[0], modd[i].rotation[1], modd[i].rotation[2], modd[i].rotation[3]);
                AddModel(modd[i].model, floatToVector(modd[i].positon), q, modd[i].scale, w);
            }

            string group = w.File.Split('.')[0];

            for (int i = 0; i < mohd.nGroups; i++)
            {
                AddGroupWMO(w, string.Format("{0}_{1:000}.WMO", group, i));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MODD
        {
            public int index;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] positon;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] rotation;
            public float scale;
            int unused;
            public string model;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MODS
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
            public string name;
            public int start;
            public int count;
            int unused;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOGI
        {
            int flags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] bmin;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] bmax;
            int unused;
        }

        static void AddGroupWMO(WMO w, string filename)
        {

            IntPtr file = Mpq.OpenFile(filename);

            Mpq.ReadChunk<MVER>(file);
            Mpq.Seek(file, 92);
            byte[] b;

            int size = Mpq.Read<int>(file);

            MOPY[] mopy = new MOPY[size / 2];
            //mopy.entries = new MOPYEntry[size / 2];

            for(int i = 0; i < mopy.Length; i++)
            {
                b = Mpq.ReadFile(file, 2);
                GCHandle array = GCHandle.Alloc(b, GCHandleType.Pinned);
                IntPtr ptr = array.AddrOfPinnedObject();
                mopy[i] = (MOPY)Marshal.PtrToStructure(ptr, typeof(MOPY));
            }

            Mpq.ReadFile(file, 4);
            size = Mpq.Read<int>(file);
            short[] indices = new short[size / 2];
            b = Mpq.ReadFile(file, size);
            Buffer.BlockCopy(b, 0, indices, 0, size);

            List<Vector3> vertices = new List<Vector3>();
            Mpq.ReadFile(file, 4);
            size = Mpq.Read<int>(file);
            float[] vertex = new float[3];

            float d = (float)(Math.PI / 180);
            float j = (float)(Math.PI / 2);

            Matrix m = Matrix.Identity;
            m = Matrix.Multiply(m, Matrix.CreateRotationX(w.Rotation.Z * d - j));
            m = Matrix.Multiply(m, Matrix.CreateRotationY(w.Rotation.Y * d - j));
            m = Matrix.Multiply(m, Matrix.CreateRotationZ(-w.Rotation.X * d));

            Vector3 v;

            for (int i = 0; i < size / 12; i++)
            {
                b = Mpq.ReadFile(file, 12);
                Buffer.BlockCopy(b, 0, vertex, 0, 12);
                v = floatToVector(vertex);

                v = Vector3.Transform(v, m);
                v += w.Position;

                vertices.Add(v);
            }

            List<int> tmp = new List<int>();

            for (int i = 0; i < indices.Length; i += 3)
            {

                if (bbox.Contains(vertices[indices[i]]) > ContainmentType.Disjoint || bbox.Contains(vertices[indices[i + 1]]) > ContainmentType.Disjoint || bbox.Contains(vertices[indices[i + 2]]) > ContainmentType.Disjoint)
                {
                    tmp.Add(indices[i] + 1);
                    tmp.Add(indices[i + 1] + 1);
                    tmp.Add(indices[i + 2] + 1);
                }
            }

            List<int> g = new List<int>(tmp);

            if (g.Count == 0)
                return;

            if (g.Max() != vertices.Count)
            {
                Dictionary<int, int> t = new Dictionary<int, int>();
                List<Vector3> v2 = new List<Vector3>();

                List<int> hej = new List<int>(g);
                hej.Sort();
                for (int i = 0; i < hej.Count - 1; i++)
                {
                    if (hej[i] == hej[i + 1])
                    {
                        hej.RemoveAt(i + 1);
                        i--;
                    }
                }

                for (int i = 0; i < hej.Count; i++)
                {
                    t.Add(hej[i], i + 1);
                    v2.Add(vertices[hej[i] - 1]);
                }

                for (int i = 0; i < g.Count; i++)
                {
                    g[i] = t[g[i]];
                }

                vertices = v2;
            }

            obj.Add(vertices.ToArray(), g.ToArray());
        }

        static Vector3 floatToVector(float[] f)
        {
            return new Vector3(f[0], f[1], f[2]);
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOPY
        {
            public byte flags;
            public byte materialId;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOVI
        {

        }

        [StructLayout(LayoutKind.Explicit)]
        struct MOHD
        {
            [FieldOffset(4)]
            public int nGroups;
            [FieldOffset(16)]
            public int nModels;
            [FieldOffset(20)]
            public int nDoodads;
            [FieldOffset(24)]
            public int nSets;
        }

        static void AddModel(string filename, Vector3 pos, Vector3 rot, float scale)
        {
            IntPtr file = Mpq.OpenFile(filename);

            Mpq.Seek(file, 0x0D8);
            byte[] b;
            //b = Mpq.ReadFile(file, 4);
            int nTriangles = Mpq.Read<int>(file);

            if (nTriangles == 0)
                return;

            Mpq.Seek(file, 0x0DC);
            b = Mpq.ReadFile(file, 4);
            Mpq.Seek(file, BitConverter.ToInt32(b, 0));
            b = Mpq.ReadFile(file, nTriangles * 2);

            short[] indices = new short[nTriangles];
            Buffer.BlockCopy(b, 0, indices, 0, b.Length);

            Mpq.Seek(file, 0x0E0);
            int nVertices = Mpq.Read<int>(file);

            Mpq.Seek(file, 0x0E4);
            b = Mpq.ReadFile(file, 4);
            Mpq.Seek(file, BitConverter.ToInt32(b, 0));
            b = Mpq.ReadFile(file, nVertices * 3 * 4);

            float[] vertices = new float[nVertices * 3];
            Buffer.BlockCopy(b, 0, vertices, 0, b.Length);

            //Vector3[] v = new Vector3[nVertices];
            List<Vector3> v = new List<Vector3>();

            for (int i = 0; i < vertices.Length; i += 3)
            {
                //v[i / 3] = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]);
                v.Add(new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]));
            }

            pos.X -= 51200 / 3.0f;
            pos.Z -= 51200 / 3.0f;

            float d = (float)(Math.PI / 180);
            float j = (float)(Math.PI / 2);

            for(int i = 0; i < v.Count; i++)
            {
                Matrix m = Matrix.Identity;
                m = Matrix.Multiply(m, Matrix.CreateRotationX(rot.Z * d - j));
                m = Matrix.Multiply(m, Matrix.CreateRotationY(rot.Y * d - j));
                m = Matrix.Multiply(m, Matrix.CreateRotationZ(-rot.X * d));
                m = Matrix.Multiply(m, scale);
                v[i] = Vector3.Transform(v[i], m);
                v[i] = new Vector3(v[i].X, v[i].Y, v[i].Z);
                v[i] += pos;
            }

            int[] tmp = new int[indices.Length];
            for (int i = 0; i < indices.Length; i += 3)
            {
                if (bbox.Contains(v[indices[i]]) > ContainmentType.Disjoint || bbox.Contains(v[indices[i + 1]]) > ContainmentType.Disjoint || bbox.Contains(v[indices[i + 2]]) > ContainmentType.Disjoint)
                {
                    tmp[i] = indices[i] + 1;
                    tmp[i + 1] = (indices[i + 1] + 1);
                    tmp[i + 2] = indices[i + 2] + 1;
                }
                else
                {
                    tmp[i] = tmp[i + 1] = tmp[i + 2] = 0;
                }
            }

            List<int> g = new List<int>();
            foreach (int f in tmp)
            {
                if (f != 0)
                    g.Add(f);
            }

            if (g.Count == 0)
                return;

            if (g.Max() != v.Count)
            {
                Dictionary<int, int> t = new Dictionary<int, int>();
                List<Vector3> v2 = new List<Vector3>();

                List<int> hej = new List<int>(g);
                hej.Sort();
                for (int i = 0; i < hej.Count-1; i++)
                {
                    if (hej[i] == hej[i + 1])
                    {
                        hej.RemoveAt(i + 1);
                        i--;
                    }
                }

                for (int i = 0; i < hej.Count; i++)
                {
                    t.Add(hej[i], i + 1);
                    v2.Add(v[hej[i] - 1]);
                }

                for (int i = 0; i < g.Count; i++)
                {
                    g[i] = t[g[i]];
                }

                v = v2;
            }

            obj.Add(v.ToArray(), g.ToArray());
        }

        static void AddModel(string filename, Vector3 pos, Quaternion rot, float scale, WMO w)
        {
            IntPtr file = Mpq.OpenFile(filename);

            Mpq.Seek(file, 0x0D8);
            byte[] b;
            //b = Mpq.ReadFile(file, 4);
            int nTriangles = Mpq.Read<int>(file);

            if (nTriangles == 0)
                return;

            Mpq.Seek(file, 0x0DC);
            b = Mpq.ReadFile(file, 4);
            Mpq.Seek(file, BitConverter.ToInt32(b, 0));
            b = Mpq.ReadFile(file, nTriangles * 2);

            short[] indices = new short[nTriangles];
            Buffer.BlockCopy(b, 0, indices, 0, b.Length);

            Mpq.Seek(file, 0x0E0);
            int nVertices = Mpq.Read<int>(file);

            Mpq.Seek(file, 0x0E4);
            b = Mpq.ReadFile(file, 4);
            Mpq.Seek(file, BitConverter.ToInt32(b, 0));
            b = Mpq.ReadFile(file, nVertices * 3 * 4);

            float[] vertices = new float[nVertices * 3];
            Buffer.BlockCopy(b, 0, vertices, 0, b.Length);

            //Vector3[] v = new Vector3[nVertices];
            List<Vector3> v = new List<Vector3>();

            for (int i = 0; i < vertices.Length; i += 3)
            {
                //v[i / 3] = new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]);
                v.Add(new Vector3(vertices[i], vertices[i + 1], vertices[i + 2]));
            }

            //pos.X -= 51200 / 3.0f;
            //pos.Z -= 51200 / 3.0f;

            float d = (float)(Math.PI / 180);
            float j = (float)(Math.PI / 2);

            Matrix m = Matrix.Identity;
            m = Matrix.Multiply(m, Matrix.CreateRotationX(w.Rotation.X*d));
            m = Matrix.Multiply(m, Matrix.CreateRotationY(w.Rotation.Y*d));
            m = Matrix.Multiply(m, Matrix.CreateRotationZ(w.Rotation.Z*d));

            for (int i = 0; i < v.Count; i++)
            {
                /*Matrix m = Matrix.Identity;
                m = Matrix.Multiply(m, Matrix.CreateRotationX(j));
                m = Matrix.Multiply(m, Matrix.CreateRotationY(j));
                m = Matrix.Multiply(m, Matrix.CreateRotationZ(j));
                m = Matrix.Multiply(m, scale);
                v[i] = Vector3.Transform(v[i], m);
                v[i] = new Vector3(v[i].X, v[i].Y, v[i].Z);*/
                v[i] = Vector3.Transform(v[i], rot) * scale;
                v[i] = Vector3.Transform(v[i], Quaternion.CreateFromAxisAngle(Vector3.UnitY, -j));
                v[i] = Vector3.Transform(v[i], Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -j));

                v[i] += new Vector3(pos.Y, pos.Z, pos.X);
                v[i] = Vector3.Transform(v[i], m);

                v[i] += w.Position;
            }

            int[] tmp = new int[indices.Length];
            for (int i = 0; i < indices.Length; i += 3)
            {
                if (bbox.Contains(v[indices[i]]) > ContainmentType.Disjoint || bbox.Contains(v[indices[i + 1]]) > ContainmentType.Disjoint || bbox.Contains(v[indices[i + 2]]) > ContainmentType.Disjoint)
                {
                    tmp[i] = indices[i] + 1;
                    tmp[i + 1] = (indices[i + 1] + 1);
                    tmp[i + 2] = indices[i + 2] + 1;
                }
                else
                {
                    tmp[i] = tmp[i + 1] = tmp[i + 2] = 0;
                }
            }

            List<int> g = new List<int>();
            foreach (int f in tmp)
            {
                if (f != 0)
                    g.Add(f);
            }

            if (g.Count == 0)
                return;

            if (g.Max() != v.Count)
            {
                Dictionary<int, int> t = new Dictionary<int, int>();
                List<Vector3> v2 = new List<Vector3>();

                List<int> hej = new List<int>(g);
                hej.Sort();
                for (int i = 0; i < hej.Count - 1; i++)
                {
                    if (hej[i] == hej[i + 1])
                    {
                        hej.RemoveAt(i + 1);
                        i--;
                    }
                }

                for (int i = 0; i < hej.Count; i++)
                {
                    t.Add(hej[i], i + 1);
                    v2.Add(v[hej[i] - 1]);
                }

                for (int i = 0; i < g.Count; i++)
                {
                    g[i] = t[g[i]];
                }

                v = v2;
            }

            obj.Add(v.ToArray(), g.ToArray());
        }
    }
}