using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using CueSheetNet.Collections;

namespace CueSheetNet.Collections;

public class TrackIndexCollection : IEditableIndexCollection
{
    private class IndexItem(int absoluteIndex, int number, CueTime time)
    {
        public int AbsoluteIndex { get; set; } = absoluteIndex;
        public int Number { get; set; } = number;
        public CueTime Time { get; set; } = time;
        public CueIndex ToIndex(CueTrack parentTrack)
        {
            return new CueIndex(Number, AbsoluteIndex,  Time);
        }
    }
    private readonly ObservableCollection<IndexItem> _indexes=[];

    internal TrackIndexCollection(CueTrack parentTrack)
    {
        ParentTrack = parentTrack;
    }

    internal CueIndex? FirstOrDefault() => Count > 0 ? this[0] : null;
    internal CueIndex? LastOrDefault() => Count > 0 ? this[^1] : null;

    public CueIndex this[int index]
    {
        get => _indexes[index].ToIndex(ParentTrack);
    }

    CueTrack ParentTrack { get; }
    public int Count => _indexes.Count;

    /// <inheritdoc cref="IIndexValidator.ValidateIndex(int, CueTime, int, bool)"/>
    protected void ValidateIndex(int index, CueTime time, int number, bool replacesItem)
    {

        if (!ParentTrack.ValidateIndex(index, time, number, replacesItem))
        {
            throw new InvalidOperationException($"{number:d2} {time} cannot be inserted at {index}");
        }
    }

    public void Add(CueTime time, int? number = null) => Insert(Count, time, number);

    public IEnumerator<CueIndex> GetEnumerator()
    {
        foreach (var item in _indexes)
        {
            yield return item.ToIndex(ParentTrack);
        }
    }

    public void Insert(int index, CueTime time, int? number = null)
    {
        int prevNumber = index==0 ? 0 : _indexes[index-1].Number;
        int nextNumber = index==Count ? int.MaxValue : _indexes[index].Number;
        int num = number switch
        {
            int i => i.Clamp(prevNumber + 1, nextNumber - 1),
            null => prevNumber+1
        };
        ValidateIndex(index, time, num, replacesItem: false);
        _indexes.Insert(index, new(index, num, time));
        UpdateAbsoluteIndex(++index);
    }

    /// <summary>
    /// Takes the index of the last inserted value and updates indices of following items.
    /// </summary>
    /// <param name="index"></param>
    private void UpdateAbsoluteIndex(int index)
    {
        for (; index < Count; index++)
        {
            _indexes[index].AbsoluteIndex = index;
        }
    }

    public void RemoveAt(int index)
    {
        _indexes.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void ChangeIndex(int index, CueTime? newTime, int? newNumber)
    {
        int number=newNumber ?? _indexes[index].Number;
        CueTime time = newTime ?? _indexes[index].Time;
        if (number < 1)
        {
            bool hasNonZerothIndex = _indexes.Any(x=>x.Number>0);
            if (!hasNonZerothIndex)
            {
                throw new InvalidOperationException("Cannot add a zeroth index without any other indices in the track.");
            }
        }
        ValidateIndex(index, time, number, replacesItem: true);

        _indexes[index].Number = number;
        _indexes[index].Time = time;
    }

    public bool Remove(int number)
    {
        var index = IndexOf(number);
        if (index < 0)
        {
            return false;
        }
        RemoveAt(index);
        return index >= 0;
    }

    public bool Remove(CueTime time)
    {
        var index = IndexOf(time);
        if (index < 0)
        {
            return false;
        }
        RemoveAt(index);
        return index >= 0;
    }

    public int IndexOf(CueTime time)
    {
        int index=0;
        foreach (var item in _indexes)
        {
            if (item.Time == time)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    public int IndexOf(int number)
    {
        int index=0;
        foreach (var item in _indexes)
        {
            if (item.Number == number)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    public CueIndex GetAudioStartIndex()
    {
        return Count switch
        {
            0 => throw new InvalidOperationException("Attempted to get audio start index from a track with no indices."),
            1 => _indexes[0].ToIndex(ParentTrack),
            _ => _indexes.First(x => x.Number > 0).ToIndex(ParentTrack),
        };
    }
    public override string ToString()
    {
        return $"{Count} indices";
    }

    //public bool SequenceEqual(TrackIndexCollection? other)
    //{
    //    if(other is null)
    //    {
    //        return false;
    //    }
    //    return _indexes.SequenceEqual(other._indexes);
    //}

}
