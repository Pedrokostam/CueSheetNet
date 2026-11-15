using CueSheetNet.Components;
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
        Assert.IsTrue(cue.Tracks[1].Indices[^1] != lastTrackLastIndex);
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

    private const string ManyIndices = """
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
        """;

    private const string NoZerothIndices = """
        FILE "a.wav" WAVE
          TRACK 01 AUDIO
            INDEX 01 00:00:00
          TRACK 02 AUDIO
            INDEX 01 01:00:00
          TRACK 03 AUDIO
            INDEX 01 03:00:00
        """;

    private static void AssertPositionsAscending(CueTrack track)
    {
        for (int i = 1; i < track.Indices.Count; i++)
        {
            Assert.IsTrue(track.Indices[i - 1].Position < track.Indices[i].Position,
                $"{track}: index positions {string.Join(",", track.Indices.Select(x => x.Position))}");
        }
    }

    [TestMethod]
    public void TracksWithThreeOrMoreIndicesAreSlicedCorrectly()
    {
        var cue = CueSheet.ParseFromString(ManyIndices);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, cue.Tracks[0].Indices.Select(x => x.Position).ToArray());
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, cue.Tracks[1].Indices.Select(x => x.Position).ToArray());
        Assert.AreEqual(1, cue.Tracks[2].Indices.Count);
        Assert.AreEqual(3, cue.Files[0].Tracks.Count);
    }

    [TestMethod]
    public void InsertBeforeZerothIndexKeepsPositionsUnique()
    {
        // bug: CueSheet.InsertIndex gives Position 1 when the preceding link belongs to another track
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
        cue.InsertIndex(cue.Files[0], new CueTime(2, 59, 0));
        AssertPositionsAscending(cue.Tracks[1]);
    }

    [TestMethod]
    public void InsertBeforeFirstIndexDoesNotRenumberTrackStart()
    {
        // bug: CueSheet.InsertIndex silently turns the existing INDEX 01 into INDEX 02
        var cue = CueSheet.ParseFromString(NoZerothIndices);
        var start = cue.Tracks[2].Indices[0];
        try
        {
            cue.InsertIndex(cue.Files[0], new CueTime(2, 0, 0));
        }
        catch (Exception)
        {
            return;
        }
        Assert.AreEqual(1, start.Position);
    }

    [TestMethod]
    public void InsertAfterEacSplitIndexKeepsFileInTimeOrder()
    {
        // bug: CueSheet.InsertIndex places the link before the split INDEX 00 of the next track
        var cue = CueSheet.Parse(Utils.GetFile("LoadingTests", "MultiFile.cue"));
        cue.InsertIndex(cue.Files[0], new CueTime(2, 50, 0));
        var times = cue.Files[0].GetAllIndices().Select(x => x.Time).ToArray();
        CollectionAssert.AreEqual(times.Order().ToArray(), times);
    }

    [TestMethod]
    public void NextFileOfLastFileIsNull()
    {
        // bug: CueDataFile.NextFile compares against Data.Count instead of Files.Count
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
        Assert.IsNull(cue.Files[^1].NextFile());
    }

    [TestMethod]
    public void InsertTrackAfterLastIndexOfLastFile()
    {
        // bug: throws from CueDataFile.NextFile
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
        cue.InsertTrack(cue.Files[0], new TrackInsertParameters("AUDIO"), new CueTime(5, 0, 0));
        Assert.AreEqual(3, cue.Tracks.Count);
    }

    [TestMethod]
    public void InsertTrackWithoutFreeNumberKeepsPositionsUnique()
    {
        // bug: CueSheet.NormalizeGaps applies the gap to the inserted track as well
        var cue = CueSheet.ParseFromString("""
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
              TRACK 04 AUDIO
                INDEX 01 01:00:00
              TRACK 05 AUDIO
                INDEX 01 02:00:00
            """);
        cue.InsertTrack(cue.Files[0], new TrackInsertParameters("AUDIO"), new CueTime(1, 30, 0));
        var positions = cue.Tracks.Select(x => x.Position).ToArray();
        Assert.AreEqual(positions.Length, positions.Distinct().Count(), string.Join(",", positions));
    }

    [TestMethod]
    public void InsertTrackKeepsLaterNumberingGaps()
    {
        // bug: CueSheet.NormalizeGaps latches the first non-zero gap and applies it to all later tracks
        var cue = CueSheet.ParseFromString("""
            FILE "a.wav" WAVE
              TRACK 01 AUDIO
                INDEX 01 00:00:00
              TRACK 04 AUDIO
                INDEX 01 01:00:00
              TRACK 05 AUDIO
                INDEX 01 02:00:00
              TRACK 09 AUDIO
                INDEX 01 03:00:00
            """);
        var old = cue.Tracks.Skip(1).ToArray();
        cue.InsertTrack(cue.Files[0], new TrackInsertParameters("AUDIO"), new CueTime(0, 30, 0));
        CollectionAssert.AreEqual(new[] { 4, 5, 9 }, old.Select(x => x.Position).ToArray());
    }

    [TestMethod]
    public void InsertIndexWithNegativeTimeThrows()
    {
        var cue = CueSheet.Parse(Utils.GetFile("Equality", "Base.cue"));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => cue.InsertIndex(cue.Files[0], new CueTime(-1)));
    }

    [TestMethod]
    public void InsertIndexWithTrackFromOtherFileThrows()
    {
        var cue = CueSheet.Parse(Utils.GetFile("LoadingTests", "MultiFile.cue"));
        Assert.ThrowsException<ArgumentException>(() => cue.InsertIndex(cue.Files[1], new CueTime(1, 0, 0), cue.Tracks[0]));
    }
}
