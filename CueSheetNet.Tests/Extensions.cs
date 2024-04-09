using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;
internal static class Extensions
{
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

    /// <summary>
    /// Extension method for below NETCORE 2.0 (StringSplitOptions parameter was missing).
    /// <para/>
    /// Returns a value indicating whether a specified character occurs within this string, using the specified comparison rules.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="value"></param>
    /// <param name="comparison"></param>
    /// <returns></returns>
    public static bool Contains(this string input, string value, StringComparison comparison)
    {
        return input.IndexOf(value, comparison) != -1;
    }

    public static T[] Prepend<T>(this IEnumerable<T> collection, T toPrepend)
    {
        var list = new List<T>(collection);
        list.Insert(0, toPrepend);
        return [.. list];
    }

    /// <summary>
    /// The last item 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collection"></param>
    /// <param name="toPrepend">Collection to be prepended. The last element will be the first.</param>
    /// <returns></returns>
    public static T[] Prepend<T>(this IEnumerable<T> collection, params T[] toPrepend)
    {
        var list = new List<T>(collection);
        foreach (var item in toPrepend)
        {
            list.Insert(0, item);
        }
        return [.. list];
    }
}
