using System.Collections;
using System.Diagnostics;

namespace CueSheetNet;

public partial class CueReader2
{
    private class Chain<T>(Action<T>? validator) : IEnumerable<T> where T : class, IChainLink<T>
    {
        public Chain() : this(null)
        {
        }
        public T? ChainStart { get; private set; }
        public T? ChainEnd { get; private set; }

        public Chain<T>? FollowingChain { get; private set; }
        public Chain<T>? PrecedingChain { get; private set; }

        private readonly Action<T>? _validator=validator;

        public void PromoteLastItemToFollowingChain()
        {
            Debug.Assert(FollowingChain is not null);
            var promotedItem = ChainEnd;
            ChainEnd = promotedItem?.Previous;
            FollowingChain.ChainStart = promotedItem;
            if (FollowingChain.ChainEnd is null)
            {
                FollowingChain.ChainEnd = promotedItem;
            }
            promotedItem?.GetPromoted();
        }

        public void JoinChainAfter(Chain<T>? chain)
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
        }
        public IEnumerator<T> GetEnumerator()
        {
            var item = ChainStart;
            while (item is not null)
            {
                yield return item;
                item = item.Next;
            }
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
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}