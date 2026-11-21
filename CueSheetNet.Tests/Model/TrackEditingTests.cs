using CueSheetNet.Components;

namespace CueSheetNet.Tests;

[TestClass]
public class TrackEditingTests
{
    [TestMethod]
    public void InsertTrackAfterLastIndexOfLastFile()
    {
        // bug: throws from CueDataFile.NextFile
        var cue = Samples.Base.Load();
        cue.InsertTrack(cue.Files[0], new TrackInsertParameters("AUDIO"), new CueTime(5, 0, 0));
        Assert.AreEqual(3, cue.Tracks.Count);
    }

    [TestMethod]
    public void InsertTrackWithoutFreeNumberKeepsPositionsUnique()
    {
        // bug: CueSheet.NormalizeGaps applies the gap to the inserted track as well
        var cue = TestSheets.Parse("""
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
              TRACK 04 AUDIO
                INDEX 01 01:00:00
              TRACK 05 AUDIO
                INDEX 01 02:00:00
            """);
        cue.InsertTrack(cue.Files[0], new TrackInsertParameters("AUDIO"), new CueTime(1, 30, 0));
        var positions = cue.Tracks.Select(x => x.Position).ToArray();
        Assert.AreEqual(positions.Length, positions.Distinct().Count(), string.Join(",", positions));
    }

    [TestMethod]
    public void InsertTrackKeepsLaterNumberingGaps()
    {
        // bug: CueSheet.NormalizeGaps latches the first non-zero gap and applies it to all later tracks
        var cue = TestSheets.Parse("""
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
              TRACK 04 AUDIO
                INDEX 01 01:00:00
              TRACK 05 AUDIO
                INDEX 01 02:00:00
              TRACK 09 AUDIO
                INDEX 01 03:00:00
            """);
        var old = cue.Tracks.Skip(1).ToArray();
        cue.InsertTrack(cue.Files[0], new TrackInsertParameters("AUDIO"), new CueTime(0, 30, 0));
        CollectionAssert.AreEqual(new[] { 4, 5, 9 }, old.Select(x => x.Position).ToArray());
    }

    [TestMethod]
    public void Hellion()
    {
        var cue = Samples.JudasPriestScreamingForVengeance.Load();
        var electricEyeStart = cue.Tracks[1].Start;
        Assert.AreEqual(10, cue.Tracks.Count);
        cue.RemoveTrack(1, preserveGap: true);
        cue.Tracks[0].Title = "The Hellion / Electric Eye";
        Assert.AreEqual(3, cue.Tracks[1].Position);
        Assert.AreEqual(9, cue.Tracks.Count);
        cue.Tracks[0].Title = "The Hellion";
        cue.InsertTrack(cue.Files[0], TrackInsertParameters.Audio with { Title = "Electric Eye", }, electricEyeStart);
        Assert.AreEqual(2, cue.Tracks[1].Position);
        Assert.AreEqual(3, cue.Tracks[2].Position);
        Assert.AreEqual(10, cue.Tracks.Count);
    }

    [TestMethod]
    public void Hellion10()
    {
        var cue = Samples.JudasPriestScreamingForVengeance.Load();
        int pos = 10;
        foreach (var track in cue.Tracks)
        {
            track.SetPosition(pos);
            pos += 10;
        }
        Assert.AreEqual(10, cue.Tracks.Count);
        CollectionAssert.AreEqual(new int[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }, cue.Tracks.Select(x => x.Position).ToArray());
        var electricEyeStart = cue.Tracks[1].Start;
        cue.RemoveTrack(1, preserveGap: true);
        Assert.AreEqual(9, cue.Tracks.Count);
        cue.Tracks[0].Title = "The Hellion / Electric Eye";
        Assert.AreEqual(30, cue.Tracks[1].Position);
        cue.Tracks[0].Title = "The Hellion";
        cue.InsertTrack(cue.Files[0], TrackInsertParameters.Audio with { Title = "Electric Eye", Position = 20 }, electricEyeStart);
        Assert.AreEqual(20, cue.Tracks[1].Position);
        Assert.AreEqual(30, cue.Tracks[2].Position);
        Assert.AreEqual(10, cue.Tracks.Count);
    }

    [TestMethod]
    public void NonConsecutiveTrackNumbersArePreserved()
    {
        var cue = Samples.NonConsecutive.Load();
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, cue.Tracks.Select(t => t.Position).ToArray());

        var reloaded = cue.Clone();
        CollectionAssert.AreEqual(new[] { 1, 3, 4 }, reloaded.Tracks.Select(t => t.Position).ToArray());
    }
}
