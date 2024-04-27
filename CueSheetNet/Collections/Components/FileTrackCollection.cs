using System.Collections;

namespace CueSheetNet.Collections;


public sealed class FileTrackCollection : IEditableTrackCollection
{
    private readonly CueDataFile _parentFile;
    readonly List<CueTrack> _tracks = [];
    readonly Dictionary<CueTrack,int> _trackPositions= new Dictionary<CueTrack, int>(ReferenceEqualityComparer.Instance);

    internal FileTrackCollection(CueDataFile parentFile)
    {
        _parentFile = parentFile;
    }

    public CueTrack this[int index] => _tracks[index];

    public  int Count => _tracks.Count;

    private void InsertImpl(int index, CueTrack track)
    {
        if (index == Count && _tracks[^1].EacEndIndex is not null)
        {
            throw new InvalidOperationException("Cannot add tracks after a track with EAC-style gap trim.");
        }
        _tracks.Insert(index, track);
        //_trackPositions[track] = index; - UpdatePositionsAndNumber takes care of that
        UpdatePositionsAndNumber(index);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    private void UpdatePositionsAndNumber(int index)
    {
        for (; index < Count; index++)
        {
            CueTrack cueTrack = _tracks[index];
            // CUE sheet starts track numbering at 1
            cueTrack.Number = index + 1;
            _trackPositions[cueTrack] = index;
        }
    }

    public CueTrack Add(TrackType trackType)
    {
        return Insert(Count, trackType);
    }

    public IEnumerator<CueTrack> GetEnumerator() => _tracks.GetEnumerator();

    public int IndexOf(string title, StringComparison stringComparison)
    {
        for (int i = 0; i < Count; i++)
        {
            if (_tracks[i].Title.Equals(title, stringComparison))
            {
                return i;
            }
        }
        return -1;
    }

    public int IndexOf(string title) => IndexOf(title, StringComparison.CurrentCulture);

    public int IndexOf(CueTrack track)
    {
        if (_trackPositions.TryGetValue(track, out var index))
        {
            return index;
        }
        return -1;
    }

    internal int IndexOfNextTrack(CueTrack track)
    {
        var index = IndexOf(track);
        if (index < 0)
        {
            return -1;
        }
        if (index == Count - 1)
        {
            return -1;
        }
        return index + 1;
    }

    internal int IndexOfPreviousTrack(CueTrack track)
    {
        var index= IndexOf(track);
        if (index < 0)
        {
            return -1;
        }

        return index - 1;
    }

    public CueTrack Insert(int index, TrackType trackType)
    {
        var track = new CueTrack(_parentFile, trackType);
        InsertImpl(index, track);
        return track;
    }

    public bool Remove(CueTrack track)
    {
        if (_trackPositions.TryGetValue(track, out int index))
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index) => RemoveAtImpl(index);

    private void RemoveAtImpl(int index)
    {
        _trackPositions.Remove(_tracks[index]);
        _tracks.RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
