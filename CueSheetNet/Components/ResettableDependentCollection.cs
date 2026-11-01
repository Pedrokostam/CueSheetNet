using System.Collections;
using System.Collections.ObjectModel;


namespace CueSheetNet.Components;

internal class ResettableCollection<T> : IReadOnlyList<T>
{
    private Func<IEnumerable<T>> Populator { get; }
    public int InvalidationCounter { get; private set; }
    public int LastValidatedAt { get; private set; } = -1;
    private readonly List<T> _data = [];
    private ReadOnlyCollection<T> Data
    {
        get
        {
            if (LastValidatedAt != InvalidationCounter)
            {
                _data.Clear();
                _data.AddRange(Populator());
                LastValidatedAt = InvalidationCounter;
            }
            return _data.AsReadOnly();
        }
    }
    public void Invalidate()
    {
        _data.Clear();
        InvalidationCounter++;
    }
    public void Revalidate()
    {
        InvalidationCounter++;
        _data.Clear();
        _data.AddRange(Populator());
        LastValidatedAt = InvalidationCounter;
    }
    public int Count => ((ICollection<T>)Data).Count;

    public bool IsReadOnly { get; } = true;

    public T this[int index]
    {
        get => ((IList<T>)Data)[index];
        set => ((IList<T>)Data)[index] = value;
    }

    public ResettableCollection(Func<IEnumerable<T>> factory)
    {
        Populator = factory;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)Data).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Data).GetEnumerator();
    }

    public int IndexOf(T item) => Data.IndexOf(item);
    public int FindIndex(Predicate<T> predicate)
    {
        var c = Data.Count;
        for (int i = 0; i < c; i++)
        {
            if (predicate(_data[i]))
            {
                return i;
            }
        }
        return -1;
    }
}
internal class ResettableDependentCollection<T, R> : IReadOnlyList<T>
{
    private Func<IEnumerable<R>, IEnumerable<T>> Populator { get; }
    private IEnumerable<R> Source { get; }
    public int InvalidationCounter { get; private set; }
    public int LastValidatedAt { get; private set; } = -1;

    private readonly List<T> _data = [];
    private ReadOnlyCollection<T> Data
    {
        get
        {
            if (LastValidatedAt != InvalidationCounter)
            {
                _data.Clear();
                _data.AddRange(Populator(Source));
                LastValidatedAt = InvalidationCounter;
            }
            return _data.AsReadOnly();
        }
    }
    public void Invalidate()
    {
        InvalidationCounter++;
    }
    public int Count => ((ICollection<T>)Data).Count;

    public bool IsReadOnly { get; } = true;

    public T this[int index]
    {
        get => ((IList<T>)Data)[index];
        set => ((IList<T>)Data)[index] = value;
    }

    public ResettableDependentCollection(Func<IEnumerable<R>, IEnumerable<T>> factory, IEnumerable<R> source)
    {
        Populator = factory;
        Source = source;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)Data).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)Data).GetEnumerator();
    }

    public int IndexOf(T item) => Data.IndexOf(item);
    public int FindIndex(Predicate<T> predicate)
    {
        var c = Data.Count;
        for (int i = 0; i < c; i++)
        {
            if (predicate(_data[i]))
            {
                return i;
            }
        }
        return -1;
    }
}
