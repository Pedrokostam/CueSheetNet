using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CueSheetNet.FileHandling;
using Moq;
namespace CueSheetNet.Tests;
[TestClass]
public class SavingTests
{
    [TestInitialize]
    public void Init()
    {
    }
    CueReader reader = new();
    CueWriter writer = new();
    [TestMethod]
    public void LoadCloneSame()
    {
        foreach (var file in Utils.GetFiles("*.cue", "SavingTests"))
        {
            var cue = reader.ParseCueSheet(file);
            var clone = cue.Clone();
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
    [TestMethod]
    public void SaveReloadSame()
    {
        foreach (var file in Utils.GetFiles("*.cue", "SavingTests"))
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
        foreach (var file in Utils.GetFiles("*.cue", "SavingTests"))
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
