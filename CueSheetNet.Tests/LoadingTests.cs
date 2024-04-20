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
        return  CueSheet.Read(Utils.GetFile("LoadingTests", name));
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
