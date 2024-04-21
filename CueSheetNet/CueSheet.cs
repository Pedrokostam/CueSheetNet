using System.Collections.ObjectModel;
using CueSheetNet.Collections;
using CueSheetNet.FileHandling;
using CueSheetNet.Internal;

namespace CueSheetNet;

public class CueSheet : IEquatable<CueSheet>, IRemCommentable
{
    public RemarkCollection Remarks { get; } = [];
    public CommentCollection Comments { get; } = [];
    public string? Catalog { get; set; }
    public string? Composer { get; set; }
    public int? Date { get; set; }
    public string? DiscID { get; set; }
    public string? Performer { get; set; }
    /// <summary>
    /// AKA Album Name
    /// </summary>
    public string? Title { get; set; }
    public CueType SheetType { get; internal set; }
    public Encoding? SourceEncoding { get; internal set; }
    #region CD text file
    private FileInfo? _cdTextFile;
    public FileInfo? CdTextFile
    {
        get => _cdTextFile;
    }
    
    public void RemoveCdTextFile() => SetCdTextFile(value: null);

    public void SetCdTextFile(string? value)
    {
        if (value == null)
            _cdTextFile = null;
        else
        {
            string absPath = Path.Combine(SourceFile?.DirectoryName ?? ".", value);
            _cdTextFile = new FileInfo(absPath);
        }
    }
    #endregion

    #region Source file

    public FileInfo? SourceFile { get; private set; }
    public void RemoveCuePath() => SetCuePath(value: null);
    internal void SetCuePath(string? value)
    {
        if (value == null)
            SourceFile = null;
        else
        {
            SourceFile = new FileInfo(value);
        }
        RefreshFiles();
    }

    #endregion

    public CueTime? Duration
    {
        get
        {
            CueTime totalDuration = CueTime.Zero;
            foreach (var fileDuration in Container.Files.Select(x => x.Meta?.CueDuration))
            {
                if (fileDuration == null)
                    return null;

                totalDuration += fileDuration.Value;
            }
            return totalDuration;
        }
    }

    #region Tracks
    public ReadOnlyCollection<CueTrack> Tracks => Container.Tracks.AsReadOnly();
    public CueTrack? LastTrack => Container.Tracks.LastOrDefault();

    public CueTrack AddTrack(int index, TrackType type, CueDataFile file)
    {
        if (file.ParentSheet != this)
            throw new InvalidOperationException("Specified file does not belong to this cuesheet");
        return AddTrack(index, type, file.Index);
    }

    public CueTrack AddTrack(int index, TrackType type, int fileIndex = -1) =>
        Container.AddTrack(index, type, fileIndex);

    #endregion

    #region Files
    public void ChangeFile(int index, string newPath, FileType? type = null)
    {
        Files[index].SetFile(newPath, type);
    }

    public CueDataFile AddFile(string path, FileType type) => Container.AddFile(path, type);

    public CueDataFile? LastFile => Container.Files.LastOrDefault();

    private readonly List<ICueFile> _associatedFiles = [];
    public ReadOnlyCollection<ICueFile> AssociatedFiles => _associatedFiles.AsReadOnly();

    #endregion

    #region Index
    internal ReadOnlyCollection<CueIndexImpl> IndexesImpl => Container.Indexes.AsReadOnly();

    internal CueIndexImpl? GetIndexImplOrDefault(int index) =>
        Container.Indexes.Count > index ? Container.Indexes[index] : null;

    internal CueIndex GetCueIndexAt(int index) => new CueIndex(Container.Indexes[index]);

    internal CueIndex? GetCueIndexAt_Safe(int index)
    {
        if (Container.Indexes.Count > index)
        {
            return GetCueIndexAt(index);
        }

        return null;
    }

    public CueIndex[] Indexes => Container.Indexes.Select(x => new CueIndex(x)).ToArray();

    public CueIndex AddIndex(CueTime time, CueDataFile file, CueTrack track)
    {
        if (track.ParentFile != file)
            throw new InvalidOperationException(
                "Specified track does not belong to specified file"
            );
        if (file.ParentSheet != this)
            throw new InvalidOperationException("Specified file does not belong to this cuesheet");
        if (track.ParentSheet != this)
            throw new InvalidOperationException("Specified track does not belong to this cuesheet");
        return AddIndex(time, file.Index, track.Index);
    }

    public CueIndex AddIndex(CueTime time, int fileIndex = -1, int trackIndex = -1) =>
        new(Container.AddIndex(time, fileIndex, trackIndex));

    #endregion

    #region Fileops
    /// <summary>
    /// Save the CueSheet to the location specified by its <see cref="SourceFile"/>.
    /// After changing, directory is not reverted if saving was unsuccessful.
    /// <para/>File is saved using default <see cref="CueWriterSettings"/>. To use different settings use <see cref="CueWriter"/>
    /// <para/>Does not do anything with <see cref="CueDataFile" />s of the Cuesheet, or other associated files (use <see cref="CopyFiles(string, string?)"/> or <see cref="MoveFiles(string, string?)"/> for that).
    /// </summary>
    public void Save()
    {
        //if (directory is not null)
        ExceptionHelper.ThrowIfNull(SourceFile);
        CueWriter writer = new();
        writer.SaveCueSheet(this);
    }

    public void Save(string path)
    {
        SetCuePath(path);
        Save();
    }

    #endregion
    
    internal CueContainer Container { get; }

    internal void SetParsingMode(bool parsing)
    {
        this.Container.ParsingMode = parsing;
    }
    /// <summary>
    /// Creates a new blank CueSheet.
    /// <para>
    /// To load an existing CUE sheet use <see cref="CueReader"/> or <see cref="CueSheet.Read(string)"/>.</para>
    /// </summary>
    public CueSheet()
    {
        Container = new(this);
    }

    /// <summary>
    /// Creates a new blank CUE sheet and sets its source path.
    /// </summary>
    /// <param name="cuePath"></param>
    private CueSheet(string? cuePath)
        : this()
    {
        SetCuePath(cuePath);
        //Refresh(); // is it needed
    }

    public ReadOnlyCollection<CueDataFile> Files => Container.Files.AsReadOnly();

    public static CueSheet Clone(CueSheet cueSheet) => cueSheet.Clone();

    public bool Equals(CueSheet? other) => Equals(other, StringComparison.CurrentCulture);

    public bool Equals(CueSheet? other, StringComparison stringComparison)
    {
        if (other == null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (Container.Indexes.Count != other.Container.Indexes.Count)
            return false;
        if (Container.Tracks.Count != other.Container.Tracks.Count)
            return false;
        if (Container.Files.Count != other.Container.Files.Count)
            return false;
        for (int i = 0; i < Container.Indexes.Count; i++)
        {
            CueIndexImpl one = Container.Indexes[i];
            CueIndexImpl two = other.Container.Indexes[i];
            if (
                one.Number != two.Number
                || one.Time != two.Time
                || one.File.Index != two.File.Index
            )
                return false;
        }
        for (int i = 0; i < Container.Tracks.Count; i++)
        {
            CueTrack one = Container.Tracks[i];
            CueTrack two = other.Container.Tracks[i];
            if (!one.Equals(two, StringComparison.InvariantCulture))
                return false;
        }
        for (int i = 0; i < Container.Files.Count; i++)
        {
            CueDataFile one = Container.Files[i];
            CueDataFile two = other.Container.Files[i];
            if (!one.Equals(two))
                return false;
        }
        bool finalCheck =
            string.Equals(
                CdTextFile?.Name,
                other.CdTextFile?.Name,
                StringComparison.OrdinalIgnoreCase
            ) //Paths are compared without caring for case
            //&& string.Equals(SourceFile?.Name, other.SourceFile?.Name, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Performer, other.Performer, stringComparison)
            && string.Equals(Catalog, other.Catalog, StringComparison.Ordinal)
            && string.Equals(Composer, other.Composer, stringComparison)
            && string.Equals(Title, other.Title, stringComparison);
        return finalCheck;
    }

    public CueIndex[] GetIndexesOfFile(int fileIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfFile_Range(fileIndex);
        if (start == end)
            return [];
        return Container
            .Indexes.Skip(start)
            .Take(end - start)
            .Select(x => new CueIndex(x))
            .ToArray();
    }
    public CueTrack[] GetTracksOfFile(int fileIndex)
    {
        (int start, int end) = Container.GetCueTracksOfFile_Range(fileIndex);
        if (start == end)
            return [];
        return Container.Tracks.Skip(start).Take(end - start).ToArray();
    }

    internal IEnumerable<CueTrack> GetTracksOfFile_IEnum(int fileIndex)
    {
        (int start, int end) = Container.GetCueTracksOfFile_Range(fileIndex);
        if (start == end)
            return [];
        return Container.Tracks.Skip(start).Take(end - start);
    }


    public CueIndex[] GetIndexesOfTrack(int trackIndex)
    {
        (int start, int end) = Container.GetCueIndicesOfTrack_Range(trackIndex);
        if (start == end)
            return [];
        return Container.Indexes.Skip(start).Take(end - start).Select(x => (CueIndex)x).ToArray();
    }

    public void Refresh()
    {
        RefreshFiles();
        Container.Refresh();
        UpdateGlobalPerformer();
        UpdateGlobalComposer();
        UpdateCueType();
    }

    private void UpdateGlobalPerformer()
    {
        if (Performer is null)
        {
            string? p = null;
            foreach (CueTrack track in Tracks)
            {
                if (!track.CommonFieldsSet.HasFlag(FieldsSet.Performer))
                {
                    // performer is not set, cannot set global
                    return;
                }
                if (p is null)
                {
                    // set performer to current track's
                    p = track.Performer;
                }
                else if (!string.Equals(p, track.Performer, StringComparison.OrdinalIgnoreCase))
                {
                    // tracks have different performers
                    // cannot set global
                    return;
                }
            }
            Performer = p;
        }
    }

    private void UpdateGlobalComposer()
    {
        if (Composer is null)
        {
            string? p = null;
            foreach (CueTrack track in Tracks)
            {
                if (!track.CommonFieldsSet.HasFlag(FieldsSet.Composer))
                {
                    // Composer is not set, cannot set global
                    return;
                }
                if (p is null)
                {
                    // set performer to current track's
                    p = track.Composer;
                }
                else if (!string.Equals(p, track.Composer, StringComparison.OrdinalIgnoreCase))
                {
                    // tracks have different Composers
                    // cannot set global
                    return;
                }
            }
            Composer = p;
        }
    }

    private void UpdateCueType()
    {
        CueType type = CueType.Unknown;
        if (Tracks.Count == 0)
        {
            SheetType = type;
            return;
        }
        if (Files.Count == 1)
        {
            type |= CueType.SingleFile;
        }
        else if (Files.Count > 1)
        {
            type |= CueType.MultipleFiles;
        }
        if (Tracks[0].HasZerothIndex)
        {
            type |= CueType.HTOA;
        }
        bool simgaps = false;
        bool audio = false;
        bool data = false;
        foreach (var track in Tracks)
        {
            audio |= track.Type.ContainsAudioData;
            data |= !track.Type.ContainsAudioData;
            simgaps |= track.PreGap != default || track.PostGap != default;
        }
        if (audio)
        {
            type |= CueType.Audio;
        }
        if (data)
        {
            type |= CueType.Data;
        }
        if (simgaps)
        {
            type |= CueType.SimulatedGaps;
        }
        if (Files.Count > 1)
        {
            bool intergaps = false;
            foreach (var file in Files)
            {
                (int first, int afterLast) = Container.GetCueIndicesOfFile_Range(file.Index);
                intergaps |= IndexesImpl[afterLast - 1].Number == 0;
            }
            if (intergaps)
            {
                type |= CueType.InterfileGaps;
            }
        }
        SheetType = type;
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
        _associatedFiles.AddRange(CuePackage.GetAssociatedFiles(this));
    }

    public static bool operator ==(CueSheet? left, CueSheet? right)
    {
        return (left, right) switch
        {
            (null, null) => true,
            (not null, null) => false,
            (null, not null) => false,
            _ => left.Equals(right, StringComparison.InvariantCulture)
        };
    }

    public static bool operator !=(CueSheet? left, CueSheet? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Creates an independent deep copy of cuesheet contents.
    /// Copy is functionally the same, but may not be identical (formatting, etc.).
    /// <para>
    /// No objects are shared, everything is created anew.
    /// </para>
    /// </summary>
    /// <returns>Deep copy of this <see cref="CueSheet"/> instance.</returns>
    public CueSheet Clone()
    {
        CueSheet newCue =
            new(SourceFile?.FullName)
            {
                Catalog = Catalog,
                Composer = Composer,
                Date = Date,
                DiscID = DiscID,
                Performer = Performer,
                Title = Title,
            };
        newCue.Container.CloneFrom(Container);
        newCue.Comments.Add(Comments);
        newCue.Remarks.Add(Remarks); // remarks are struct, so they are always copied
        newCue.SetCdTextFile(CdTextFile?.FullName);
        newCue.Refresh();
        return newCue;
    }

    public override string ToString()
    {
        return string.Format(
            "CueSheet: {0} - {1} - {2}",
            Performer ?? "No performer",
            Title ?? "No title",
            SourceFile?.Name ?? "No file"
        );
    }

    public override bool Equals(object? obj)
    {
        return obj is CueSheet sheet && Equals(sheet, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Performer,
            Title,
            Date,
            Files.Count,
            Tracks.Count,
            Remarks.Count,
            Comments.Count
        );
    }

    internal string DefaultFilename
    {
        get => $"{Performer ?? "No Artist"} - {Title ?? "No Title"}";
    }

    /// <summary>
    /// Read the file at the specified <paramref name="path"/> using the default <see cref="CueReader">reader</see>.
    /// </summary>
    /// <param name="path">File path of the CUE sheet.</param>
    /// <returns>A new instance of <see cref="CueSheet"/>.</returns>
    public static CueSheet Read(string path) => new CueReader().ParseCueSheet(path);
}
