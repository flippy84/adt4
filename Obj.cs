using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;

namespace adt4
{
    class Obj
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        int last = 0;

        public void Add(Vector3[] v, int[] i)
        {
            vertices.AddRange(v);
            foreach (int j in i)
            {
                indices.Add(j + last);
            }
            last += v.Length;
        }

        public void Save(string filename)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            StreamWriter file = File.CreateText(filename);

            foreach (Vector3 v in vertices)
                file.WriteLine("v {0} {1} {2}", v.X.ToString(nfi), v.Y.ToString(nfi), v.Z.ToString(nfi));

            for (int i = 0; i < indices.Count; i += 3)
            {
                file.WriteLine("f {0} {1} {2}", indices[i], indices[i + 1], indices[i + 2]);
            }

            file.Close();
        }
    }
}
