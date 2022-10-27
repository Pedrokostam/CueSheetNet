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
public class CueSheet
{
    public CueType SheetType { get; internal set; }
    public string? Composer { get; set; }
    public List<string> Comments = new();

    public string CommentString => string.Join("\r\n", Comments);
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
    public IReadOnlyList<CueFile> Files => Container.Files.AsReadOnly();
    public IReadOnlyList<CueTrack> Tracks => Container.Tracks.AsReadOnly();
    public CueIndex[] Indexes => Container.Indexes.Select(x => new CueIndex(x)).ToArray();
    public CueIndex[] GetIndicesOfTrack(int trackIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfTrack(trackIndex);
        if (start == end) return Array.Empty<CueIndex>();
        return Container.Indexes.Skip(start).Take(end - start).Select(x => new CueIndex(x)).ToArray();
    }
    internal (int Start, int End) GetCueIndicesOfTrack(int trackIndex) => Container.GetCueIndicesOfTrack(trackIndex, true);
    public CueIndex[] GetIndicesOfFile(int fileIndex)
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
            foreach (var fdur in Container.Files.Select(x => x.Duration))
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
    public CueTrack AddTrack(int index, int fileIndex = -1) => Container.AddTrack(index, fileIndex);
    internal CueIndexImpl AddIndex(CueTime time, int fileIndex = -1, int trackIndex = -1) => Container.AddIndex(time, fileIndex, trackIndex);
    public void AddComment(string comment) => Comments.Add(comment);
    public void RemoveCommet(string comment)
    {
        int ind = Comments.FindIndex(x => x.Equals(comment, StringComparison.OrdinalIgnoreCase));
        if (ind >= 0)
            Comments.RemoveAt(ind);
    }
    public void RemoveComment(int index)
    {
        if (index >= 0 && index < Comments.Count)
            Comments.RemoveAt(index);
    }
    public void ClearComments() => Comments.Clear();

    public static CueSheet ParseCueSheet(string cuePath)
    {
        CueReader parser = new(cuePath);
        return parser.ParseCueSheet();
    }
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
