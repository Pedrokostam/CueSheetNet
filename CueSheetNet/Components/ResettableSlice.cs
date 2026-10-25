using System.Collections;

namespace CueSheetNet.Components;

internal class ResettableSlice<TSource, TData> : IReadOnlyList<TSource>
    where TData : notnull
    where TSource : notnull
{
    private readonly ResettableCollection<TSource, TData> _source;
    private readonly IEnumerable<TData> _date;
    private readonly Func<TData, bool> _sourcePredicate;
    private readonly Func<TData, TSource> _getterDataSource;

    private int Start { get; set; }
    private int _count;
    public int Count
    {
        get
        {
            Validate();
            return _count;
        }
    }

    private int InvalidationCounter { get; set; }

    public TSource this[int index]
    {
        get
        {
            Validate();
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _count);
            return _source[index + Start];
        }
    }

    public ResettableSlice(IEnumerable<TData> data, ResettableCollection<TSource, TData> source, Func<TData, TSource> getterDataSource, Func<TData, bool> sourcePredicate)
    {
        _date = data;
        _source = source;
        _getterDataSource = getterDataSource;
        _sourcePredicate = sourcePredicate;
    }

    private void Validate()
    {
        if (InvalidationCounter != _source.InvalidationCounter)
        {
            var x = GetBound(_date, _source, _sourcePredicate, _getterDataSource);
            Start = x.Start;
            _count = x.Count;
            InvalidationCounter = _source.InvalidationCounter;
        }
    }

    public IEnumerator<TSource> GetEnumerator()
    {
        Validate();
        for (int i = 0; i < _count; i++)
        {
            yield return _source[i + Start];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    private static (int Start, int Count) GetBound(IEnumerable<TData> data, IEnumerable<TSource> source, Func<TData, bool> sourcePredicate, Func<TData, TSource> getterDataSource)
    {
        // imagine
        // dragons
        // TItem - PTrack
        // TContainer - PFile
        // TSource - Link
        // data => Sheet.Data
        // container => Sheet.Tracks
        // parent => File_2
        //getterContainer => Link=>File

        int start = -1;

        var enumSource = source.GetEnumerator();
        var enumData = data.GetEnumerator();

        do
        {
            if (!enumData.MoveNext())
            {
                throw new KeyNotFoundException();
            }
        } while (!sourcePredicate(enumData.Current));
        // enumData is at the parent element in data    
        // Data at File_2 => We also have the first track

        // move along source (Tracks)
        do
        {
            if (!enumSource.MoveNext())
            {
                throw new KeyNotFoundException();
            }
            start++;
            // until we find the matching track from enumData
        } while (getterDataSource(enumData.Current)!.Equals(enumSource.Current));
        // start is known

        // now find which is the last item that belongs to parent
        TSource lastItem = getterDataSource(enumData.Current);
        while (enumData.MoveNext() && sourcePredicate(enumData.Current))
        {
            lastItem = getterDataSource(enumData.Current);
        }

        int count = 1;
        while (enumSource.MoveNext())
        {
            count++;
            if (!lastItem.Equals(enumSource.Current))
            {
                break;
            }
        }

        return (start, count);
    }
}