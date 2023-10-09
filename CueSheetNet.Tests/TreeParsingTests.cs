using CueSheetNet.FileHandling;
using CueSheetNet.Logging;
using CueSheetNet.NameParsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;
[TestClass]
public class TreeParsingTests
{
    [TestMethod("Test if all parse token are valid")]
    public void TestAllAvailable()
    {
        var t = CueTreeFormatter.AvailableProperties;
        Assert.AreNotEqual(t.Length, 0);
    }
    private static string P(CueSheet sheet, string pattern) => CueTreeFormatter.ParseFormatPattern(sheet, pattern);
    [TestMethod("Compares various parse pattern to their parsed results")]
    public void TestParsing()
    {
        string path = Utils.GetFile("SavingTests", "Jethro Tull - Aqualung.cue");
        CueSheet sheet = CueSheet.Read(path);
        string oldName = Path.GetFileNameWithoutExtension(sheet.SourceFile.FullName);
        Assert.AreEqual(oldName, P(sheet, null));
        Assert.AreEqual(oldName, P(sheet, ""));
        Assert.AreEqual(oldName, P(sheet, "%old%"));
        Assert.AreEqual(oldName, P(sheet, "%current%"));
        Assert.AreEqual(oldName, P(sheet, "%Name%"));
        Assert.AreEqual("Jethro Tull", P(sheet, "%artist%"));
        Assert.AreEqual("Jethro Tull", P(sheet, "%performer%"));
        Assert.AreEqual("Aqualung", P(sheet, "%album%"));
        Assert.AreEqual("Aqualung", P(sheet, "%title%"));
        Assert.AreEqual("970A2F0B", P(sheet, "%DISCID%"));
        string expected = "Jethro Tull" + Path.DirectorySeparatorChar + "1971" + Path.DirectorySeparatorChar + "Aqualung";
        Assert.AreEqual(expected, P(sheet, "%artist%/%date%/%title%"));
        Assert.AreEqual(expected, P(sheet, "%ARtist%/%DAte%/%tItLE%"));
        ArrayLogger arlog = new(LogLevel.Warning);
        sheet.Date = null;
        sheet.DiscID = null;
        Logger.Register(arlog);
        Assert.AreEqual(oldName, P(sheet, "%Discid%/%Year%"));
        var log = arlog.LogEntries.Where(x => x.Level == LogLevel.Warning).FirstOrDefault();
        Assert.IsNotNull(log); 
    }
}
