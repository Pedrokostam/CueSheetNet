using CueSheetNet.Exceptions;

namespace CueSheetNet.Tests;

[TestClass]
public class CueIndexTests
{
    // Base.cue: track 1 INDEX 01 00:00:00; track 2 INDEX 00 03:00:00, INDEX 01 03:02:00 — all in one file.
    private static CueSheet Load() => CueReader.CreateFromFile(Utils.GetFile("Equality", "Base.cue")).Parse();

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
        Assert.ThrowsException<InvalidIndexEditException>(() => cue.Indices[index].MoveTime(new CueTime(frames)));
        Assert.AreEqual(original, cue.Indices[index].Time);
    }
}
