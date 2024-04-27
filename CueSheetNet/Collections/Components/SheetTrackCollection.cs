using System;
using System.Collections;

namespace CueSheetNet.Collections;


public class SheetIndexCollection : IIndexCollection
{
    readonly CueTrackCollection _tracks;
    public CueIndex this[int index]
    {
        get
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            int start=0;
            int end=0;
            for (int i = 0; i < _tracks.Count; i++)
            {
                end += _tracks[i].Indices.Count;
                if (index < end)
                {
                    return _tracks[i].Indices[index - start];
                }
                start = end;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public int Count => _tracks.Sum(x=>x.Indices.Count);


    internal SheetIndexCollection(CueTrackCollection cueTracks)
    {
        _tracks = cueTracks;
    }
    public IEnumerator<CueIndex> GetEnumerator()
    {
        foreach (var track in _tracks)
        {
            foreach (var index in track.Indices)
            {
                yield return index;
            }
        }
    }

    public int IndexOf(CueTime time)
    {
        int index = 0;
        foreach (var track in _tracks)
        {
            foreach (var cueIndex in track.Indices)
            {
                if (cueIndex.Time == time)
                {
                    return index;
                }
                index++;
            }
        }
        return -1;
    }

    public int IndexOf(int number)
    {
        int index = 0;
        foreach (var track in _tracks)
        {
            foreach (var cueIndex in track.Indices)
            {
                if (cueIndex.Number == number)
                {
                    return index;
                }
                index++;
            }
        }
        return -1;
    }

    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}

public class SheetTrackCollection : ITrackCollection
{
    readonly CueFileCollection _files;

    internal SheetTrackCollection(CueFileCollection cueFiles)
    {
        _files = cueFiles;
    }

    public CueTrack this[int index]
    {
        get
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
            int start=0;
            int end=0;
            for (int i = 0; i < _files.Count; i++)
            {
                end += _files[i].Tracks.Count;
                if (index < end)
                {
                    return _files[i].Tracks[index - start];
                }
                start = end;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public int Count => _files.Sum(x => x.Tracks.Count);

    public IEnumerator<CueTrack> GetEnumerator()
    {
        foreach (var file in _files)
        {
            foreach (var track in file.Tracks)
            {
                yield return track;
            }
        }
    }

    public int IndexOf(string title, StringComparison stringComparison)
    {
        int index = 0;
        foreach (var file in _files)
        {
            foreach (var track in file.Tracks)
            {
                if (track.Title.Equals(title, stringComparison))
                {
                    return index;
                }
                index++;
            }
        }
        return -1;
    }

    public int IndexOf(CueTrack track)
    {
        int index = 0;
        foreach (var file in _files)
        {
            foreach (CueTrack fileTrack in file.Tracks)
            {
                if (fileTrack.Equals(track,StringComparison.Ordinal))
                {
                    return index;
                }
                index++;
            }
        }
        return -1;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}