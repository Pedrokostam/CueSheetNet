using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Collections;
internal class JoinableChain<T>(Action<T>? validator) : Chain<T> where T : class, IChainLink<T>
{
    public JoinableChain() : this(null)
    {
    }

    public JoinableChain<T>? FollowingChain { get; private set; }
    public JoinableChain<T>? PrecedingChain { get; private set; }
    public int Count { get; private set; }
    public bool IsReadOnly { get; }

    private readonly Action<T>? _validator=validator;

    public void PromoteLastItemToFollowingChain()
    {
        ExceptionHelper.ThrowIfNull(FollowingChain,"Cannot promote item, if there is no following chain");
        var promotedItem = ChainEnd;
        ChainEnd = promotedItem?.Previous;
        FollowingChain.ChainStart = promotedItem;
        if (FollowingChain.ChainEnd is null)
        {
            FollowingChain.ChainEnd = promotedItem;
        }
        promotedItem?.GetPromoted();
    }

    public void JoinChainAfter(JoinableChain<T>? chain)
    {
        Debug.Assert(chain != this);
        if (chain is null)
        {
            return;
        }
        chain.FollowingChain = this;
        this.PrecedingChain = chain;

        Link(PrecedingChain?.ChainEnd, ChainStart);

    }

    public void RemoveFirst()
    {
        var removed = ChainStart;
        var newFirst = removed?.Next;
        ChainStart = newFirst;
        Link(PrecedingChain?.ChainEnd, newFirst);

        Orphanize(removed);
        if (ChainStart is null)
        {
            ChainEnd = null;
        }
    }
    public void RemoveLast()
    {
        var removed = ChainEnd;
        var newLast = removed?.Previous;
        ChainEnd = newLast;

        Link(newLast, FollowingChain?.ChainStart);

        Orphanize(removed);
        if (ChainEnd is null)
        {
            ChainStart = null;
        }
    }

    private void Link(T? first, T? second)
    {
        if (first is not null)
        {
            first.Next = second;
        }
        if (second is not null)
        {
            second.Previous = first;
        }
    }

    public static void Orphanize(T? item)
    {
        if (item is null)
            return;
        item.Next = null;
        item.Previous = null;
    }

    public void Add(T item)
    {
        _validator?.Invoke(item);

        if (ChainEnd is null && ChainStart is null)
        {
            ChainStart = item;
            ChainEnd = item;

            Link(PrecedingChain?.ChainEnd, item);
        }
        else
        {
            var lastElem = ChainEnd;
            item.Previous = lastElem;
            Link(lastElem, item);
            ChainEnd = item;

        }
        Count++;
    }

    public IEnumerator<T> Reverse()
    {
        var item = ChainEnd;
        while (item is not null)
        {
            yield return item;
            item = item.Previous;
        }
    }

}