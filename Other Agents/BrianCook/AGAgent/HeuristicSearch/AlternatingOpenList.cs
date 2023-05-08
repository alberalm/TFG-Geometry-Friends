using System.Collections.Generic;

namespace GeometryFriendsAgents
{
    public class AlternatingOpenList<TState, TEntry> : OpenList<TState, TEntry>
    {
        List<OpenList<TState, TEntry>> sublists;
        int nextSublist;

        public override bool IsEmpty
        {
            get
            {
                foreach (var sublist in sublists)
                    if (!sublist.IsEmpty)
                        return false;
                return true;
            }
        }

        public AlternatingOpenList(List<OpenList<TState, TEntry>> sublists)
        {
            this.sublists = sublists;
            nextSublist = -1; 
        }

        public override void Insert(EvaluationContext<TState> context, TEntry entry)
        {
            foreach (var sublist in sublists)
                sublist.Insert(context, entry);
        }

        public override TEntry RemoveNext()
        {
            nextSublist = (nextSublist + 1) % sublists.Count;
            int startingIndex = nextSublist;
            while (sublists[nextSublist].IsEmpty)
            {
                nextSublist = (nextSublist + 1) % sublists.Count;
                if (startingIndex == nextSublist)
                    break;
            }
            return sublists[nextSublist].RemoveNext();
        }

        public override bool IsDeadEnd(EvaluationContext<TState> context)
        {
            foreach (var sublist in sublists)
                if (sublist.IsDeadEnd(context))
                    return true;
            return false;
        }

        public override void Clear()
        {
            foreach (var sublist in sublists)
                sublist.Clear();
        }

        public override void OnExpanded(TEntry entry)
        {
            foreach (var sublist in sublists)
                sublist.OnExpanded(entry);
        }
    }
}
