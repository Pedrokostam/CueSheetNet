namespace CueSheetNet.Components;

internal readonly record struct Link(CueDataFile File, CueTrack Track, CueIndex Index)
{
}