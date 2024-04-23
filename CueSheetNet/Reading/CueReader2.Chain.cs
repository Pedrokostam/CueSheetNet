using System.Collections;
using System.Diagnostics;

namespace CueSheetNet;

public partial class CueReader2
{
    private class Chain<T> : IEnumerable<T> where T : class, IChainLink<T>
    {
        public T? First { get; private set; }
        public T? Last { get; private set; }

        public Chain<T>? FollowingChain { get; private set; }
        public Chain<T>? PrecedingChain { get; private set; }

        public void PromoteLastItemToFollowingChain()
        {
            Debug.Assert(FollowingChain is not null);
            var promotedItem = Last;
            Last = promotedItem.Previous;
            FollowingChain.First = promotedItem;
            if(FollowingChain.Last is null)
            {
                FollowingChain.Last=promotedItem;
            }
            promotedItem.GetPromoted();
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

            if (First is not null)
            {
                First.Previous = PrecedingChain.Last;
                PrecedingChain.Last.Next = First.Previous;
            }
        }

        public void RemoveFirst()
        {
            var removed = First;
            var newFirst = removed.Next;
            First = newFirst;
            if(PrecedingChain is not null)
            {
                PrecedingChain.Last.Next=newFirst;
                if(newFirst is not null)
                    newFirst.Previous = PrecedingChain.Last;
            }
            removed.Next = null;
            removed.Previous = null;
            if(First is null)
            {
                Last = null;
            }
        }
        public void RemoveLast()
        {
            var removed = Last;
            var newLast = removed.Previous;
            Last= newLast;  
            if (FollowingChain is not null)
            {
                FollowingChain.First.Previous = newLast;
                if(newLast is not null)
                    newLast.Next = FollowingChain.First;
            }
            removed.Next = null;
            removed.Previous = null;
            if (Last is null)
            {
                First = null;
            }
        }

        public void Add(T item)
        {
            if (Last is null && First is null)
            {
                First = item;
                Last = item;

                if (PrecedingChain is not null)
                {
                   var  precedingsLast = PrecedingChain.Last;
                    First.Previous = precedingsLast;
                    precedingsLast.Next = First;
                }
            }
            else
            {
                var lastElem = Last;
                item.Previous = lastElem;
                lastElem.Next = item;
                Last = item;
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            var item = First;
            while (item is not null)
            {
                yield return item;
                item = item.Next;
            }
        }
        public IEnumerator<T> Reverse()
        {
            var item = Last;
            while (item is not null)
            {
                yield return item;
                item = item.Previous;
            }
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}