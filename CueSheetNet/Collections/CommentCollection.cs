using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Collections;

/// <summary>
/// Stores all comments of the parent object. Provides methods to modify the collection.
/// </summary>
public sealed class CommentCollection : StringBasedCollection<string>
{
    protected override bool TestEqual(string one, string other, IEqualityComparer<string> comparer)
    {
        return comparer.Equals(one, other);
    }
}
