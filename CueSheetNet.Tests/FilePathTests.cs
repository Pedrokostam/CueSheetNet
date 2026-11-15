namespace CueSheetNet.Tests;

[TestClass]
public class FilePathTests
{
    private static CueDataFile AddFile(CueSheet cue, string path)
    {
        cue.InsertFile(0, path, "WAVE");
        return cue.Files[0];
    }

    [DataTestMethod]
    [DataRow(".")]
    [DataRow("./")]
    [DataRow(" . ")]
    public void DotPathDoesNotThrowIndexOutOfRange(string path)
    {
        // bug: FilePath.GetParts reads s[1] when the trimmed path is a single '.'
        try
        {
            AddFile(new CueSheet(), path);
        }
        catch (Exception e)
        {
            Assert.IsNotInstanceOfType(e, typeof(IndexOutOfRangeException));
        }
    }

    [TestMethod]
    public void UncPathStaysQualified()
    {
        // bug: FilePath.Update collapses "\\" to "\" and GetParts drops the empty root parts
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Windows-only path syntax");
        var file = AddFile(new CueSheet(), @"\\server\share\a.flac");
        Assert.IsTrue(file.IsQualified, file.Path);
    }

    [DataTestMethod]
    [DataRow("a/b.flac")]
    [DataRow(@"a\b.flac")]
    [DataRow(@"x/a\b.flac")]
    public void SeparatorsAreSplit(string path)
    {
        // bug: FilePath.Partos uses MemoryExtensions.Split with a ReadOnlySpan<char>, which is one string separator ("\/"), not SplitAny
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Backslash is not a separator on Unix");
        var file = AddFile(new CueSheet(), path);
        Assert.AreEqual("b.flac", file.Name, file.Path);
    }

    [TestMethod]
    public void QualifiedPathOnOtherDriveResolvesToItself()
    {
        // bug: FilePath.AbsolutePath always prefixes the sheet directory
        if (!OperatingSystem.IsWindows())
            Assert.Inconclusive("Drive letters are Windows-only");
        var file = AddFile(new CueSheet(@"D:\x\s.cue"), @"C:\a\b.flac");
        Assert.AreEqual(@"C:\a\b.flac", file.AbsolutePath);
    }

    [TestMethod]
    public void UnixRootedPathIsQualified()
    {
        // bug: FilePath.IsFirstPartQualified checks Length == 0 && [0] == '/', which is never true
        if (OperatingSystem.IsWindows())
            Assert.Inconclusive("Unix-only path syntax");
        var file = AddFile(new CueSheet(), "/home/x/a.flac");
        Assert.IsTrue(file.IsQualified, file.Path);
    }

    [TestMethod]
    public void BackslashPathIsSplitOnUnix()
    {
        // bug: on Unix both separator constants are '/', so Windows-made sheets keep "CD1\track.flac" as one part
        if (Path.AltDirectorySeparatorChar!=Path.DirectorySeparatorChar)
            Assert.Inconclusive("Behavior only present on platforms with only one directory separator");
        var file = AddFile(new CueSheet(), @"CD1\track.flac");
        Assert.AreEqual("track.flac", file.Name);
    }

    [TestMethod]
    public void CloneDoesNotShareCdTextFile()
    {
        // bug: CueExtensions.Clone copies CdTextFile by reference; it still belongs to the original sheet
        var cue = CueSheet.ParseFromString("""
            CDTEXTFILE "disc.cdt"
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
            """);
        Assert.IsNotNull(cue.CdTextFile);
        Assert.IsFalse(ReferenceEquals(cue.CdTextFile, cue.Clone().CdTextFile));
    }
}
