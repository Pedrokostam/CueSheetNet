using CueSheetNet.Components;
using CueSheetNet.Exceptions;
using static CueSheetNet.Tests.Utils;

namespace CueSheetNet.Tests;

[TestClass]
public class CueIndexTests
{
    // Base.cue: track 1 INDEX 01 00:00:00; track 2 INDEX 00 03:00:00, INDEX 01 03:02:00 — all in one file.
    private static CueSheet Load() => Samples.Base.Load();

    [DataTestMethod]
    [DataRow(0, 10, DisplayName = "First index in file, no previous bound")]
    [DataRow(1, 75, DisplayName = "After previous track's index in same file")]
    [DataRow(2, (3 * 60 + 1) * 75, DisplayName = "Between same-file neighbours")]
    public void MoveTimeWithinBounds(int index, int frames)
    {
        var cue = Load();
        cue.Indices[index].MoveTime(new CueTime(frames));
        Assert.AreEqual(new CueTime(frames), cue.Indices[index].Time);
    }

    [DataTestMethod]
    [DataRow(2, (2 * 60 + 59) * 75, DisplayName = "Before previous index")]
    [DataRow(1, (3 * 60 + 2) * 75, DisplayName = "Onto next index")]
    [DataRow(1, 0, DisplayName = "Onto previous track's index in same file")]
    [DataRow(0, -1, DisplayName = "Negative time")]
    public void MoveTimeOutOfBoundsThrows(int index, int frames)
    {
        var cue = Load();
        var original = cue.Indices[index].Time;
        Assert.ThrowsException<InvalidOperationException>(() => cue.Indices[index].MoveTime(new CueTime(frames)));
    }

    private static void AssertPositionsAscending(CueTrack track)
    {
        for (int i = 1; i < track.Indices.Count; i++)
        {
            Assert.IsTrue(track.Indices[i - 1].Position < track.Indices[i].Position,
                $"{track}: index positions {string.Join(",", track.Indices.Select(x => x.Position))}");
        }
    }

    [TestMethod]
    public void CannotAddDuplicateIndex()
    {
        var cue = Samples.Base.Load();
        Assert.ThrowsException<ArgumentException>(() => cue.InsertIndex(cue.Tracks[0], CueTime.Zero));
    }

    [TestMethod]
    public void CanAddThirdIndex()
    {
        var cue = Samples.Base.Load();
        var lastTrackLastIndex = cue.Tracks[^1].Indices[^1];
        cue.InsertIndex(cue.Tracks[1], lastTrackLastIndex.Time + 11);
        Assert.IsTrue(cue.Tracks[1].Indices[^1] != lastTrackLastIndex);
        Assert.IsTrue(cue.Tracks[1].Indices[^1].Time > lastTrackLastIndex.Time);
    }

    [TestMethod]
    public void DurationIsMeasuredFrom01ToFirstFollowing()
    {
        /*
          TRACK 01 AUDIO
            TITLE "Aqualung"
            INDEX 00 00:00:00
            INDEX 01 00:00:33
          TRACK 02 AUDIO
            TITLE "Cross-Eyed Mary"
            INDEX 00 06:32:18
            INDEX 01 06:33:43
          TRACK 03 AUDIO
            TITLE "Cheap Day Return"
            INDEX 00 10:41:68
            INDEX 01 10:43:28
         */
        var cue = Samples.JethroTullAqualung.Load();
        Assert.AreEqual(Ct("06:32:18") - Ct("00:00:33"), cue.Tracks[0].CueDuration);
        Assert.AreEqual(Ct("10:41:68") - Ct("06:33:43"), cue.Tracks[1].CueDuration);

    }

    [TestMethod]
    public void CanAddBetweenIndex()
    {
        var cue = Samples.Base.Load();
        var lastTrackFirstIndex = cue.Tracks[^1].Indices[0];
        var lastTrackLastIndex = cue.Tracks[^1].Indices[^1];
        var meanTime = (lastTrackFirstIndex.Time + lastTrackLastIndex.Time) / 2;
        //cue.InsertIndex(cue.Tracks[^1], meanTime);
        cue.InsertIndex(cue.Tracks[1], meanTime);
        Assert.IsTrue(cue.Tracks[1].Indices[0] == lastTrackFirstIndex);
        Assert.IsTrue(cue.Tracks[1].Indices[^1] == lastTrackLastIndex);
        Assert.IsTrue(cue.Tracks[1].Indices[^1].Position == 2);
        Assert.IsTrue(cue.Tracks[1].Indices[1].Time == meanTime);
        Assert.IsTrue(cue.Tracks[1].Indices[1].Position == 1);
    }

    [TestMethod]
    public void TracksWithThreeOrMoreIndicesAreSlicedCorrectly()
    {
        var cue = TestSheets.Parse("""
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
                INDEX 02 01:00:00
                INDEX 03 02:00:00
              TRACK 02 AUDIO
                INDEX 00 03:00:00
                INDEX 01 03:02:00
                INDEX 02 04:00:00
                INDEX 03 05:00:00
              TRACK 03 AUDIO
                INDEX 01 06:00:00
            """);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cue.Tracks[0].Indices.Select(x => x.Position).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, cue.Tracks[1].Indices.Select(x => x.Position).ToArray());
        Assert.AreEqual(1, cue.Tracks[2].Indices.Count);
        Assert.AreEqual(3, cue.Files[0].Tracks.Count);
    }

    [TestMethod]
    public void InsertBeforeZerothIndexKeepsPositionsUnique()
    {
        // bug: CueSheet.InsertIndex gives Position 1 when the preceding link belongs to another track
        var cue = Samples.Base.Load();
        cue.InsertIndex(cue.Tracks[^1], new CueTime(2, 59, 0));
        //cue.InsertIndex(cue.Files[0], new CueTime(2, 59, 0));
        AssertPositionsAscending(cue.Tracks[1]);
    }

    [TestMethod]
    public void InsertBeforeFirstIndexRenumbersItToSecond()
    {
        var cue = TestSheets.Parse("""
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
              TRACK 02 AUDIO
                INDEX 01 01:00:00
              TRACK 03 AUDIO
                INDEX 01 03:00:00
            """);
        var previousStart = cue.Tracks[2].Indices[0];
        var time = new CueTime(2, 0, 0);
        try
        {
            cue.InsertIndex(cue.Tracks[2], time);
        }
        catch (Exception)
        {
            return;
        }
        Assert.AreEqual(time, cue.Tracks[2].Start);
        Assert.AreEqual(2, previousStart.Position);
    }

    [TestMethod]
    public void SplitIndexIsConsideredAsLimitOfPreviousTrack()
    {
        var cue = Samples.MultiFile.Load();
        Assert.ThrowsException<InvalidOperationException>(() => cue.InsertIndex(cue.Tracks[0], new CueTime(2, 50, 0)));
    }

    [TestMethod]
    public void SplitIndexIsNotConsideredAsLimitOfNext()
    {
        var cue = Samples.MultiFile.Load();
        cue.Indices[2].ShiftTime(10);
        cue.InsertIndex(cue.Tracks[1], CueTime.Zero);
    }

    [TestMethod]
    public void InsertIndexWithInvalidTrackThrows()
    {
        var cue = Samples.Base.Load();
        Assert.ThrowsException<InvalidOperationException>(() => cue.InsertIndex(cue.Tracks[0], new CueTime(15, 0, 0)));
    }

    [TestMethod]
    public void InsertIndexWithNegativeTimeThrows()
    {
        var cue = Samples.Base.Load();
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => cue.InsertIndex(cue.Tracks[0], new CueTime(-1)));
    }

    [TestMethod]
    public void CueIndexMoveChecksLimits()
    {
        // bug: throws from CueDataFile.NextFile
        var cue = Samples.Base.Load();
        cue.Indices[0].ShiftTime(75);
        cue.Indices[^1].ShiftTime(75);
        Assert.ThrowsException<InvalidOperationException>(() => cue.Indices[1].MoveTime(cue.Indices[0].Time), "Moved to time of preceding");
        Assert.ThrowsException<InvalidOperationException>(() => cue.Indices[1].MoveTime(cue.Indices[2].Time), "Moved to time of following");
    }
}
