namespace CueSheetNet.Tests;

[TestClass]
public class TrackNavigationTests
{
    private const string ThreeTracks = """
        FILE "a.wav" WAVE
          TRACK 01 AUDIO
            INDEX 01 00:00:00
          TRACK 02 AUDIO
            INDEX 01 01:00:00
          TRACK 03 AUDIO
            INDEX 01 03:00:00
        """;

    [TestMethod]
    public void DurationIsDistanceToNextTrack()
    {
        // bug: CueTrack.Duration returns the next index's absolute time
        var cue = CueSheet.ParseFromString(ThreeTracks);
        Assert.AreEqual(new CueTime(2, 0, 0), cue.Tracks[1].Duration);
    }

    [TestMethod]
    public void PreviousTrackOfFirstTrackIsNull()
    {
        // bug: CueTrack.GetChain reads Tracks[-1]
        var cue = CueSheet.ParseFromString(ThreeTracks);
        Assert.IsNull(cue.Tracks[0].PreviousTrack());
    }

    [TestMethod]
    public void PreviousTrackInFileOfFirstTrackIsNull()
    {
        // bug: CueTrack.GetChain reads Tracks[-1]
        var cue = CueSheet.ParseFromString(ThreeTracks);
        Assert.IsNull(cue.Tracks[0].PreviousTrackInFile());
    }

    [TestMethod]
    public void NextTrackOfLastTrackIsNull()
    {
        var cue = CueSheet.ParseFromString(ThreeTracks);
        Assert.IsNull(cue.Tracks[^1].NextTrack());
    }

    [TestMethod]
    public void RecordCopyOfTrackIsNotEqualToOriginal()
    {
        // bug: CueTrack is a record; a detached `with` copy compares equal and shares the index slice
        var cue = CueSheet.ParseFromString(ThreeTracks);
        var track = cue.Tracks[1];
        var copy = track with { };
        Assert.AreNotEqual(track, copy);
    }

    [TestMethod]
    public void CloneDoesNotMaterializeInheritedRemarksOnTracks()
    {
        // bug: CueTrack copy constructor copies inherited Genre/Upc as explicit remarks
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
        var clone = cue.Clone();
        Assert.IsNull(clone.Tracks[0].Remarks.GetField("GENRE"));
    }
}
