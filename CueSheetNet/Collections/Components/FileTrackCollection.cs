using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace CueSheetNet.Collections;


public sealed class FileTrackCollection : IEditableTrackCollection
{
    private readonly CueDataFile _parentFile;
    private SheetFileCollection AllFiles => _parentFile.ParentSheet.Files;

    readonly ReferenceKeyedCollection<CueTrack> _tracks = [];

    internal FileTrackCollection(CueDataFile parentFile)
    {
        _parentFile = parentFile;
    }

    public CueTrack this[int index] => _tracks[index];

    public int Count => _tracks.Count;

    private void InsertImpl(int index, CueTrack track)
    {
        if (index == Count && _tracks.LastOrDefault()?.EacEndIndex is not null)
        {
            throw new InvalidOperationException("Cannot add tracks after a track with EAC-style gap trim.");
        }
        _tracks.Insert(index, track);
        //_trackPositions[track] = index; - UpdatePositionsAndNumber takes care of that
        UpdatePositionsAndNumber(index);
    }

    internal CueTrack? FirstOrDefault()=> Count> 0?  _tracks[0] : null;
    internal CueTrack? LastOrDefault()=> Count> 0?  _tracks[^1] : null;


    /// <summary>
    /// 
    /// </summary>
    /// <param name="index"></param>
    private void UpdatePositionsAndNumber(int index)
    {
        int currentNumber;
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        if (index == 0) // last track of previous file
        {
            currentNumber = (AllFiles.GetPreviousFile(_parentFile)?.Tracks[^1].Number + 1) ?? 1;
        }
        else // previous track
        {
            currentNumber = _tracks[index-1].Number+1;
        }
        // CUE sheet starts track numbering at 1
        for (int i = index; i < Count; i++)
        {
            CueTrack cueTrack = _tracks[i];
            if (cueTrack.Number < currentNumber)
            {
                cueTrack.Number = currentNumber;
            }
            currentNumber = cueTrack.Number+1;
        }
        foreach(var file in AllFiles.GetNextFiles(_parentFile))
        {
            file.Tracks.UpdatePositionsAndNumber(0);
        }
    }

    internal void HandleEacTrack()
    {
        var eacTrack = _tracks[^1];
        // mark second to last track as having eac index
        _tracks[^2].EacEndIndex = eacTrack.Indices.FirstOrDefault()?.Time;
        eacTrack.Indices.RemoveAt(0);
        var nextFile = AllFiles.GetNextFile(_parentFile);
        if(nextFile?.Tracks.Count != 0)
        {
            throw new InvalidOperationException();
        }
        var newTrack = nextFile.Tracks.Add(eacTrack.Type);
        eacTrack.ClonePartial(newTrack);
        Remove(eacTrack);
    }

    internal CueTrack? GetPreviousTrack(CueTrack track) => _tracks.GetPreviousItem(track);
    internal CueTrack? GetNextTrack(CueTrack track) => _tracks.GetNextItem(track);

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
        for (int i = 0; i < Count; i++)
        {
            if (_tracks[i].Equals(track, StringComparison.CurrentCulture))
            {
                return i;
            }
        }
        return -1;
    }

    internal CueTrack? PreviousTrack(CueTrack track)=> _tracks.GetPreviousItem(track);
    internal CueTrack? NextTrack(CueTrack track) => _tracks.GetNextItem(track);
    public CueTrack Insert(int index, TrackType trackType)
    {
        var track = new CueTrack(_parentFile, trackType);
        InsertImpl(index, track);
        return track;
    }

    public bool Remove(CueTrack track)
    {
        var index = _tracks.GetIndexOrNegative(track);
        if (index>=0)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        var removed = _tracks[index];
        _tracks.RemoveAt(index);
        removed.Invalidate();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString()
    {
        return $"{Count} tracks";
    }
}
