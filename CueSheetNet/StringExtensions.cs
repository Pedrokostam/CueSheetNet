using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;
internal static class StringExtensions
{

#if !NETCOREAPP2_0_OR_GREATER
    /// <summary>
    /// Replacement before NETCORE 2 was always ordina. This extensions allows us to provide the comparison which will ignored (we don't have to make a different call)
    /// </summary>
    /// <param name="input"></param>
    /// <param name="toReplace"></param>
    /// <param name="replacement"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0001:StringComparison is missing", Justification = "Misdetected, there is no overload")]
    public static string Replace(this string input, string toReplace, string replacement,StringComparison comparison)
    {
        if (comparison != StringComparison.Ordinal)
        {
            throw new ArgumentException("Comparison method in this extension must be Ordinal", nameof(comparison));
        }
        return input.Replace(toReplace, replacement);
    }
    /// <summary>
    /// Extension method for beloew NETCORE 2, which accepts one char separator and splitoptions.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="separator"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static string[] Split(this string input, char separator, StringSplitOptions options)
    {
        var separators = new char[] { separator };
        return input.Split(separators, options);
    }
#endif
}
