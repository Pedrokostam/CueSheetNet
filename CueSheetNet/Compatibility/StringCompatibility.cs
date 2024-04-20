#if !NET6_0_OR_GREATER // NET 6 contains native implementation of all below methods
using System.Data;

namespace CueSheetNet;
internal static class StringCompatibility
{
    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// Copies the contents of this string into the destination span.
    /// <para>Introduced in NET 6 as instance method.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="destination">The span into which to copy this string's contents.</param>
    /// <exception cref="ArgumentException">The destination span is shorter than the source string.</exception>
    public static void CopyTo(this string input, Span<char> destination)
    {
        if (destination.Length < input.Length)
            throw new ArgumentException("Target span is too short", nameof(destination));
        for (int i = 0; i < input.Length; i++)
        {
            destination[i] = input[i];
        }
    }
#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER)
    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// Determines whether this string instance starts with the specified value.
    /// <para>Introduced in NET Core 2.1 and NETStandard2.1 as instance method.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="value">The character to seek.</param>
    /// <param name="comparison">One of the enumeration values that specifies the rules to use in the comparison.</param>
    /// <returns><see langword="true"/> if the <paramref name="value"/> parameter occurs within this string; otherwise, <see langword="false"/></returns>
    public static bool Contains(this string input, char value, StringComparison comparison)
    {
        return input.IndexOf(value.ToString(), comparison) != -1;
    }
#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER)
    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another specified string, using the provided culture and case sensitivity.
    /// <para>This compatibility method only allows <see cref="StringComparison.Ordinal"/>.</para>
    /// <para>Introduced in NET Core 2.0 and NETStandard2.1 as instance method.</para>
    /// Returns a new string in which all occurrences of a specified string in the current instance are replaced with another specified string, using the provided comparison type.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="oldValue">The string to be replaced.</param>
    /// <param name="newValue">The string to replace all occurrences of <paramref name="oldValue"/>/>.</param>
    /// <param name="comparison">One of the enumeration values that determines how <paramref name="oldValue"/> is searched within this instance.</param>
    /// <returns>A string that is equivalent to the current string except that all instances of <paramref name="oldValue"/> are replaced with <paramref name="newValue"/>. If <paramref name="oldValue"/> is not found in the current instance, the method returns the current instance unchanged.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "MA0001:StringComparison is missing", Justification = "This method is the implementation with StringComparison")]
    public static string Replace(this string input, string oldValue, string newValue, StringComparison comparison)
    {
        if (comparison != StringComparison.Ordinal)
        {
            throw new ArgumentException("Comparison method in this extension must be Ordinal", nameof(comparison));
        }
        return input.Replace(oldValue, newValue);
    }
    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// Splits a string into substrings based on a specified delimiting value and, optionally, options.
    /// <para>Introduced in NET Core 2.0 and NETStandard2.1 as instance method.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="separator">A value that delimits the substrings in this string.</param>
    /// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.</param>
    /// <returns>An array whose elements contain the substrings from this instance that are delimited by <paramref name="separator"/>.</returns>
    public static string[] Split(this string input, char separator, StringSplitOptions options)
    {
        char[] separators = [separator];
        return input.Split(separators, options);
    }

    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// Returns the hash code for this string using the specified rules.
    /// <para>Introduced in NET Core 2.0 and NETStandard2.1 as instance method.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="comparison">One of the enumeration values that specifies the rules to use in the comparison.</param>
    /// <returns>A 32-bit signed integer hash code.</returns>
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

    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// Returns a value indicating whether a specified character occurs within this string, using the specified comparison rules.
    /// <para>Introduced in NET Core 2.0 and NETStandard2.1 as instance method.</para>
    /// </summary>
    /// <remarks>This method performs an ordinal (case-sensitive and culture-insensitive) comparison.</remarks>
    /// <param name="input"></param>
    /// <param name="value">The value to compare.</param>
    /// <returns><see langword="true"/> if <paramref name="value"/> matches the beginning of this string; otherwise, <see langword="false"/></returns>
    public static bool StartsWith(this string input, char value)
    {
        return input.IndexOf(value) == 0;
    }
#endif
#endif
}
#endif
