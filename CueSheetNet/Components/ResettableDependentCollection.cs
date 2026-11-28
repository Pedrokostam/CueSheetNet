using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;


namespace CueSheetNet.Components;

[DebuggerDisplay("Count = {Count}")]
internal sealed class ResettableCollection<T> : IReadOnlyList<T>
{
    static ILogger Loggerio => Loggingq.EditingLogger;
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
        InvalidationCounter++;
    }
    public bool IsEmptyOrUninitialized => _data.Count == 0;
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
[DebuggerDisplay("Count = {Count}")]
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
    /// <summary>
    /// Returns the number of items in the collection WITHOUT checking for invalidation
    /// </summary>
    public int LastCount => _data.Count;

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
