using System.Globalization;
using System.Text.RegularExpressions;

namespace CueSheetNet.Tests;

[TestClass]
public class WritingTests
{
    [TestMethod]
    public void RoundTripKeepsTrackRemarkUnderTrack()
    {
        // bug: RoundTripTests compares sorted lines, which hides REM COMPOSER moving from track 1 to the sheet (CueReader.ParseREM)
        var cue = Samples.Base.Load();
        var lines = CueWriter.Create().WithRedundantFields(RedundantFieldBehavior.IfExplicitlySet).WriteToString(cue)
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).ToList();
        int track = lines.FindIndex(x => x.StartsWith("TRACK 01", StringComparison.Ordinal));
        int composer = lines.FindIndex(x => x.StartsWith("REM COMPOSER", StringComparison.Ordinal));
        Assert.IsTrue(composer > track, string.Join(" | ", lines));
    }

    [TestMethod]
    public void ExplicitFieldsSettingsWork()
    {
        // has performer
        var reg = new Regex("PERFORMER \"Judas Priest\"");
        var cue = Samples.JudasPriestScreamingForVengeance.Load();
        var perfCount = reg.Count(Samples.JudasPriestScreamingForVengeance.ReadText());
        var writtenEx = CueWriter.Create().WithRedundantFields(RedundantFieldBehavior.IfExplicitlySet).WriteToString(cue);
        Assert.AreEqual(reg.Count(writtenEx), perfCount);
        var writtenDif = CueWriter.Create().WithRedundantFields(RedundantFieldBehavior.Never).WriteToString(cue);
        Assert.AreEqual(reg.Count(writtenDif), 1);
        cue.Tracks[0].Performer = "Jude Pastor";
        var reg2 = new Regex("PERFORMER \"Jude Pastor\"");
        var writtenNev = CueWriter.Create().WithRedundantFields(RedundantFieldBehavior.Never).WriteToString(cue);
        Assert.AreEqual(reg.Count(writtenNev), 1);
        Assert.AreEqual(reg2.Count(writtenNev), 1);
    }

    [TestMethod]
    public void NewlineInTitleDoesNotInjectLines()
    {
        // bug: CueWriter does not escape newlines inside values
        var cue = Samples.Base.Load();
        cue.Title = "first\nsecond";
        var written = cue.WriteToString();
        CueSheet.ParseFromString(written);
    }

    [TestMethod]
    public void QuoteInFilePathRoundTrips()
    {
        // bug: CueWriter does not escape quotes in FILE paths
        var cue = new CueSheet();
        cue.InsertFile(0, "a\"b.wav", "WAVE");
        string written;
        try
        {
            written = cue.WriteToString();
        }
        catch (Exception)
        {
            return;
        }
        var reparsed = CueSheet.ParseFromString(written);
        Assert.AreEqual("a\"b.wav", reparsed.Files[0].Path);
    }

    [TestMethod]
    public void SaveFallsBackWhenSourceEncodingCannotRepresentText()
    {
        // bug: CueWriter documents a fallback to the default encoding but throws EncoderFallbackException
        var cue = Samples.Cp1252.Load();
        const string nonAscii = "残酷な天使のテーゼ";
        cue.Title = nonAscii;
        string dir = Path.Combine("TestItems", "TempData", "WritingEncoding");
        Directory.CreateDirectory(dir);
        string target = Path.Combine(dir, "out.cue");
        cue.SaveAs(target);
        Assert.AreEqual(nonAscii, CueSheet.Parse(target).Title);
    }

    [TestMethod]
    public void ReplayGainIsCultureInvariant()
    {
        // bug: CueExtensions stores ReplayGain with the current culture but parses it with InvariantCulture
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pl-PL"); // uses comma as separator
            var cue = Samples.Base.Load();
            cue.ReplayGainAlbumGain = 1.5;
            Assert.AreEqual(1.5, cue.ReplayGainAlbumGain);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
