﻿using System;
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
}