using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public static int GetWidth(this int maxCount)=>GetNumberOfDigits(maxCount);
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
    /// <inheritdoc cref="GetPaddedNumber(int, int)"/>
    public static string Pad(this int number, int digitCount) =>GetPaddedNumber(number, digitCount);   
}
