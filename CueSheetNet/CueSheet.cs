using System.Collections.ObjectModel;
using CueSheetNet.Collections;
using CueSheetNet.Extensions;
using CueSheetNet.FileHandling;
using CueSheetNet.Internal;

namespace CueSheetNet;

// TODO remove iequatable
public class CueSheet : IRemCommentable
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
            foreach (var fileDuration in Files.Select(x => x.Meta?.CueDuration))
            {
                if (fileDuration == null)
                    return null;

                totalDuration += fileDuration.Value;
            }
            return totalDuration;
        }
    }

    #region Tracks
    public SheetTrackCollection Tracks { get; }
    public CueTrack? LastTrack => Tracks.Count > 0 ? Tracks[^1] : null;

    //public CueTrack AddTrack(int index, TrackType type, CueDataFile file)
    //{
    //    if (file.ParentSheet != this)
    //        throw new InvalidOperationException("Specified file does not belong to this cuesheet");
    //    return AddTrack(index, type, file.Index);
    //}

    //public CueTrack AddTrack(int index, TrackType type, int fileIndex = -1) =>
    //    Container.AddTrack(index, type, fileIndex);

    #endregion

    #region Files
    //public void ChangeFile(int index, string newPath, FileType? type = null)
    //{
    //    Files[index].SetFile(newPath, type);
    //}

    //public CueDataFile AddFile(string path, FileType type) => Container.AddFile(path, type);

    public SheetFileCollection Files { get; }
    public CueDataFile? LastFile => Files.Count > 0 ? Files[^1] : null;

    private readonly List<ICueFile> _associatedFiles = [];
    public ReadOnlyCollection<ICueFile> AssociatedFiles => _associatedFiles.AsReadOnly();

    #endregion

    #region Index
    public SheetIndexCollection Indexes { get; }

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
        ExceptionHelper.ThrowIfNull(SourceFile, nameof(SourceFile));
        CueWriter writer = new();
        writer.SaveCueSheet(this);
    }

    public void Save(string path)
    {
        SetCuePath(path);
        Save();
    }

    #endregion

    //internal CueContainer Container { get; }

    // internal void SetParsingMode(bool parsing)
    // {
    //     this.Container.ParsingMode = parsing;
    // }
    /// <summary>
    /// Creates a new blank CueSheet.
    /// <para>
    /// To load an existing CUE sheet use <see cref="CueReader"/> or <see cref="CueSheet.Read(string)"/>.</para>
    /// </summary>
    public CueSheet()
    {
        Files = new(this);
        Tracks = new(Files);
        Indexes = new(Tracks);
    }

    /// <summary>
    /// Creates a new blank CUE sheet and sets its source path.
    /// </summary>
    /// <param name="cuePath"></param>
    internal CueSheet(string? cuePath) : this()
    {
        SetCuePath(cuePath);
        //Refresh(); // is it needed
    }


    public static CueSheet Clone(CueSheet cueSheet) => cueSheet.Clone();

    public void Refresh()
    {
        RefreshFiles();
        //Container.Refresh();
        UpdateGlobalPerformer();
        UpdateGlobalComposer();
        UpdateCueType();
    }

    private void UpdateGlobalPerformer()
    {
        if (Performer is null)
        {
            string? newPerformer = null;
            foreach (CueTrack track in Tracks)
            {
                if (!track.CommonFieldsSet.HasFlag(FieldsSet.Performer))
                {
                    // performer is not set, cannot set global
                    return;
                }
                if (newPerformer is null)
                {
                    // set performer to current track's
                    newPerformer = track.Performer;
                }
                else if (!newPerformer.OrdEquals(track.Performer))
                {
                    // tracks have different performers
                    // cannot set global
                    return;
                }
            }
            Performer = newPerformer;
        }
    }

    private void UpdateGlobalComposer()
    {
        if (Composer is null)
        {
            string? newComposer = null;
            foreach (CueTrack track in Tracks)
            {
                if (!track.CommonFieldsSet.HasFlag(FieldsSet.Composer))
                {
                    // Composer is not set, cannot set global
                    return;
                }
                if (newComposer is null)
                {
                    // set performer to current track's
                    newComposer = track.Composer;
                }
                else if (!newComposer.OrdEquals(track.Composer))
                {
                    // tracks have different Composers
                    // cannot set global
                    return;
                }
            }
            Composer = newComposer;
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
        if (Tracks.Any(x => x.EacEndIndex is not null))
        {
            type |= CueType.InterfileGaps;
        }

        SheetType = type;
    }

    private void RefreshFiles()
    {
        SourceFile?.Refresh();
        foreach (var file in Files)
        {
            file.RefreshFileInfo();
        }
        CdTextFile?.Refresh();

        _associatedFiles.Clear();
        _associatedFiles.AddRange(CuePackage.GetAssociatedFiles(this));
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
        //newCue.Container.CloneFrom(Container);
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

    internal string DefaultFilename
    {
        get => $"{Performer ?? "No Artist"} - {Title ?? "No Title"}";
    }

    /// <summary>
    /// Read the file at the specified <paramref name="path"/> using the default <see cref="CueReader">reader</see>.
    /// </summary>
    /// <param name="path">File path of the CUE sheet.</param>
    /// <returns>A new instance of <see cref="CueSheet"/>.</returns>
    public static CueSheet Read(string path) => new CueReader2().Read(path);
}
