using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.Components;

public class Remarks : IList<Remark>
{
    private readonly List<Remark> _items = [];

    public bool TryGetField<T>(string field, [NotNullWhen(returnValue: true)] out T? result) where T : IParsable<T>
    {
        var remark = _items
            .OfType<FieldRemark>()
            .LastOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(field, x.Field));
        if (remark is null)
        {
            result = default;
            return false;
        }
        return remark.TryGet(out result);
    }
    public string? GetField(string field)
    {
        return _items
            .OfType<FieldRemark>()
            .LastOrDefault(x => StringComparer.OrdinalIgnoreCase.Equals(field, x.Field))?.Value;
    }
    public void Add(string line) => _items.Add(Remark.Create(line));
    public void Add(string field, string value) => _items.Add(Remark.Create(field, value));
    public void AddUnique(string field, string value)
    {
        _items.RemoveAll(x => x is FieldRemark f && f.Field.Equals(field, StringComparison.OrdinalIgnoreCase));
        _items.Add(Remark.Create(field, value));
    }
    public bool RemoveField(string field)
    {
        return _items.RemoveAll(x => x is FieldRemark f && f.Field.Equals(field, StringComparison.OrdinalIgnoreCase)) > 0;
    }

    public IEnumerable<FieldRemark> GetFieldRemarks() => _items.OfType<FieldRemark>();

    public Remark this[int index]
    { get => ((IList<Remark>)_items)[index]; set => ((IList<Remark>)_items)[index] = value; }

    public int Count => ((ICollection<Remark>)_items).Count;

    public bool IsReadOnly => ((ICollection<Remark>)_items).IsReadOnly;

    public void Add(Remark item)
    {
        ((ICollection<Remark>)_items).Add(item);
    }

    public void Clear()
    {
        ((ICollection<Remark>)_items).Clear();
    }

    public bool Contains(Remark item)
    {
        return ((ICollection<Remark>)_items).Contains(item);
    }

    public void CopyTo(Remark[] array, int arrayIndex)
    {
        ((ICollection<Remark>)_items).CopyTo(array, arrayIndex);
    }

    public IEnumerator<Remark> GetEnumerator()
    {
        return ((IEnumerable<Remark>)_items).GetEnumerator();
    }

    public int IndexOf(Remark item)
    {
        return ((IList<Remark>)_items).IndexOf(item);
    }

    public void Insert(int index, Remark item)
    {
        ((IList<Remark>)_items).Insert(index, item);
    }

    public bool Remove(Remark item)
    {
        return ((ICollection<Remark>)_items).Remove(item);
    }

    public void RemoveAt(int index)
    {
        ((IList<Remark>)_items).RemoveAt(index);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_items).GetEnumerator();
    }
}

