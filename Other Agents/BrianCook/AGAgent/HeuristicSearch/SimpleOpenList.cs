using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GeometryFriendsAgents
{
    public class SimpleOpenList<TState, TEntry> : OpenList<TState, TEntry>
    {
        public float bucketMultiplier = 1000000;

        class Bucket : List<TEntry> { } 
        SortedDictionary<int, Bucket> buckets = new SortedDictionary<int, Bucket>();
        ScalarEvaluator<TState> evaluator;
        public override bool IsEmpty { get { return buckets.Count == 0; } }

        public SimpleOpenList(ScalarEvaluator<TState> evaluator)
        {
            this.evaluator = evaluator;
        }

        public override void Insert(EvaluationContext<TState> context, TEntry entry)
        {
            var value = context.Get(evaluator).Value;
            var bucketKey = (int)(value * bucketMultiplier);
            Bucket bucket;
            if (!buckets.TryGetValue(bucketKey, out bucket))
            {
                bucket = new Bucket();
                buckets[bucketKey] = bucket;
            }
            bucket.Add(entry);
        }

        public override TEntry RemoveNext()
        {
            Debug.Assert(buckets.Count > 0);
            var entry = buckets.First();
            var bucket = entry.Value;
            Debug.Assert(bucket.Count > 0);
            var next = bucket[0];
            bucket.RemoveAt(0);
            if (bucket.Count == 0)
                buckets.Remove(entry.Key);
            return next;
        }

        public override bool IsDeadEnd(EvaluationContext<TState> context)
        {
            var value = context.Get(evaluator);
            return value.IsInfinite;
        }

        public override void Clear()
        {
            buckets.Clear();
        }

        public override void OnExpanded(TEntry entry)
        {
        }
    }
}
