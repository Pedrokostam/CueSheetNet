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
    /// <summary>
    /// A sheet with a single file, track, index 01 00:00:01. No fields set.
    /// </summary>
    /// <returns></returns>
    public static CueSheet NewCue()
    {
        var cue = new CueSheet();
        cue.InsertFile(0, "Tescik.mp3", "WAVE", null);
        cue.InsertTrack(cue.Files[^1], TrackInsertParameters.Audio, CueTime.One);
        return cue;
    }
}
