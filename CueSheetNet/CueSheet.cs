using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace CueSheetNet;

[Flags]
public enum CueType
{
    Unknown = 0,

    /// <summary>Single continuous file</summary>
    SingleFile = 0b1,

    /// <summary>Multiple files</summary>
    MultipleFiles = 0b10,

    /// <summary>Gaps trimmed from files and simulated</summary>
    SimulatedGaps = 0b1000,

    /// <summary>Gaps appended to previous tracks</summary>
    GapsAppended = 0b10000,

    /// <summary>Gaps prepended to next tracks</summary>
    GapsPrepended = 0b100000,

    /// <summary>Hidden Track One Audio</summary>
    HTOA = 0b1000000,

    /// <summary>Gaps appended to next tracks</summary>
    EacStyle = MultipleFiles | GapsAppended,

    MultipleFilesWithAppendedGaps = EacStyle,
    SingleFileWithHiddenTrackOneAudio = SingleFile | HTOA,
    MultipleFilesWithPrependedGaps = MultipleFiles | GapsPrepended,
    MultipleFileWithSimulatedGaps = MultipleFiles | SimulatedGaps,
}

public class CueSheet : IEquatable<CueSheet>, IRemarkableCommentable
{
    #region Rem

    internal readonly List<Remark> RawRems = new();
    public ReadOnlyCollection<Remark> Remarks => RawRems.AsReadOnly();

    public void AddRemark(string type, string value) => AddRemark(new Remark(type, value));

    public void AddRemark(Remark entry) => RawRems.Add(entry);
    public void AddRemark(IEnumerable<Remark> entries)
    {
        foreach (Remark remark in entries)
        {
            RawRems.Add(remark);
        }
    }

    public void ClearRemarks() => RawRems.Clear();
    public void RemoveRemark(int index)
    {
        if (index >= 0 || index < RawRems.Count)
            RawRems.RemoveAt(index);
    }

    public void RemoveRemark(string field, string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase) => RemoveRemark(new Remark(field, value), comparisonType);

    public void RemoveRemark(Remark entry, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        int ind = RawRems.FindIndex(x => x.Equals(entry, comparisonType));
        if (ind >= 0)
            RawRems.RemoveAt(ind);
    }

    #endregion Rem

    #region Comments

    internal readonly List<string> RawComments = new();
    public ReadOnlyCollection<string> Comments => RawComments.AsReadOnly();

    public void AddComment(IEnumerable<string> comments)
    {
        foreach (string comment in comments)
        {
            AddComment(comment);
        }
    }
    public void AddComment(string comment) => RawComments.Add(comment);

    public void ClearComments() => RawComments.Clear();

    public void RemoveComment(string comment, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        int ind = RawComments.FindIndex(x => x.Equals(comment, comparisonType));
        if (ind >= 0)
            RawComments.RemoveAt(ind);
    }

    public void RemoveComment(int index)
    {
        if (index >= 0 && index < RawComments.Count)
            RawComments.RemoveAt(index);
    }
    #endregion Comments

    private FileInfo? _CdTextFile;
    private FileInfo? _fileInfo;
    public CueSheet()
    {
        Container = new(this);
    }

    internal CueSheet(string? cuePath) : this()
    {
        SetCuePath(cuePath);
    }

    public string? Catalog { get; set; }
    public FileInfo? CdTextFile { get => _CdTextFile; }
    public string? Composer { get; set; }
    public int? Date { get; set; }
    public string? DiscID { get; set; }
    public CueTime? Duration
    {
        get
        {
            CueTime sum = CueTime.Zero;
            foreach (var fdur in Container.Files.Select(x => x.Meta?.CueDuration))
            {
                if (fdur == null) return null;
                sum += fdur.Value;
            }
            return sum;
        }
    }

    public FileInfo? FileInfo => _fileInfo;
    public ReadOnlyCollection<CueFile> Files => Container.Files.AsReadOnly();
    public CueIndex[] Indexes => Container.Indexes.Select(x => new CueIndex(x)).ToArray();
    public CueFile? LastFile => Container.Files.LastOrDefault();
    public CueTrack? LastTrack => Container.Tracks.LastOrDefault();
    public string? Performer { get; set; }
    public CueType SheetType { get; internal set; }
    public Encoding? SourceEncoding { get; internal set; }
    public string? Title { get; set; }
    public ReadOnlyCollection<CueTrack> Tracks => Container.Tracks.AsReadOnly();

    internal ReadOnlyCollection<CueIndexImpl> IndexesImpl => Container.Indexes.AsReadOnly();

    private CueContainer Container { get; }

    public static CueSheet Clone(CueSheet cueSheet) => cueSheet.Clone();
    public void ChangeFile(int index, string newPath)
    {
        Files[index].SetFile(newPath);
    }
    public CueFile AddFile(string path, string type) => Container.AddFile(path, type);

    public CueIndex AddIndex(CueTime time, CueFile file, CueTrack track)
    {
        if (track.ParentFile != file)
            throw new InvalidOperationException("Specified track does not belong to specified file");
        if (file.ParentSheet != this)
            throw new InvalidOperationException("Specified file does not belong to this cuesheet");
        if (track.ParentSheet != this)
            throw new InvalidOperationException("Specified track does not belong to this cuesheet");
        return AddIndex(time, file.Index, track.Index);
    }

    public CueIndex AddIndex(CueTime time, int fileIndex = -1, int trackIndex = -1) => new CueIndex(AddIndexInternal(time, fileIndex, trackIndex));

    public void AddIndex()
    {
        throw new NotImplementedException();
    }

    public CueTrack AddTrack(int index, CueFile file)
    {
        if (file.ParentSheet != this)
            throw new InvalidOperationException("Specified file does not belong to this cuesheet");
        return AddTrack(index, file.Index);
    }

    public CueTrack AddTrack(int index, int fileIndex = -1) => Container.AddTrack(index, fileIndex);


    public CueSheet Clone()
    {
        CueSheet newCue = new(FileInfo?.FullName)
        {
            Catalog = Catalog,
            Composer = Composer,
            Date = Date,
            DiscID = DiscID,
            Performer = Performer,
            Title = Title,
        };
        newCue.Container.CloneFrom(Container);
        newCue.AddComment(RawComments);
        newCue.AddRemark(RawRems.Select(x => x with { }));// creates new remark
        newCue.SetCdTextFile(CdTextFile?.FullName);
        return newCue;
    }

    public bool Equals(CueSheet? other) => Equals(other, StringComparison.CurrentCulture);

    public bool Equals(CueSheet? other, StringComparison stringComparison)
    {
        if (other == null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Container.Indexes.Count != other.Container.Indexes.Count) return false;
        if (Container.Tracks.Count != other.Container.Tracks.Count) return false;
        if (Container.Files.Count != other.Container.Files.Count) return false;
        for (int i = 0; i < Container.Indexes.Count; i++)
        {
            CueIndexImpl one = Container.Indexes[i];
            CueIndexImpl two = other.Container.Indexes[i];
            if (one.Number != two.Number || one.Time != two.Time)
                return false;
        }
        for (int i = 0; i < Container.Tracks.Count; i++)
        {
            CueTrack one = Container.Tracks[i];
            CueTrack two = other.Container.Tracks[i];
            if (!one.Equals(two))
                return false;
        }
        for (int i = 0; i < Container.Files.Count; i++)
        {
            CueFile one = Container.Files[i];
            CueFile two = other.Container.Files[i];
            if (!one.Equals(two))
                return false;
        }
        if (
               !string.Equals(CdTextFile?.Name, other.CdTextFile?.Name, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(FileInfo?.Name, other.FileInfo?.Name, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Performer, other.Performer, stringComparison)
            || !string.Equals(Catalog, other.Catalog)
            || !string.Equals(Composer, other.Composer, stringComparison)
            || !string.Equals(Title, other.Title, stringComparison)
           )
            return false;
        return true;
    }

    public CueIndex[] GetIndexesOfFile(int fileIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfFile_Range(fileIndex);
        if (start == end) return Array.Empty<CueIndex>();
        return Container.Indexes.Skip(start).Take(end - start).Select(x => new CueIndex(x)).ToArray();
    }

    public CueIndex[] GetIndexesOfTrack(int trackIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfTrack_Range(trackIndex);
        if (start == end) return Array.Empty<CueIndex>();
        return Container.Indexes.Skip(start).Take(end - start).Select(x => new CueIndex(x)).ToArray();
    }

    public void RemoveCdTextFile() => SetCdTextFile(null);

    public void RemoveCuePath() => SetCuePath(null);

    public void SetCdTextFile(string? value)
    {
        if (value == null)
            _CdTextFile = null;
        else
        {
            string absPath = Path.Combine(FileInfo?.DirectoryName ?? ".", value);
            _CdTextFile = new FileInfo(absPath);
        }
    }
    public void SetCuePath(string? value)
    {
        if (value == null)
            _fileInfo = null;
        else
        {
            _fileInfo = new FileInfo(value);
        }
        RefreshFiles();
    }

    public bool SetTrackHasZerothIndex(int trackIndex, bool hasZerothIndex)
    {
        CueTrack? track = Container.Tracks.ElementAtOrDefault(trackIndex);
        if (track is null) throw new KeyNotFoundException("Specified track does not exist");
        (int Start, int End) = Container.GetCueIndicesOfTrack_Range(trackIndex, true);
        int count = End - Start;
        //0
        if (count == 0) throw new InvalidOperationException("Track has no indices");
        //1
        if (count == 1)
        {
            if (hasZerothIndex)
                throw new InvalidOperationException("Cannot set zero index for track with only one index");
            else
                return SetZerothIndexImpl(hasZerothIndex, track);
        }
        //2+
        if (Container.Indexes[Start].Time > Container.Indexes[Start + 1].Time) //if 0th time is larger than 1st it means the track is split
        {
            if (!hasZerothIndex)
                throw new InvalidOperationException("Cannot remove zero index in track split across 2 files");
            else
                return SetZerothIndexImpl(hasZerothIndex, track);
        }
        //2+ indices, one file
        else
            return SetZerothIndexImpl(hasZerothIndex, track);
    }

    public override string ToString()
    {
        return String.Format("CueSheet: {0} - {1} - {2}", Performer ?? "No performer", Title ?? "No title", FileInfo?.Name ?? "No file");
    }


    internal CueIndexImpl AddIndexInternal(CueTime time, int fileIndex = -1, int trackIndex = -1) => Container.AddIndex(time, fileIndex, trackIndex);

    internal (int Start, int End) GetCueIndicesOfTrack(int trackIndex) => Container.GetCueIndicesOfTrack_Range(trackIndex, true);

    internal void RefreshIndices()
    {
        Container.RefreshFileIndices();
        Container.RefreshTracksIndices();
        Container.RefreshIndexIndices();
    }

    //    //foreach (var item in sheet.Comments)
    //    //    AddComment(item);
    //    //foreach (var item in sheet.Remarks)
    //    //    AddRemark(item);
    private static bool SetZerothIndexImpl(bool hasZerothIndex, CueTrack track)
    {
        bool old = track.HasZerothIndex;
        track.HasZerothIndex = hasZerothIndex;
        return old != hasZerothIndex;
    }

    private void RefreshFiles()
    {
        if (FileInfo != null)
            FileInfo.Refresh();
        foreach (var file in Container.Files)
        {
            file.RefreshFileInfo();
        }
        if (CdTextFile != null)
            CdTextFile.Refresh();
    }
    /// <summary>
    /// Save the CueSheet to the location specified by its fileinfo. If <paramref name="path"/> is not null, it is set as cue path of this instance.
    /// After changing, path is not reverted if saving was unsuccessful.
    /// </summary>
    /// <param name="path"></param>
    public void Save(string? path)
    {
        if (path is not null)
            SetCuePath(path);
        ArgumentException.ThrowIfNullOrEmpty(path);
        CueWriter writer = new();
        writer.SaveCueSheet(this);
    }

    public override bool Equals(object? obj)
    {
        if (obj is CueSheet) return Equals((CueSheet)obj);
        else return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Performer, Title, Date, Files.Count, Tracks.Count, Remarks.Count, Comments.Count);
    }
    public static bool operator ==(CueSheet? left, CueSheet? right)
    {
        if (left is not null) return left.Equals(right); //not null and whatever
        else if (right is not null) return false; // null and not null
        else return true; // null and null
    }
    public static bool operator !=(CueSheet? left, CueSheet? right)
    {
        return !(left == right);
    }
    //public CueSheet(CueSheet sheet)
    //{
    //    //Catalog = sheet.Catalog;
    //    //Composer = sheet.Composer;
    //    //Title = sheet.Title;
    //    //Date = sheet.Date;
    //    //SheetType = sheet.SheetType;
    //    //DiscID = sheet.DiscID;

    //    //SetCdTextFile(sheet.CdTextFile?.FullName);
    //public bool MoveIndex(int trackNumber, int indexNumber, CueTime difference)
    //{
    //    CueTrack? track = Tracks.FindInOrdered(trackNumber, x => x.Number);
    //    if (track == null) return false;
    //    return track.MoveIndex(indexNumber, difference);
    //}
    //public bool ChangeIndex(int trackNumber, int indexNumber, CueTime newTime)
    //{
    //    CueTrack? track = Tracks.FindInOrdered(trackNumber, x => x.Number);
    //    if (track == null) return false;
    //    return track.ChangeIndex(indexNumber, newTime);
    //}
    //public bool RemoveIndex(int trackNumber, int indexNumber)
    //{
    //    CueTrack? track = Tracks.FindInOrdered(trackNumber, x => x.Number);
    //    if (track == null) return false;
    //    return track.RemoveIndex(indexNumber);
    //}
    //public int InsertIndex(int trackNumber, int emplacement, CueTime cueTime)
    //{
    //    CueTrack? track = Tracks.FindInOrdered(trackNumber, x => x.Number);
    //    if (track == null) return -1;
    //    return track.InsertIndex(cueTime);
    //}
    //public bool RemoveTrack(int trackNumber)
    //{
    //    CueTrack? track = Tracks.FindInOrdered(trackNumber, x => x.Number);
    //    if (track == null) return false;
    //    IEnumerable<CueFile> files = track.Indices.DistinctBy(x => x.File).Select(x => x.File);
    //    bool res = false;
    //    foreach (CueFile file in files)
    //        res |= file.RemoveTrack(track);
    //    track.Active = !res;
    //    return res;
    //}
    //public bool RemoveFile(int fileNumber)=> _Files.Remove(fileNumber);
}