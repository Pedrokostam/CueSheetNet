namespace CueSheetNet;

public record struct CueTime : IComparable<CueTime>
{
    private const int SecondsPerMinute = 60;
    private const int MillisecondsPerSecond = 1000;
    public const int FramesPerSecond = 75;
    public const int FramesPerMinute = FramesPerSecond * SecondsPerMinute;
    public const double MillisecondsPerFrame = 1000D / FramesPerSecond;
    public const int MaxValue = 100 * FramesPerMinute - 1;
    public static readonly CueTime Zero = new(0);
    public static readonly CueTime Max = new(MaxValue);
    public static readonly CueTime Min = new(-MaxValue);
    public int Minutes => (TotalFrames - Frames - SecondsPerMinute * Seconds) / FramesPerMinute;
    public int Seconds => ((TotalFrames - Frames) / FramesPerSecond) % SecondsPerMinute;
    public double Milliseconds => MillisecondsPerFrame * Frames;
    public int Frames => TotalFrames % FramesPerSecond;
    public bool Negative => TotalFrames < 0;
    public int TotalFrames { get; }
    public double TotalSeconds => TotalFrames / (double)FramesPerSecond;
    public double TotalMilliseconds => TotalFrames * MillisecondsPerFrame;
    public double TotalMinutes => TotalFrames / (double)FramesPerMinute;

    public CueTime(TimeSpan timeSpan) : this((int)(timeSpan.TotalMinutes * FramesPerMinute))
    { }
    public CueTime(int totalFrames)
    {
        if (totalFrames > MaxValue)
            throw new ArgumentOutOfRangeException($"Specified number of frames ({totalFrames}) is greater than {MaxValue}");
        if (totalFrames < -MaxValue)
            throw new ArgumentOutOfRangeException($"Specified number of frames ({totalFrames}) is less than {-MaxValue}");
        TotalFrames = totalFrames;
    }
    public CueTime(int minutes, int seconds, int frames) : this(frames + FramesPerSecond * seconds + FramesPerMinute * minutes)
    {
        bool allNonNegative = minutes >= 0 && seconds >= 0 && frames >= 0;
        bool allNonPositive = minutes <= 0 && seconds <= 0 && frames <= 0;
        if (!(allNonNegative || allNonPositive))
            throw new ArgumentException($"Parameters must all be either be all non-negative or all non-positive");
    }
    public TimeSpan ToTimeSpan() => TimeSpan.FromMinutes(TotalMinutes);
    public static CueTime FromMilliseconds(double millis) => new((int)(millis / MillisecondsPerFrame));
    public static CueTime FromSeconds(double seconds) => new((int)(seconds * FramesPerSecond));
    public static CueTime FromMinutes(double minutes) => new((int)(minutes * FramesPerMinute));
    public override string ToString() => $"{(Negative ? "-" : "")}{Math.Abs(Minutes):d2}:{Math.Abs(Seconds):d2}:{Math.Abs(Frames):d2}";
    public void Deconstruct(out int minutes, out int seconds, out int frames)
    {
        minutes = Minutes;
        seconds = Seconds;
        frames = Frames;
    }
    public int CompareTo(CueTime other) => TotalFrames.CompareTo(other.TotalFrames);

    public static bool operator <(CueTime left, CueTime right) => left.CompareTo(right) < 0;
    public static bool operator >(CueTime left, CueTime right) => left.CompareTo(right) > 0;
    public static bool operator >=(CueTime left, CueTime right) => left.CompareTo(right) >= 0;
    public static bool operator <=(CueTime left, CueTime right) => left.CompareTo(right) <= 0;
    public static CueTime operator +(CueTime left, CueTime right) => new(left.TotalFrames + right.TotalFrames);
    public static CueTime operator +(CueTime left, int right) => new(left.TotalFrames + right);
    public static CueTime operator +(int left, CueTime right) => new(left + right.TotalFrames);
    public static CueTime operator -(CueTime time) => new(-time.TotalFrames);
    public static CueTime operator -(CueTime left, CueTime right) => new(left.TotalFrames - right.TotalFrames);
    public static CueTime operator -(CueTime left, int right) => new(left.TotalFrames - right);
    public static CueTime operator /(CueTime left, double right) => new((int)(left.TotalFrames / right));
    public static CueTime operator *(CueTime left, double right) => new((int)(left.TotalFrames * right));
    public static CueTime operator *(double left, CueTime right) => new((int)(right.TotalFrames * left));

}
