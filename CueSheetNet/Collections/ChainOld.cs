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