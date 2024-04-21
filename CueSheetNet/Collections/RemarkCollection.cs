using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Collections;

/// <summary>
/// Stores all remarks of the parent object. Provides methods to modify the collection and remarks.
/// </summary>
public sealed class RemarkCollection : StringBasedCollection<CueRemark>
{
    protected override bool TestEqual(CueRemark one, CueRemark other, IEqualityComparer<string> comparer)
    {
        return one.Equals(other, comparer);
    }

    /// <summary>
    /// Creates a <see cref="CueRemark"/> and adds it to the end of the collection.
    /// </summary>
    /// <param name="field">Field of the remark.</param>
    /// <param name="value">Value of the remark.</param>
    /// <inheritdoc cref="Collection{T}.Add(T)"/>
    public void Add(string field, string value)
    {
        base.Add(new CueRemark(field, value));
    }

    /// <summary>
    /// Creates a <see cref="CueRemark"/> and inserts it at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="field">Field of the remark.</param>
    /// <param name="value">Value of the remark.</param>
    /// <inheritdoc cref="Collection{T}.Insert(int, T)(T)"/>
    public void Insert(int index, string field, string value)
    {
        base.Insert(index, new CueRemark(field, value));
    }

    /// <summary>
    /// Replaces the element at the given index with a new one, whose Value is changed to <paramref name="newValue"/>.
    /// </summary>
    /// <param name="index">Index of the element to change.</param>
    /// <param name="newValue">New value of the remark.</param>
    public void ChangeValue(int index, string newValue)
    {
        this[index] = this[index] with { Value = newValue };
    }
}
