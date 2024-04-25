using System.Collections;
using System.Xml.Linq;

namespace CueSheetNet.Collections;

internal class ChainOld<T> : IEnumerable<T> where T : class, IChainLink<T>
{
    public T? ChainEnd { get; protected set; }
    public T? ChainStart { get; protected set; }
    public int Count { get; private set; }
    public IEnumerator<T> GetEnumerator()
    {
        var item = ChainStart;
        while (item is not null)
        {
            yield return item;
            item = item.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
public interface IKette<T, TGlied> where TGlied : IKettenGlied<T>
{
    public int Count { get; }
    public T? Head { get; }
    public T? Tail { get; }
    public void AddLast(T item);
    public void AddFirst(T item);
}
public interface IKettenGlied<T>
{
    IKette<T, IKettenGlied<T>> Parent { get; protected set; }
    IKettenGlied<T>? Previous { get; set; }
    IKettenGlied<T>? Next { get; set; }
    T Value { get; }
}
public class KettenGlied<T> : IKettenGlied<T>
{
    public IKette<T, IKettenGlied<T>> Parent { get; set; }
    public IKettenGlied<T>? Previous { get; set; }
    public IKettenGlied<T>? Next { get; set; }
    public T Value { get; }
    public KettenGlied(IKette<T, IKettenGlied<T>> parent, T value)
    {
        
    }
}
public class Kette<T, TGLied> : IKette<T, TGLied> where TGLied : IKettenGlied<T>
{
    public int Count { get; }
    public T? Head { get; }
    public T? Tail { get; }

    public void AddFirst(T item)
    {
        if(T is TGLied)
        throw new NotImplementedException();
    }

    public void AddLast(T item)
    {
        throw new NotImplementedException();
    }
}
