using System.Globalization;

namespace CueSheetNet.Helpers;
internal static class CueTimeFormatHelper
{
    /// <summary>
    /// Appends the the time property specified at the <paramref name="index"/> of <paramref name="format"/> to <paramref name="stringBuilder"/> and return the appendee's width.
    /// </summary>
    /// <remarks>
    /// Respects repeated characters.
    /// </remarks>
    /// <param name="stringBuilder">StringBuilder to append to.</param>
    /// <param name="format">Format string.</param>
    /// <param name="index">Index of currently reviewed part of the format string.</param>
    /// <returns>The width of appended part.</returns>
    public static int AppendCoreTimeProperty(CueTime cueTime,
        StringBuilder stringBuilder,
        ReadOnlySpan<char> format,
        int index
    )
    {
        int charLength = StringHelper.CountSubsequence(format, index);
        int num = GetTimeValueByChar(cueTime,format[index]);
        AppendPaddedInteger(stringBuilder, num, charLength);
        return charLength;
    }

    /// <summary>
    /// Appends milliseconds according to the <paramref name="format"/> at the <paramref name="index"/>.
    /// </summary>
    /// <inheritdoc cref="AppendCoreTimeProperty(CueTime cueTime,StringBuilder, ReadOnlySpan{char}, int)(StringBuilder, ReadOnlySpan{char}, int)"/>
    public static int AppendMilliseconds(CueTime cueTime,
        StringBuilder stringBuilder,
        ReadOnlySpan<char> format,
        int index
    )
    {
        int charLength = StringHelper.CountSubsequence(format, index);
        AppendPaddeFractionalPart(stringBuilder, cueTime.Milliseconds, charLength);
        return charLength;
    }
    /// <summary>
    /// Appends a characters that is not part of formatting terms.
    /// </summary>
    /// <param name="stringBuilder">StringBuilder to append to.</param>
    /// <param name="format">Format string.</param>
    /// <param name="index">Index of currently reviewed part of the format string.</param>
    /// <returns>The width of appended part (1).</returns>
    public static int AppendOther(StringBuilder strb, ReadOnlySpan<char> format, int index)
    {
        strb.Append(format[index]);
        return 1;
    }

    /// <summary>
    /// Appends the raw next character or nothing if at the end for <paramref name="format"/>.
    /// </summary>
    /// <returns>The width of appended part (1 or 2).</returns>
    /// <inheritdoc cref="AppendOther(StringBuilder, ReadOnlySpan{char}, int)"/>
    public static int AppendEscaped(StringBuilder strb, ReadOnlySpan<char> format, int index)
    {
        if (index < format.Length - 1)
        {
            return 1;
        }
        strb.Append(format[index + 1]);
        return 2;
    }
    private static StringBuilder AppendPaddedInteger(StringBuilder sb, int number, int minWidth) =>
        sb.Append(number.ToString(CultureInfo.InvariantCulture).PadRight(minWidth, '0'));

    private static StringBuilder AppendPaddeFractionalPart(
        StringBuilder sb,
        double number,
        int minWidth
    )
    {
        // .0000... will take care of padding/trimming, at the cost of a decimal separator at the beginning.
        string fmt = "." + new string('0', minWidth);
        var formatted = (Math.Abs(number) / 1000).ToString(fmt, CultureInfo.InvariantCulture);
        // remove the first element to get the number
        return sb.Append(formatted[1..]);
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
    private static int GetTimeValueByChar(CueTime cueTime, char character)
    {
        return character switch
        {
            'm' or 'M' => Math.Abs(cueTime.Minutes),
            's' or 'S' => Math.Abs(cueTime.Seconds),
            'f' or 'F' => Math.Abs(cueTime.Frames),
            _ => 0
        };
    }
}
