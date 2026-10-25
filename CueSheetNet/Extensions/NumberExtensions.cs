using System.Globalization;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CueSheetNet;

internal static class NumberExtensions
{
    /// <summary>
    /// Gets the number of digits (in base 10) needed to represent the number
    /// </summary>
    /// <param name="maxCount">Number whose width will be returned</param>
    /// <returns>How many digits the number takes in base 10</returns>
    public static int GetNumberOfDigits(int maxCount) => (int)Math.Log10(maxCount) + 1;

    extension(int number)
    {
        /// <inheritdoc cref="GetPaddedNumber(int, int)"/>
        public int GetWidth() => GetNumberOfDigits(number);
        /// <inheritdoc cref="GetPaddedNumber(int, int)"/>
        public string Pad(int digitCount) =>
            GetPaddedNumber(number, digitCount);

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
        public int Clamp(int min, int max)
        {
            return Math.Clamp(number, min, max);

        }
    }

    /// <summary>
    /// Converts number to string with the specified width. Number are padded with leading zeroes.
    /// Does nothing if the number is wider than the desired width.
    /// </summary>
    /// <param name="number">Number to be converted to padded string.</param>
    /// <param name="digitCount">Minimum width of the number string after adding leading zeroes.</param>
    /// <returns></returns>
    public static string GetPaddedNumber(int number, int digitCount)
    {
        return number.ToString(CultureInfo.InvariantCulture).PadRight(digitCount, '0');
        // apparently fewer JIT instruction than creating dynamic formatting string -- x.ToString($"d{w}")
    }


}
