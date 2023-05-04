using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GeometryFriendsAgents
{
    public static class Helpers
    {
        public static bool EnableDebugLogging = false;

        public static double Distance(PointF a, PointF b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        public static float SquaredDistance(PointF a, PointF b)
        {
            return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
        }
        public static float Distance(float[] a, float[] b)
        {
            float total = 0;
            for (int i = 0; i < a.Length; i++)
                total += (a[i] - b[i]) * (a[i] - b[i]);
            return (float)Math.Sqrt(total);
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector)
        {
            return source.MinBy(selector, null);
        }
        public static PointF Normalize(PointF p)
        {
            var length = (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
            if (length > 0)
                return new PointF(p.X / length, p.Y / length);
            return p;
        }

        public static float Magnitude(PointF p)
        {
            return (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }

        public static float DotProduct(PointF a, PointF b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            comparer = comparer ?? Comparer<TKey>.Default;

            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                var min = sourceIterator.Current;
                var minKey = selector(min);
                while (sourceIterator.MoveNext())
                {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, minKey) < 0)
                    {
                        min = candidate;
                        minKey = candidateProjected;
                    }
                }
                return min;
            }
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
                    Func<TSource, TKey> selector)
        {
            return source.MaxBy(selector, null);
        }

        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source,
            Func<TSource, TKey> selector, IComparer<TKey> comparer)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (selector == null) throw new ArgumentNullException("selector");
            comparer = comparer ?? Comparer<TKey>.Default;

            using (var sourceIterator = source.GetEnumerator())
            {
                if (!sourceIterator.MoveNext())
                {
                    throw new InvalidOperationException("Sequence contains no elements");
                }
                var max = sourceIterator.Current;
                var maxKey = selector(max);
                while (sourceIterator.MoveNext())
                {
                    var candidate = sourceIterator.Current;
                    var candidateProjected = selector(candidate);
                    if (comparer.Compare(candidateProjected, maxKey) > 0)
                    {
                        max = candidate;
                        maxKey = candidateProjected;
                    }
                }
                return max;
            }
        }

        public static PointF Add(PointF a, PointF b)
        {
            return new PointF(a.X + b.X, a.Y + b.Y);
        }

        public static PointF Subtract(PointF a, PointF b)
        {
            return new PointF(a.X - b.X, a.Y - b.Y);
        }

        public static PointF Times(PointF a, float factor)
        {
            return new PointF(a.X * factor, a.Y * factor);
        }

        public static PointF Divide(PointF a, float divisor)
        {
            return new PointF(a.X / divisor, a.Y / divisor);
        }
        public static void FastRemoveAt<T>(List<T> list, int n)
        {
            list[n] = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
        }

        public static bool GetCommandLineArg(string argname)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (string.Compare(args[i], argname, true) == 0)
                    return true;
            return false;
        }

        public static int GetCommandLineArg(string argname, int defaultValue)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
                if (string.Compare(args[i], argname, true) == 0 && i < args.Length - 1)
                    return int.Parse(args[i + 1]);
            return defaultValue;
        }

        public static bool IsControlKeyDown()
        {
            return (GetKeyState(VK_CONTROL) & KEY_PRESSED) != 0;
        }

        public static bool IsLeftKeyDown()
        {
            return (GetKeyState('A') & KEY_PRESSED) != 0;
        }

        public static bool IsRightKeyDown()
        {
            return (GetKeyState('D') & KEY_PRESSED) != 0;
        }

        public static bool IsJumpKeyDown()
        {
            return (GetKeyState('W') & KEY_PRESSED) != 0;
        }

        private const int KEY_PRESSED = 0x8000;
        private const int VK_CONTROL = 0x11;
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern short GetKeyState(int key);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern int GetCurrentThreadId();

        static Random random = new Random();
        public static T SelectWeightedRandom<T>(List<T> list, Func<T, double> getWeight)
        {
            var totalWeight = 0.0;
            foreach (var element in list)
                totalWeight += getWeight(element);
            var r = random.NextDouble() * totalWeight;
            var p = 0.0;
            foreach (var element in list)
            {
                p += getWeight(element);
                if (r <= p)
                    return element;
            }
            if (list.Count > 0)
                return list[0];
            return default(T);
        }

        public static Color AverageColor(float weightA, Color colorA, float weightB, Color colorB)
        {
            var totalWeight = weightA + weightB;
            var r = (colorA.R * weightA + colorB.R * weightB) / totalWeight + 0.5f;
            var g = (colorA.G * weightA + colorB.G * weightB) / totalWeight + 0.5f;
            var b = (colorA.B * weightA + colorB.B * weightB) / totalWeight + 0.5f;
            return Color.FromArgb((int)r, (int)g, (int)b);
        }
        static public bool IsSubclass(Type a, Type b)
        {
            return b.IsAssignableFrom(a);
        }
        static public bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                    return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        static public Type FindType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                    if (type.FullName == typeName)
                        return type;
            return null;
        }

        static public List<Type> GetSubtypes<T>()
        {
            var subTypes = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                {
                    if ((IsSubclassOfRawGeneric(typeof(T), type) || IsSubclass(type, typeof(T))) && !type.IsAbstract)
                        subTypes.Add(type);
                }
            subTypes.Sort((a, b) => { return string.Compare(a.Name, b.Name); });

            return subTypes;
        }
        static public List<int> ParseIntList(string text)
        {
            var list = new List<int>();

            var ranges = text.Split(',');
            if (ranges.Length == 0 || ranges[0].Length == 0)
                return list;
            foreach (var range in ranges)
            {
                int fromValue, toValue, step;
                if (range.Contains('-'))
                {
                    var inner = range.Split('-');
                    fromValue = int.Parse(inner[0]);
                    toValue = int.Parse(inner[1]);
                    step = 1;
                }
                else if (range.Contains(':'))
                {
                    var inner = range.Split(':');
                    fromValue = int.Parse(inner[0]);
                    toValue = int.Parse(inner[2]);
                    step = int.Parse(inner[1]);
                }
                else
                {
                    fromValue = int.Parse(range);
                    toValue = fromValue;
                    step = 1;
                }
                for (var val = fromValue; val <= toValue; val += step)
                    list.Add(val);
            }

            return list;
        }

        static public List<double> ParseDoubleList(string text)
        {
            var list = new List<double>();

            var ranges = text.Split(',');
            if (ranges.Length == 0 || ranges[0].Length == 0)
                return list;
            foreach (var range in ranges)
            {
                double fromValue, toValue, step;
                if (range.Contains('-'))
                {
                    var inner = range.Split('-');
                    fromValue = double.Parse(inner[0]);
                    toValue = double.Parse(inner[1]);
                    step = 1;
                }
                else if (range.Contains(':'))
                {
                    var inner = range.Split(':');
                    fromValue = double.Parse(inner[0]);
                    toValue = double.Parse(inner[2]);
                    step = double.Parse(inner[1]);
                }
                else
                {
                    fromValue = double.Parse(range);
                    toValue = fromValue;
                    step = 1;
                }
                for (var val = fromValue; val <= toValue; val += step)
                    list.Add(val);
            }

            return list;
        }

        public static double Median<T>(
            this IEnumerable<T> source)
        {
            if (Nullable.GetUnderlyingType(typeof(T)) != null)
                source = source.Where(x => x != null);

            int count = source.Count();
            if (count == 0)
                return 0;

            source = source.OrderBy(n => n);

            int midpoint = count / 2;
            if (count % 2 == 0)
                return (Convert.ToDouble(source.ElementAt(midpoint - 1)) + Convert.ToDouble(source.ElementAt(midpoint))) / 2.0;
            else
                return Convert.ToDouble(source.ElementAt(midpoint));
        }

        private static Random _random = new Random();
        public static T RandomElement<T>(this IEnumerable<T> enumerable)
        {
            return enumerable.ElementAt(_random.Next(enumerable.Count()));
        }

        public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> enumerable, int numberOfElements)
        {
            var shuffled = enumerable.Shuffle();
            return shuffled.Take(numberOfElements);
        }
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            var list = enumerable.ToList();

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }

        public static string ToOutputString<T>(this IEnumerable<T> enumerable)
        {
            return "{" + string.Join(",", enumerable.Select(x => x.ToString()).ToArray()) + "}";
        }

        public static IEnumerable<T> ConcatItems<T>(this IEnumerable<T> target, params T[] items) => target.Concat(items);
    }

    public class FunctionalComparer<T> : IComparer<T>
    {
        private Func<T, T, int> comparer;
        public FunctionalComparer(Func<T, T, int> comparer)
        {
            this.comparer = comparer;
        }
        public static IComparer<T> Create(Func<T, T, int> comparer)
        {
            return new FunctionalComparer<T>(comparer);
        }
        public int Compare(T x, T y)
        {
            return comparer(x, y);
        }
    }

    public struct Vector3f
    {
        public float x;
        public float y;
        public float z;

        public Vector3f(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3f CrossProduct(Vector3f a, Vector3f b)
        {
            return new Vector3f(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        }
    }
}
