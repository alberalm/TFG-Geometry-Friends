namespace GeometryFriendsAgents
{
    public struct EvaluationResult
    {
        public float Value { get; private set; }
        public bool IsInfinite { get { return Value == Infinity; } }

        public EvaluationResult(float value)
        {
            Value = value;
        }

        public void Add(float value)
        {
            if (IsInfinite)
                return;
            Value += value;
        }

        public void Multiply(float value)
        {
            if (IsInfinite)
                return;
            Value *= value;
        }

        public static float Infinity = float.PositiveInfinity;
    }
}
