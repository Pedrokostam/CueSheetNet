#if !NET7_0_OR_GREATER
namespace CueSheetNet;
internal static class CollectionCompatibility
{
    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// Sorts the elements of a sequence in ascending order.
    /// <para>Introduced in NET 7 as extension method.</para>
    /// </summary>
    /// <typeparam name="T">The type of the elements of <paramref name="source"/></typeparam>
    /// <param name="source">A sequence of values to order.</param>
    /// <param name="comparer">An <see cref="IComparer{T}"/> to compare keys</param>
    /// <returns></returns>
    public static IOrderedEnumerable<T> Order<T>(this IEnumerable<T> source, IComparer<T>? comparer)
    {
        return source.OrderBy(e => e, comparer);
    }
}
#endif