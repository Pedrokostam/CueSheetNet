namespace CueSheetNet.Tests;

[TestClass]
public class RoundTripTests
{
    public static IEnumerable<object[]> Files =>
        new[] { "SavingTests", "LoadingTests", "Equality", "Structure" }
            .SelectMany(folder => Utils.GetFiles("*.cue", folder).Select(f => new object[] { folder, Path.GetFileName(f) }));

    // The writer emits its own canonical line order and drops redundant quotes, so lines are compared as a sorted multiset.
    private static string[] Normalize(string text) => text
        .Split('\n')
        .Select(l => l.Trim().Replace("\"", ""))
        .Where(l => l.Length > 0)
        .Order(StringComparer.Ordinal)
        .ToArray();

    [TestMethod]
    [DynamicData(nameof(Files))]
    public void WrittenLinesMatchSource(string folder, string fileName)
    {
        string path = Utils.GetFile(folder, fileName);
        var cue = CueReader.CreateFromFile(path).Parse();
        string written = CueWriter.Create().WriteToString(cue);
        string source = File.ReadAllText(path, cue.SourceEncoding!);

        CollectionAssert.AreEqual(Normalize(source), Normalize(written), written);
    }

    [TestMethod]
    [DynamicData(nameof(Files))]
    public void WriteIsStable(string folder, string fileName)
    {
        var cue = CueReader.CreateFromFile(Utils.GetFile(folder, fileName)).Parse();
        string first = CueWriter.Create().WriteToString(cue);
        string second = CueWriter.Create().WriteToString(CueReader.CreateFromString(first).Parse());

        Assert.AreEqual(first, second);
    }
}
