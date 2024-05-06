using System.Collections;

namespace CueSheetNet.Collections;

public class SheetIndexCollection : IIndexCollection
{
    readonly SheetTrackCollection _tracks;
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


    internal SheetIndexCollection(SheetTrackCollection cueTracks)
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

    //public override bool Equals(object? obj)
    //{
    //    return base.Equals(obj as SheetIndexCollection);
    //}

    //public bool Equals(SheetIndexCollection? other)
    //{
    //    if (other is null)
    //    {
    //        return false;
    //    }
    //    foreach ((CueIndex?, CueIndex?) zip in this.ZipLongestValue(other))
    //    {
    //        if(!Equals(zip.Item1,zip.Item2))
    //        {
    //            return false;
    //        }
    //    }
    //    return true;
    //}
}
