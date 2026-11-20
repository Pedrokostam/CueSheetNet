namespace CueSheetNet.Tests;

[TestClass]
public class CloningTests
{
    [TestMethod("Test if cloned sheet is equal to its source")]
    [DynamicData(nameof(Samples.SaveRows), typeof(Samples))]
    public void LoadCloneSame(Sample sample)
    {
        var cue = sample.Load();
        RoundTripTests.AssertSame(cue.Clone(), cue, sample);
    }

    [TestMethod]
    public void CloneDoesNotShareCdTextFile()
    {
        // bug: CueExtensions.Clone copies CdTextFile by reference; it still belongs to the original sheet
        var cue = TestSheets.Parse("""
            CDTEXTFILE "disc.cdt"
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
            """);
        Assert.IsNotNull(cue.CdTextFile);
        Assert.IsFalse(ReferenceEquals(cue.CdTextFile, cue.Clone().CdTextFile));
    }

    [TestMethod]
    public void CloneDoesNotMaterializeInheritedRemarksOnTracks()
    {
        // bug: CueTrack copy constructor copies inherited Genre/Upc as explicit remarks
        var cue = Samples.Base.Load();
        var clone = cue.Clone();
        Assert.IsNull(clone.Tracks[0].Remarks.GetField("GENRE"));
    }
}
