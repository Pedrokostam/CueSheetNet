using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
public class CueSheet :IEquatable<CueSheet>, IRemCommentable
{
    #region Rem
    internal readonly List<RemEntry> RawRems = new();
    public ReadOnlyCollection<RemEntry> Remarks => RawRems.AsReadOnly();
    public void ClearRems() => RawRems.Clear();

    public void AddRem(string type, string value) => AddRem(new RemEntry(type, value));
    public void AddRem(RemEntry entry) => RawRems.Add(entry);

    public void RemoveRem(int index)
    {
        if (index >= 0 || index < RawRems.Count)
            RawRems.RemoveAt(index);
    }

    public void RemoveRem(string field, string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)=>RemoveRem(new RemEntry(field,value),comparisonType);
    public void RemoveRem(RemEntry entry, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        int ind = RawRems.FindIndex(x => x.Equals(entry, comparisonType));
        if (ind >= 0)
            RawRems.RemoveAt(ind);
    }
    #endregion
    #region Comments
    internal readonly List<string> RawComments = new();
    public  ReadOnlyCollection<string> Comments=>RawComments.AsReadOnly();
    public void AddComment(string comment) => RawComments.Add(comment);
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
    public void ClearComments() => RawComments.Clear();
    #endregion
    public CueType SheetType { get; internal set; }
    public string? Composer { get; set; }
    public string? Performer { get; set; }
    public string? Title { get; set; }
    private FileInfo? _CdTextFile;
    public FileInfo? CdTextFile { get => _CdTextFile; }
    public void RemoveCdTextFile() => SetCdTextFile(null);
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

    public FileInfo? FileInfo => _fileInfo;
    private FileInfo? _fileInfo;
    public void RemoveCuePath() => SetCuePath(null);
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

    public string? Catalog { get; set; }
    public string? DiscID { get; set; }
    public ReadOnlyCollection<CueFile> Files => Container.Files.AsReadOnly();
    public ReadOnlyCollection<CueTrack> Tracks => Container.Tracks.AsReadOnly();
    internal ReadOnlyCollection<CueIndexImpl> IndexesImpl => Container.Indexes.AsReadOnly();
    public CueIndex[] Indexes => Container.Indexes.Select(x => new CueIndex(x)).ToArray();
    public CueIndex[] GetIndexesOfTrack(int trackIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfTrack(trackIndex);
        if (start == end) return Array.Empty<CueIndex>();
        return Container.Indexes.Skip(start).Take(end - start).Select(x => new CueIndex(x)).ToArray();
    }
    internal (int Start, int End) GetCueIndicesOfTrack(int trackIndex) => Container.GetCueIndicesOfTrack(trackIndex, true);
    public CueIndex[] GetIndexesOfFile(int fileIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfFile(fileIndex);
        if (start == end) return Array.Empty<CueIndex>();
        return Container.Indexes.Skip(start).Take(end - start).Select(x => new CueIndex(x)).ToArray();
    }
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
    public int? Date { get; set; }
    private CueContainer Container { get; }

    public CueFile? LastFile => Container.Files.LastOrDefault();
    public CueTrack? LastTrack => Container.Tracks.LastOrDefault();

    public CueSheet(string cuePath) : this()
    {
        SetCuePath(cuePath);
    }

    public CueSheet()
    {
        Container = new(this);
    }

    public CueFile AddFile(string path, string type) => Container.AddFile(path, type);
    public CueTrack AddTrack(int index, CueFile file)
    {
        if (file.ParentSheet != this)
            throw new InvalidOperationException("Specified file does not belong to this cuesheet");
        return AddTrack(index, file.Index);
    }
    public CueTrack AddTrack(int index, int fileIndex = -1) => Container.AddTrack(index, fileIndex);
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
    internal CueIndexImpl AddIndexInternal(CueTime time, int fileIndex = -1, int trackIndex = -1) => Container.AddIndex(time, fileIndex, trackIndex);
    public bool SetTrackHasZerothIndex(int trackIndex, bool hasZerothIndex)
    {
        CueTrack? track = Container.Tracks.ElementAtOrDefault(trackIndex);
        if (track is null) throw new KeyNotFoundException("Specified track does not exist");
        (int Start, int End) = Container.GetCueIndicesOfTrack(trackIndex, true);
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

    private static bool SetZerothIndexImpl(bool hasZerothIndex, CueTrack track)
    {
        bool old = track.HasZerothIndex;
        track.HasZerothIndex = hasZerothIndex;
        return old != hasZerothIndex;
    }
    internal void RefreshIndices()
    {
        Container.RefreshFileIndices();
        Container.RefreshTracksIndices();
        Container.RefreshIndexIndices();
    }

    public void AddIndex()
    {
        throw new NotImplementedException();
    }
    public override string ToString()
    {
        return String.Format("CueSheet: {0} - {1} - {2}",Performer ?? "No performer",Title ?? "No title", FileInfo?.Name ?? "No file"); 
    }
    public bool Equals(CueSheet? other)=>Equals( other,StringComparison.CurrentCulture);
    public bool Equals(CueSheet? other, StringComparison stringComparison)
    {
        if (other == null) return false;
        if(ReferenceEquals(this, other)) return true;
        if(Container.Indexes.Count != other.Container.Indexes.Count) return false;
        if(Container.Tracks.Count != other.Container.Tracks.Count) return false;
        if(Container.Files.Count != other.Container.Files.Count) return false;
        for (int i = 0; i < Container.Indexes.Count; i++)
        {
            CueIndexImpl one = Container.Indexes[i];
            CueIndexImpl two = other.Container.Indexes[i];
            if (one.Number!=two.Number || one.Time!=two.Time)
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
