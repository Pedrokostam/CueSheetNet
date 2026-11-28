using CueSheetNet.Components;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.Comparing;

public sealed class RemarksEqualityComparer : IEqualityComparer<Remarks>, IEqualityComparer<Remark>
{
    public static readonly RemarksEqualityComparer InvariantCulture = new(StringComparer.InvariantCulture);
    public static readonly RemarksEqualityComparer InvariantCultureIgnoreCase = new(StringComparer.InvariantCultureIgnoreCase);
    public static readonly RemarksEqualityComparer CurrentCulture = new(StringComparer.CurrentCulture);
    public static readonly RemarksEqualityComparer CurrentCultureIgnoreCase = new(StringComparer.CurrentCultureIgnoreCase);
    public static readonly RemarksEqualityComparer Ordinal = new(StringComparer.Ordinal);
    public static readonly RemarksEqualityComparer OrdinalIgnoreCase = new(StringComparer.OrdinalIgnoreCase);
    public RemarksEqualityComparer(StringComparer stringComparer)
    {
        StringComparer = stringComparer;
    }

    public RemarksEqualityComparer()
    {
        StringComparer = null;
    }

    public RemarksEqualityComparer(StringComparison stringComparison)
    {
        StringComparer = StringHelper.GetStringComparer(stringComparison);
    }
    [AllowNull]
    public StringComparer StringComparer { get; init => field = value ?? StringComparer.InvariantCulture; }

    public bool Equals(Remarks? x, Remarks? y) => FindFirstDifference(x, y, "Remarks") is null;

    public CueDifference? FindFirstDifference(Remarks? x, Remarks? y, string path)
    {
        if (ReferenceEquals(x, y))
            return null;
        if (x is null || y is null)
            return new(path, x, y);
        if (x.Count != y.Count)
            return new($"{path}.Count", x.Count, y.Count);
        var itemA = x.OrderBy(r => r.Line, StringComparer).ToList();
        var itemB = y.OrderBy(r => r.Line, StringComparer).ToList();
        for (int i = 0; i < itemA.Count; i++)
        {
            if (!Equals(itemA[i], itemB[i]))
                return new($"{path}[{i}]", itemA[i].Line, itemB[i].Line);
        }
        return null;
    }

    public int GetHashCode([DisallowNull] Remarks obj)
    {
        int baza = 1781;
        foreach (var item in obj)
        {
            baza = HashCode.Combine(baza, item.GetType().GetHashCode());
        }
        return HashCode.Combine(baza, obj.Count);
    }

    public bool Equals(Remark? x, Remark? y)
    {
        return StringComparer.Equals(x?.Line, y?.Line);
    }

    public int GetHashCode([DisallowNull] Remark obj)
    {
        return StringComparer.GetHashCode(obj.Line);
    }
}
