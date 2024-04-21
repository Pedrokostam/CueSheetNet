//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CueSheetNet.Collections;
//public abstract class CueComponentCollection<T> : IList<T>
//{
//    protected readonly IList<T> _components;

//    protected CueComponentCollection( int initialCapacity)
//    {
//        _components = new List<T>(initialCapacity);
//    }


//    public T this[int index]
//    {
//        get => _components[index];
//        //set => _components[index] = value;
//    }

//    public int Count => _components.Count;

//    public bool IsReadOnly => false;

//    public  void Add(T item) => _components.Add(item);

//    public void Clear() => _components.Clear();

//    public  bool Contains(T item) => _components.Contains(item);
//    public  bool Contains(T item,IEqualityComparer<T> comparer) => _components.Contains(item,comparer);

//    public void CopyTo(T[] array, int arrayIndex) => _components.CopyTo(array, arrayIndex);

//    public IEnumerator<T> GetEnumerator() => _components.GetEnumerator();

//    public int IndexOf(T item) => _components.IndexOf(item);

//    public void Insert(int index, T item) => _components.Insert(index, item);

//    public bool Remove(T item) => _components.Remove(item);

//    public void RemoveAt(int index) => _components.RemoveAt(index);

//    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

//    private bool SequenceEquals_Ordered(IEnumerable<T> other, IEqualityComparer<T> comparer)
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

//    private bool SequenceEquals_Unordered(IEnumerable<T> other, IEqualityComparer<T> comparer)
//    {
//        foreach (var thatItem in other)
//        {
//            bool exists = this.Any(x => comparer.Equals(x, thatItem));
//            if (!exists)
//                return false;
//        }
//        return true;
//    }

//    protected bool SequenceEquals(IEnumerable<T>? other, bool unordered, IEqualityComparer<T>? comparer)
//    {
//        if (other is null)
//            return false;

//        if (other is ICollection col && col.Count != Count)
//            return false;

//        comparer ??= EqualityComparer<T>.Default;

//        return unordered ? SequenceEquals_Unordered(other, comparer) : SequenceEquals_Ordered(other, comparer);
//    }
//}