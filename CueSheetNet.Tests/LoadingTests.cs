using CueSheetNet.Tests;
using CueSheetNet;
using CueSheetNet.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;

[TestClass]
public  class LoadingTests
{
    CueSheet TestItemJethro { get; set; } = default!;
    CueSheet TestItemBallet { get; set; } = default!;
    CueSheet TestItemMulti { get; set; } = default!;

    [TestInitialize]
    public void Init()
    {
        TestItemJethro = Load("Jethro Tull - Aqualung.cue");
        TestItemBallet = Load("Spandau Ballet - True.cue");
        TestItemMulti = Load("MultiFile.cue");
    }
    private CueSheet Load(string name)
    {
        return CueReader.CreateFromFile(Utils.GetFile("LoadingTests", name)).Parse();
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
    public void TracKTitle()
    {
        Assert.AreEqual("Aqualung", TestItemJethro.Tracks[0].Title);
        Assert.AreEqual("Pleasure", TestItemBallet.Tracks[0].Title);
        Assert.AreEqual("Uno", TestItemMulti.Tracks[0].Title);
    }
    [TestMethod]
    public void TracKPerformer()
    {
        Assert.AreEqual("Jethro Tull", TestItemJethro.Tracks[0].Performer);
        Assert.AreEqual("Spandau Ballet", TestItemBallet.Tracks[0].Performer);
        Assert.AreEqual("The Multis", TestItemMulti.Tracks[0].Performer);
    }
    //[TestMethod]
    //public void Performer()
    //{
    //    Assert.AreEqual("Jethro Tull", TestItemJethro.Performer);
    //    Assert.AreEqual("Spandau Ballet", TestItemBallet.Performer);
    //    Assert.AreEqual("The Multis", TestItemMulti.Performer);
    //}
}
