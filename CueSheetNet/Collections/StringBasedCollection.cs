using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CueSheetNet.Collections;
using CueSheetNet.Extensions;

namespace CueSheetNet.Collections;
/// <summary>
/// Base class for collection of items, which can utilize <see cref="string"/> <see cref="IEqualityComparer{T}"/> and <see cref="StringComparison"/> in their Equals method.
/// <para>
/// Default string comparer is <see cref="StringComparer.Ordinal"/>.</para>
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class StringBasedCollection<T> : Collection<T>
{

    /// <summary>
    /// Add all elements present in <paramref name="items"/> to this collection.
    /// </summary>
    /// <param name="items">An enumerable of <typeparamref name="T"/> objects</param>
    public void Add(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            base.Add(item);
        }
    }
    /// <summary>
    /// Checks whether the given <paramref name="item"/> is in this collection using <paramref name="comparer"/> to test eqaulity of strings.
    /// </summary>
    /// <param name="comparer">String comparer to be used for testing strings in the collection.</param>
    /// <inheritdoc cref="Collection{T}.Contains(T)"/>
    public virtual bool Contains(T item, IEqualityComparer<string>? comparer)
    {
        if (comparer is null || comparer == StringComparer.Ordinal)
        {
            return base.Contains(item);
        }
        comparer ??= StringComparer.Ordinal;
        var found = this.FirstOrDefault(x => TestEqual(x,item,comparer));
        return !Equals(found, default);
    }

    /// <summary>
    /// Checks whether the given <paramref name="item"/> is in this collection using <paramref name="comparisonType"/> to test eqaulity of strings.
    /// </summary>
    /// <param name="comparisonType"><see cref="StringComparison"/> type to be used for string equality testing.</param>
    /// <inheritdoc cref="Collection{T}.Contains(T)"/>
    public virtual bool Contains(T item, StringComparison comparisonType)
    {
        return Contains(item, StringHelper.GetComparer(comparisonType));
    }

    /// <summary>
    /// Searches for the specified object (using <paramref name="comparer"/> to test strings) and returns its zero-based index in this collection.
    /// </summary>
    /// <param name="comparer">String comparer to be used for testing strings in the collection.</param>
    /// <inheritdoc cref="Collection{T}.IndexOf(T)"/>
    public virtual int IndexOf(T item, IEqualityComparer<string>? comparer)
    {
        if (comparer is null || comparer == StringComparer.Ordinal)
        {
            return base.IndexOf(item);
        }
        int index = 0;
        foreach (var @this in this)
        {
            if (TestEqual(@this, item, comparer))
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    /// <summary>
    /// Searches for the specified object (using <paramref name="comparisonType"/> to test strings) and returns its zero-based index in this collection.
    /// </summary>
    /// <param name="comparisonType"><see cref="StringComparison"/> type to be used for string equality testing.</param>
    /// <inheritdoc cref="Collection{T}.IndexOf(T)"/>
    public virtual int IndexOf(T item, StringComparison comparisonType)
    {
        return IndexOf(item, StringHelper.GetComparer(comparisonType));
    }

    /// <summary>
    /// Removes the first occurence of a specific object. Uses <paramref name="comparisonType"/> to test strings.
    /// </summary>
    /// <param name="comparisonType"><see cref="StringComparison"/> type to be used for string equality testing.</param>
    /// <inheritdoc cref="Collection{T}.Remove(T)"/>
    public virtual void Remove(T item, StringComparison comparisonType)
    {
        Remove(item, StringHelper.GetComparer(comparisonType));
    }

    /// <summary>
    /// Removes the first occurence of a specific object. Uses <paramref name="comparer"/> to test strings.
    /// </summary>
    /// <param name="comparer">String comparer to be used for testing strings in the collection.</param>
    /// <inheritdoc cref="Collection{T}.Remove(T)"/>
    public virtual void Remove(T item, IEqualityComparer<string>? comparer)
    {
        if (comparer is null || comparer == StringComparer.Ordinal)
        {
            base.Remove(item);
        }
        else
        {
            int index = IndexOf(item,comparer);
            if (index != -1)
            {
                RemoveAt(index);
            }
        }
    }

    /// <summary>
    /// Used to tests object equality using the specified comparer.
    /// <para>
    /// The derived class must implement it, because <typeparamref name="T"/> does not necessarilly need to be a <see cref="string"/>.</para>
    /// </summary>
    /// <param name="one">The item to test.</param>
    /// <param name="other">The seconds item to test.</param>
    /// <param name="comparer">String comparer used in comparison.<para/>The comparer is guaranteed to be not-null.</param>
    /// <returns>
    /// <see langword="true"/> if the objects are equal, otherwise <see langword="false"/>
    /// </returns>
    protected abstract bool TestEqual(T one, T other, IEqualityComparer<string> comparer);

    /// <param name="comparisonType"></param>
    /// <param name="comparisonType"><see cref="StringComparison"/> type to be used for string equality testing.</param>
    /// <inheritdoc cref="SequenceEqual(IEnumerable{T}, IEqualityComparer{string}?)"/>
    public virtual bool SequenceEqual(IEnumerable<T> other, StringComparison comparisonType)
    {
        return SequenceEqual(other, StringHelper.GetComparer(comparisonType));
    }

    /// <summary>
    /// Test if this sequence has the same items (by value) in the same order as the <paramref name="other"/>  sequence.
    /// </summary>
    /// <param name="other">The sequence to compare with.</param>
    /// <param name="comparer">String comparer used in comparison</param>
    /// <returns>
    /// <see langword="true"/> if the sequences are equal, otherwise <see langword="false"/>
    /// </returns>
    public virtual bool SequenceEqual(IEnumerable<T> other, IEqualityComparer<string>? comparer)
    {
        if (comparer is null || comparer == StringComparer.Ordinal)
        {
            return Items.SequenceEqual(other);
        }
        if (other is ICollection<T> coll && coll.Count != Count)
        {
            return false;
        }
        int index = 0;
        foreach (T item in other)
        {
            if (!TestEqual(this[index], item, comparer))
            {
                return false;
            }
            index++;
        }
        if (index != Count)
        {
            return false;
        }
        return true;
    }
}
