namespace GeometryFriendsAgents
{
    public abstract class OpenList<TState, TEntry>
    {
        public bool IsPreferred;
        public abstract bool IsEmpty { get; }
        public abstract void Insert(EvaluationContext<TState> context, TEntry entry);
        public abstract void OnExpanded(TEntry entry);
        public abstract TEntry RemoveNext();
        public abstract bool IsDeadEnd(EvaluationContext<TState> context);
        public abstract void Clear();
    }
}
