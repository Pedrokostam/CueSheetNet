using CueSheetNet.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Comparing;

public record CueSheetEqualityComparer : IEqualityComparer<CueSheet>
{
    [Flags]
    public enum ComparisonOptions
    {
        /// <summary>
        /// Checks the number of indices tracks and files. 
        /// <para>
        /// The sheets must have the exact same indices split the same way across tracks and files.
        /// </para>
        /// <para>
        /// Tracks must have the same types, flags, positions.
        /// </para>
        /// <para>
        /// Files (except sheet source files) must have corresponding paths and types.
        /// </para>
        /// </summary>
        Structure = 0,
        /// <summary>
        /// Checks the most common fields used in sheets: Title, Performer.
        /// </summary>
        CommonFields = 1 << 1,
        /// <summary>
        /// Checks less commonly used fields: CdTextFile, Catalog, Songwriter.
        /// </summary>
        OptionalFields = 1 << 2,
        /// <summary>
        /// Remarks of elements must correspond to each other.
        /// </summary>
        /// <remarks>1 << 3</remarks>
        Remarks = 1 << 3,

        All = Structure | CommonFields | OptionalFields | Remarks,

    }
    public static readonly CueSheetEqualityComparer InvariantCulture = new(StringComparer.InvariantCulture);
    public static readonly CueSheetEqualityComparer InvariantCultureIgnoreCase = new(StringComparer.InvariantCultureIgnoreCase);
    public static readonly CueSheetEqualityComparer CurrentCulture = new(StringComparer.CurrentCulture);
    public static readonly CueSheetEqualityComparer CurrentCultureIgnoreCase = new(StringComparer.CurrentCultureIgnoreCase);
    public static readonly CueSheetEqualityComparer Ordinal = new(StringComparer.Ordinal);
    public static readonly CueSheetEqualityComparer OrdinalIgnoreCase = new(StringComparer.OrdinalIgnoreCase);

    public CueSheetEqualityComparer(StringComparer stringComparer)
    {
        StringComparer = stringComparer;
        RemarksComparer = new(StringComparer);
    }
    public CueSheetEqualityComparer(StringComparer stringComparer, ComparisonOptions options)
    {
        StringComparer = stringComparer;
        Options = options;
        RemarksComparer = new(StringComparer);
    }
    public CueSheetEqualityComparer()
    {
        StringComparer = null;
        RemarksComparer = new(StringComparer);
    }
    public CueSheetEqualityComparer(ComparisonOptions options)
    {
        StringComparer = null;
        Options = options;
        RemarksComparer = new(StringComparer);
    }
    public CueSheetEqualityComparer(StringComparison stringComparison)
    {
        StringComparer = StringHelper.GetStringComparer(stringComparison);
        RemarksComparer = new(StringComparer);
    }
    public CueSheetEqualityComparer(StringComparison stringComparison, ComparisonOptions options)
    {
        StringComparer = StringHelper.GetStringComparer(stringComparison);
        Options = options;
        RemarksComparer = new(StringComparer);
    }

    [AllowNull]
    public StringComparer StringComparer { get; init => field = value ?? StringComparer.InvariantCulture; }
    public ComparisonOptions Options { get; init; } = ComparisonOptions.All;
    public RemarksEqualityComparer RemarksComparer { get; }

    public bool Equals(CueSheet? x, CueSheet? y)
    {
        if (ReferenceEquals(x, y))
            return true;
        if (x is null || y is null)
            return false;
        if (x.Data.Count != y.Data.Count)
            return false;
        if (Options.HasFlag(ComparisonOptions.CommonFields))
        {
            var res = StringComparer.Equals(x.Title, y.Title)
                && StringComparer.Equals(x.Performer, y.Performer);
            if (!res)
            {
                return res;
            }
        }
        if (Options.HasFlag(ComparisonOptions.OptionalFields))
        {
            if (!StringComparer.Equals(x.Songwriter, y.Songwriter)
                || !StringComparer.Equals(x.Catalog, y.Catalog)
                || !PathsEqual(x.CdTextFile, y.CdTextFile))
            {
                return false;
            }
        }
        if (Options.HasFlag(ComparisonOptions.CommonFields))
        {
            if (!StringComparer.Equals(x.Title, y.Title))
            {
                return false;
            }
        }
        for (int i = 0; i < x.Data.Count; i++)
        {
            if (!LinksEqual(x.Data[i], y.Data[i]))
                return false;
        }
        if (Options.HasFlag(ComparisonOptions.Remarks))
        {
            return RemarksComparer.Equals(x.Remarks, y.Remarks);
        }
        return true;
    }
    private bool LinksEqual(Link linkX, Link linkY)
    {
        var res = IndicesEqual(linkX.Index, linkY.Index)
               && linkX.Track.Position == linkY.Track.Position
               && linkX.Track.Type == linkY.Track.Type
               && PathsEqual(linkX.File, linkY.File)
               && linkX.File.Type == linkY.File.Type
               && linkX.Track.PreGap == linkY.Track.PreGap
               && linkX.Track.PostGap == linkY.Track.PostGap;
        if (!res)
        {
            return res;
        }
        if (Options.HasFlag(ComparisonOptions.CommonFields))
        {
            res = StringComparer.Equals(linkX.Track.Title, linkY.Track.Title)
                && StringComparer.Equals(linkX.Track.Performer, linkY.Track.Performer);
            if (!res)
            {
                return res;
            }
        }
        if (Options.HasFlag(ComparisonOptions.OptionalFields))
        {
            if (!StringComparer.Equals(linkX.Track.Songwriter, linkY.Track.Songwriter)
                || !StringComparer.Equals(linkX.Track.Isrc, linkY.Track.Isrc))
            {
                return false;
            }
        }
        if (Options.HasFlag(ComparisonOptions.Remarks))
        {
            return RemarksComparer.Equals(linkX.Track.Remarks, linkY.Track.Remarks);
        }
        return true;
    }
    private static bool IndicesEqual(CueIndex a, CueIndex b) =>
        a.Position == b.Position && a.Time == b.Time && a.IsSplit == b.IsSplit;

    private static bool PathsEqual(FilePath? a, FilePath? b)
    {
        if (a is null || b is null)
            return a is null && b is null;
        if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
        {
            return string.Equals(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
        }
        return string.Equals(a.Path, b.Path, StringComparison.Ordinal);
    }

    public int GetHashCode([DisallowNull] CueSheet obj)
    {
        int tracks = 0, files = 0;
        Link? previous = null;
        foreach (var link in obj.Data)
        {
            if (!ReferenceEquals(previous?.Track, link.Track))
                tracks++;
            if (!ReferenceEquals(previous?.File, link.File))
                files++;
            previous = link;
        }
        var hashy = HashCode.Combine(tracks, files, obj.Data.Count);
        if (Options.HasFlag(ComparisonOptions.CommonFields))
        {
            hashy = HashCode.Combine(hashy, StringComparer.GetHashCode(obj.Title), StringComparer.GetHashCode(obj.Performer ?? ""));
        }
        if (Options.HasFlag(ComparisonOptions.Remarks))
        {
            hashy = HashCode.Combine(hashy, RemarksComparer.GetHashCode(obj.Remarks));
        }
        return hashy;
    }
}
