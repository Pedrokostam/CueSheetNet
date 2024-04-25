using System.Collections;

namespace CueSheetNet.Collections;

internal class Chain<T> : IEnumerable<T> where T : class, IChainLink<T>
{
    public T? ChainEnd { get; protected set; }
    public T? ChainStart { get; protected set; }
    public IEnumerator<T> GetEnumerator()
    {
        var item = ChainStart;
        while (item is not null)
        {
            yield return item;
            item = item.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();
}