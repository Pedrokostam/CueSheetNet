namespace CueSheetNet.Tests;

[TestClass]
public class RoundTripTests
{
    internal static void AssertSame(CueSheet expected, CueSheet actual, Sample sample)
    {
        if (sample.File.Contains("wrong", StringComparison.OrdinalIgnoreCase))
            Assert.AreNotEqual(expected, actual, sample.ToString());
        else
            TestSheets.AssertSheetsEqual(expected, actual, sample.ToString());
    }

    // The writer emits its own canonical line order and drops redundant quotes, so lines are compared as a sorted multiset.
    private static string[] Normalize(string text) => text
        .Split('\n')
        .Select(l => l.Trim().Replace("\"", ""))
        .Where(l => l.Length > 0)
        .Order(StringComparer.Ordinal)
        .ToArray();

    [TestMethod]
    [DynamicData(nameof(Samples.RoundTripRows), typeof(Samples))]
    public void WrittenLinesMatchSource(Sample sample)
    {
        var cue = sample.Load();
        string written = CueWriter.Create().WriteToString(cue);
        string source = File.ReadAllText(sample.Path, cue.SourceEncoding!);

        string[] normalizedSource = Normalize(source);
        string[] normalizedWritten = Normalize(written);
        CollectionAssert.AreEqual(normalizedSource, normalizedWritten, written);
    }

    [TestMethod]
    [DynamicData(nameof(Samples.RoundTripRows), typeof(Samples))]
    public void WriteIsStable(Sample sample)
    {
        var cue = sample.Load();
        string first = CueWriter.Create().WriteToString(cue);
        var cue2 = CueReader.CreateFromString(first).Parse();
        string second = CueWriter.Create().WriteToString(cue2);

        Assert.AreEqual(first, second);
    }

    [TestMethod("Test if save and reloaded sheet is equal to its source")]
    [DynamicData(nameof(Samples.SaveRows), typeof(Samples))]
    public void SaveReloadSame(Sample sample)
    {
        var cue = sample.Load();
        cue.Title = cue.Title; // If the sheet has not title set, it will come from the filename, which changes after saving
        var clone = cue.Clone();
        var temp = Path.GetTempFileName();
        CueWriter.Create().SaveAs(cue, temp);
        var reloaded = CueReader.CreateFromFile(temp).Parse();
        File.Delete(temp);
        AssertSame(clone, reloaded, sample);
    }

    [TestMethod("Test if saved sheet is equal to its source's clone")]
    [DynamicData(nameof(Samples.SaveRows), typeof(Samples))]
    public void SaveCloneSame(Sample sample)
    {
        var cue = sample.Load();
        cue.Title = cue.Title; // If the sheet has not title set, it will come from the filename, which changes after saving
        var clone = cue.Clone();
        var temp = Path.GetTempFileName();
        CueWriter.Create().SaveAs(cue, temp);
        File.Delete(temp);
        AssertSame(clone, cue, sample);
    }
}
