namespace CueSheetNet.Tests;
[TestClass]
public class SavingTests
{
    public static IEnumerable<object[]> Files =>
        Utils.GetFiles("*.cue", "SavingTests").Select(f => new object[] { Path.GetFileName(f) });

    private static CueSheet Load(string fileName) => CueReader.CreateFromFile(Utils.GetFile("SavingTests", fileName)).Parse();

    private static void AssertSame(CueSheet expected, CueSheet actual, string fileName)
    {
        if (fileName.Contains("wrong", StringComparison.OrdinalIgnoreCase))
            Assert.AreNotEqual(expected, actual, fileName);
        else
            Assert.AreEqual(expected, actual, fileName);
    }

    [TestMethod("Test if cloned sheet is equal to its source")]
    [DynamicData(nameof(Files))]
    public void LoadCloneSame(string fileName)
    {
        var cue = Load(fileName);
        AssertSame(cue.Clone(), cue, fileName);
    }

    [TestMethod("Test if save and reloaded sheet is equal to its source")]
    [DynamicData(nameof(Files))]
    public void SaveReloadSame(string fileName)
    {
        var cue = Load(fileName);
        var clone = cue.Clone();
        var temp = Path.GetTempFileName();
        CueWriter.Create().SaveAs(cue, temp);
        var reloaded = CueReader.CreateFromFile(temp).Parse();
        File.Delete(temp);
        AssertSame(clone, reloaded, fileName);
    }

    [TestMethod("Test if saved sheet is equal to its source's clone")]
    [DynamicData(nameof(Files))]
    public void SaveCloneSame(string fileName)
    {
        var cue = Load(fileName);
        var clone = cue.Clone();
        var temp = Path.GetTempFileName();
        CueWriter.Create().SaveAs(cue, temp);
        File.Delete(temp);
        AssertSame(clone, cue, fileName);
    }
}
