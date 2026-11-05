using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CueSheetNet.Extensions;

internal static class StreamExtensions
{
    extension(Stream stream)
    {
        public IList<byte> GetBytes()
        {
            try
            {
                var arr = new byte[stream.Length - stream.Position];
                using var memory = new MemoryStream(arr);
                stream.CopyTo(memory);
                return memory.GetBuffer();

            }
            catch (NotSupportedException)
            {
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }
    }
}
/// <summary>
/// Provides extension methods for collections and enumerables.
/// </summary>
internal static class CollectionExtensions
{
    extension<T>(IReadOnlyList<T> readOnlyList) where T : class
    {
        /// <summary>
        /// Searches for the first occurrence of the specified item using reference equality.
        /// </summary>
        /// <param name="item">The item to locate.</param>
        /// <returns>
        /// The zero-based index of the first element that references the specified item, or <c>-1</c> if not found.
        /// </returns>
        public int IndexOfRef(T item)
        {
            for (int i = 0; i < readOnlyList.Count; i++)
            {
                if (ReferenceEquals(item, readOnlyList[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    extension<T>(IReadOnlyList<T> readOnlyList)
    {
        /// <summary>
        /// Searches for the first element that matches the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to evaluate for each element.</param>
        /// <returns>
        /// The zero-based index of the first matching element, or <c>-1</c> if no element matches.
        /// </returns>
        public int FindIndex(Predicate<T> predicate)
        {
            for (int i = 0; i < readOnlyList.Count; i++)
            {
                if (predicate(readOnlyList[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    extension<T>(IList<T> list) where T : class
    {
        /// <summary>
        /// Searches for the first occurrence of the specified item using reference equality.
        /// </summary>
        /// <param name="item">The item to locate.</param>
        /// <returns>
        /// The zero-based index of the first element that references the specified item, or <c>-1</c> if not found.
        /// </returns>
        public int IndexOfRef(T item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(item, list[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    extension<T>(IList<T> list)
    {
        /// <summary>
        /// Searches for the first element that matches the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to evaluate for each element.</param>
        /// <returns>
        /// The zero-based index of the first matching element, or <c>-1</c> if no element matches.
        /// </returns>
        public int FindIndex(Predicate<T> predicate)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (predicate(list[i]))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    extension<T>(IEnumerable<T> enumerable) where T : class
    {

        /// <summary>
        /// Searches for the first occurrence of the specified item using reference equality.
        /// </summary>
        /// <param name="item">The item to locate.</param>
        /// <returns>
        /// The zero-based index of the first element that references the specified item, or <c>-1</c> if not found.
        /// </returns>
        public int IndexOfRef(T item)
        {
            int i = 0;
            foreach (T e in enumerable)
            {
                if (ReferenceEquals(item, e))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }
    }

    extension<T>(IEnumerable<T> enumerable)
    {
        /// <summary>
        /// Searches for the first element that matches the specified predicate.
        /// </summary>
        /// <param name="predicate">The predicate to evaluate for each element.</param>
        /// <returns>
        /// The zero-based index of the first matching element, or <c>-1</c> if no element matches.
        /// </returns>
        public int FindIndex(Predicate<T> predicate)
        {
            int i = 0;
            foreach (T e in enumerable)
            {
                if (predicate(e))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate and returns the index
        /// of the element that follows it.
        /// </summary>
        /// <param name="predicate">The Predicate<T> delegate that defines the conditions of the element to search for.</param>
        /// <returns>The zero-based index of the element after the found chunk; otherwise, -1 if no such element is found or if
        /// the found chunk is the last in the collection.</returns>
        public int FindIndexAfter(Predicate<T> predicate)
        {
            int i = 0;
            bool predicateReached = false;
            foreach (var item in enumerable)
            {
                bool match = predicate(item);
                if (predicateReached && !match)
                {
                    return i;
                }
                predicateReached |= match;
                i++;
            }
            return -1;
        }
    }

}