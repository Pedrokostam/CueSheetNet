#pragma warning disable MA0008 // Add StructLayoutAttribute

using CueSheetNet.Components;
using CueSheetNet.Exceptions;
using CueSheetNet.Extensions;
using CueSheetNet.Internal;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Timers;

namespace CueSheetNet;

public interface ICueConformance
{
    /// <summary>
    /// Gets a value indicating whether verification is performed against the file duration.
    /// </summary>
    bool VerifyAgainstFileDuration { get; }
    /// <summary>
    /// If True, PREGAP and POSTGAP can only be placed once per file, at the first and last track, respectively.
    /// <br/>
    /// If False, all tracks may have one or both.
    /// </summary>
    bool LimitPrePostGapPositions { get; }

    char IndentationCharacter { get; }
    char Quotation { get; }
}

public record CueSettings
{
    public bool VerifyAgainstFileDuration { get; set; } = false;
}

public class CueSheet : IRemarkable
{

    internal bool UseQualifiedPaths { get; private set; }
    public CueSettings Settings { get; set; } = new();
    public Remarks Remarks { get; } = [];
    internal void Invalidate()
    {
        _tracks.Invalidate();
        _files.Invalidate();
        _indices.Invalidate();
    }
    internal List<Link> Data { get; } = [];

    public CueSheet() : this((FileInfo?)null) { }
    public CueSheet(string? filepath) : this(string.IsNullOrWhiteSpace(filepath) ? null : new FileInfo(filepath)) { }
    public CueSheet(FileInfo? sourceFile)
    {
        _tracks = new(x => x.Select(y => y.Track).Distinct(), Data);
        _files = new(x => x.Select(y => y.File).Distinct(), Data);
        _indices = new(x => x.Select(y => y.Index).Distinct(), Data);
        _additionalFiles = new ResettableCollection<CueExtraFile>(() => AdditionalFileHelper.FindExtraFiles(this));
        SourceFile = sourceFile;
    }

    internal readonly ResettableDependentCollection<CueTrack, Link> _tracks;
    internal readonly ResettableDependentCollection<CueDataFile, Link> _files;
    internal readonly ResettableDependentCollection<CueIndex, Link> _indices;
    private readonly ResettableCollection<CueExtraFile> _additionalFiles;

    private string? _title;
    private FileInfo? _sourceFile;

    public Encoding? SourceEncoding { get; internal set; }
    public IReadOnlyList<CueTrack> Tracks => _tracks;
    public IReadOnlyList<CueDataFile> Files => _files;
    public IReadOnlyList<CueIndex> Indices => _indices;
    public IReadOnlyList<CueExtraFile> AdditionalFiles => _additionalFiles;

    public void RefreshAdditionalFiles()
    {
        SourceFile?.Refresh();
        _additionalFiles.Invalidate();
    }
    public FileInfo? SourceFile
    {
        get => _sourceFile; internal set
        {
            if (!FileSystemInfoComparer.Default.Equals(value, _sourceFile))
            {
                _sourceFile = value;
                RefreshAdditionalFiles();
            }

        }
    }
    public string? Catalog { get; set; }
    public CueExtraFile? CdTextFile { get; set; }
    // General sheet-level metadata that can be inherited by tracks
    public string? Performer { get; set; }
    public string? Songwriter { get; set; }
    [AllowNull]
    public string Title { get => _title ?? GetDefaultTitle(); set => _title = value?.NonEmpty(); }

    internal string GetDefaultTitle()
    {
        string? n = null;
        if (SourceFile is not null)
        {
            n = Path.ChangeExtension(SourceFile?.Name, null);
        }
        else if (_files.Count > 0)
        {
            n = _files[0].Name;
        }
        return n ?? "Unnamed";
    }

    /// <summary>
    /// Modifies all applicable filepaths related to this sheet depending on <paramref name="keepPathQualified"/>:
    /// <list type="bullet">
    /// <item>True -  Relative paths will become qualified, unless <see cref="SourceFile"/> is null; already qualified paths will remain as such.</item>
    /// <item>False -  Qualified paths will be turned relative, unless <see cref="SourceFile"/> is null, or the two do not share common root.</item> 
    /// </list>
    /// </summary>
    /// <param name="keepPathQualified"></param>
    public void ChangeFilePathsType(bool keepPathQualified)
    {
        UseQualifiedPaths = keepPathQualified;
        foreach (var item in AdditionalFiles)
        {
            item.ChangeFilePathType(keepPathQualified);
        }
        foreach (var item in Files)
        {
            item.ChangeFilePathType(keepPathQualified);
        }
        CdTextFile?.ChangeFilePathType(keepPathQualified);
    }
    /// <summary>
    /// Inserts a new index at the specified time in the given file and track.
    /// </summary>
    /// <param name="file">The file to insert the index into.</param>
    /// <param name="time">The time position for the new index.</param>
    /// <param name="track">The track to insert the index into. Ultimately, the track will depened on the time position. If this parameter is not null, it must match the time position in the file.</param>
    /// <exception cref="ArgumentException">Thrown when the specified track does not belong to the given file.</exception>
    /// <exception cref="DuplicateTimeException">Thrown when an index with the specified time already exists in the file.</exception>
    /// <exception cref="IndexOutsideOfTrackException">Thrown when the index is inserted outside the bounds of the specified track.</exception>
    public void InsertIndex(CueDataFile file, CueTime time, CueTrack? track = null)
    {
        NegativeIndexTimeException.ThrowIfNegative(time);
        ThrowIfOutsideOfFile(time, file);
        if (track is not null && !ReferenceEquals(track.File, file))
            throw new ArgumentException($"Track {track} does not belong to {file}");
        var allFileIndices = file.GetAllIndices().ToList();
        if (allFileIndices.Exists(x => x.Time == time))
            throw new DuplicateTimeException(time, file);
        int insertionIndex = allFileIndices.FindIndex(x => x.Time > time);
        CueTrack expectedTrack;
        int dataInsertionIndex;
        if (insertionIndex < 0) // no time in the given file is greater than the given time
        {
            expectedTrack = file.Tracks[^1]; // the index is being added to the last track
            dataInsertionIndex = Data.FindIndex(x => ReferenceEquals(x.Index, expectedTrack.Indices[^1])) + 1;
        }
        else
        {
            CueIndex cueIndexToBeMovedForward = allFileIndices[insertionIndex]; // the cueindex at the insertion point will be pushed by one position
            dataInsertionIndex = Data.FindIndex(x => ReferenceEquals(x.Index, cueIndexToBeMovedForward));
            expectedTrack = Data[dataInsertionIndex].Track;
        }
        track ??= expectedTrack;
        if (!ReferenceEquals(track, expectedTrack))
            throw new IndexOutsideOfTrackException(track, time);
        var precedingCueIndexPosition = dataInsertionIndex switch
        {
            0 => 0,
            _ when ReferenceEquals(Data[dataInsertionIndex - 1].Track, track) => Data[dataInsertionIndex - 1].Index.Position,
            _ => 0,
        };
        var link = new Link(file, track, new CueIndex(precedingCueIndexPosition + 1, time, this));
        Data.Insert(dataInsertionIndex, link);
        Invalidate();
        for (int i = dataInsertionIndex + 1; i < Data.Count; i++)
        {
            if (!ReferenceEquals(Data[i].Track, track))
            {
                break;
            }
            Data[i].Index.Position++;
        }
    }

    public void InsertTrack(CueDataFile file, TrackInsertParameters trackParams, CueTime firstIndex) //null -> last
    {
        NegativeIndexTimeException.ThrowIfNegative(firstIndex);
        ThrowIfOutsideOfFile(firstIndex, file);
        var allFileIndices = file.GetAllIndices().ToList();
        if (allFileIndices.Exists(x => x.Time == firstIndex))
            throw new DuplicateTimeException(firstIndex, file);
        int insertionIndex = allFileIndices.FindIndex(x => x.Time > firstIndex);
        int dataInsertionIndex;
        if (insertionIndex < 0) // the track is put at the end of the file
        {
            var nextFile = file.NextFile();
            dataInsertionIndex = nextFile switch
            {
                null => Data.Count, // its last file append at the very end of data
                _ => Data.FindIndex(x => ReferenceEquals(x.File, nextFile)), // take the first index of the next file as insertion
            };
        }
        else // its between existing indices - and we can only insert a track after the last index of another track
        {
            CueIndex cueIndexToBeMovedForward = allFileIndices[insertionIndex];
            dataInsertionIndex = Data.FindIndex(x => ReferenceEquals(x.Index, cueIndexToBeMovedForward));
            if (dataInsertionIndex > 0)
            {
                if (ReferenceEquals(Data[dataInsertionIndex - 1].Track, cueIndexToBeMovedForward.Track))
                {
                    // the indices that will be separated belong to the same track (it's a no-no)
                    // unless the prior index is split
                    if (!Data[dataInsertionIndex - 1].Index.IsSplit)
                    {
                        // if not split - invalid operation
                        throw new InvalidTrackPlacementException(firstIndex);
                    }
                }
            }
        }
        var link = new Link(file,
                            CueTrack.FromInsertionParams(trackParams, this),
                            new CueIndex(1, firstIndex, this));
        Data.Insert(dataInsertionIndex, link);
        Invalidate();
        if (dataInsertionIndex > 0 && Data[dataInsertionIndex - 1].Index.IsSplit)
        {
            Data[dataInsertionIndex - 1] = Data[dataInsertionIndex - 1] with { Track = link.Track };
        }
        NormalizeGaps(-1, link.Track.AbsolutePosition);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="from">Not inclusive</param>
    /// <param name="change"></param>
    private void NormalizeGaps(int change, int inclusiveStartIndex = 0)
    {
        if (_tracks.Count == 0 || _tracks.Count < inclusiveStartIndex)
        {
            return;
        }
        int gap = 0;
        for (int i = 0; i < _tracks.Count; i++)
        {
            var track = _tracks[i];
            if (gap == 0)
            {
                gap = track.Gap;
            }
            if (i < inclusiveStartIndex)
            {
                track.Gap = gap;
            }
            else
            {
                track.Gap = gap + change;
            }
        }
    }


    public void InsertFile(int insertionIndex, string filepath, string type, TrackInsertParameters? trackInsertParameters = null ) => InsertFile(insertionIndex, filepath, FileType.FromString(type), trackInsertParameters);
    public void InsertFile(int? index, string filepath, FileType type, TrackInsertParameters? trackInsertParameters = null)
    {
        CueTime time = CueTime.Zero; // first index in a file MUST be 00:00:00
        var fileToAdd = new CueDataFile(filepath, this, type);
        ThrowIfOutsideOfFile(time, fileToAdd);
        NegativeIndexTimeException.ThrowIfNegative(time);
        int fileInsertionIndex = index ?? _files.Count;
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fileInsertionIndex, _files.Count, nameof(index));
        ArgumentOutOfRangeException.ThrowIfNegative(fileInsertionIndex, nameof(index));
        CueDataFile? prevFile = fileInsertionIndex switch
        {
            0 => null,
            _ => _files[fileInsertionIndex - 1]
        };
        CueDataFile? nextFile = fileInsertionIndex switch
        {
            int x when x < _files.Count => _files[fileInsertionIndex],
            _ => null,
        };
        int dataInsertionIndex = nextFile switch
        {
            null => Data.Count, // no next file - put the new file at the end
            _ => Data.FindIndex(x => ReferenceEquals(x.File, nextFile)), // next file with be pushed
        };
        var link = new Link(fileToAdd,
                            CueTrack.FromInsertionParams(trackInsertParameters, this),
                            new CueIndex(1, time, this));
        Data.Insert(dataInsertionIndex, link);
        Invalidate();
        if (dataInsertionIndex > 0 && Data[dataInsertionIndex - 1].Index.IsSplit)
        {
            Data[dataInsertionIndex - 1] = Data[dataInsertionIndex - 1] with { Track = link.Track };
        }
        NormalizeGaps(-1, link.Track.AbsolutePosition);
    }


    internal void AddIndex(CueIndex index, CueTrack track, CueDataFile file)
    {
        NegativeIndexTimeException.ThrowIfNegative(index);
        if (Data.Count == 0)
        {
            AddFirstIndex(index, track!, file!);
            return;
        }
        var lastData = Data[^1];
        file ??= lastData.File;
        track ??= lastData.Track;
        ThrowIfOutsideOfFile(index, file);
        bool sameTrack = ReferenceEquals(lastData.Track, track);
        bool markPrevAsSplit = false;

        if (sameTrack)
        {
            if (lastData.Index.Position != index.Position - 1)
                throw new InvalidIndexPositionException(lastData.Index, index);

            if (!ReferenceEquals(lastData.File, file))
            {
                if (lastData.Index.Position != 0)
                {
                    // indices of one track split across files (and not in an eac compatible way)
                    throw new TrackSplitAcrossFileException(track);
                }
                // EAC - new index belongs to the same track, but different file
                markPrevAsSplit = true;
            }
        }
        else
        {
            if (index.Position is < 0 or > 1)
                throw new InvalidIndexPositionException(index);
        }

        if (ReferenceEquals(lastData.File, file))
        {
            if (lastData.Index.Time >= index.Time)
                throw new InvalidIndexTimeException(lastData.Index, index);
        }
        if (markPrevAsSplit)
            lastData.Index.IsSplit = true;
        Data.Add(new(file, track, index));
    }

    private void AddFirstIndex(CueIndex index, CueTrack track, CueDataFile file)
    {
        if (track is null || file is null)
        {
            throw new UnexpectedIndexException();
        }
        if (index.Position is < 0 or > 1)
        {
            throw new InvalidIndexPositionException(index);
        }
        ThrowIfOutsideOfFile(index, file);
        Data.Add(new(file, track, index));
    }

    /// <summary>
    /// Validates that the specified index time does not exceed the duration of the file if verification is enabled.
    /// </summary>
    /// <param name="index">The cue index containing the time to validate against the file duration.</param>
    /// <inheritdoc cref="ThrowIfOutsideOfFile(CueTime, CueDataFile)"/>
    private void ThrowIfOutsideOfFile(CueIndex index, CueDataFile? file) => ThrowIfOutsideOfFile(index.Time, file);
    /// <summary>
    /// Validates that the specified time does not exceed the duration of the file if verification is enabled.
    /// </summary>
    /// <param name="time">The cue time to validate against the file duration.</param>
    /// <param name="file">The cue data file containing metadata for validation.</param>
    /// <exception cref="IndexOutsideOfFileException">Thrown when the specified time is outside the bounds of the file duration.</exception>
    private void ThrowIfOutsideOfFile(CueTime time, CueDataFile? file)
    {
        if (!Settings.VerifyAgainstFileDuration || file?.Meta is null)
        {
            return;
        }
        if (file.Meta.Value.CueDuration <= time)
        {
            throw new IndexOutsideOfFileException(file.Name, time);
        }
    }
}

