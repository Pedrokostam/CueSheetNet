#if !NET6_0_OR_GREATER // NET 6 contains native implementation of all below methods
namespace CueSheetNet;
internal static class StringExtensions
{
#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER)
    /// <summary>
    /// Extension method for below NETCORE 2.0 (StringComparison parameter was missing).
    /// <para/>
    /// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another specified string, using the provided comparison type.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="toReplace"></param>
    /// <param name="replacement"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static string Replace(this string input, string toReplace, string replacement, StringComparison comparison)
    {
        if (comparison != StringComparison.Ordinal)
        {
            throw new ArgumentException("Comparison method in this extension must be Ordinal", nameof(comparison));
        }
#pragma warning disable MA0001 // StringComparison is missing - This framework does not have an overload with StringComparison
        return input.Replace(toReplace, replacement);
#pragma warning restore MA0001 // StringComparison is missing
    }
    /// <summary>
    /// Extension method for below NETCORE 2.0 (StringSplitOptions parameter was missing).
    /// <para/>
    /// Splits a string into substrings based on a specified delimiting character and, optionally, options.
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

    /// <summary>
    /// Extension method for below NETCORE 2.0 (StringComparison parameter was missing).
    /// <para/>
    /// Returns the hash code for this string using the specified rules.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="value"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static int GetHashCode(this string input, StringComparison comparison)
    {
        var comparer = comparison switch
        {
            StringComparison.CurrentCulture => StringComparer.CurrentCulture,
            StringComparison.CurrentCultureIgnoreCase => StringComparer.CurrentCultureIgnoreCase,
            StringComparison.InvariantCulture => StringComparer.InvariantCulture,
            StringComparison.InvariantCultureIgnoreCase => StringComparer.InvariantCultureIgnoreCase,
            StringComparison.Ordinal => StringComparer.Ordinal,
            StringComparison.OrdinalIgnoreCase => StringComparer.OrdinalIgnoreCase,
            _ => throw new NotSupportedException(),
        };
        return comparer.GetHashCode(input);
    }

#endif
#if !NETCOREAPP2_1_OR_GREATER
    public static bool StartsWith(this string input, char character)
    {
        if (input.Length == 0) return false;
        return input[0] == character;
    }

    /// <summary>
    /// Extension method for below NETCORE 2.0 (StringSplitOptions parameter was missing).
    /// <para/>
    /// Returns a value indicating whether a specified character occurs within this string, using the specified comparison rules.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="value"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static bool Contains(this string input, char value, StringComparison comparison)
    {
        return input.IndexOf(value.ToString(), comparison) != -1;
    }
#endif
#if !NET6_0_OR_GREATER
    public static void CopyTo(this string input, Span<char> destination)
    {
        if (destination.Length < input.Length)
            throw new ArgumentException("Target span is too short", nameof(destination));
        for (int i = 0; i < input.Length; i++)
        {
            destination[i] = input[i];
        }
    }
#endif
}
//#endif
