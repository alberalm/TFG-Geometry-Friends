using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GeometryFriendsAgents
{
    public class IntArrayEqualityComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length)
                return false;
            for (int i = 0; i < x.Length; i++)
                if (x[i] != y[i])
                    return false;
            return true;
        }

        public int GetHashCode(int[] x)
        {
            int result = 17;
            for (int i = 0; i < x.Length; i++)
                unchecked
                {
                    result = result * 23 + x[i];
                }
            return result;
        }
    }

    public class TileCoding
    {
        public int Size { get; private set; }
        int overfullCount = 0;
        Dictionary<int[], int> indices;
        public bool IsFull { get { return indices.Count >= Size; } }
        public int Count { get { return indices.Count; } }
        public bool IsReadOnly { get; set; }
        IntArrayEqualityComparer intArrayComparer;

        public TileCoding(int size)
        {
            Size = size;
            intArrayComparer = new IntArrayEqualityComparer();
            indices = new Dictionary<int[], int>(intArrayComparer);
        }

        int getIndex(int[] coords)
        {
            int index;
            if (indices.TryGetValue(coords, out index))
                return index;
            if (IsReadOnly)
                throw new ApplicationException("Tile index not found for specified coords");
            if (indices.Count >= Size)
            {
                if (overfullCount == 0)
                    Debug.WriteLine("Warning: TileCoding dictionary full, starting to allow collisions");
                overfullCount++;
                return intArrayComparer.GetHashCode(coords) % Size;
            }
            index = indices.Count;
            indices[coords] = index;
            return index;
        }
        void addTiles(List<int> outputIndices, int numTilings, double[] x, params int[] ints)
        {
            for (int tiling = 0; tiling < numTilings; tiling++)
            {
                var coords = new int[x.Length + ints.Length + 1];
                int c = 0;
                var tilingX2 = tiling * 2;
                coords[c++] = tiling;
                var b = tiling;
                foreach (var f in x)
                {
                    var q = (int)Math.Floor(f * numTilings);
                    coords[c++] = (q + b) / numTilings;
                    b += tilingX2;
                }
                foreach (var i in ints)
                    coords[c++] = i;
                outputIndices.Add(getIndex(coords));
                Debug.Assert(c == coords.Length);
            }
        }
    }
}
