using System.Globalization;
using System.Runtime.CompilerServices;

namespace CueSheetNet;

internal static class NumberExtensions
{
    /// <summary>
    /// Gets the number of digits (in base 10) needed to represent the number
    /// </summary>
    /// <param name="maxCount">Number whose width will be returned</param>
    /// <returns>How many digits the number takes in base 10</returns>
    public static int GetNumberOfDigits(int maxCount) => (int)Math.Log10(maxCount) + 1;

    /// <inheritdoc cref="GetPaddedNumber(int, int)"/>
    public static int GetWidth(this int maxCount) => GetNumberOfDigits(maxCount);

    /// <summary>
    /// Converts number to string with the specified width. Number are padded with leading zeroes.
    /// Does nothing if the number is wider than the desired width.
    /// </summary>
    /// <param name="number">Number to be converted to padded string.</param>
    /// <param name="digitCount">Minimum width of the number string after adding leading zeroes.</param>
    /// <returns></returns>
    public static string GetPaddedNumber(int number, int digitCount)
    {
        return number.ToString(CultureInfo.InvariantCulture).PadLeft(digitCount, '0');
        // apparently fewer JIT instruction than creating dynamic formatting string -- x.ToString($"d{w}")
    }

    /// <inheritdoc cref="GetPaddedNumber(int, int)"/>
    public static string Pad(this int number, int digitCount)
    {
        return GetPaddedNumber(number, digitCount);
    }

    /// <summary>
    /// Clamps the given value, making it fit in the given range.
    /// <see cref="Math.Clamp(int, int, int)"/> is functionally the same, but it is not present before NET 6.0.
    /// </summary>
    /// <param name="number"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">When the minimum is greater than maximum.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(this int number, int min, int max)
    {
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return Math.Clamp(number, min, max);
#else
        if (min > max)
        {
            throw new ArgumentException("Minimum cannot be greater than maximum");
        }
        if (number < min)
        {
            return min;
        }
        if (number > max)
        {
            return max;
        }
        return number;
#endif
    }
}
