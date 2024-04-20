using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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
        Assert.AreEqual("Aqualung",TestItemJethro.Title);
        Assert.AreEqual("True",TestItemBallet.Title);
        Assert.AreEqual("MultiFile", TestItemMulti.Title);
    }
    [TestMethod]
    public void Performer()
    {
        Assert.AreEqual( "Jethro Tull",TestItemJethro.Performer);
        Assert.AreEqual( "Spandau Ballet",TestItemBallet.Performer);
        Assert.AreEqual( "The Multis", TestItemMulti.Performer);
    }
    [TestMethod]
    public void CueComment()
    {
        Assert.AreEqual("Comment value without quotes", TestItemJethro.Comments[0]);
        Assert.AreEqual("Comment value with quotes", TestItemJethro.Comments[1]);
    }
    [TestMethod]
    public void CueRemark()
    {
        Assert.AreEqual(new CueRemark("customremark","No quotes"), TestItemJethro.Remarks[0]);
        Assert.AreEqual(new CueRemark("customremark2", "With quotes"), TestItemJethro.Remarks[1]);
    }
    //[TestMethod]
    //public void Performer()
    //{
    //    Assert.AreEqual(TestItemJethro.Performer, "Jethro Tull");
    //    Assert.AreEqual(TestItemBallet.Performer, "Spandau Ballet");
    //    Assert.AreEqual(TestItemMulti.Performer, "The Multis");
    //}
}
