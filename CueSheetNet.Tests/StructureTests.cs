namespace CueSheetNet.Tests;

[TestClass]
public class StructureTests
{
    private static CueSheet Load(string folder, string name) => CueReader.CreateFromFile(Utils.GetFile(folder, name)).Parse();

    [TestMethod]
    public void NonConsecutiveTrackNumbersArePreserved()
    {
        var cue = Load("Structure", "NonConsecutive.cue");
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, cue.Tracks.Select(t => t.Position).ToArray());

        var reloaded = cue.Clone();
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, reloaded.Tracks.Select(t => t.Position).ToArray());
    }

    [TestMethod]
    public void MultiFileEacLayout()
    {
        // Track 2 and 3 start with INDEX 00 at the end of the previous file (EAC "gaps appended to previous track").
        var cue = Load("SavingTests", "MultiFile.cue");

        Assert.AreEqual(3, cue.Tracks.Count);
        Assert.AreEqual(3, cue.Files.Count);
        Assert.AreEqual(5, cue.Indices.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cue.Tracks.Select(t => t.Position).ToArray());

        CollectionAssert.AreEqual(new[] { false, true, false, true, false }, cue.Indices.Select(i => i.IsSplit).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 0, 1, 0, 1 }, cue.Indices.Select(i => i.Position).ToArray());

        CollectionAssert.AreEqual(new[] { false, true, true }, cue.Tracks.Select(t => t.IsEacStyle).ToArray());
        CollectionAssert.AreEqual(new[] { false, true, true }, cue.Tracks.Select(t => t.HasZerothIndex).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 2, 2 }, cue.Tracks.Select(t => t.Indices.Count).ToArray());

        // A track belongs to the file holding its first non-split index.
        CollectionAssert.AreEqual(
            new[] { "MultiFile - 0 - Uno.wav", "MultiFile - 1 - Dos.wav", "MultiFile - 2 - Tres.wav" },
            cue.Tracks.Select(t => t.File.Name).ToArray());
    }
}
