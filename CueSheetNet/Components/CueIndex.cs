using System.Security.Cryptography;
using CueSheetNet.Internal;

namespace CueSheetNet;

public readonly record struct CueIndex
{
    public const int MaxNumber = 99;
    private readonly int _Number;
    public int Number
    {
        get => _Number;
        init
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(Number), $"Index must not be negative");
            if (value > MaxNumber) throw new ArgumentOutOfRangeException(nameof(Number), "Index must be less than 100");
            _Number = value;
        }
    }
    public int AbsoluteIndex { get; init; }
    public CueTime Time { get; init; }
    public CueFile File { get; init; }
    public CueTrack Track { get; init; }
    internal CueIndex(CueIndexImpl iimpl)
        : this(iimpl.Number,
               iimpl.Index,
               iimpl.File,
               iimpl.Track,
               iimpl.Time) { }
    public CueIndex(int number, int absoluteIndex, CueFile file, CueTrack track, int minutes, int seconds, int frames)
        : this(number,
               absoluteIndex,
               file,
               track,
               new CueTime(minutes,seconds,frames)) { }
    public CueIndex(int number, int absoluteIndex, CueFile file, CueTrack track, CueTime cueTime)
        : this(number,
               absoluteIndex,
               file,
               track,
               cueTime.TotalFrames) { }
    public CueIndex(int number, int absoluteIndex, CueFile file, CueTrack track, TimeSpan timeSpan)
        : this(number,
               absoluteIndex,
               file,
               track,
               CueTime.FromTimeSpan(timeSpan)) { }
    public CueIndex(int number,
                    int absoluteIndex,
                    CueFile file,
                    CueTrack track,
                    int totalFrames)
    {
        AbsoluteIndex = absoluteIndex;
        File = file ?? throw new ArgumentNullException(nameof(file));
        Track = track ?? throw new ArgumentNullException(nameof(track));
        _Number = number;
        Time = new(totalFrames);
    }
    public override string ToString() => $"INDEX {Number:d2} {Time} ({AbsoluteIndex:d2}, {Track.Title}, {File.FileInfo.Name})";

}
