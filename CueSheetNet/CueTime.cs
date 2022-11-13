﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;

namespace CueSheetNet;

public readonly record struct CueTime : IComparable<CueTime>, IComparable<int>, IComparable
{
    private const int SecondsPerMinute = 60;

    private const int MillisecondsPerSecond = 1000;

    public const int FramesPerSecond = 75;

    public const int FramesPerMinute = FramesPerSecond * SecondsPerMinute; // 45'000

    public const double MillisecondsPerFrame = (double)MillisecondsPerSecond / FramesPerSecond; // 13.333333

    public static readonly CueTime Zero = new(0);

    public static readonly CueTime Max = new(int.MaxValue);

    public static readonly CueTime Min = new(int.MinValue);

    public int Minutes => (TotalFrames - Frames - SecondsPerMinute * Seconds) / FramesPerMinute;

    public int Seconds => ((TotalFrames - Frames) / FramesPerSecond) % SecondsPerMinute;

    public double Milliseconds => MillisecondsPerFrame * Frames;

    public int Frames => TotalFrames % FramesPerSecond;

    public bool Negative => TotalFrames < 0;

    public int TotalFrames { get; }

    public double TotalSeconds => TotalFrames / (double)FramesPerSecond;

    public double TotalMilliseconds => TotalFrames * MillisecondsPerFrame;

    public double TotalMinutes => TotalFrames / (double)FramesPerMinute;

    public static int MillisecondsToFrames(double milliseconds)
    {
        double frames = milliseconds / MillisecondsPerFrame;
        double rounded = Math.Round(frames);
        return (int)rounded;
    }

    public CueTime(TimeSpan timeSpan) : this(MillisecondsToFrames(timeSpan.TotalMilliseconds)) { }

    public CueTime(int totalFrames)
    {
        TotalFrames = totalFrames;
    }

    public CueTime(int minutes, int seconds, int frames) : this(checked(frames + FramesPerSecond * seconds + FramesPerMinute * minutes))
    {
        bool allNonNegative = minutes >= 0 && seconds >= 0 && frames >= 0;
        bool allNonPositive = minutes <= 0 && seconds <= 0 && frames <= 0;
        if (seconds > 59 || seconds < -59) throw new ArgumentOutOfRangeException("The seconds part has to be less than 60 (or more than -60");
        if (frames > 74 || seconds < -74) throw new ArgumentOutOfRangeException("The frames part has to be less than 75 (or more than -75");
        if (!(allNonNegative || allNonPositive))
            throw new ArgumentException($"Parameters must all be either be all non-negative or all non-positive");
    }

    public TimeSpan ToTimeSpan() => TimeSpan.FromMilliseconds(TotalMilliseconds);

    public static implicit operator TimeSpan(CueTime cueTime) => cueTime.ToTimeSpan();
    public static explicit operator CueTime(TimeSpan timeSpan) => new(timeSpan);

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

    public override int GetHashCode() => TotalFrames.GetHashCode();

    #region Parsing
    /// <summary>
    /// Parses string to CueTime (±mm:ss:ff)
    /// </summary>
    /// <param name="span"></param>
    /// <returns>CueTime instance corresponding to <see cref="s"/></returns>
    /// <exception cref="FormatException"></exception>
    public static CueTime Parse(ReadOnlySpan<char> span)
    {
        ReadOnlySpan<char> spanTrimmed = span.Trim();
        if (spanTrimmed.Length == 0) throw new ArgumentException("Empty CueTime string");
        List<int> inds = new(4) { -1 };
        for (int i = 0; i < spanTrimmed.Length; i++)
        {
            if (spanTrimmed[i] == ':')
            {
                inds.Add(i);
            }
        }
        inds.Add(spanTrimmed.Length);
        if (inds.Count < 4) throw new ArgumentException("CueTime string has less than 3 parts");
        Span<int> nums = stackalloc int[3];
        int numCount = 0;
        for (int i = 1; i < inds.Count; i++)
        {
            int rangeStart = inds[i - 1] + 1;//plus one, because it was included in previous range
            int rangeEnd = inds[i];
            int x = int.Parse(spanTrimmed[rangeStart..rangeEnd], NumberStyles.Integer, CultureInfo.InvariantCulture);
            nums[numCount] = x;
            if (++numCount > 2)
                break;
        }
        int multiplier = nums[0] >= 0 ? 1 : -1;
        int _minutes = Math.Abs(nums[0]);
        int _seconds = Math.Abs(nums[1]);
        int _frames = Math.Abs(nums[2]);

        return new CueTime(
                _minutes * multiplier,
                _seconds * multiplier,
                _frames * multiplier);
    }

    public static CueTime Parse([NotNull] string? str)
    {
        ArgumentNullException.ThrowIfNull(str);
        return Parse(str.AsSpan());
    }
    /// <summary>
    /// Tries to parse string (±mm:ss:ff)
    /// </summary>
    /// <param name="span">Text to be parsed. Whether the time is positive or negative depends only on the minutes part</param>
    /// <param name="cueTime"></param>
    /// <returns>True if parsed correctly, false if there were problems</returns>
    public static bool TryParse(ReadOnlySpan<char> span, out CueTime cueTime)
    {
        cueTime = default;
        ReadOnlySpan<char> spanTrimmed = span.Trim();
        List<int> inds = new(4) { -1 };
        for (int i = 0; i < spanTrimmed.Length; i++)
        {
            if (spanTrimmed[i] == ':')
            {
                inds.Add(i);
            }
        }
        inds.Add(spanTrimmed.Length);
        if (inds.Count < 4) return false;
        Span<int> nums = stackalloc int[3];
        int numCount = 0;
        for (int i = 1; i < inds.Count; i++)
        {
            int rangeStart = inds[i - 1] + 1;//plus one, because it was included in previous range
            int rangeEnd = inds[i];
            if (!int.TryParse(spanTrimmed[rangeStart..rangeEnd], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x))
                return false;
            nums[numCount] = x;
            if (++numCount > 2)
                break;
        }
        int multiplier = nums[0] >= 0 ? 1 : -1;
        int _minutes = Math.Abs(nums[0]);
        int _seconds = Math.Abs(nums[1]);
        int _frames = Math.Abs(nums[2]);

        try
        {
            cueTime = new CueTime(
                _minutes * multiplier,
                _seconds * multiplier,
                _frames * multiplier);
        }
        catch (OverflowException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        //ensure every part is non-negative or non-positive
        return true;
    }
    /// <summary>
    /// Tries to parse string (±mm:ss:ff)
    /// </summary>
    /// <param name="s">Text to be parsed. Whether the time is positive or negative depends only on the minutes part</param>
    /// <param name="cueTime"></param>
    /// <returns>True if parsed correctly, false if there were problems</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out CueTime cueTime)
    {
        cueTime = default;
        if (s == null) return false;
        return TryParse(s.AsSpan(), out cueTime);
    }

    #endregion
    #region Comparison and Equality
    public static int Compare(CueTime ct1, CueTime ct2) => ct1.TotalFrames.CompareTo(ct2.TotalFrames);

    public int CompareTo(object? obj)
    {
        if (obj == null) return 1;
        return obj switch
        {
            CueTime => CompareTo((CueTime)obj),
            int => CompareTo((int)obj),
            TimeSpan => CompareTo((TimeSpan)obj),
            _ => throw new ArgumentException("Compared object must be a CueTime, Int, or TimeSpan"),
        };
    }

    public int CompareTo(CueTime other) => TotalFrames.CompareTo(other.TotalFrames);

    /// <summary>
    /// Compares the time to the integer, treating it as the number of frames
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(int other) => TotalFrames.CompareTo(other);

    public static bool Equals(CueTime ct1, CueTime ct2) => ct1.TotalFrames == ct2.TotalFrames;

    #endregion
    #region Math
    /// <summary>
    /// Divides the time by the divisor
    /// </summary>
    /// <param name="time">The time</param>
    /// <param name="divisor">The divisor</param>
    /// <returns></returns>
    /// <exception cref="DivideByZeroException">Thrown if parameter <paramref name="divisor"/> is zero</exception>
    public CueTime Divide(int divisor)
    {
        if (divisor == 0) throw new DivideByZeroException();
        return new(TotalFrames / divisor);
    }

    /// <summary>
    /// Divides the time by the divisor
    /// </summary>
    /// <param name="time">The time</param>
    /// <param name="divisor">The divisor</param>
    /// <returns></returns>
    /// <exception cref="DivideByZeroException">Thrown if parameter <paramref name="divisor"/> is zero</exception>
    public CueTime Divide(double divisor)
    {
        if (double.IsNaN(divisor)) throw new ArgumentException("Divisor must be a number");
        if (divisor == 0) throw new DivideByZeroException();
        return new((int)(TotalFrames / divisor));
    }

    public CueTime Multiply(int multiplier) => new(TotalFrames * multiplier);

    public CueTime Multiply(double multiplier)
    {
        if (double.IsNaN(multiplier)) throw new ArgumentException("Multiplier must be a number");
        return new((int)(TotalFrames * multiplier));
    }

    public CueTime Add(CueTime right) => new(TotalFrames + right.TotalFrames);

    public CueTime AddFrames(int right) => new(TotalFrames + right);

    public CueTime Subtract(CueTime right) => new(TotalFrames - right.TotalFrames);

    public CueTime SubtractFrames(int right) => new(TotalFrames - right);

    #endregion
    #region Operators
    public static bool operator <(CueTime left, CueTime right) => left.CompareTo(right) < 0;

    public static bool operator >(CueTime left, CueTime right) => left.CompareTo(right) > 0;

    public static bool operator >=(CueTime left, CueTime right) => left.CompareTo(right) >= 0;

    public static bool operator <=(CueTime left, CueTime right) => left.CompareTo(right) <= 0;

    public static bool operator <(CueTime left, int right) => left.CompareTo(right) < 0;

    public static bool operator >(CueTime left, int right) => left.CompareTo(right) > 0;

    public static bool operator >=(CueTime left, int right) => left.CompareTo(right) <= 0;

    public static bool operator <=(CueTime left, int right) => left.CompareTo(right) >= 0;

    public static bool operator <(int left, CueTime right) => right.CompareTo(left) < 0;

    public static bool operator >(int left, CueTime right) => right.CompareTo(left) > 0;

    public static bool operator >=(int left, CueTime right) => right.CompareTo(left) <= 0;

    public static bool operator <=(int left, CueTime right) => right.CompareTo(left) >= 0;

    public static CueTime operator +(CueTime left, CueTime right) => left.Add(right);

    public static CueTime operator +(CueTime left, int right) => left.AddFrames(right);

    public static CueTime operator +(int left, CueTime right) => right.AddFrames(left);

    public static CueTime operator -(CueTime time) => new(-time.TotalFrames);

    public static CueTime operator --(CueTime time) => time.SubtractFrames(1);

    public static CueTime operator +(CueTime time) => time;

    public static CueTime operator ++(CueTime time) => time.AddFrames(1);

    public static CueTime operator -(CueTime left, CueTime right) => left.Subtract(right);

    public static CueTime operator -(CueTime left, int right) => left.SubtractFrames(right);

    /// <summary>
    /// Divides the time by the divisor
    /// </summary>
    /// <param name="time">The time</param>
    /// <param name="divisor">The divisor</param>
    /// <returns></returns>
    /// <exception cref="DivideByZeroException">Thrown if parameter <paramref name="divisor"/> is zero</exception>
    public static CueTime operator /(CueTime time, double divisor) => time.Divide(divisor);

    /// <summary>
    /// Divides the time by the divisor
    /// </summary>
    /// <param name="time">The time</param>
    /// <param name="divisor">The divisor</param>
    /// <returns></returns>
    /// <exception cref="DivideByZeroException">Thrown if parameter <paramref name="divisor"/> is zero</exception>
    public static CueTime operator /(CueTime left, int divisor) => left.Divide(divisor);

    public static CueTime operator *(CueTime left, double multiplier) => left.Multiply(multiplier);

    public static CueTime operator *(CueTime left, int multiplier) => left.Multiply(multiplier);

    public static CueTime operator *(int multiplier, CueTime right) => right.Multiply(multiplier);

    public static CueTime operator *(double multiplier, CueTime right) => right.Multiply(multiplier);

    #endregion
}
