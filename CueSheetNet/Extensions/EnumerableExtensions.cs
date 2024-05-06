using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Extensions;
internal static class EnumerableExtensions
{
    public static IEnumerable<(T?,T?)> ZipLongestReference<T>(this IEnumerable<T> first,  IEnumerable<T> second) where T:class
    {
        using var enum1 = first.GetEnumerator();
        using var enum2 = second.GetEnumerator();
        while (true)
        {
            bool enum1Good = enum1.MoveNext();
            bool enum2Good = enum2.MoveNext();
            if(!enum1Good && !enum2Good)
            {
                yield break;
            }
            T? enum1Item = enum1Good ? enum1.Current : default;
            T? enum2Item = enum2Good ? enum2.Current : default;
            yield return (enum1Item, enum2Item);
        }
    }

    public static IEnumerable<(T?, T?)> ZipLongestValue<T>(this IEnumerable<T> first, IEnumerable<T> second) where T:struct
    {
        using var enum1 = first.GetEnumerator();
        using var enum2 = second.GetEnumerator();
        while (true)
        {
            bool enum1Good = enum1.MoveNext();
            bool enum2Good = enum2.MoveNext();
            if (!enum1Good && !enum2Good)
            {
                yield break;
            }
            T? enum1Item = enum1Good ? enum1.Current : new T?();
            T? enum2Item = enum2Good ? enum2.Current : new T?();
            yield return (enum1Item, enum2Item);
        }
    }

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
