using CueSheetNet.Exceptions;
using CueSheetNet.Syntax;
using System.Text.RegularExpressions;

namespace CueSheetNet.Tests;

[TestClass]
public class SheetParsingTests
{
    private static void AssertFormatError(string content)
    {
        try
        {
            CueSheet.ParseFromString(content);
        }
        catch (Exception e)
        {
            Assert.IsTrue(e is CueParsingException, $"Expected a format error, got {e.GetType().Name}: {e.Message}");
            return;
        }
        Assert.Fail("Expected a format error, nothing was thrown");
    }

    [TestMethod]
    public void RemBeforeFirstIndexOfFirstTrackBelongsToTrack()
    {
        // bug: CueReader.ParseREM uses SheetHasTracks (Data.Count > 0), which is false before track 1's first INDEX
        var cue = Samples.Base.Load();
        Assert.AreEqual("Composer One", cue.Tracks[0].Remarks.GetField("COMPOSER"));
        Assert.IsNull(cue.Remarks.GetField("COMPOSER"));
    }

    [TestMethod]
    public void OneWordRemIsKeptAsText()
    {
        // bug: CueReader.ParseREM slices with a negative length for a single-word REM
        var cue = CueSheet.ParseFromString("""
            REM foo
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
            """);
        Assert.IsTrue(cue.Remarks.Any(x => x.Line == "foo"));
    }

    [TestMethod]
    public void RemWithExtraSpacesParsesValue()
    {
        // bug: CueReader.ParseREM assumes exactly one space after REM
        var cue = CueSheet.ParseFromString("""
            REM   GENRE Rock
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
            """);
        Assert.AreEqual("Rock", cue.Genre);
    }

    [DataTestMethod]
    [DataRow("FILE \"a.wav\" WAVE\n  TRACK 01 AUDIO\n    INDEX 01", DisplayName = "INDEX without time")]
    [DataRow("FILE \"a.wav\" WAVE\n  TRACK 01 AUDIO\n    INDEX", DisplayName = "bare INDEX")]
    [DataRow("FILE \"a.wav\" WAVE\n  TRACK 01", DisplayName = "TRACK without type")]
    [DataRow("FILE \"a.wav\" WAVE\n  TRACK", DisplayName = "bare TRACK")]
    [DataRow("FILE \"a.wav\" WAVE\n  TRACK 01 AUDIO\n    PREGAP", DisplayName = "PREGAP without time")]
    [DataRow("FILE", DisplayName = "bare FILE")]
    public void TruncatedLineThrowsFormatError(string content)
    {
        // bug: truncated lines throw ArgumentOutOfRangeException from Slice
        AssertFormatError(content);
    }

    [DataTestMethod]
    [DataRow("INDEX 01 00:00:00", DisplayName = "INDEX before TRACK")]
    [DataRow("PREGAP 00:02:00", DisplayName = "PREGAP before TRACK")]
    public void MisplacedLineThrowsFormatError(string content)
    {
        // bug: CueReader.ParseIndex/ParseGap throw NotImplementedException
        AssertFormatError(content);
    }

    [TestMethod]
    public void UnknownKeywordThrowsFormatException()
    {
        Assert.ThrowsException<CueParsingException>(() => CueSheet.ParseFromString("FOO bar"));
    }

    [DataTestMethod]
    [DataRow("   ")]
    [DataRow("\t")]
    [DataRow("\t\t\t")]
    [DataRow("   ")] // nonbreaking space
    public void ExcessiveWhitespaceNotAProblem(string whitespaceReplacement)
    {
        var content = """
            REM GENRE Rock
            REM COMMENT "Basesheet"
            CATALOG 1234567890123
            PERFORMER "TestArtist"
            TITLE "TestAlbum"
            FILE "Disc.flac" WAVE
              TRACK 01 AUDIO
                TITLE "First"
                PERFORMER "TestArtist"
                REM COMPOSER "ComposerOne"
                INDEX 01 00:00:00
              TRACK 02 AUDIO
                TITLE "Second"
                PERFORMER "TestArtist"
                INDEX 00 03:00:00
                INDEX 01 03:02:00
            """;
        var replacedContent = Regex.Replace(content, @"[^\S\r\n]", whitespaceReplacement);
        var cue = CueSheet.ParseFromString(content);
        var cue2 = CueSheet.ParseFromString(replacedContent);
        TestSheets.AssertSheetsEqual(cue, cue2);
    }

    [TestMethod]
    public void ReaderSettingsForwardedToSheet()
    {
        var path = Samples.Base.Path;
        var cue = CueReader.CreateFromFile(path).WithQualifiedPaths(true).Parse();
        Assert.IsTrue(cue.Files[0].IsQualified);

    }

    [DataTestMethod]
    [DataRow("\n\n")]
    [DataRow("\r\r")]
    [DataRow("\r \n")]
    [DataRow("\n \r")]
    [DataRow("\n")]
    [DataRow("\r")]
    [DataRow("\r\n")]
    [DataRow("\n\r")]
    public void AnyNewlineWorks(string newline)
    {
        var content = Samples.Base.ReadText();
        var cue = CueSheet.ParseFromString(content);
        var replacedContent = content.ReplaceLineEndings(newline);
        var cue2 = CueSheet.ParseFromString(replacedContent);
        TestSheets.AssertSheetsEqual(cue, cue2, newline.Replace("\r","\\r").Replace("\n","\\n"));
    }

    [TestMethod]
    public void SongwriterIsRead()
    {
        var cue = CueSheet.ParseFromString("""
            SONGWRITER "Sheet Writer"
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                SONGWRITER "Track Writer"
                INDEX 01 00:00:00
              TRACK 02 AUDIO
                INDEX 01 01:00:00
            """);
        Assert.AreEqual("Sheet Writer", cue.Songwriter);
        Assert.AreEqual("Track Writer", cue.Tracks[0].Songwriter);
        Assert.AreEqual("Sheet Writer", cue.Tracks[1].Songwriter);
    }

    [TestMethod]
    public void CatalogIsrcAndFlagsAreRead()
    {
        var cue = CueSheet.ParseFromString("""
            CATALOG 1234567890123
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                FLAGS DCP 4CH PRE SCMS
                ISRC USABC1234567
                INDEX 01 00:00:00
            """);
        Assert.AreEqual("1234567890123", cue.Catalog);
        Assert.AreEqual("USABC1234567", cue.Tracks[0].Isrc);
        Assert.AreEqual(TrackFlags.DigitalCopyPermitted | TrackFlags.FourChannel | TrackFlags.PreEmphasis | TrackFlags.SerialCopyManagementSystem, cue.Tracks[0].Flags);
    }

    CueSheet TestItemJethro { get; set; } = default!;
    CueSheet TestItemBallet { get; set; } = default!;
    CueSheet TestItemMulti { get; set; } = default!;

    [TestInitialize]
    public void Init()
    {
        TestItemJethro = Samples.JethroTullAqualung.Load();
        TestItemBallet = Samples.SpandauBalletTrue.Load();
        TestItemMulti = Samples.MultiFile.Load();
    }

    [TestMethod]
    public void Title()
    {
        Assert.AreEqual("Aqualung", TestItemJethro.Title);
        Assert.AreEqual("True", TestItemBallet.Title);
        Assert.AreEqual("MultiFile", TestItemMulti.Title);
    }
    [TestMethod]
    public void Performer()
    {
        Assert.AreEqual("Jethro Tull", TestItemJethro.Performer);
        Assert.AreEqual("Spandau Ballet", TestItemBallet.Performer);
        Assert.AreEqual("The Multis", TestItemMulti.Performer);
    }

    [TestMethod]
    public void TrackTitle()
    {
        Assert.AreEqual("Aqualung", TestItemJethro.Tracks[0].Title);
        Assert.AreEqual("Pleasure", TestItemBallet.Tracks[0].Title);
        Assert.AreEqual("Uno", TestItemMulti.Tracks[0].Title);
    }
    [TestMethod]
    public void TrackPerformer()
    {
        Assert.AreEqual("Jethro Tull", TestItemJethro.Tracks[0].Performer);
        Assert.AreEqual("Spandau Ballet", TestItemBallet.Tracks[0].Performer);
        Assert.AreEqual("The Multis", TestItemMulti.Tracks[0].Performer);
    }

    [TestMethod]
    public void TestParsingMinimum()
    {
        var cue = Samples.MinimalFoobarCue.Load();
        Assert.AreEqual("A",cue.Files[0].Name);
        Assert.AreEqual("MinimalFoobarCue 01",cue.Tracks[0].Title);
        Assert.AreEqual(null,cue.Tracks[0].CueDuration );
        Assert.AreEqual(new CueTime(99), cue.Tracks[0].Start );
    }
}
