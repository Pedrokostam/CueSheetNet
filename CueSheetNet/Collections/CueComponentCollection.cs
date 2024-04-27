//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CueSheetNet.Collections;
//public abstract class CueComponentCollection<IItem> : IList<IItem>
//{
//    protected readonly IList<IItem> _components;

//    protected CueComponentCollection( int initialCapacity)
//    {
//        _components = new List<IItem>(initialCapacity);
//    }


//    public IItem this[int index]
//    {
//        get => _components[index];
//        //set => _components[index] = value;
//    }

//    public int Count => _components.Count;

//    public bool IsReadOnly => false;

//    public  void Add(IItem item) => _components.Add(item);

//    public void Clear() => _components.Clear();

//    public  bool Contains(IItem item) => _components.Contains(item);
//    public  bool Contains(IItem item,IEqualityComparer<IItem> comparer) => _components.Contains(item,comparer);

//    public void CopyTo(IItem[] array, int arrayIndex) => _components.CopyTo(array, arrayIndex);

//    public IEnumerator<IItem> GetEnumerator() => _components.GetEnumerator();

//    public int IndexOf(IItem item) => _components.IndexOf(item);

//    public void Insert(int index, IItem item) => _components.Insert(index, item);

//    public bool Remove(IItem item) => _components.Remove(item);

//    public void RemoveAt(int index) => _components.RemoveAt(index);

//    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

//    private bool SequenceEquals_Ordered(IEnumerable<IItem> other, IEqualityComparer<IItem> comparer)
//    {
//        int index = 0;
//        foreach (var item in other)
//        {
//            if (!comparer.Equals(item, this[index]))
//            {
//                return false;
//            }
//            index++;
//        }
//        // If wee went through fewer elements than this collection has, return false
//        if (index != Count)
//            return false;
//        return true;
//    }

//    private bool SequenceEquals_Unordered(IEnumerable<IItem> other, IEqualityComparer<IItem> comparer)
//    {
//        foreach (var thatItem in other)
//        {
//            bool exists = this.Any(x => comparer.Equals(x, thatItem));
//            if (!exists)
//                return false;
//        }
//        return true;
//    }

//    protected bool SequenceEquals(IEnumerable<IItem>? other, bool unordered, IEqualityComparer<IItem>? comparer)
//    {
//        if (other is null)
//            return false;

//        if (other is ICollection col && col.Count != Count)
//            return false;

//        comparer ??= EqualityComparer<IItem>.Default;

//        return unordered ? SequenceEquals_Unordered(other, comparer) : SequenceEquals_Ordered(other, comparer);
//    }
//}