#if !NET6_0_OR_GREATER // SequenceEqual with IComparer were introduced in NET 6
namespace CueSheetNet;
internal static class SpanCompatibility
{
    /// <summary>
    /// <para>COMPATIBILITY</para>
    /// Determines whether two sequences are equal by comparing the elements using an <see cref="IEqualityComparer{T}"/>.
    /// <para>Introduced in NET 6 as extension.</para>
    /// </summary>
    /// <typeparam name="T">The type of elements in the sequence.</typeparam>
    /// <param name="span">The first sequence to compare.</param>
    /// <param name="other">The second sequence to compare.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when comparing elements, or <see langword="null"/> to use the default <see cref="IEqualityComparer{T}"/> for the type of an element.</param>
    /// <returns><see langword="true"/> if the two sequences are equal; otherwise, <see langword="false"/>.</returns>
    public static bool SequenceEqual<T>(this Span<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null) where T : notnull
    {
        return ((ReadOnlySpan<T>)span).SequenceEqual(other, comparer);
    }

    /// <inheritdoc cref="SequenceEqual{T}(Span{T}, ReadOnlySpan{T}, IEqualityComparer{T}?)"/>
    private static bool SequenceEqual<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> other, IEqualityComparer<T>? comparer = null) where T : notnull
    {
        if (span.Length != other.Length)
        {
            return false;
        }
        comparer ??= EqualityComparer<T>.Default;
        for (int i = 0; i < span.Length; i++)
        {
            if (!comparer.Equals(span[i], other[i]))
            {
                return false;
            }

        }
        return true;
    }
}
#endif