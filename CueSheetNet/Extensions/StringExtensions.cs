using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Extensions;
internal static class StringExtensions
{
    /// <summary>
    /// Performs string comparison using <see cref="StringComparer.Ordinal"/>
    /// </summary>
    /// <inheritdoc cref="string.Equals(string?, string?)"/>
    /// <returns></returns>
    public static bool OrdEquals(this string? a, string? b)
    {
        return StringComparer.Ordinal.Equals(a, b);
    }

    
}
