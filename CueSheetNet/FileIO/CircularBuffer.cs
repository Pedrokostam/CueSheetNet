namespace CueSheetNet.FileIO;

internal class CircularBuffer<T>
where T : notnull
{
    public int Current { get; private set; }
    public int PushCounter { get; private set; }
    public int Capacity { get; }
    private readonly T[] Buffer;
    private int LastOrderedCounter = -1;
    private readonly T[] LastOrdered;
    public CircularBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException("Circular buffer has to have positive capacity");
        Capacity = capacity;
        Buffer = new T[capacity];
        LastOrdered = new T[capacity];
    }
    public int Push(T item)
    {
        Buffer[Current] = item;
        Current = (Current + 1) % Capacity;
        PushCounter++;
        return Current;
    }
    public T[] GetOrdered()
    {
        if (LastOrderedCounter == PushCounter) return LastOrdered;
        for (int i = 1; i <= Capacity; i++)
        {
            int orderedIndex;
            if (Current - i < 0)
            {
                orderedIndex = Capacity + Current - i;
            }
            else
            {
                orderedIndex = Current - i;
            }
            LastOrdered[Capacity - i] = Buffer[orderedIndex];
        }
        LastOrderedCounter = PushCounter;
        return LastOrdered;
    }
    public bool MatchSequence(IList<T> sequence)
    {
        if (LastOrderedCounter != PushCounter)
            GetOrdered();
        if (sequence.Count > Capacity) throw new ArgumentOutOfRangeException("Sequence to compare is longer than buffer");
        for (int i = sequence.Count; i >= 0; i--)
        {
            if (!LastOrdered[i].Equals(sequence[i]))
                return false;
        }
        return true;
    }
    public bool MatchAnySequence(IList<T> sequence1, IList<T> sequence2)
    {
        GetOrdered();
        if (sequence1.Count > sequence2.Count) throw new ArgumentException("Sequences need to have the same length");
        if (sequence1.Count > Capacity) throw new ArgumentOutOfRangeException("Sequence to compare is longer than buffer");
        for (int i = 1; i <= sequence1.Count; i++)
        {
            T s1 = sequence1[^i];
            T s2 = sequence2[^i];
            T b = LastOrdered[^i];
            if (!(b.Equals(s1) || b.Equals(s2)))
                return false;
        }
        return true;
    }
    public T GetFromEnd(int fromEnd)
    {
        fromEnd++;
        int orderedIndex;
        if (Current - fromEnd < 0)
        {
            orderedIndex = Capacity + Current - fromEnd;
        }
        else
        {
            orderedIndex = Current - fromEnd;
        }
        return Buffer[orderedIndex];
    }
}
