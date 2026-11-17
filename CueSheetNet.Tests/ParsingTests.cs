using CueSheetNet.Exceptions;
using CueSheetNet.Syntax;
using System.Text.RegularExpressions;

namespace CueSheetNet.Tests;

[TestClass]
public class ParsingTests
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
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
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
        Assert.AreEqual(cue, cue2);
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
        var content = File.ReadAllText(Utils.GetFile("Equality", "Base.cue"));
        var cue = CueSheet.ParseFromString(content);
        var replacedContent = content.ReplaceLineEndings(newline);
        var cue2 = CueSheet.ParseFromString(replacedContent);
        Assert.AreEqual(cue, cue2,message: newline.Replace("\r","\\r").Replace("\n","\\n"));
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
}
