using System;

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
    /// <summary>
    /// Parses string to CueTime (±mm:ss:ff)
    /// </summary>
    /// <param name="s"></param>
    /// <returns>CueTime instance corresponding to <see cref="s"/></returns>
    /// <exception cref="FormatException"></exception>
    public static CueTime Parse(ReadOnlySpan<char> s)
    {
        if (TryParse(s, out CueTime cue))
            return cue;
        else
            throw new FormatException("Incorrect CueTime format string");
    }
    /// <summary>
    /// Tries to parse string (±mm:ss:ff)
    /// </summary>
    /// <param name="s"></param>
    /// <param name="cueTime"></param>
    /// <returns>True if parsed correctly, false if there were problems</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out CueTime cueTime)
    {
        Span<int> nums = stackalloc int[3];
        ReadOnlySpan<char> span = s.Trim();
        int elemCount = 0;
        int last = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == ':')
            {
                if (!int.TryParse(span[last..i], out nums[elemCount]))
                {
                    cueTime = default;
                    return false;
                }
                else
                {
                    last = i + 1;
                }
                if (++elemCount >= 3)
                    break;
            }
        }
        //if less than 2 elements - not possible to parse
        if (elemCount <= 2)
        {
            //return false
            cueTime = default;
            return false;
        }
        if (!int.TryParse(span[last..], out nums[elemCount]))
        {
            //return false
            cueTime = default;
            return false;
        }
        int multiplier = nums[0] >= 0 ? 1 : -1;
        //ensure every part is non-negative or non-positive
        cueTime = new CueTime(
            Math.Abs(nums[0])*multiplier, 
            Math.Abs(nums[1])*multiplier, 
            Math.Abs(nums[2])*multiplier);
        return true;
    }
    public static bool TryParse(string s, out CueTime cueTime) => TryParse(s.AsSpan(), out cueTime);
    public override int GetHashCode() => TotalFrames.GetHashCode();
    #region Operators
    public static bool operator <(CueTime left, CueTime right) => left.CompareTo(right) < 0;
    public static bool operator >(CueTime left, CueTime right) => left.CompareTo(right) > 0;
    public static bool operator >=(CueTime left, CueTime right) => left.CompareTo(right) >= 0;
    public static bool operator <=(CueTime left, CueTime right) => left.CompareTo(right) <= 0;
    public static bool operator <(CueTime left, int right) => left.TotalFrames < right;
    public static bool operator >(CueTime left, int right) => left.TotalFrames > right;
    public static bool operator >=(CueTime left, int right) => left.TotalFrames >= right;
    public static bool operator <=(CueTime left, int right) => left.TotalFrames <= right;
    public static bool operator <(int left, CueTime right) => left < right.TotalFrames;
    public static bool operator >(int left, CueTime right) => left > right.TotalFrames;
    public static bool operator >=(int left, CueTime right) => left >= right.TotalFrames;
    public static bool operator <=(int left, CueTime right) => left <= right.TotalFrames;
    public static CueTime operator +(CueTime left, CueTime right) => new(left.TotalFrames + right.TotalFrames);
    public static CueTime operator +(CueTime left, int right) => new(left.TotalFrames + right);
    public static CueTime operator +(int left, CueTime right) => new(left + right.TotalFrames);
    public static CueTime operator -(CueTime time) => new(-time.TotalFrames);
    public static CueTime operator --(CueTime time) => new(time.TotalFrames - 1);
    public static CueTime operator +(CueTime time) => time;
    public static CueTime operator ++(CueTime time) => new(time.TotalFrames + 1);
    public static CueTime operator -(CueTime left, CueTime right) => new(left.TotalFrames - right.TotalFrames);
    public static CueTime operator -(CueTime left, int right) => new(left.TotalFrames - right);
    public static CueTime operator /(CueTime left, double right) => new((int)(left.TotalFrames / right));
    public static CueTime operator /(CueTime left, int right) => new((left.TotalFrames / right));
    public static CueTime operator *(CueTime left, double right) => new((int)(left.TotalFrames * right));
    public static CueTime operator *(CueTime left, int right) => new((left.TotalFrames * right));
    public static CueTime operator *(int left, CueTime right) => new((right.TotalFrames * left));
    public static CueTime operator *(double left, CueTime right) => new((int)(right.TotalFrames * left));
    public static CueTime operator <<(CueTime left, int right) => new(left.TotalFrames >> right);
    public static CueTime operator >>(CueTime left, int right) => new(left.TotalFrames >> right);
    #endregion
}
