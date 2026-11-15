using CueSheetNet.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Tests;


[TestClass]
public class EditingTests
{
    [TestMethod]
    public void CannotAddDuplicateIndex()
    {
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
        Assert.ThrowsException<DuplicateTimeException>(() => cue.InsertIndex(cue.Files[0], CueTime.Zero));
    }

    [TestMethod]
    public void CanAddThirdIndex()
    {
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
        var lastTrackLastIndex = cue.Tracks[^1].Indices[^1];
        cue.InsertIndex(cue.Files[0], lastTrackLastIndex.Time + 11);
        Assert.IsTrue(cue.Tracks[1].Indices[^1] == lastTrackLastIndex);
        Assert.IsTrue(cue.Tracks[1].Indices[^1].Time > lastTrackLastIndex.Time);
    }

    [TestMethod]
    public void CanAddBetweenIndex()
    {
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
        var lastTrackFirstIndex = cue.Tracks[^1].Indices[0];
        var lastTrackLastIndex = cue.Tracks[^1].Indices[^1];
        var meanTime = (lastTrackFirstIndex.Time + lastTrackLastIndex.Time) / 2;
        cue.InsertIndex(cue.Files[0], meanTime);
        Assert.IsTrue(cue.Tracks[1].Indices[0] == lastTrackFirstIndex);
        Assert.IsTrue(cue.Tracks[1].Indices[^1] == lastTrackLastIndex);
        Assert.IsTrue(cue.Tracks[1].Indices[1].Time == meanTime);
    }
}
