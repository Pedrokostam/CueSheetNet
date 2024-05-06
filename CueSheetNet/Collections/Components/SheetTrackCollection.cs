using System;
using System.Collections;
using System.Reflection;
using CueSheetNet.Extensions;

namespace CueSheetNet.Collections;

public class SheetTrackCollection : ITrackCollection
{
    readonly SheetFileCollection _files;

    internal SheetTrackCollection(SheetFileCollection cueFiles)
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

    public int IndexOf(string title, IEqualityComparer<string>? comparer)
    {
        comparer ??= StringComparer.Ordinal;
        int index = 0;
        foreach (var file in _files)
        {
            foreach (var track in file.Tracks)
            {
                if (comparer.Equals(track.Title, title))
                {
                    return index;
                }
                index++;
            }
        }
        return -1;
    }

    public int IndexOf(string title, StringComparison stringComparison)
    {
        return IndexOf(title, StringHelper.GetComparer(stringComparison));
    }

    public int IndexOf(CueTrack track)
    {
        int index = 0;
        foreach (var file in _files)
        {
            foreach (CueTrack fileTrack in file.Tracks)
            {
                if (Equals(fileTrack, track))
                {
                    return index;
                }
                index++;
            }
        }
        return -1;
    }

    public override string ToString()
    {
        return $"{Count} tracks";
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    //public override bool Equals(object? obj)
    //{
    //    return base.Equals(obj as SheetTrackCollection);
    //}

    //public bool Equals(SheetTrackCollection? other)
    //{
    //    if (other is null)
    //    {
    //        return false;
    //    }
    //    if (other is null)
    //    {
    //        return false;
    //    }
    //    foreach ((CueTrack?, CueTrack?) zip in this.ZipLongestReference(other))
    //    {
    //        if (!Equals(zip.Item1, zip.Item2))
    //        {
    //            return false;
    //        }
    //    }
    //    return true;
    //}
}