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

public sealed record CueSheetEqualityComparer : IEqualityComparer<CueSheet>
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

        /// <summary>
        /// When this flag is set, tracks will not be compared using values inherited from the sheet.
        /// <para>
        /// For example the following cue sheets will be considere unequal under this flag, and equal otherwise. The track PERFOMER is not set in one case.
        /// <code>
        /// PERFORMER "ARTIST"
        /// FILE File.wav WAVE
        ///    TRACK 01 AUDIO
        ///    PERFORMER "ARTIST"
        ///    INDEX 01 00:00:00
        /// </code>
        /// <code>
        /// PERFORMER "ARTIST"
        /// FILE File.wav WAVE
        ///    TRACK 01 AUDIO
        ///    INDEX 01 00:00:00
        /// </code>
        /// </para>
        /// </summary>
        ExplicitValues = 1 << 4,
        /// <inheritdoc cref="ExplicitValues"/>
        Strict = ExplicitValues,

        Full = Structure | CommonFields | OptionalFields | Remarks,
        FullStrict = Structure | CommonFields | OptionalFields | Remarks | ExplicitValues,

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
    public ComparisonOptions Options { get; init; } = ComparisonOptions.Full;
    public RemarksEqualityComparer RemarksComparer { get; }

    public bool Equals(CueSheet? x, CueSheet? y) => FindFirstDifference(x, y) is null;

    /// <summary>Returns the first difference in comparison order, or null when the sheets are equal.</summary>
    public CueDifference? FindFirstDifference(CueSheet? x, CueSheet? y)
    {
        if (ReferenceEquals(x, y))
            return null;
        if (x is null || y is null)
            return new("Sheet", x, y);
        if (x.Data.Count != y.Data.Count)
            return new("Data.Count", x.Data.Count, y.Data.Count);
        if (Options.HasFlag(ComparisonOptions.CommonFields))
        {
            if (Differs(x.Title, y.Title))
                return new("Title", x.Title, y.Title);
            if (Differs(x.Performer, y.Performer))
                return new("Performer", x.Performer, y.Performer);
        }
        if (Options.HasFlag(ComparisonOptions.OptionalFields))
        {
            if (Differs(x.Songwriter, y.Songwriter))
                return new("Songwriter", x.Songwriter, y.Songwriter);
            if (Differs(x.Catalog, y.Catalog))
                return new("Catalog", x.Catalog, y.Catalog);
            if (!PathsEqual(x.CdTextFile, y.CdTextFile))
                return new("CdTextFile", x.CdTextFile, y.CdTextFile);
        }
        for (int i = 0; i < x.Data.Count; i++)
        {
            if (FindLinkDifference(x.Data[i], y.Data[i], $"Data[{i}]") is { } diff)
                return diff;
        }
        if (Options.HasFlag(ComparisonOptions.Remarks))
            return RemarksComparer.FindFirstDifference(x.Remarks, y.Remarks, "Remarks");
        return null;
    }

    private bool Differs(string? a, string? b) => !StringComparer.Equals(a, b);


    private CueDifference? FindLinkDifference(Link x, Link y, string at)
    {
        if (!IndicesEqual(x.Index, y.Index))
            return new($"{at}.Index", x.Index, y.Index);
        if (x.Track.Position != y.Track.Position)
            return new($"{at}.Track.Position", x.Track.Position, y.Track.Position);
        if (x.Track.Type != y.Track.Type)
            return new($"{at}.Track.Type", x.Track.Type, y.Track.Type);
        if (!PathsEqual(x.File, y.File))
            return new($"{at}.File.Path", x.File.Path, y.File.Path);
        if (x.File.Type != y.File.Type)
            return new($"{at}.File.Type", x.File.Type, y.File.Type);
        if (x.Track.Pregap != y.Track.Pregap)
            return new($"{at}.Track.Pregap", x.Track.Pregap, y.Track.Pregap);
        if (x.Track.Postgap != y.Track.Postgap)
            return new($"{at}.Track.Postgap", x.Track.Postgap, y.Track.Postgap);
        if (Options.HasFlag(ComparisonOptions.CommonFields))
        {
            if (Options.HasFlag(ComparisonOptions.ExplicitValues))
            {
                if (!CueTrack.ExplicitFieldsCompare(FieldsSet.Title, x.Track, y.Track, StringComparer))
                    return new($"{at}.Track.Title (explicit)", x.Track.Title, y.Track.Title);
                if (!CueTrack.ExplicitFieldsCompare(FieldsSet.Performer, x.Track, y.Track, StringComparer))
                    return new($"{at}.Track.Performer (explicit)", x.Track.Performer, y.Track.Performer);
            }
            else
            {
                if (Differs(x.Track.Title, y.Track.Title))
                    return new($"{at}.Track.Title", x.Track.Title, y.Track.Title);
                if (Differs(x.Track.Performer, y.Track.Performer))
                    return new($"{at}.Track.Performer", x.Track.Performer, y.Track.Performer);
            }
        }
        if (Options.HasFlag(ComparisonOptions.OptionalFields))
        {
            if (Options.HasFlag(ComparisonOptions.ExplicitValues))
            {
                if (!CueTrack.ExplicitFieldsCompare(FieldsSet.Songwriter, x.Track, y.Track, StringComparer))
                    return new($"{at}.Track.Performer (explicit)", x.Track.Performer, y.Track.Performer);
            }
            else
            {
                if (Differs(x.Track.Songwriter, y.Track.Songwriter))
                    return new($"{at}.Track.Songwriter", x.Track.Songwriter, y.Track.Songwriter);
            }
            if (Differs(x.Track.Isrc, y.Track.Isrc))
                return new($"{at}.Track.Isrc", x.Track.Isrc, y.Track.Isrc);
        }
        if (Options.HasFlag(ComparisonOptions.Remarks))
            return RemarksComparer.FindFirstDifference(x.Track.Remarks, y.Track.Remarks, $"{at}.Track.Remarks");
        return null;
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
