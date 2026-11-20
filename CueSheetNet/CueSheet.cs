#pragma warning disable MA0008 // Add StructLayoutAttribute

using CueSheetNet.Comparing;
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

public class CueSheet : IRemarkable, IEquatable<CueSheet>
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

    /// <summary>
    /// Refreshes the source file info and invalidates additional files.
    /// </summary>
    public void RefreshFileInfos()
    {
        SourceFile?.Refresh();
        _additionalFiles.Invalidate();
    }
    public FileInfo? SourceFile
    {
        get => _sourceFile; internal set
        {
            if (!FileSystemInfoComparer.Instance.Equals(value, _sourceFile))
            {
                _sourceFile = value;
                RefreshFileInfos();
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
        if (!_additionalFiles.IsEmptyOrUninitialized)
        {
            foreach (var item in AdditionalFiles)
            {
                item.ChangeFilePathType(keepPathQualified);
            }
        }
        foreach (var item in Files)
        {
            item.ChangeFilePathType(keepPathQualified);
        }
        CdTextFile?.ChangeFilePathType(keepPathQualified);
    }
    public void InsertZerothIndex()
    {

    }

    internal (CueTime? lower, CueTime? upper) GetLimitsOfTrack(CueTrack track)
    {
        var prevTrackFile = track.PreviousTrackInFile();
        var nextTrackFile = track.NextTrackInFile();
        var lowerTimeLimit = prevTrackFile?.Indices[^1].Time;
        var upperTimeLimit = nextTrackFile?.Indices[^1].Time;
        if (upperTimeLimit is null)
        {
            if (!ReferenceEquals(track.File.GetAllIndices().Last(), track.Indices[^1]))
            {
                // If there are no other tracks in the file,
                // but the last index of the file is not the last index of track (file contains split index)
                // set that index as the limit
                upperTimeLimit = track.File.GetAllIndices().Last().Time;
            }
            else if (Settings.VerifyAgainstFileDuration && track.File.Meta?.CueDuration is CueTime duration)
            {
                upperTimeLimit = duration;
            }
        }

        return (lowerTimeLimit, upperTimeLimit);
    }
    public void InsertIndex(CueTrack track, CueTime time)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentOutOfRangeException.ThrowIfNegative(time);
        (var lowerTimeLimit, var upperTimeLimit) = GetLimitsOfTrack(track);
        if (time > upperTimeLimit || time < lowerTimeLimit)
        {
            throw new InvalidOperationException($"Time {time} is outside of tracks {track} range. Limits are {lowerTimeLimit?.ToString() ?? "None"} to {upperTimeLimit?.ToString() ?? "None"}");
        }
        var prevFile = track.File.PreviousFile();
        bool isTrackSplit = prevFile?.GetAllIndices().LastOrDefault()?.IsSplit ?? false;
        int insertionIndex = 0;
        int pos = 1;
        bool trackReached = false;
        for (insertionIndex = 0; insertionIndex < Data.Count; insertionIndex++)
        {
            if (!ReferenceEquals(Data[insertionIndex].Track, track))
            {
                if (trackReached)
                {
                    break;
                }
                continue;
            }
            trackReached = true;
            pos = Data[insertionIndex].Index.Position;
            if (Data[insertionIndex].Index.Time == time)
            {
                throw new ArgumentException($"Cannot insert duplicate index {time}", nameof(time));
            }
            if (Data[insertionIndex].Index.Time > time)
            {
                break;
            }
        }
        Data.Insert(insertionIndex, new Link(track.File, track, new CueIndex(pos, time, this)));
        Invalidate();
        for (int j = insertionIndex + 1; j < Data.Count; j++)
        {
            if (!ReferenceEquals(Data[j].Track, track))
            {
                break;
            }
            Data[j].Index.Position++;
        }
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
    public void InsertIndexs(CueDataFile file, CueTime time, CueTrack? track = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(time);
        ThrowIfOutsideOfFile(time, file);
        if (track is not null && !ReferenceEquals(track.File, file))
            throw new ArgumentException($"Track {track} does not belong to {file}");
        var allFileIndices = file.GetAllIndices().ToList();
        if (allFileIndices.Exists(x => x.Time == time))
            throw new ArgumentException($"Indices in a file must be unique (file {file.Path} already has an index with time {time})");
        int insertionIndex = allFileIndices.FindIndex(x => x.Time >= time);
        CueTrack expectedTrack;
        int dataInsertionIndex;
        if (insertionIndex < 0) // no time in the given file is greater than the given time
        {
            if (allFileIndices.Count > 0 && allFileIndices[^1].IsSplit)
            {
                throw new InvalidOperationException($"Cannot add an index after the EAC-style index: {allFileIndices[^1]}");
            }
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
            throw new InvalidOperationException($"Time {time} is outside of tracks {track} range.");
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
        ArgumentOutOfRangeException.ThrowIfNegative(firstIndex);
        ThrowIfOutsideOfFile(firstIndex, file);
        var allFileIndices = file.GetAllIndices().ToList();
        if (allFileIndices.Exists(x => x.Time == firstIndex))
            throw new ArgumentException("Cannot insert a new track at an index that is already specified in the file");
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
                        throw new InvalidOperationException("Cannot insert a track in the middle of another track");
                    }
                }
            }
        }
        CueTrack? prevTrack = dataInsertionIndex > 0 ? Data[dataInsertionIndex - 1].Track : null;
        if (prevTrack is not null && trackParams.Position is int desiredPosition && desiredPosition < prevTrack.AbsolutePosition + 1)
        {
            throw new InvalidOperationException("Cannot set track's position below its natural index");
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
        if (trackParams.Position is not null)
        {

            link.Track.SetPosition(trackParams.Position.Value);
        }
    }

    internal void NormalizeGaps(int change, int startIndex)
    {
        int prevPosition = startIndex > 0 ? _tracks[startIndex - 1].Position : 0;
        for (int i = startIndex; i < _tracks.Count; i++)
        {
            var track = _tracks[i];
            int newGap = track.Gap + change;
            if (i + newGap + 1 <= prevPosition)
                newGap = prevPosition - i; // same as previous track's gap, so Position = prevPosition + 1
            track.Gap = newGap;
            prevPosition = i + newGap + 1;
        }
    }

    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="from">Not inclusive</param>
    ///// <param name="change"></param>
    //private void NormalizeGaps(int change, int inclusiveStartIndex = 0)
    //{
    //    if (_tracks.Count == 0 || _tracks.Count < inclusiveStartIndex)
    //    {
    //        return;
    //    }
    //    int gap = 0;
    //    for (int i = 0; i < _tracks.Count; i++)
    //    {
    //        var track = _tracks[i];
    //        if (i > inclusiveStartIndex)
    //        {
    //            track.Gap += change;
    //        }

    //    }
    //}

    public void RemoveIndex(CueTrack track, int position)
    {
        var index = track.Indices.FirstOrDefault(x => x.Position == position) ?? throw new ArgumentException($"Index {position} does not exist in track {track}");
        RemoveIndex(index);
    }
    public void RemoveIndex(CueTrack track, CueTime time)
    {
        var index = track.Indices.FirstOrDefault(x => x.Time == time) ?? throw new ArgumentException($"Index {time} does not exist in track {track}");
        RemoveIndex(index);
    }
    public void RemoveIndex(CueIndex index)
    {
        var track = index.Track;
        if (track.Indices.Count == 1)
        {
            throw new InvalidOperationException("Cannot remove track's only index - remove the track instead");
        }
        Data.RemoveAt(index.AbsolutePosition);
        if (index.Position == 0)
        {
            return;
        }
        foreach (var trackIndex in track.Indices)
        {
            if (trackIndex.Position > index.Position)
            {
                trackIndex.Position--;
            }
        }
        Invalidate();
    }
    public void RemoveTrack(int absolutePosition, bool preserveGap = false)
    {
        var track = Tracks.FirstOrDefault(x => x.AbsolutePosition == absolutePosition) ?? throw new ArgumentException($"Could not find track at position {absolutePosition} (note: do not use {nameof(CueTrack.Position)}, use {nameof(CueTrack.AbsolutePosition)}");
        RemoveTrack(track, preserveGap);
    }
    public void RemoveTrack(CueTrack track, bool preserveGap = false)
    {
        var file = track.File;
        int absIndex = track.AbsolutePosition;
        if (track.IsEacStyle)
        {
            if (track.NextTrackInFile() is not null)
            {
                throw new InvalidOperationException("Cannot remove split track if there are other tracks in the file");
            }
        }
        if (file.Tracks.Count == 1)
        {
            throw new InvalidOperationException("Cannot remove file's only track - remove the file instead");
        }
        Data.RemoveAll(x => ReferenceEquals(x.Track, track) && ReferenceEquals(x.File, file));
        if (preserveGap)
        {
            NormalizeGaps(1, absIndex);
        }
        Invalidate();
    }
    public void RemoveFile(CueDataFile file, bool preserveGaps = false)
    {
        if (Files.Count == 1)
        {
            throw new InvalidOperationException("Cannot remove sheet's only file - add another file first");
        }
        int trackBefore = file.PreviousFile()?.Tracks[^1].AbsolutePosition ?? 0;
        int gap = file.Tracks.Count;
        if (file.Tracks[0].IsEacStyle)
        {
            if (file.PreviousFile() is null)
            {
                throw new UnreachableException("File with first track split must not be the first file of sheet");
            }
            if (file.NextFile() is not CueDataFile nexto)
            {
                throw new InvalidOperationException("Cannot remove the last file of sheet if that file contained tracks with split index.");
            }
            if (nexto.Tracks[0].HasZerothIndex)
            {
                throw new InvalidOperationException("Cannot remove a file with a split track if the next file starts with a zero-index track");
            }
            var prevFileSplitIndexLinkIndex = Data.IndexOfRef(file) - 1;
            var nextFileFirstIndex = Data.IndexOfRef(nexto);
            Data[prevFileSplitIndexLinkIndex] = Data[prevFileSplitIndexLinkIndex] with { Track = Data[nextFileFirstIndex].Track };
        }
        Data.RemoveAll(x => ReferenceEquals(x.File, file));
        if (preserveGaps)
        {
            NormalizeGaps(gap, trackBefore);
        }
        Invalidate();
    }
    public void Spit()
    {

    }


    public void InsertFile(int? insertionIndex, string filepath, string type, TrackInsertParameters? trackInsertParameters = null) => InsertFile(insertionIndex, filepath, FileType.FromString(type), trackInsertParameters);
    public void InsertFile(int? index, string filepath, FileType type, TrackInsertParameters? trackInsertParameters = null)
    {
        CueTime time = CueTime.Zero; // first index in a file MUST be 00:00:00
        var fileToAdd = new CueDataFile(filepath, this, type);
        ThrowIfOutsideOfFile(time, fileToAdd);
        ArgumentOutOfRangeException.ThrowIfNegative(time);
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
        ArgumentOutOfRangeException.ThrowIfNegative(index.Time);
        ArgumentOutOfRangeException.ThrowIfNegative(index.Position);
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
                throw new InvalidOperationException("Lower position index must not occur after higher higher indices");

            if (!ReferenceEquals(lastData.File, file))
            {
                if (lastData.Index.Position != 0)
                {
                    // indices of one track split across files (and not in an eac compatible way)
                    throw new InvalidOperationException("Adding a track index to another file is only possible for zeroth indices");
                }
                // EAC - new index belongs to the same track, but different file
                markPrevAsSplit = true;
            }
        }
        else
        {
            if (index.Position > 1)
                throw new ArgumentException("Track's first index must have position equal to either 0 or 1", nameof(index));
        }

        if (ReferenceEquals(lastData.File, file))
        {
            if (lastData.Index.Time >= index.Time)
                throw new ArgumentException("Added index must be have later time than preceding");
        }
        if (markPrevAsSplit)
            lastData.Index.IsSplit = true;
        AddData(file, track, index);
    }

    private void AddFirstIndex(CueIndex index, CueTrack track, CueDataFile file)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(index);
        if (index.Position is < 0 or > 1)
        {
            throw new ArgumentException("First index must be either 0 or 1", nameof(index));
        }
        ThrowIfOutsideOfFile(index, file);
        AddData(file, track, index);
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
    /// <exception cref="ArgumentException">Thrown when the specified time is outside the bounds of the file duration.</exception>
    private void ThrowIfOutsideOfFile(CueTime time, CueDataFile? file)
    {
        if (!Settings.VerifyAgainstFileDuration || file?.Meta is null)
        {
            return;
        }
        if (file.Meta.Value.CueDuration <= time)
        {

            throw new ArgumentException($"Time {time} is outside of file {file.Path} range (duration is: {file.Meta.Value.CueDuration})");
        }
    }

    private void AddData(Link link)
    {
        link.File.ChangeFilePathType(UseQualifiedPaths);
        Data.Add(link);
    }
    private void AddData(CueDataFile file, CueTrack track, CueIndex index)
    {
        file.ChangeFilePathType(UseQualifiedPaths);
        Data.Add(new(file, track, index));
    }

    public bool Equals(CueSheet? other)
    {
        return CueSheetEqualityComparer.CurrentCulture.Equals(this, other);
    }
    public bool Equals(CueSheet? other, CueSheetEqualityComparer comparer)
    {
        return comparer.Equals(this, other);
    }
    public override bool Equals(object? obj)
    {
        return Equals(obj as CueSheet, CueSheetEqualityComparer.CurrentCulture);
    }
    public override int GetHashCode()
    {
        return CueSheetEqualityComparer.CurrentCulture.GetHashCode(this);
    }
    public int GetHashCode(StringComparison comparison)
    {
        return new CueSheetEqualityComparer(comparison).GetHashCode(this);
    }
    public int GetHashCode(CueSheetEqualityComparer comparer)
    {
        return comparer.GetHashCode(this);
    }
}

