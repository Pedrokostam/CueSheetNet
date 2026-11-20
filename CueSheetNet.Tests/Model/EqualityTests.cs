using CueSheetNet.Comparing;
using static CueSheetNet.Comparing.CueSheetEqualityComparer.ComparisonOptions;

namespace CueSheetNet.Tests;

[TestClass]
public class EqualityTests
{
    private static CueSheet Load(string name)
    {
        string file = name is "Songwriter" or "TrackSongwriter" or "Same" ? "Base" : name;
        var sheet = Samples.EqualityVariant(file).Load();
        // CueReader rejects the SONGWRITER keyword, so songwriters are set here instead of in the files.
        sheet.Songwriter = name == "Songwriter" ? "Other Writer" : "Test Writer";
        sheet.Tracks[0].Songwriter = name == "TrackSongwriter" ? "Other Writer" : "Track Writer";
        return sheet;
    }

    private const CueSheetEqualityComparer.ComparisonOptions NoCommon = Structure | OptionalFields | Remarks;
    private const CueSheetEqualityComparer.ComparisonOptions NoOptional = Structure | CommonFields | Remarks;
    private const CueSheetEqualityComparer.ComparisonOptions NoRemarks = Structure | CommonFields | OptionalFields;

    private static readonly bool CaseInsensitivePaths = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    // Each variant differs from Base.cue in the single aspect its name describes.
    public static IEnumerable<object[]> Variants => [
        ["Same", Full, StringComparison.Ordinal, true],
        ["Same", Structure, StringComparison.Ordinal, true],

        ["TitleCase", Full, StringComparison.Ordinal, false],
        ["TitleCase", Full, StringComparison.OrdinalIgnoreCase, true],
        ["Title", Full, StringComparison.Ordinal, false],
        ["Title", NoCommon, StringComparison.Ordinal, true],
        ["Performer", Full, StringComparison.Ordinal, false],
        ["Performer", NoCommon, StringComparison.Ordinal, true],

        ["Songwriter", Full, StringComparison.Ordinal, false],
        ["Songwriter", NoOptional, StringComparison.Ordinal, true],
        ["Catalog", Full, StringComparison.Ordinal, false],
        ["Catalog", NoOptional, StringComparison.Ordinal, true],

        ["Remark", Full, StringComparison.Ordinal, false],
        ["Remark", NoRemarks, StringComparison.Ordinal, true],
        ["RemarkOrder", Full, StringComparison.Ordinal, true],

        ["TrackTitle", Full, StringComparison.Ordinal, false],
        ["TrackTitle", NoCommon, StringComparison.Ordinal, true],
        ["TrackPerformer", Full, StringComparison.Ordinal, false],
        ["TrackPerformer", NoCommon, StringComparison.Ordinal, true],
        ["TrackSongwriter", Full, StringComparison.Ordinal, false],
        ["TrackSongwriter", NoOptional, StringComparison.Ordinal, true],
        ["TrackRemark", Full, StringComparison.Ordinal, false],
        ["TrackRemark", NoRemarks, StringComparison.Ordinal, true],
        ["Isrc", Full, StringComparison.Ordinal, false],
        ["Isrc", NoOptional, StringComparison.Ordinal, true],

        ["IndexTime", Full, StringComparison.Ordinal, false],
        ["IndexTime", Structure, StringComparison.Ordinal, false],
        ["MissingIndex", Structure, StringComparison.Ordinal, false],
        ["ExtraTrack", Structure, StringComparison.Ordinal, false],
        ["TrackType", Structure, StringComparison.Ordinal, false],
        ["Pregap", Structure, StringComparison.Ordinal, false],
        ["Postgap", Structure, StringComparison.Ordinal, false],
        ["FileType", Structure, StringComparison.Ordinal, false],
        ["FilePath", Structure, StringComparison.Ordinal, false],
        ["FilePathCase", Structure, StringComparison.Ordinal, CaseInsensitivePaths],
    ];

    [TestMethod]
    [DynamicData(nameof(Variants))]
    public void CompareWithBase(string variant, CueSheetEqualityComparer.ComparisonOptions options, StringComparison comparison, bool expected)
    {
        var comparer = new CueSheetEqualityComparer(comparison, options);
        var a = Load("Base");
        var b = Load(variant);

        var ab = comparer.FindFirstDifference(a, b);
        var ba = comparer.FindFirstDifference(b, a);
        Assert.AreEqual(expected, ab is null, $"a == b: {ab}");
        Assert.AreEqual(expected, ba is null, $"b == a: {ba}");
        if (expected)
            Assert.AreEqual(comparer.GetHashCode(a), comparer.GetHashCode(b), "Equal sheets must have equal hash codes");
    }

    [TestMethod]
    public void ObjectEqualsUsesDefaultComparer()
    {
        var a = Load("Base");
        Assert.IsTrue(a.Equals((object)Load("Same")));
        Assert.IsFalse(a.Equals((object)Load("Title")));
        Assert.AreEqual(a.GetHashCode(), Load("Same").GetHashCode());
    }

    [TestMethod]
    public void NullAndReference()
    {
        var a = Load("Base");
        var comparer = CueSheetEqualityComparer.Ordinal;
        Assert.IsTrue(comparer.Equals(a, a));
        Assert.IsTrue(comparer.Equals(null, null));
        Assert.IsFalse(comparer.Equals(a, null));
        Assert.IsFalse(comparer.Equals(null, a));
        Assert.IsFalse(a.Equals((object?)null));
        Assert.IsFalse(a.Equals((object)"Base"));
    }

    [TestMethod]
    public void EmptySheetsAreEqual()
    {
        Assert.AreEqual(new CueSheet { Title = "X" }, new CueSheet { Title = "X" });
        Assert.AreEqual(new CueSheet { Title = "X" }.GetHashCode(), new CueSheet { Title = "X" }.GetHashCode());
    }

    [TestMethod]
    public void RemarksOrderIgnoredWithCaseSensitiveComparer()
    {
        Components.Remarks x = [], y = [];
        x.Add("a");
        x.Add("A");
        y.Add("A");
        y.Add("a");
        Assert.IsTrue(RemarksEqualityComparer.Ordinal.Equals(x, y));
    }
}
