using System.Collections;
using System.Collections.ObjectModel;
#pragma warning disable MA0008 // Add StructLayoutAttribute

namespace CueSheetNet.Components;

internal class ResettableCollection<T, R> : IReadOnlyList<T>
{
    private Func<IEnumerable<R>, IEnumerable<T>> Populator { get; }
    private IEnumerable<R> Source { get; }
    public int InvalidationCounter { get; private set; }

    private readonly List<T> _data = [];
    private ReadOnlyCollection<T> Data
    {
        get
        {
            if (_data.Count == 0 && Source.Any())
            {
                _data.AddRange(Populator(Source));
            }
            return _data.AsReadOnly();
        }
    }
    public void Invalidate()
    {
        _data.Clear();
        InvalidationCounter++;
    }
    public int Count => ((ICollection<T>)Data).Count;

    public bool IsReadOnly { get; } = true;

    public T this[int index]
    {
        get => ((IList<T>)Data)[index];
        set => ((IList<T>)Data)[index] = value;
    }

    public ResettableCollection(Func<IEnumerable<R>, IEnumerable<T>> factory, IEnumerable<R> source)
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

#pragma warning restore MA0008 // Add StructLayoutAttribute