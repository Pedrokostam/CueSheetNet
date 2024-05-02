using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Collections;
internal class ReferenceKeyedCollection<T> : Collection<T> where T : class
{
    readonly Dictionary<T,int> ItemsPositions= new Dictionary<T, int>(ReferenceEqualityComparer.Instance);

    public int this[T item] => ItemsPositions[item];
    public int GetIndexOrNegative(T item)
    {
        if(ItemsPositions.TryGetValue(item, out int index))
        {
            return index;
        }

        return -1;
    }
    private void UpdateIndexes(int inclusiveStartIndex)
    {
        for (int i = inclusiveStartIndex; i < Items.Count; i++)
        {
            ItemsPositions[Items[i]] = i;
        }
    }

    protected override void InsertItem(int index, T item)
    {
        base.InsertItem(index, item);
        UpdateIndexes(index);
    }
    protected override void RemoveItem(int index)
    {
        base.RemoveItem(index);
        UpdateIndexes(index);
    }

    public int GetIndexOfPrevious(T item) => GetIndexOfShifted(item, -1);
    public int GetIndexOfNext(T item) => GetIndexOfShifted(item, 1);

    public T? GetNextItem(T item)
    {
        var index = GetIndexOfNext(item);
        return index < 0 ? null : Items[index];
    }

    public IEnumerable<T> GetNextItems(T item)
    {
        var index = GetIndexOfNext(item);
        if(index < 0)
        {
            return [];
        }
        return Items.Skip(index);
    }

    public IEnumerable<T> GetPreviousItems(T item)
    {
        var index = GetIndexOfNext(item);
        if (index < 0)
        {
            return [];
        }
        return Items.Take(index);
    }

    public T? GetPreviousItem(T item)
    {
        var index = GetIndexOfPrevious(item);
        return index < 0 ? null : Items[index];
    }

    public int GetIndexOfShifted(T item, int shift)
    {
        if (item is null)
        {
            return -1;
        }
        if (ItemsPositions.TryGetValue(item, out var index))
        {
            index += shift;
            if (index < 0 || index >= Count)
            {
                return -1;
            }
            return index;
        }
        return -1;
    }
}
