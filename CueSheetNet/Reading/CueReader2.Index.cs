using System.Diagnostics;
using System.Globalization;
using CueSheetNet.Collections;

namespace CueSheetNet;

public partial class CueReader2
{
    //private class Index : IChainLink<Index>
    //{
    //    private Index? next;
    //    private Index? previous;

    //    public Index? Previous
    //    {
    //        get => previous; set
    //        {
    //            previous = value;
    //            if (previous is not null)
    //            {

    //                Debug.Assert(previous != next);
    //                Debug.Assert(previous != this);
    //            }
    //        }
    //    }
    //    public Index? Next
    //    {
    //        get => next; set
    //        {
    //            next = value;
    //            if (next is not null)
    //            {
    //                Debug.Assert(previous != next);
    //                Debug.Assert(next != this);
    //            }
    //        }
    //    }
    //    public int Number { get; }
    //    public CueTime Time { get; set; }

    //    public Track ParentTrack { get; private set; }

    //    /// <summary>
    //    /// Creates index and adds it as the last index of <paramref name="parent"/>
    //    /// </summary>
    //    /// <param name="number"></param>
    //    /// <param name="parent"></param>
    //    public Index(int number, CueTime time, Track parent)
    //    {
    //        Number = number;
    //        Time = time;
    //        ParentTrack = parent;
    //        ParentTrack.Indexes.Add(this);
    //    }
    //    public override string ToString()
    //    {
    //        return $"{Number}-{Time}-{ParentTrack.Number}";
    //    }
    //    public IEnumerable<Index> FollowSince()
    //    {
    //        yield return this;
    //        var i = this.Next;
    //        while (i is not null)
    //        {
    //            yield return i;
    //            i = i.Next;
    //        }
    //    }

    //    public void GetPromoted()
    //    {
    //        ParentTrack = Next.ParentTrack;
    //    }

    //}

    /// <summary>
    /// Parses index and adds it to the track.
    /// </summary>
    /// <param name="line"></param>
    /// <param name="lastTrack"></param>
    /// <exception cref="FormatException"></exception>
    private void ParseIndex(Line line, CueTrack lastTrack)
    {

        string number = GetKeyword(line.Text, 6); // INDEX_
        if (!int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out int num))
        {
            //Logger.LogError("Incorrect Index number format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException(
                $"Incorrect Index number format at line {line}"
            );
        }
        if (!CueTime.TryParse(line.Text.AsSpan(6 + number.Length + 1), formatProvider: null, out CueTime cueTime))
        {
            //Logger.LogError("Incorrect Index format at line {Line number}: \"{Line}\"", CurrentLineIndex, CurrentLine);
            throw new FormatException($"Incorrect Index format at line {line}");
        }
        lastTrack.Indices.Add(cueTime, num);
    }
}