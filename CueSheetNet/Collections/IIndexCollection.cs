using System.Collections;

namespace CueSheetNet.Collections;

public interface IIndexCollection : IEnumerable<CueIndex>
{
    public int Count { get; }
    public CueIndex this[int index] { get; }

    /// <summary>
    /// Changes the time of the CueIndex at the specified absolute <paramref name="index"/>.
    /// </summary>
    /// <param name="index">Absolute index of item (for the whole container) to be updated. This is not the <see cref="CueIndex.Number">Number</see> of the CueIndex.</param>
    /// <param name="newTime">New time of the index. Set it to <see langword="null"/> to leave it unchanged.</param>
    /// <param name="newNumber">New number of the index. Set it to <see langword="null"/> to leave it unchanged. Number cannot be zero.
    /// <para>
    /// All indexes following this will have their number increased if necessary.
    /// </para></param>
    public void ChangeIndex(int index, CueTime? newTime, int? newNumber);
    /// <summary>
    /// Creates and inserts a new <see cref="CueIndex"/> after every other element in the container.
    /// </summary>
    /// <inheritdoc cref="Insert(int, CueTime, int?)"/>
    public void Add(CueTime time, int? number = null);
    /// <summary>
    /// Creates and inserts a new <see cref="CueIndex"/> at the specified position.
    /// </summary>
    /// <param name="index">The position at which a new item should be inserted.</param>
    /// <param name="time">Time of the new item. Must be greater than the preceding element's, and smaller than the following's.</param>
    /// <param name="number">Number of the new item. If set to <see langword="null"/>, the next available value will be used.</param>
    public void Insert(int index, CueTime time, int? number = null);
    /// <summary>
    /// Removes item with the specified <paramref name="number"/>.
    /// </summary>
    /// <param name="number"></param>
    /// <returns><see langword="true"/> if item was deleted; otherwise, <see langword="false"/></returns>
    public bool Remove(int number);
    /// <summary>
    /// Removes item with the specified <paramref name="time"/>.
    /// </summary>
    /// <param name="number"></param>
    /// <returns><see langword="true"/> if item was deleted; otherwise, <see langword="false"/></returns>
    public bool Remove(CueTime time);
    /// <summary>
    /// Removes item with at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="number"></param>
    public void RemoveAt(int index);
    public int IndexOf(CueTime time);
    public int IndexOf(int number);
}

public interface IIndexValidator
{
    /// <summary>
    /// Checks whether <paramref name="value"/> can be inserted at the given index.
    /// Requirements:
    /// <list type="table">
    ///     <item>
    ///         <term><see cref="CueIndex.Time">Time</see></term>
    ///         <description>later than preceding index, earlier than following.</description>
    ///     </item>
    ///     <item>
    ///         <term><see cref="CueIndex.Number">Number</see></term>
    ///         <description>greater than preceding index, smaller than following. Only checks in the file of the parent track.</description>
    ///     </item>
    /// </list>
    /// <para>
    /// The preceding values is at <c>(<paramref name="index"/>-1)</c><br/>
    /// The following value if at <c>(<paramref name="index"/>)</c> or <c>(<paramref name="index"/>+1)</c>, depending on <paramref name="replacesItem"/>.
    /// </para>
    /// </summary>
    /// <param name="replacesItem">Set it to <see langword="true"/> if the validation should not test against the item at the <paramref name="index"/>.
    /// <para/>
    /// Set it to <see langword="false"/> if that value should be treated as if it was a following value.
    /// </param>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <exception cref="NotImplementedException"></exception>
    public bool ValidateIndex(int index, CueTime time, int number, bool replacesItem);
}

public class TrackEx : IIndexValidator
{
    public bool ValidateIndex(int index, CueTime time, int number, bool replacesItem)
    {
        throw new NotImplementedException();
    }
}

public class TrackIndexCollection : IIndexCollection
{
    private List<CueIndex> _indexes=[];
    public CueIndex this[int index]
    {
        get => _indexes[index];
    }

    TrackEx ParentTrack { get; }

    public int Count { get; }

    /// <inheritdoc cref="IIndexValidator.ValidateIndex(int, CueTime, int, bool)"/>
    protected void ValidateIndex(int index, CueTime time, int number, bool replacesItem)
    {

        if (!ParentTrack.ValidateIndex(index, time, number, replacesItem))
        {
            throw new InvalidOperationException($"{number:d2} {time} cannot be inserted at {index}");
        }
    }

    public void Add(CueIndex cueIndex) => Insert(Count - 1, cueIndex);

    public void ChangeIndex(int index, CueIndex newValue)
    {
        ValidateIndex(index, number: newValue, replacesItem: true);
        _indexes[index] = newValue;
    }

    public void ChangeIndex(CueIndex oldValue, CueIndex newValue)
    {
        var index = _indexes.IndexOf(oldValue);
        if (index < 0)
        {
            throw new ArgumentException($"{oldValue} could not be found in the collection.");
        }
        ChangeIndex(index, newValue);
    }

    public IEnumerator<CueIndex> GetEnumerator() => _indexes.GetEnumerator();

    public void Insert(int index, CueIndex cueIndex)
    {
        ValidateIndex(Count - 1, number: cueIndex, replacesItem: false);
        _indexes.Insert(index, cueIndex);
    }

    public void Remove(CueIndex cueIndex)
    {
        if (Count < 1)
        {
            throw new InvalidOperationException("Track cannot have no indices");
        }

        _indexes.Remove(cueIndex);
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
            throw new InvalidOperationException("Cannot add zeroth index manually.");
        }
        ValidateIndex(index, time, number, replacesItem: true);
        _indexes[index] = _indexes[index] with { Number = number, Time = time };
    }

    public void Add(CueTime time, int? number = null)
    {
        throw new NotImplementedException();
    }

    public void Insert(int index, CueTime time, int? number = null)
    {
        throw new NotImplementedException();
    }

    public bool Remove(int number)
    {
        throw new NotImplementedException();
    }

    public bool Remove(CueTime time)
    {
        throw new NotImplementedException();
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
}