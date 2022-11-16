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
public class CueSheet : IEquatable<CueSheet>, IRemarkableCommentable
{
    #region Rem
    internal readonly List<Remark> RawRems = new();
    public ReadOnlyCollection<Remark> Remarks => RawRems.AsReadOnly();
    public void ClearRemarks() => RawRems.Clear();

    public void AddRemark(string type, string value) => AddRemark(new Remark(type, value));
    public void AddRemark(Remark entry) => RawRems.Add(entry);

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
    #endregion
    #region Comments
    internal readonly List<string> RawComments = new();
    public ReadOnlyCollection<string> Comments => RawComments.AsReadOnly();
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

    public CueSheet(string? cuePath) : this()
    {
        SetCuePath(cuePath);
    }

    public CueSheet()
    {
        Container = new(this);
    }

    public CueSheet(CueSheet sheet)
    {
        Catalog = sheet.Catalog;
        Composer = sheet.Composer;
        Title = sheet.Title;
        Date = sheet.Date;
        SheetType = sheet.SheetType;
        DiscID = sheet.DiscID;

        SetCdTextFile(sheet.CdTextFile?.FullName);

        foreach (var item in sheet.Comments)
            AddComment(item);
        foreach (var item in sheet.Remarks)
            AddRemark(item);

        Container = new(this);
        //foreach (var item in sheet.Files)
        //    Container.AddFile(item.FileInfo.FullName, item.Type);
        //foreach (var item in sheet.Tracks)
        //{
        //    CueTrack track = Container.AddTrack(item.Number, item.ParentFile.Index);
        //    if (item.CommonFieldsSet.HasFlag(FieldSetFlags.Title))
        //        track.Title = item.Title;
        //    track.Composer = item.CommonFieldsSet.HasFlag(FieldSetFlags.Composer) ? item.Composer : null;
        //    track.Performer = item.CommonFieldsSet.HasFlag(FieldSetFlags.Performer) ? item.Performer : null;
        //    track.Flags = item.Flags;
        //    track.HasZerothIndex = item.HasZerothIndex;
        //    track.Index = item.Index;
        //    track.ISRC = item.ISRC;
        //    track.PreGap = item.PreGap;
        //    track.PostGap = item.PostGap;

        //    foreach (var com in item.Comments)
        //        track.AddComment(com);
        //    foreach (var rem in item.Remarks)
        //        track.AddRemark(rem);
        //}
        int lastTrackIndex = -1;
        int lastFileIndex = -1;
        foreach (var cimpl in sheet.IndexesImpl)
        {
            int currFileIndex = Math.Max(cimpl.File.Index, cimpl.Track.Index);
            if (lastFileIndex != currFileIndex)
            {
                Container.AddFile(cimpl.File.FileInfo.FullName, cimpl.File.Type);
                lastFileIndex= currFileIndex;
            }
            if (lastTrackIndex!= cimpl.Track.Index)
            {
                lastTrackIndex= cimpl.Track.Index;
                CueTrack item = cimpl.Track;
                CueTrack track = Container.AddTrack(item.Number, item.ParentFile.Index);
                if (item.CommonFieldsSet.HasFlag(FieldSetFlags.Title))
                    track.Title = item.Title;
                track.Composer = item.CommonFieldsSet.HasFlag(FieldSetFlags.Composer) ? item.Composer : null;
                track.Performer = item.CommonFieldsSet.HasFlag(FieldSetFlags.Performer) ? item.Performer : null;
                track.Flags = item.Flags;
                track.HasZerothIndex = item.HasZerothIndex;
                track.Index = item.Index;
                track.ISRC = item.ISRC;
                track.PreGap = item.PreGap;
                track.PostGap = item.PostGap;

                foreach (var com in item.Comments)
                    track.AddComment(com);
                foreach (var rem in item.Remarks)
                    track.AddRemark(rem);
            }
            Container.AddIndex(cimpl.Time);
        }

    }

    public CueSheet Copy() => new(this);

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
        return String.Format("CueSheet: {0} - {1} - {2}", Performer ?? "No performer", Title ?? "No title", FileInfo?.Name ?? "No file");
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
