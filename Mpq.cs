using System;
using System.Text;
using System.Runtime.InteropServices;

namespace adt4
{
    static class Mpq
    {
        const int MPQ_OPEN_READ_ONLY = 0x0100;
        const int SFILE_OPEN_PATCHED_FILE = 0x00000001;
        static IntPtr handle;

        static Mpq()
        {
            string path = @"E:\Wow 3.3.5a\World of Warcraft\Data\";
            SFileOpenArchive(path + "common.MPQ", 0, MPQ_OPEN_READ_ONLY, out handle);
            string[] patches = { "common-2.MPQ", "expansion.MPQ", "patch.MPQ", "patch-2.MPQ", "patch-3.MPQ" };

            foreach (string s in patches)
                SFileOpenPatchArchive(handle, path + s, "", 0);
        }

        public static IntPtr OpenFile(string filename)
        {
            IntPtr file;
            SFileOpenFileEx(handle, filename, SFILE_OPEN_PATCHED_FILE, out file);
            return file;
        }

        public static int Seek(IntPtr file, int pos)
        {
            int pos_h;
            return SFileSetFilePointer(file, pos, out pos_h, 0);
        }

        public static byte[] ReadFile(IntPtr file, int count)
        {
            byte[] buf = new byte[count];
            int read;
            SFileReadFile(file, buf, count, out read, IntPtr.Zero);
            return buf;
        }

        public static T Read<T>(IntPtr h) where T : new()
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

        public static T ReadChunk<T>(IntPtr h) where T : new()
        {
            byte[] b;
            int size;
            GCHandle pinnedArray;
            IntPtr addr;
            T type = new T();
            string id;

            b = Mpq.ReadFile(h, 4);

            Array.Reverse(b);
            id = Encoding.Default.GetString(b);

            b = Mpq.ReadFile(h, 4);
            size = BitConverter.ToInt32(b, 0);

            b = Mpq.ReadFile(h, size);

            pinnedArray = GCHandle.Alloc(b, GCHandleType.Pinned);
            addr = pinnedArray.AddrOfPinnedObject();
            type = (T)Marshal.PtrToStructure(addr, typeof(T));
            pinnedArray.Free();

            return type;
        }

        public static void SkipChunk(IntPtr h)
        {
            byte[] b;
            int size;
            GCHandle pinnedArray;
            IntPtr addr;
            string id;

            b = Mpq.ReadFile(h, 4);

            Array.Reverse(b);
            id = Encoding.Default.GetString(b);

            b = Mpq.ReadFile(h, 4);
            size = BitConverter.ToInt32(b, 0);

            b = Mpq.ReadFile(h, size);
        }

        public static bool HasFile(string file)
        {
            string[] s = file.Split('\\');
            file = s[s.Length - 1];
            return SFileHasFile(handle, file);
        }

        static public int Position(IntPtr file)
        {
            int pos_h;
            return SFileSetFilePointer(file, 0, out pos_h, 1);
        }

        [DllImport("StormLib.dll")]
        static extern bool SFileOpenArchive([MarshalAs(UnmanagedType.LPStr)] string name, int p, uint flags, out IntPtr handle);

        [DllImport("StormLib.dll")]
        static extern bool SFileOpenPatchArchive(IntPtr handle, string name, string prefix, uint flags);

        [DllImport("StormLib.dll")]
        static extern bool SFileCloseArchive(IntPtr handle);

        [DllImport("StormLib.dll")]
        static extern bool SFileOpenFileEx(IntPtr handle, string name, uint searchScope, out IntPtr file);

        [DllImport("StormLib.dll")]
        static extern bool SFileReadFile(IntPtr handle, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, int count, out int read, IntPtr io);

        [DllImport("StormLib.dll")]
        static extern int SFileSetFilePointer(IntPtr handle, int pos, out int pos_h, int move_method);

        [DllImport("StormLib.dll")]
        static extern bool SFileHasFile(IntPtr handle, [MarshalAs(UnmanagedType.LPStr)] string name);
    }
}
