using CueSheetNet.Components;
using CueSheetNet.Exceptions;
using CueSheetNet.Extensions;

namespace CueSheetNet;

/// <summary>
/// Provides information about a CUE index, including its position, place in the sheet, parent file and parent sheet.
/// </summary>
public sealed class CueIndex
{

    private readonly CueSheet _sheet;

    public bool IsSplit { get; internal set; } = false;

    internal bool StartsContent => Position == 1;

    internal CueIndex(int position, CueTime time, CueSheet sheet)
    {
        _sheet = sheet;
        Time = time;
        Position = position;
    }
    /// <summary>
    /// Maximum number of indices in a file.
    /// </summary>
    public const int MaxNumber = 99;

    /// <summary>
    /// Index's position in the track.
    /// </summary>
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
        // Index times are relative to their file, so any index in the same file (regardless of track) bounds the move.
        int position = AbsolutePosition;
        var file = _sheet.Data[position].File;
        bool aboveLower = position > 0 && ReferenceEquals(_sheet.Data[position - 1].File, file)
            ? newTime > _sheet.Data[position - 1].Index.Time
            : newTime >= CueTime.Zero;
        bool belowUpper = position + 1 < _sheet.Data.Count && ReferenceEquals(_sheet.Data[position + 1].File, file)
            ? newTime < _sheet.Data[position + 1].Index.Time
            : file.Meta is not { } meta || newTime < meta.CueDuration;
        if (!aboveLower || !belowUpper)
            throw new InvalidOperationException($"Cannot move index before or after its neighbors ({newTime}).");
        Time = newTime;
    }

    public void ShiftTime(int frames)
    {
        MoveTime(Time + frames);
    }
    public void ShiftTime(CueTime timeshift)
    {
        MoveTime(Time + timeshift);
    }
    public override string ToString()
    {
        int position = AbsolutePosition;
        if (position < 0)
            return $"INDEX {Position:d2} {Time} (detached)";
        var link = _sheet.Data[position];
        return $"INDEX {Position:d2} {Time} ({position:d2}, {link.Track.Title}, {link.File.Name})";
    }
}
