namespace CueSheetNet.Comparing;

/// <summary>Where two sheets first differ. <see cref="Path"/> locates the field, the values are what was compared.</summary>
public sealed record CueDifference(string Path, object? Expected, object? Actual)
{
    public override string ToString() => $"{Path}: expected <{Expected ?? "null"}> but was <{Actual ?? "null"}>";
}
