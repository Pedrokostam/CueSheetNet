namespace CueSheetNet.Components;

internal readonly record struct Link(CueDataFile File, CueTrack Track, CueIndex Index)
{
    public override string ToString()
    {
        return $"Link: File {File.Position:d2}, Track {Track.Position:d2}, Index {Index.Position:d2} {Index.Time}";
    }
}