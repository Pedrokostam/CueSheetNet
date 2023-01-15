using System.Diagnostics;
using System.Xml.Schema;

namespace CueSheetNet;

[DebuggerDisplay("CIImpl- Num: {Number}, Abs: {Index} - File {File.Index}, Track {Track.Index}")]
internal class CueIndexImpl : CueItemBase
{
    public CueFile File { get; }

    public CueTrack Track { get; }

    public CueIndexImpl(CueTrack track, CueFile file) : base(file.ParentSheet)
    {
        Track = track;
        File = file;
    }

    internal CueIndexImpl ClonePartial(CueTrack newOwnerTrack, CueFile newOwnerFile)
    {
        return new(newOwnerTrack, newOwnerFile)
        {
            Index= Index,
            Time= Time,
            Orphaned=Orphaned,
            Number=Number,
        };
    }

    public int Index { get; internal set; }

    public int Number { get; internal set; }

    public CueTime Time { get; set; }
    public override string ToString()
    {
        return "CueIndexImpl "+Index.ToString("D2")+", " + Number.ToString("D2") + ", " + File.Index.ToString("D2") + ", " + Track.Index.ToString("D2");
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(File.GetHashCode(), Track.GetHashCode(), Time.GetHashCode(), Number.GetHashCode());
    }
}
