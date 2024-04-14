using CueSheetNet.Internal;

namespace CueSheetNet;

/// <summary>
/// Provides information about a CUE index, including its position, place in the sheet, parent file and parent sheet.
/// </summary>
public readonly record struct CueIndex
{
    /// <summary>
    /// Maximum number of indices in a file.
    /// </summary>
    public const int MaxNumber = 99;

    private readonly int _Number;

    /// <summary>
    /// Number of index in its file.
    /// </summary>
    public int Number
    {
        get => _Number;
        init
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), $"Index must not be negative");
            if (value > MaxNumber)
                throw new ArgumentOutOfRangeException(nameof(value), "Index must be less than 100");
            _Number = value;
        }
    }

    /// <summary>
    /// Number of index in the whole sheet.
    /// </summary>
    public int AbsoluteIndex { get; init; }

    /// <summary>
    /// Position of the index in its file.
    /// </summary>
    public CueTime Time { get; init; }

    /// <summary>
    /// The file which contains this index.
    /// </summary>
    public CueDataFile File { get; init; }

    /// <summary>
    /// The track which contains this index.
    /// </summary>
    public CueTrack Track { get; init; }

    internal CueIndex(CueIndexImpl iimpl)
        : this(iimpl.Number, iimpl.Index, iimpl.File, iimpl.Track, iimpl.Time) { }

    public CueIndex(
        int number,
        int absoluteIndex,
        CueDataFile file,
        CueTrack track,
        int minutes,
        int seconds,
        int frames
    )
        : this(number, absoluteIndex, file, track, new CueTime(minutes, seconds, frames)) { }

    public CueIndex(
        int number,
        int absoluteIndex,
        CueDataFile file,
        CueTrack track,
        CueTime cueTime
    )
        : this(number, absoluteIndex, file, track, cueTime.TotalFrames) { }

    public CueIndex(
        int number,
        int absoluteIndex,
        CueDataFile file,
        CueTrack track,
        TimeSpan timeSpan
    )
        : this(number, absoluteIndex, file, track, CueTime.FromTimeSpan(timeSpan)) { }

    public CueIndex(
        int number,
        int absoluteIndex,
        CueDataFile file,
        CueTrack track,
        int totalFrames
    )
    {
        AbsoluteIndex = absoluteIndex;
        File = file ?? throw new ArgumentNullException(nameof(file));
        Track = track ?? throw new ArgumentNullException(nameof(track));
        _Number = number;
        Time = new(totalFrames);
    }

    public override string ToString() =>
        $"INDEX {Number:d2} {Time} ({AbsoluteIndex:d2}, {Track.Title}, {File.SourceFile.Name})";
}
