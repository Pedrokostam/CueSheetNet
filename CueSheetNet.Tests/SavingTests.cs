using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;
[TestClass]
public class SavingTests
{
    CueReader reader = new();
    CueWriter writer = new();
    [TestMethod]
    public void SaveReloadSame()
    {
        string dir = Path.Join(Directory.GetCurrentDirectory(), "TestItems", "SavingTests");
        foreach (var file in Directory.EnumerateFiles(dir, "*.cue"))
        {
            var cue = reader.ParseCueSheet(file);
            var clone = cue.Clone();
            var temp = Path.GetTempFileName();
            writer.SaveCueSheet(cue, temp);
            var cue2 = reader.ParseCueSheet(temp);
            if (Path.GetFileNameWithoutExtension(file).Contains("wrong", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreNotEqual(clone, cue2, Path.GetFileNameWithoutExtension(file));
            }
            else
            {
                Assert.AreEqual(clone, cue2, Path.GetFileNameWithoutExtension(file));
            }
            File.Delete(temp);
        }
    }
    [TestMethod]
    public void SaveCloneSame()
    {
        string dir = Path.Join(Directory.GetCurrentDirectory(), "TestItems", "SavingTests");
        foreach (var file in Directory.EnumerateFiles(dir, "*.cue"))
        {
            var cue = reader.ParseCueSheet(file);
            var clone = cue.Clone();
            var temp = Path.GetTempFileName();
            writer.SaveCueSheet(cue, temp);
            File.Delete(temp);
            if (Path.GetFileNameWithoutExtension(file).Contains("wrong", StringComparison.OrdinalIgnoreCase))
            {
                Assert.AreNotEqual(clone, cue, Path.GetFileNameWithoutExtension(file));
            }
            else
            {
                Assert.AreEqual(clone, cue, Path.GetFileNameWithoutExtension(file));
            }
        }
    }
}
