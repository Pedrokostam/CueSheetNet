using CueSheetNet.Comparing;
using CueSheetNet.Components;

namespace CueSheetNet.Tests;

internal static class TestSheets
{
    public static CueSheet Parse(string text) => CueSheet.ParseFromString(text);

    public static void AssertSheetsEqual(CueSheet expected, CueSheet actual, string? message = null)
    {
        var diff = CueSheetEqualityComparer.CurrentCulture.FindFirstDifference(expected, actual);
        Assert.IsNull(diff, message is null ? diff?.ToString() : $"{message}: {diff}");
    }

    public static CueSheet NewCue()
    {
        var cue = new CueSheet();
        cue.InsertFile(0, "Tescik.mp3", "WAVE", null);
        cue.InsertTrack(cue.Files[^1], new TrackInsertParameters("AUDIO"), CueTime.One);
        return cue;
    }
}
