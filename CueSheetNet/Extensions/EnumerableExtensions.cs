using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Extensions;
internal static class EnumerableExtensions
{
    /// <typeparam name="T">Nullable reference type.</typeparam>
    /// <inheritdoc cref="AddNotNull{T}(IList{T}, T?)"/>
    public static void AddNotNull<T>(this IList<T> list, T? item) where T : class
    {
        if (item is null)
            return;
        list.Add(item);
    }
    /// <summary>
    /// Adds the item to <paramref name="list"/> if it is not null.
    /// </summary>
    /// <typeparam name="T">Nullable value type.</typeparam>
    /// <param name="list"></param>
    /// <param name="item">Possibly null item.</param>
    public static void AddNotNull<T>(this IList<T> list, T? item) where T: struct
    {
        if (item is null)
            return;
        list.Add(item.Value);
    }
}
