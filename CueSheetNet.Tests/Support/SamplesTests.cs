namespace CueSheetNet.Tests;

[TestClass]
public class SamplesTests
{
    [TestMethod]
    public void EverySampleFileExists()
    {
        var samples = typeof(Samples).GetFields().Where(f => f.FieldType == typeof(Sample)).Select(f => (Sample)f.GetValue(null)!);
        foreach (var sample in samples)
            Assert.IsTrue(File.Exists(sample.Path), $"Missing test item {sample}");
    }

    [TestMethod]
    public void EveryCueUnderSheetsAndEqualityIsReferenced()
    {
        var fields = typeof(Samples).GetFields().Where(f => f.FieldType == typeof(Sample)).Select(f => (Sample)f.GetValue(null)!);
        var referenced = fields.Concat(Samples.RoundTripCorpus).Select(s => Path.GetFullPath(s.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in new[] { "Sheets", "Equality" })
            foreach (var file in Directory.EnumerateFiles(Utils.GetFile(folder), "*.cue", SearchOption.AllDirectories))
                Assert.IsTrue(referenced.Contains(Path.GetFullPath(file)), $"Not referenced by Samples: {file}");
    }
}
