using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;
#if !NET6_0_OR_GREATER // SequenceEqual with IComparer were introduced in NET 6
internal static class SpanExtensions
{
    // SequenceEqual with IComparer were introduced in NET 6
    public static bool SequenceEqual<T>(this Span<T> dataBuffer, ReadOnlySpan<T> template, IEqualityComparer<T>? comparer = null) where T : notnull
    {
        return ((ReadOnlySpan<T>)dataBuffer).SequenceEqual(template, comparer);
    }
    private static bool SequenceEqual<T>(this ReadOnlySpan<T> dataBuffer, ReadOnlySpan<T> template, IEqualityComparer<T>? comparer = null) where T : notnull
    {
        bool sequenceEqual = dataBuffer.Length == template.Length;
        if (sequenceEqual)
        {
            for (int i = 0; i < dataBuffer.Length; i++)
            {
                if (comparer is not null)
                {
                    sequenceEqual = comparer.Equals(dataBuffer[i], template[i]);
                }
                else
                {
                    sequenceEqual = (dataBuffer[i].Equals(template[i]));
                }
                if (!sequenceEqual) break;
            }
        }
        return sequenceEqual;
    }
}
#endif