using CueSheetNet.Components;
using CueSheetNet.Exceptions;
using CueSheetNet.Extensions;
using CueSheetNet.Internal;

namespace CueSheetNet;

/// <summary>
/// Provides information about a CUE index, including its position, place in the sheet, parent file and parent sheet.
/// </summary>
public class CueIndex
{

    private readonly Sheet _sheet;

    public bool IsSplit { get; internal set; } = false;

    internal bool StartsContent => Position == 1;

    internal CueIndex(int position, CueTime time, Sheet sheet)
    {
        _sheet = sheet;
        Time = time;
        Position = position;
    }
    /// <summary>
    /// Maximum number of indices in a file.
    /// </summary>
    public const int MaxNumber = 99;

    public int Position
    {
        get;
        internal set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(Position));
            field = value;
        }
    }

    /// <summary>
    /// Position of the index in the whole sheet.
    /// </summary>
    public int AbsolutePosition => _sheet.Data.FindIndex(x => ReferenceEquals(x.Index, this));
    public int AbsolutePositionInFile
    {
        get
        {
            if (_sheet.Files.Count == 1)
            {
                return AbsolutePosition;
            }
            return File.Tracks.SelectMany(x => x.Indices).IndexOfRef(this);
        }
    }

    /// <summary>
    /// Time placement of the index in its file.
    /// </summary>
    public CueTime Time { get; private set; }

    /// <summary>
    /// The file which contains this index.
    /// </summary>
    public CueDataFile File => _sheet.Data[AbsolutePosition].File;

    /// <summary>
    /// The track which contains this index.
    /// </summary>
    public CueTrack Track => _sheet.Data[AbsolutePosition].Track;

    public CueIndex? PrevioustIndexInTrack()
    {
        var prevIndex = AbsolutePosition - 1;
        if (prevIndex < 0)
        {
            return null;
        }
        var prev = _sheet.Data[prevIndex];
        if (ReferenceEquals(prev.Track, this.Track) && ReferenceEquals(prev.File, this.Track.File))
        {
            return prev.Index;
        }
        return null;
    }

    public CueIndex? NextIndexInTrack()
    {
        var nextIndex = AbsolutePosition + 1;
        if (nextIndex >= _sheet.Data.Count)
        {
            return null;
        }
        var next = _sheet.Data[nextIndex];
        if (ReferenceEquals(next.Track, this.Track) && ReferenceEquals(next.File, this.Track.File))
        {
            return next.Index;
        }
        return null;
    }

    public void MoveTime(CueTime newTime)
    {
        CueTime? boundStart = PrevioustIndexInTrack()?.Time;
        CueTime? boundEnd = NextIndexInTrack()?.Time ?? Track.Duration;
        if (boundStart > newTime && newTime < boundEnd)
        {
            Time = newTime;
        }
        else
        {
            throw new InvalidIndexEditException(nameof(Time), Time, newTime);
        }
    }

    public void ShiftTime(CueTime timeshift)
    {
        MoveTime(Time + timeshift);
    }
    public override string ToString() =>
        $"INDEX {Position:d2} {Time} ({AbsolutePosition:d2}, {Track.Title}, {File.Name})";
}
