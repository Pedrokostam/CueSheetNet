using CueSheetNet;
using CueSheetNet.Components;
using CueSheetNet.Tests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheetNet.Tests;

[TestClass]
public  class LoadingTests
{
    Sheet TestItemJethro { get; set; } = default!;
    Sheet TestItemBallet { get; set; } = default!;
    Sheet TestItemMulti { get; set; } = default!;

    [TestInitialize]
    public void Init()
    {
        TestItemJethro = Load("Jethro Tull - Aqualung.cue");
        TestItemBallet = Load("Spandau Ballet - True.cue");
        TestItemMulti = Load("MultiFile.cue");
    }
    private Sheet Load(string name)
    {
        return new CueReader().ParseCueSheet(Utils.GetFile("LoadingTests", name));
    }
    [TestMethod]
    public void Title()
    {
        Assert.AreEqual(TestItemJethro.Title, "Aqualung");
        Assert.AreEqual(TestItemBallet.Title, "True");
        Assert.AreEqual(TestItemMulti.Title, "MultiFile");
    }
    [TestMethod]
    public void Performer()
    {
        Assert.AreEqual(TestItemJethro.Performer, "Jethro Tull");
        Assert.AreEqual(TestItemBallet.Performer, "Spandau Ballet");
        Assert.AreEqual(TestItemMulti.Performer, "The Multis");
    }
    //[TestMethod]
    //public void Performer()
    //{
    //    Assert.AreEqual(TestItemJethro.Performer, "Jethro Tull");
    //    Assert.AreEqual(TestItemBallet.Performer, "Spandau Ballet");
    //    Assert.AreEqual(TestItemMulti.Performer, "The Multis");
    //}
}
