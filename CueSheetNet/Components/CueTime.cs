using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace CueSheetNet;
/// <summary>
/// Represents a time interval measured in Cue Frames. Cue Frame is equivalent to 1 CD sector (not to be confused with CD frame, which is a part of a sector) 
/// </summary>
public readonly record struct CueTime
    : IComparable<CueTime>
    , IComparable
    , IFormattable
#if NET7_0_OR_GREATER // static interface members introduces in NET7
// Static interface members were introduces in NET7.0
    , IParsable<CueTime>
    , ISpanParsable<CueTime>
    , IDecrementOperators<CueTime>
    , IIncrementOperators<CueTime>
    , IAdditionOperators<CueTime, CueTime, CueTime>
    , IAdditionOperators<CueTime, int, CueTime>
    , IAdditiveIdentity<CueTime, CueTime>
    , ISubtractionOperators<CueTime, CueTime, CueTime>
    , ISubtractionOperators<CueTime, int, CueTime>
    , IMultiplyOperators<CueTime, int, CueTime>
    , IMultiplyOperators<CueTime, double, CueTime>
    , IMultiplyOperators<CueTime, decimal, CueTime>
    , IDivisionOperators<CueTime, int, CueTime>
    , IDivisionOperators<CueTime, double, CueTime>
    , IDivisionOperators<CueTime, decimal, CueTime>
    , IDivisionOperators<CueTime, CueTime, double>
    , IUnaryNegationOperators<CueTime, CueTime>
    , IUnaryPlusOperators<CueTime, CueTime>
    , IModulusOperators<CueTime, CueTime, CueTime>
    , IEqualityOperators<CueTime, CueTime, bool>
    , IComparisonOperators<CueTime, CueTime, bool>
#endif
{
    public int TotalFrames { get; } // Int is sufficient - it can describe up to 331 days of continuous playback (or about 4.5 TB of WAVE)

    public CueTime(int totalFrames)
    {
        TotalFrames = totalFrames;
    }

    public CueTime(int minutes, int seconds, int frames)
    {
        bool allNonNegative = minutes >= 0 && seconds >= 0 && frames >= 0;
        bool allNonPositive = minutes <= 0 && seconds <= 0 && frames <= 0;
        if (!(allNonNegative || allNonPositive))
        {
            throw new ArgumentException($"Parameters must all be either be all non-negative or all non-positive");
        }

        TotalFrames = CalculateTotalFrames(minutes, seconds, frames);
    }

    public void Deconstruct(out int minutes, out int seconds, out int frames)
    {
        minutes = Minutes;
        seconds = Seconds;
        frames = Frames;
    }

    public override string ToString()
    {
        if (Negative)
        {
            return $"-{-Minutes:d2}:{-Seconds:d2}:{-Frames:d2}";
        }
        return $"{Minutes:d2}:{Seconds:d2}:{Frames:d2}";
    }

    public override int GetHashCode() => TotalFrames.GetHashCode();

    #region Constants
    private const int SecondsPerMinute = 60;

    private const int MillisecondsPerSecond = 1000;

    public const int FramesPerSecond = 75;

    /// <summary>133'333.(3)</summary>
    public const double TicksPerFrame = (double)TimeSpan.TicksPerSecond / FramesPerSecond;

    /// <summary>4'500</summary>
    public const int FramesPerMinute = FramesPerSecond * SecondsPerMinute; // 4'500

    /// <summary>13.(3)</summary>
    public const double MillisecondsPerFrame = (double)MillisecondsPerSecond / FramesPerSecond; // 13.333333

    /// <summary>
    /// CueTime corresponding to <see cref="TotalFrames"/> of 0.
    /// </summary>
    public static readonly CueTime Zero = new(0);

    /// <summary>
    /// CueTime corresponding to <see cref="TotalFrames"/> of <see cref="int.MaxValue"/>.
    /// </summary>
    public static readonly CueTime TheoreticalMax = new(int.MaxValue);

    /// <summary>
    /// CueTime corresponding to 99:59:74, which is the maximum an ordinary CueSheet syntax can represent.
    /// </summary>
    public static readonly CueTime Max = new(99, 59, 74);

    /// <summary>
    /// CueTime corresponding to <see cref="TotalFrames"/> of <see cref="int.MinValue"/>.
    /// </summary>
    public static readonly CueTime ThereoticalMin = new(int.MinValue);

    /// <summary>
    /// CueTime corresponding to -99:59:74.
    /// </summary>
    public static readonly CueTime Min = new(-99, -59, -74);
    #endregion

    #region Properties
    public int Minutes => (TotalFrames - Frames - SecondsPerMinute * Seconds) / FramesPerMinute;

    public int Seconds => ((TotalFrames - Frames) / FramesPerSecond) % SecondsPerMinute;

    public double Milliseconds => MillisecondsPerFrame * Frames;

    public int Frames => TotalFrames % FramesPerSecond;

    public bool Negative => TotalFrames < 0;

    public double TotalSeconds => TotalFrames / (double)FramesPerSecond;

    public double TotalMilliseconds => TotalFrames * MillisecondsPerFrame;

    public double TotalMinutes => TotalFrames / (double)FramesPerMinute;

    /// <summary>
    /// Indicates whether the number of equivalent Ticks is a whole number, i.e. has no fractional part. Every 3 frames (or every 40ms) is integer.
    /// </summary>
    public bool IsTickWhole => TotalFrames % 3 == 0;

    /// <summary>
    /// Tick equivalent for <see cref="TimeSpan"/> represented as a real number.
    /// </summary>
    public double Ticks => TotalFrames * TicksPerFrame;

    /// <summary>
    /// Tick equivalent for <see cref="TimeSpan"/> truncated to <see cref="long"/>
    /// </summary>
    public long LongTicks => (long)(TotalFrames * TicksPerFrame);
    #endregion

    #region Statics
    /// <summary>
    /// Calculates the equivalent milliseconds to the given frames. Truncates it to the nearest integer.
    /// </summary>
    /// <param name="milliseconds"></param>
    /// <returns>Equivalent number of frames, rounded to the nearest integer</returns>
    /// <exception cref="OverflowException">When after conversion the number of frames exceeds the size of int</exception>
    public static int MillisecondsToFrames(double milliseconds)
    {
        double frames = milliseconds / MillisecondsPerFrame;
        // Round to 4 digits - There are 10000 ticks per millisecond, so anything after the 4th decimal place is below 1 tick, so it's a rounding error.
        double round = Math.Round(frames, 4);
        int intFrames = checked((int)round);
        // If there are fractional frames, we must truncate them.
        // Otherwise, it would be possible to have a time longer than the length of the audio file.
        // Casting to int is better, as it always does it towards zero.
        // Math.Floor would not work correctly with negative milliseconds.
        return intFrames;
    }

    public static int TicksToFrames(long ticks)
    {
        double frames = ticks / TicksPerFrame;
        double round = Math.Round(frames);
        return checked((int)round);
    }

    /// <summary>
    /// Calculates total frames from the specified components. Operation is checked - <see cref="OverflowException"/> is thrown if overflow happens. Components don'spanTrimmedSliced have to have the same sign.
    /// </summary>
    /// <param name="minutes"></param>
    /// <param name="seconds"></param>
    /// <param name="frames"></param>
    /// <exception cref="OverflowException">Thrown if multiplication results in overflow</exception>
    /// <returns></returns>
    public static int CalculateTotalFrames(int minutes, int seconds, int frames) => checked(frames + FramesPerSecond * seconds + FramesPerMinute * minutes);

    /// <summary>
    /// Calculates total frames from the specified components. Operation is unchecked - overflow can cause incorrect results. Components don'spanTrimmedSliced have to have the same sign.
    /// </summary>
    /// <param name="minutes"></param>
    /// <param name="seconds"></param>
    /// <param name="frames"></param>
    /// <returns></returns>
    public static int CalculateTotalFrames_Unchecked(int minutes, int seconds, int frames) => unchecked(frames + FramesPerSecond * seconds + FramesPerMinute * minutes);
    #endregion

    #region Conversions
    public static CueTime FromTimeSpan(TimeSpan timeSpan) => new(totalFrames: TicksToFrames(timeSpan.Ticks));

    public TimeSpan ToTimeSpan() => TimeSpan.FromTicks(LongTicks);

    public static CueTime FromMilliseconds(double millis) => new((int)(millis / MillisecondsPerFrame));

    public static CueTime FromSeconds(double seconds) => new((int)(seconds * FramesPerSecond));

    public static CueTime FromMinutes(double minutes) => new((int)(minutes * FramesPerMinute));
    #endregion

    #region Parsing
    /// <summary>
    /// Parses ReadOnlySpan to CueTime (±mm:ss:ff). The parsed time is negative, only if the minute part is negative.
    /// The frame and seconds parts do not affect the negativity.
    /// </summary>
    /// <param name="span"></param>
    /// <returns>CueTime instance corresponding to <see cref="s"/></returns>
    /// <exception cref="ArgumentException"></exception>
    public static CueTime Parse(ReadOnlySpan<char> span)
    {
        ReadOnlySpan<char> spanTrimmed = span.Trim();
        if (spanTrimmed.Length == 0) throw new ArgumentException("Empty CueTime string", nameof(span));
        List<int> inds = SeekSeparator(spanTrimmed);
        if (inds.Count < 4) throw new ArgumentException($"CueTime string has less than 3 parts ({span.ToString()})", nameof(span));
        Span<int> nums = stackalloc int[3];
        int numCount = 0;
        for (int i = 1; i < inds.Count; i++)
        {
            int rangeStart = inds[i - 1] + 1;//plus one, because it was included in previous range
            //That';'s why the SeekSeparator add -1 as the first element
            //so that the first rangeStart will be equal to 0
            int rangeEnd = inds[i];
            int x = int.Parse(Slice(spanTrimmed, rangeStart, rangeEnd), NumberStyles.Integer, CultureInfo.InvariantCulture);
            nums[numCount] = x;
            if (++numCount > 2)
                break;
        }
        int multiplier = nums[0] >= 0 ? 1 : -1;
        int _minutes = Math.Abs(nums[0]);
        int _seconds = Math.Abs(nums[1]);
        int _frames = Math.Abs(nums[2]);
        int totalFrames = checked(CalculateTotalFrames(_minutes, _seconds, _frames) * multiplier);
        return new CueTime(totalFrames);
    }

    /// <summary>
    /// Finds all separators (':') in the given span.
    /// </summary>
    /// <param name="spanTrimmed"></param>
    /// <returns>A list with indices of the separator occurences. The first element of the list is always -1</returns>
    private static List<int> SeekSeparator(ReadOnlySpan<char> spanTrimmed)
    {
        List<int> inds = new(4) { -1 };
        for (int i = 0; i < spanTrimmed.Length; i++)
        {
            char czar = spanTrimmed[i];
            if (czar == ':')
            {
                inds.Add(i);
            }
        }
        inds.Add(spanTrimmed.Length);
        return inds;
    }

    /// <summary>
    /// Parses string to CueTime (±mm:ss:ff). The parsed time is negative, only if the minute part is negative.
    /// The frame and seconds parts do not affect the negativity.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">If string is null</exception>
    public static CueTime Parse([NotNull] string? str)
    {
        ExceptionHelper.ThrowIfNull(str);
        return Parse(str.AsSpan());
    }

    /// <summary>
    /// Helper function that return either string or span sliced to given range, depending on target framework.
    /// </summary>
    /// <param name="span">Span to slice</param>
    /// <param name="start">Inclusive start of slice</param>
    /// <param name="end">Exclusive end of slice</param>
    /// <returns>String for NetStandard2.0, ReadOnlySpan elsewhere</returns>
#if !NETSTANDARD2_0 // int.Parse cannot use Spans only in NETSTANDARD2.0
    private static ReadOnlySpan<char> Slice(ReadOnlySpan<char> span, int start, int end) => span[start..end];
#else
    // While System.Memory adds range slices, we still need to a return string, because int.TryParse requires it.
    private static string Slice(ReadOnlySpan<char> span, int start, int end) => span[start..end].ToString();
#endif

    /// <summary>
    /// Tries to parse string (±mm:ss:ff). The parsed time is negative, only if the minute part is negative.
    /// The frame and seconds parts do not affect the negativity.
    /// Only the last parts (frames) is required. Other parts are assumed to be zero, if missing.
    /// </summary>
    /// <param name="span">Text to be parsed. Whether the time is positive or negative depends only on the minutes part</param>
    /// <param name="cueTime"></param>
    /// <returns>True if parsed correctly, false if there were problems</returns>
    public static bool TryParse(ReadOnlySpan<char> span, out CueTime cueTime)
    {
        cueTime = default;
        ReadOnlySpan<char> spanTrimmed = span.Trim();
        List<int> inds = SeekSeparator(spanTrimmed);
        if (inds.Count <= 1) return false;
        Span<int> nums = stackalloc int[Math.Min(3, inds.Count - 1)];
        int numCount = 0;
        for (int i = 1; i < inds.Count; i++)
        {
            int rangeStart = inds[i - 1] + 1;//plus one, because it was included in previous range
            //That's why the SeekSeparator add -1 as the first element
            //so that the first rangeStart will be equal to 0
            int rangeEnd = inds[i];
            if (!int.TryParse(Slice(spanTrimmed, rangeStart, rangeEnd), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x))
                return false;
            nums[numCount] = x;
            if (++numCount > 2)
                break;
        }
        int multiplier = nums[0] >= 0 ? 1 : -1;
        int _minutes = nums.Length == 3 ? Math.Abs(nums[^3]) : 0;// If there are 3 elements, take the third from the end (i.e the zeroth)
        int _seconds = nums.Length >= 2 ? Math.Abs(nums[^2]) : 0;// If there are 2 or 3 elements, take the second from the end (i.e the zeroth or the first)
        int _frames = Math.Abs(nums[^1]);// take last element
        try
        {
            int totalFrames = CalculateTotalFrames(_minutes, _seconds, _frames) * multiplier;
            cueTime = new CueTime(totalFrames);
        }
        catch (OverflowException)
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
        return CompareTo((CueTime)obj);
    }

    public int CompareTo(CueTime other) => TotalFrames.CompareTo(other.TotalFrames);

    public static bool Equals(CueTime ct1, CueTime ct2) => ct1.TotalFrames == ct2.TotalFrames;
    #endregion

    #region Math
    /// <summary>
    /// Divides the time by the divisor
    /// </summary>
    /// <param name="time">The time</param>
    /// <param name="divisor">The divisor</param>
    /// <returns>CueTime equivalent to the number frames of input CueTime divided by the divisor, truncated towards zero</returns>
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
    /// <returns>CueTime equivalent to the number frames of input CueTime divided by the divisor, truncated towards zero</returns>
    /// <exception cref="DivideByZeroException">Thrown if parameter <paramref name="divisor"/> is zero</exception>
    public CueTime Divide(decimal divisor)
    {
        if (divisor == 0) throw new DivideByZeroException();
        return new((int)(TotalFrames / divisor));
    }

    /// <summary>
    /// Divides the time by the divisor
    /// </summary>
    /// <param name="divisor">The divisor</param>
    /// <returns>CueTime equivalent to the number frames of input CueTime divided by the divisor, truncated towards zero</returns>
    /// <exception cref="DivideByZeroException">Thrown if parameter <paramref name="divisor"/> is zero</exception>
    public CueTime Divide(double divisor)
    {
        if (double.IsNaN(divisor)) throw new ArgumentException("Divisor must be a number", nameof(divisor));
        if (divisor == 0) throw new DivideByZeroException();
        return new((int)(TotalFrames / divisor));
    }
    /// <summary>
    /// Multiplies the time by the multiplier
    /// </summary>
    /// <param name="multiplier"></param>
    /// <returns>CueTime equivalent to the number frames of input CueTime multiplied by the <paramref name="multiplier"/></returns>
    public CueTime Multiply(int multiplier) => new(TotalFrames * multiplier);

    /// <summary>
    /// Multiplies the time by the multiplier
    /// </summary>
    /// <param name="multiplier"></param>
    /// <returns>CueTime equivalent to the number frames of input CueTime multiplied by the <paramref name="multiplier"/>, truncated towards zero</returns>
    /// <exception cref="ArgumentException">When <paramref name="multiplier"/> is Not A Number</exception>
    public CueTime Multiply(double multiplier)
    {
        if (double.IsNaN(multiplier)) throw new ArgumentException("Multiplier must be a number", nameof(multiplier));
        return new((int)(TotalFrames * multiplier));
    }
    /// <summary>
    /// Multiplies the time by the multiplier
    /// </summary>
    /// <param name="multiplier"></param>
    /// <returns>CueTime equivalent to the number frames of input CueTime multiplied by the <paramref name="multiplier"/>, truncated towards zero</returns>
    public CueTime Multiply(decimal multiplier)
    {
        return new((int)(TotalFrames * multiplier));
    }

    public CueTime Add(CueTime time) => new(TotalFrames + time.TotalFrames);

    /// <summary>
    /// Add the <paramref name="frames"/> number of frames to the time
    /// </summary>
    /// <param name="frames"></param>
    /// <returns></returns>
    public CueTime AddFrames(int frames) => new(TotalFrames + frames);

    public CueTime Subtract(CueTime time) => new(TotalFrames - time.TotalFrames);

    public CueTime SubtractFrames(int frames) => new(TotalFrames - frames);
    #endregion

    #region Operators
    public static implicit operator TimeSpan(CueTime cueTime) => cueTime.ToTimeSpan();

    public static explicit operator CueTime(TimeSpan timeSpan) => FromTimeSpan(timeSpan);

    public static bool operator <(CueTime left, CueTime right) => left.CompareTo(right) < 0;

    public static bool operator >(CueTime left, CueTime right) => left.CompareTo(right) > 0;

    public static bool operator >=(CueTime left, CueTime right) => left.CompareTo(right) >= 0;

    public static bool operator <=(CueTime left, CueTime right) => left.CompareTo(right) <= 0;

    public static CueTime operator +(CueTime left, CueTime right) => left.Add(right);
    public static CueTime operator +(CueTime left, int right) => left.AddFrames(right);

    public static CueTime operator -(CueTime time) => new(-time.TotalFrames);

    public static CueTime operator --(CueTime time) => time.SubtractFrames(1);

    public static CueTime operator +(CueTime time) => time;

    public static CueTime operator ++(CueTime time) => time.AddFrames(1);

    public static CueTime operator -(CueTime left, CueTime right) => left.Subtract(right);

    /// <summary>
    /// Subtract the frames from the time
    /// </summary>
    /// <param name="time"></param>
    /// <param name="frames"></param>
    /// <returns></returns>
    public static CueTime operator -(CueTime time, int frames) => time.SubtractFrames(frames);

    /// <summary>
    /// Divides the time by the divisor
    /// </summary>
    /// <param name="time">The time</param>
    /// <param name="divisor">The divisor</param>
    /// <returns></returns>
    /// <exception cref="DivideByZeroException">Thrown if parameter <paramref name="divisor"/> is zero</exception>
    public static CueTime operator /(CueTime time, double divisor) => time.Divide(divisor);

    public static CueTime operator /(CueTime time, decimal divisor) => time.Divide(divisor);

    /// <summary>
    /// Divides the time by the divisor
    /// </summary>
    /// <param name="time">The time</param>
    /// <param name="divisor">The divisor</param>
    /// <returns></returns>
    /// <exception cref="DivideByZeroException">Thrown if parameter <paramref name="divisor"/> is zero</exception>
    public static CueTime operator /(CueTime left, int divisor) => left.Divide(divisor);

    public static CueTime operator *(CueTime left, double multiplier) => left.Multiply(multiplier);

    public static CueTime operator *(CueTime left, decimal multiplier) => left.Multiply(multiplier);

    public static CueTime operator *(CueTime left, int multiplier) => left.Multiply(multiplier);

    public static CueTime operator *(int multiplier, CueTime right) => right.Multiply(multiplier);

    public static CueTime operator *(double multiplier, CueTime right) => right.Multiply(multiplier);

    public static CueTime operator *(decimal multiplier, CueTime right) => right.Multiply(multiplier);

    public static CueTime operator %(CueTime left, CueTime right) => new(left.Frames % right.Frames);

    public static double operator /(CueTime left, CueTime right) => (left.Frames / right.Frames);
    #endregion

    #region Explicit Interfaces
#if NET7_0_OR_GREATER // static interface members introduces in NET7
    static CueTime IParsable<CueTime>.Parse(string s, IFormatProvider? provider) => Parse(s);

    static bool IParsable<CueTime>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out CueTime result) => TryParse(s, out result);

    static CueTime ISpanParsable<CueTime>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    static bool ISpanParsable<CueTime>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out CueTime result) => TryParse(s, out result);

    static CueTime IAdditiveIdentity<CueTime, CueTime>.AdditiveIdentity => CueTime.Zero;
#endif
    #endregion
    #region String
    public string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        switch (format)
        {
            case null or "" or "G" or "g" or @"-mm\:ss\:ff":
                return ToString();
            default:
                break;
        }
        ReadOnlySpan<char> span = format.AsSpan();
        int spanLength = span.Length;
        int i = 0;
        StringBuilder strb = new();
        while (i < spanLength)
        {
            char character = span[i];
            switch (character)
            {
                case '\\':
                    if (i < spanLength - 1)
                    {
                        strb.Append(character);
                        i += 2;
                    }
                    break;
                case '+' or '-':
                    i++;
                    if (!(character == '-' && !Negative))
                    {
                        strb.Append(Negative ? '-' : '+');
                    }
                    break;
                case 'm' or 's' or 'f':
                    {
                        int charLength = ParseRepeat(span, i);
                        if (charLength > 2)
                        {
                            throw new FormatException();
                        }
                        i += charLength;
                        int num = GetTimeValueByChar(character);
                        strb.Append(num.ToString().PadRight(charLength, '0'));
                        break;
                    }
                case 'D':
                    {
                        int charLength = ParseRepeat(span, i);
                        i += charLength;
                        string fmt = "." + new string('0', charLength);
                        strb.Append((Math.Abs(Milliseconds) / 1000).ToString(fmt)[1..]);
                        break;
                    }

                default:
                    strb.Append(character);
                    i++;
                    break;
            }
        }
        return strb.ToString();
    }
    /// <summary>
    /// Returns the value of one of the time properties, depending on the <paramref name="character"/>:
    /// <list type="table">
    ///    <item>
    ///        <term>m</term>
    ///        <description>Minutes</description>
    ///    </item>
    ///     <item>
    ///        <term>s</term>
    ///        <description>Seconds</description>
    ///    </item>
    ///     <item>
    ///        <term>f</term>
    ///        <description>Frames</description>
    ///    </item>
    ///    <item>
    ///        <term>other</term>
    ///        <description>0</description>
    ///    </item>
    ///</list>
    /// </summary>
    /// <param name="character"></param>
    /// <returns></returns>
    private int GetTimeValueByChar(char character)
    {
        return character switch
        {
            'm' => Math.Abs(Minutes),
            's' => Math.Abs(Seconds),
            'f' => Math.Abs(Frames),
            _ => 0
        };
    }

    private static int ParseRepeat(ReadOnlySpan<char> format, int pos)
    {
        char patternChar = format[pos];
        int index = pos + 1;
        while ((uint)index < (uint)format.Length && format[index] == patternChar)
        {
            index++;
        }
        return index - pos;
    }
    #endregion
}
