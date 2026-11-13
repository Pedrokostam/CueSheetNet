using CueSheetNet.NameParsing;
using CueSheetNet.Tests;
using System.Xml.Linq;

namespace CueSheetNet.Tests;

[TestClass]
public class TreeParsingTests
{
    [TestMethod("Test if all parse token are valid")]
    public void TestAllAvailable()
    {
        var t = CueTreeFormatter.AvailableProperties;
        Assert.AreNotEqual(0, t.Length);
    }
    private static string P(CueSheet sheet, string? pattern, string? replacement = null) => CueTreeFormatter.ParseFormatPattern(sheet, pattern, replacement);

    [DataTestMethod]
    [DataRow("Jethro Tull - Aqualung", null, DisplayName = "Null pattern default to filename")]
    [DataRow("Jethro Tull - Aqualung", "", DisplayName = "Empty pattern defaults to filename")]
    [DataRow("Jethro Tull - Aqualung", "%oLd%", DisplayName = "%old% return filename")]
    [DataRow("Jethro Tull - Aqualung", "%cuRrent%", DisplayName = "%current% returns filename")]
    [DataRow("Jethro Tull - Aqualung", "%naMe%", DisplayName = "%name% returns filename")]
    [DataRow("Jethro Tull", "%artist%", DisplayName = "%artist%")]
    [DataRow("Jethro Tull", "%performer%", DisplayName = "%performer%")]
    [DataRow("Aqualung", "%album%", DisplayName = "%album%")]
    [DataRow("Aqualung", "%title%", DisplayName = "%title%")]
    [DataRow("970A2F0B", "%DISCID%", DisplayName = "%discid%")]
    [DataRow("Aqualung TEST", "%album% TEST", DisplayName = "token with verbatim text")]
    [DataRow("TEST Aqualung TEST", "TEST %album% TEST", DisplayName = "token surround by verbatim text")]
    [DataRow("Aqualung", "%album%/", DisplayName = "Drop dangling path separator")]
    [DataRow("Aqualung", "%album%/   ", DisplayName = "Drop dangling path separator followed by spaces")]
    [DataRow("Jethro Tull/1971/Aqualung", "%artist%/%date%/%title%", DisplayName = "%artist%/%date%/%title%")]
    [DataRow("Jethro Tull/1971/Aqualung", "%ARtist%/%DAte%/%tItLE%", DisplayName = "%artist%/%date%/%title% mixed case")]
    [DataRow("970A2F0B\\1971", "%Discid%\\%Year%", DisplayName = "Preserve double slash")]
    public void TestParsing(string expected, string? pattern)
    {
        string path = Utils.GetFile("SavingTests", "Jethro Tull - Aqualung.cue");
        CueSheet sheet = CueReader.CreateFromFile(path).Parse();

        Assert.AreEqual(expected, P(sheet, pattern));
    }

    [DataTestMethod]
    [DataRow("ABCDEFGH", "%Catalog%/%year%", null, DisplayName = "Drop dangling path separator")]
    [DataRow("ABCDEFGH", "%Catalog%/%year%", " ", DisplayName = "Drop dangling path separator followed by space")]
    [DataRow("ABCDEFGH/TEST", "%Catalog%/%year%TEST", null, DisplayName = "Verbatim text after missing")]
    [DataRow("ABCDEFGH//TEST", "%Catalog%/%year%/TEST", null, DisplayName = "Keep double slash")]
    [DataRow("ABCDEFGH/NONE", "%Catalog%/%year%", "NONE", DisplayName = "Replace with string, keeping last slash")]
    public void TestParsingWithMissingDate(string expected, string? pattern, string? replacement)
    {
        var sheet = new CueSheet
        {
            Catalog = "ABCDEFGH"
        };
        Assert.AreEqual(expected, P(sheet, pattern, replacement));
    }
}
