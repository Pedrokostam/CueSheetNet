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
    /// <param name="maxCount"></param>
    /// <returns>How many digits the number takes in base 10</returns>
    public static int GetNumberOfDigits(int maxCount) => (int)Math.Log10(maxCount) + 1;
    /// <summary>
    /// Converts number to string with the specified width
    /// </summary>
    /// <param name="number"></param>
    /// <param name="digitCount"></param>
    /// <returns></returns>
    public static string GetPaddedNumber(int number, int digitCount)
    {
        return number.ToString(CultureInfo.InvariantCulture).PadRight(digitCount, '0');
        // apparently fewer JIT instruction than creating dynamic formatting string -- x.ToString($"d{w}")
    }
    /// <inheritdoc cref="GetPaddedNumber(int, int)"/>
    /// <returns></returns>
    public static string Pad(this int number, int digitCount) =>GetPaddedNumber(number, digitCount);   
}
