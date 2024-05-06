using System.Diagnostics;

namespace CueSheetNet.Internal;

[DebuggerDisplay("CIImpl- Num: {Number}, Abs: {Index} - File {File.Index}, Track {Track.Index}")]
internal sealed class CueIndexImpl(CueTrack track, CueDataFile file) : CueItemBase(file.ParentSheet)
{
    public CueDataFile File { get; } = file;

    public CueTrack Track { get; } = track;

    internal CueIndexImpl ClonePartial(CueTrack newOwnerTrack, CueDataFile newOwnerFile)
    {
        return new(newOwnerTrack, newOwnerFile)
        {
            Index = Index,
            Time = Time,
            Orphaned = Orphaned,
            Number = Number,
        };
    }

    /// <inheritdoc cref="CueIndex.AbsoluteIndex"/>
    public int Index { get; internal set; }

    /// <inheritdoc cref="CueIndex.Number"/>
    public int Number { get; internal set; }

    /// <inheritdoc cref="CueIndex.Time"/>
    public CueTime Time { get; set; }

    public override string ToString()
    {
        return "CueIndexImpl "
            + Index.ToString("D2")
            + ", "
            + Number.ToString("D2")
            + ", "
            + File.Index.ToString("D2")
            + ", "
            + Track.Index.ToString("D2");
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            File.GetHashCode(),
            Track.GetHashCode(),
            Time.GetHashCode(),
            Number.GetHashCode()
        );
    }

    //public static explicit operator CueIndex(CueIndexImpl cimpl) => new(cimpl);
}
