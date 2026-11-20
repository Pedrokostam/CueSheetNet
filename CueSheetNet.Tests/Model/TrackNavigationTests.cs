namespace CueSheetNet.Tests;

[TestClass]
public class TrackNavigationTests
{
    [TestMethod]
    public void DurationIsDistanceToNextTrack()
    {
        // bug: CueTrack.Duration returns the next index's absolute time
        var cue = Samples.ThreeTracks.Load();
        Assert.AreEqual(new CueTime(2, 0, 0), cue.Tracks[1].CueDuration);
    }

    [TestMethod]
    public void PreviousTrackOfFirstTrackIsNull()
    {
        // bug: CueTrack.GetChain reads Tracks[-1]
        var cue = Samples.ThreeTracks.Load();
        Assert.IsNull(cue.Tracks[0].PreviousTrack());
    }

    [TestMethod]
    public void PreviousTrackInFileOfFirstTrackIsNull()
    {
        // bug: CueTrack.GetChain reads Tracks[-1]
        var cue = Samples.ThreeTracks.Load();
        Assert.IsNull(cue.Tracks[0].PreviousTrackInFile());
    }

    [TestMethod]
    public void NextTrackOfLastTrackIsNull()
    {
        var cue = Samples.ThreeTracks.Load();
        Assert.IsNull(cue.Tracks[^1].NextTrack());
    }

    [TestMethod]
    public void NextFileOfLastFileIsNull()
    {
        // bug: CueDataFile.NextFile compares against Data.Count instead of Files.Count
        var cue = Samples.Base.Load();
        Assert.IsNull(cue.Files[^1].NextFile());
    }

    [TestMethod]
    public void MultiFileEacLayout()
    {
        // Track 2 and 3 start with INDEX 00 at the end of the previous file (EAC "gaps appended to previous track").
        var cue = Samples.MultiFile.Load();
        var q = cue.Tracks[0].Indices;

        Assert.AreEqual(3, cue.Tracks.Count);
        Assert.AreEqual(3, cue.Files.Count);
        Assert.AreEqual(5, cue.Indices.Count);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cue.Tracks.Select(t => t.Position).ToArray());

        CollectionAssert.AreEqual(new[] { false, true, false, true, false }, cue.Indices.Select(i => i.IsSplit).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 0, 1, 0, 1 }, cue.Indices.Select(i => i.Position).ToArray());

        CollectionAssert.AreEqual(new[] { false, true, true }, cue.Tracks.Select(t => t.IsEacStyle).ToArray());
        // Split indices do not belong to any track
        CollectionAssert.AreEqual(new[] { false, false, false }, cue.Tracks.Select(t => t.HasZerothIndex).ToArray());
        CollectionAssert.AreEqual(new[] { 1, 1, 1 }, cue.Tracks.Select(t => t.Indices.Count).ToArray());

        // A track belongs to the file holding its first non-split index.
        CollectionAssert.AreEqual(
            new[] { "MultiFile - 0 - Uno.wav", "MultiFile - 1 - Dos.wav", "MultiFile - 2 - Tres.wav" },
            cue.Tracks.Select(t => t.File.Name).ToArray());
    }
}
