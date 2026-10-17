using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CueSheetNet.Syntax;
using System.Numerics;
#pragma warning disable MA0008 // Add StructLayoutAttribute

namespace CueSheetNet.TEST;

internal record struct Indor(int IndexNumber, int? TrackNumber, CueTime Time)
{

}

internal class Sequencer
{
    int _nextIndex = int.MinValue;
    int GetNextIndex()
    {
        var x = _nextIndex;
        _nextIndex++;
        return x;
    }
}

public class Remcom
{
    readonly List<CueRemark> _remarks = [];
    readonly List<string> _comments = [];

    public void AddRemark(string type, string value)
    {
        RemoveRemark(type);
        _remarks.Add(new CueRemark(type, value));
    }
    public void RemoveRemark(string type)
    {
        var old = _remarks.Find(x => x.Field.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (old is not null)
        {
            _remarks.Remove(old);
        }
    }
    public void RemoveRemark(CueRemark remark) => _remarks.Remove(remark);
    public void ClearRemarks() => _remarks.Clear();
    public IReadOnlyList<CueRemark> Remarks => _remarks.AsReadOnly();

    public void AddComment(string comment) => _comments.Add(comment);
    public void RemoveComment(string comment) => _comments.Remove(comment);
    public void ClearComments() => _comments.Clear();
    public IReadOnlyList<string> Comments => _comments.AsReadOnly();
}
internal class CFile
{
    internal int SequenceNumber { get; }
    public FileInfo File { get; set; }

    internal CFile(string filePath, int sequenceNumber)
    {
        SequenceNumber = sequenceNumber;
        File = new FileInfo(filePath);
    }

}
internal class Track : Remcom
{
    internal int SequenceNumber { get; }

    // The file this track belongs to

    // Track metadata (Performer and Songwriter can be inherited from the parent sheet)
    public string? Performer { get; set; }
    public string? Songwriter { get; set; }

    public string? Isrc { get; set; }
    // Title is not inherited from the sheet and must be present on the track if needed
    public string? Title { get; set; }

    // Index records for the track (INDEX 00..99)
    public List<Indor> Indices { get; } = new List<Indor>();

    // Pregap/Postgap values (optional)
    public CueTime? Pregap { get; set; }
    public CueTime? Postgap { get; set; }

    public TrackType Type { get; internal set; }

    // Arbitrary flags (DCP, 4CH, PRE, SCMS, etc.)
    public TrackFlags Flags { get; }
    internal Track(string type)
    {
        Type = TrackType.FromString(type);
    }
}

public class Sheet : Remcom
{
    readonly Sequencer _fileSequencer = new Sequencer();
    readonly Sequencer _trackSequencer = new Sequencer();
    public string? Catalog { get; set; }
    public FileInfo? CdTextFile { get; private set; }
    public string? Composer { get; set; }
    // General sheet-level metadata that can be inherited by tracks
    public string? Performer { get; set; }
    public string? Songwriter { get; set; }
    public string? Title { get; set; }
    // Remarks/REMs - store as simple list of (type, value) pairs

    List<CFile> Files { get; } = new List<CFile>();
    internal int GetIndexOfFile(CFile file) => Files.IndexOf(file);
    internal int GetTrackBeforeFile(CFile file) => Files.TakeWhile(f => f != file).Sum(f => f.Tracks.Count);

}

#pragma warning restore MA0008 // Add StructLayoutAttribute