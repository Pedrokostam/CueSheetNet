using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using CueSheetNet.Writing;

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

    #region Tracks
    public ReadOnlyCollection<CueTrack> Tracks => Container.Tracks.AsReadOnly();
    public CueTrack? LastTrack => Container.Tracks.LastOrDefault();
    public CueTrack AddTrack(int index, CueFile file)
    {
        if (file.ParentSheet != this)
            throw new InvalidOperationException("Specified file does not belong to this cuesheet");
        return AddTrack(index, file.Index);
    }

    public CueTrack AddTrack(int index, int fileIndex = -1) => Container.AddTrack(index, fileIndex);

    #endregion

    #region Files
    public void ChangeFile(int index, string newPath)
    {
        Files[index].SetFile(newPath);
    }
    public CueFile AddFile(string path, string type) => Container.AddFile(path, type);
    public CueFile? LastFile => Container.Files.LastOrDefault();

    private List<FileInfo> _associatedFiles { get; } = new List<FileInfo>();
    public ReadOnlyCollection<FileInfo> AssociatedFiles => _associatedFiles.AsReadOnly();
    #endregion

    #region Index
    internal ReadOnlyCollection<CueIndexImpl> IndexesImpl => Container.Indexes.AsReadOnly();
    public CueIndex[] Indexes => Container.Indexes.Select(x => new CueIndex(x)).ToArray();
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

    #endregion


    #region Fileops
    /// <summary>
    /// Save the CueSheet to the location specified by its fileinfo. If <paramref name="path"/> is not null, it is set as cue path of this instance.
    /// After changing, path is not reverted if saving was unsuccessful.
    /// Does not do anything with <see cref="CueFile" />s of the Cuesheet, or other associated files.
    /// </summary>
    /// <param name="path"></param>
    /// <param name="encoding">Optional encoding. If not specified the source encoding of the Cusheet will be used, or <see cref="CueWriterSettings.DefaultEncoding"/></param>
    public void Save(string path, Encoding? encoding = null)
    {
        //if (path is not null)
        ArgumentException.ThrowIfNullOrEmpty(path);
        SetCuePath(path);
        CueWriter writer = new();
        if (encoding is not null)
        {
            CueWriterSettings settings = new() { Encoding = encoding };
            writer.Settings = settings;
        }
        writer.SaveCueSheet(this);
    }
    #endregion
    private CueContainer Container { get; }
    private FileInfo? _CdTextFile;
    private FileInfo? _sourceFile;



    public CueSheet()
    {
        Container = new(this);
    }

    public CueSheet(string? cuePath) : this()
    {
        SetCuePath(cuePath);
        Refresh();
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

    public FileInfo? SourceFile => _sourceFile;
    public ReadOnlyCollection<CueFile> Files => Container.Files.AsReadOnly();
    public string? Performer { get; set; }
    public CueType SheetType { get; internal set; }
    public Encoding? SourceEncoding { get; internal set; }
    /// <summary>
    /// AKA Album Name
    /// </summary>
    public string? Title { get; set; }



    public static CueSheet Clone(CueSheet cueSheet) => cueSheet.Clone();
    //public void ChangeFile(FileInfo file)
    //{

    //    Files[index].SetFile(newPath);
    //}
    //public void ChangeFile(FileInfo file)
    //{

    //    Files[index].SetFile(newPath);
    //}
    /// <summary>
    /// Change path of the given file (zero-based index)
    /// </summary>
    /// <param name="index">Zero-based index</param>
    /// <param name="newPath"></param>





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
        bool finalCheck = string.Equals(CdTextFile?.Name, other.CdTextFile?.Name, StringComparison.OrdinalIgnoreCase) //Paths are compared without caring for case
                       && string.Equals(SourceFile?.Name, other.SourceFile?.Name, StringComparison.OrdinalIgnoreCase)
                       && string.Equals(Performer, other.Performer, stringComparison)
                       && string.Equals(Catalog, other.Catalog)
                       && string.Equals(Composer, other.Composer, stringComparison)
                       && string.Equals(Title, other.Title, stringComparison);
        return finalCheck;
    }

    internal (int Start, int End) GetIndexesOfFile_Range(int fileIndex) => Container.GetCueIndicesOfFile_Range(fileIndex);
    public CueIndex[] GetIndexesOfFile(int fileIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfFile_Range(fileIndex);
        if (start == end) return Array.Empty<CueIndex>();
        return Container.Indexes.Skip(start).Take(end - start).Select(x => new CueIndex(x)).ToArray();
    }
    internal (int Start, int End) GetTracksOfFile_Range(int fileIndex) => Container.GetCueIndicesOfTrack_Range(fileIndex);
    public CueTrack[] GetTracksOfFile(int fileIndex)
    {
        (int start, int end) = Container.GetCueTracksOfFile_Range(fileIndex);
        if (start == end) return Array.Empty<CueTrack>();
        return Container.Tracks.Skip(start).Take(end - start).ToArray();
    }

    internal (int Start, int End) GetIndexesOfTrack_Range(int fileIndex) => Container.GetCueIndicesOfTrack_Range(fileIndex);
    public CueIndex[] GetIndexesOfTrack(int trackIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfTrack_Range(trackIndex);
        if (start == end) return Array.Empty<CueIndex>();
        return Container.Indexes.Skip(start).Take(end - start).Select(x => (CueIndex)x).ToArray();
    }

    public void RemoveCdTextFile() => SetCdTextFile(null);

    public void RemoveCuePath() => SetCuePath(null);

    public void SetCdTextFile(string? value)
    {
        if (value == null)
            _CdTextFile = null;
        else
        {
            string absPath = Path.Combine(SourceFile?.DirectoryName ?? ".", value);
            _CdTextFile = new FileInfo(absPath);
        }
    }
    public void SetCuePath(string? value)
    {
        if (value == null)
            _sourceFile = null;
        else
        {
            _sourceFile = new FileInfo(value);
        }
        RefreshFiles();
    }

    public bool SetTrackHasZerothIndex(int trackIndex, bool hasZerothIndex)
    {
        CueTrack? track = Container.Tracks.ElementAtOrDefault(trackIndex) ?? throw new ArgumentOutOfRangeException(nameof(trackIndex), "Specified track does not exist");
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

    public void Refresh()
    {
        RefreshFiles();
        RefreshIndices();
    }

    private void RefreshFiles()
    {
        SourceFile?.Refresh();
        foreach (var file in Container.Files)
        {
            file.RefreshFileInfo();
        }
        CdTextFile?.Refresh();

        _associatedFiles.Clear();
        _associatedFiles.AddRange(FileHandler.GetAssociatedFiles(this));

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

    /// <summary>
    /// Creates an independent deep copy of cuesheet contents.
    /// Copy is functionally the same, but may not be identical (formatting, etc.).
    /// No objects are shared, everything is created anew
    /// </summary>
    /// <returns>Deep copy of the <see cref="CueSheet"/></returns>
    public CueSheet Clone()
    {
        CueSheet newCue = new(SourceFile?.FullName)
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
        newCue.Refresh();
        return newCue;
    }
    public override string ToString()
    {
        return string.Format("CueSheet: {0} - {1} - {2}",
                             Performer ?? "No performer",
                             Title ?? "No title",
                             SourceFile?.Name ?? "No file");
    }
    public override bool Equals(object? obj)
    {
        return obj is CueSheet sheet && Equals(sheet);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(Performer, Title, Date, Files.Count, Tracks.Count, Remarks.Count, Comments.Count);
    }
    /// <inheritdoc cref="FileHandler.CopyCueFiles(CueSheet, string, string?)"/>
    public CueSheet CopyFiles(string destination, string? pattern)
    {
        return FileHandler.CopyCueFiles(this,destination, pattern);
    }
    /// <inheritdoc cref="FileHandler.MoveCueFiles(CueSheet, string, string?)"/>
    public CueSheet MoveFiles(string destination, string? pattern)
    {
        return FileHandler.MoveCueFiles(this,destination, pattern);
    }
    public void DeleteFiles()
    {
        FileHandler.DeleteCueFiles(this);
    }
}