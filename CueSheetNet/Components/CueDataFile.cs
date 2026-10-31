using System.Diagnostics;
using CueSheetNet.Components;
using CueSheetNet.FileHandling;
using CueSheetNet.FormatReaders;
using CueSheetNet.Internal;

namespace CueSheetNet;
/// <summary>
/// Provides properties and instance methods for a data file (including audio) that is specified in a cuesheet.
/// The class monitors the file to detect any changes to it.
/// </summary>
public class CueDataFile : FilePath
{

    private readonly ResettableSlice<CueTrack, Link> _tracks;

    /// <summary>
    /// Detects file type from the file's extension. Unknown extension are assumed to be <see cref="FileType.WAVE"/>.
    /// </summary>
    /// <param name="path">File path with extension.</param>
    /// <returns>Detected file type.</returns>
    public static FileType GetFileTypeFromPath(string path)
    {
        var extension = System.IO.Path.GetExtension(path).AsSpan()[1..];
        Span<char> lower = stackalloc char[extension.Length];
        extension.ToLowerInvariant(lower);
        return lower switch
        {
            "mp3" or "bit" => FileType.MP3,
            "aiff" or "aif" or "aifc" => FileType.AIFF,
            "wav" or "wave" => FileType.WAVE,
            "bin" or "mm2" or "iso" or "img" => FileType.BINARY,
            "mot" => FileType.MOTOROLA,
            _ => FileType.WAVE
        };
    }


    public CueDataFile(Sheet sheet, string filePath, FileType type,bool keepQualified=false) : base(filePath, sheet,keepQualified)
    {
        Type = type;
        _tracks = new(_sheet.Data, _sheet._tracks, x => x.Track, x => ReferenceEquals(x.File, this) && !x.Index.IsSplit);
    }

    public int Position => _sheet._files.FindIndex(x => ReferenceEquals(x, this));

    public IReadOnlyList<CueTrack> Tracks => _tracks;

    /// <summary>
    /// Type of the file's data.
    /// </summary>
    public FileType Type { get; internal set; }

    private FileMetadata? _meta;

    /// <summary>
    /// Metadata of this file.
    /// </summary>
    public FileMetadata? Meta
    {
        get
        {
            RefreshMeta();
            return _meta;
        }
        private set => _meta = value;
    }

    /// <summary>
    /// Signals whether the info about the file needs to be resynced.
    /// </summary>
    private bool NeedsRefresh { get; set; }
    public long FileSize => Info?.Length ?? -1;

    public void RefreshMeta()
    {
        Debug.WriteLine($"Refreshing file meta: {Path}");
        if (this.Exists)
        {
            FileMetadata? resMeta = FormatReader.ReadMetadata(
                AbsolutePath,
                Tracks.Select(x => x.Type)
            );
            Meta = resMeta;
        }
        else
        {
            Meta = null;
        }
    }

    /// <summary>
    /// Checks whether the file is an audio file.
    /// </summary>
    /// <returns><see langword="true"/> if file's Type is one of the audio type, otherwise <see langword="false"/></returns>
    public bool IsAudio() => Type.IsAudio;

    public CueDataFile? PreviousFile()
    {
        var prevIndex = Position - 1;
        if (prevIndex < 0)
        {
            return null;
        }
        return _sheet.Files[prevIndex];
    }

    public CueDataFile? NextFile()
    {
        var nextIndex = Position + 1;
        if (nextIndex >= _sheet.Data.Count)
        {
            return null;
        }
        return _sheet.Files[nextIndex];

    }

    /// <summary>
    /// Sets the <see cref="SourceFile"/> to a new path, optionally detecting its type automatically.
    /// </summary>
    /// <remarks>
    /// Also starts monitoring the new file, to detect changes made to it.
    /// </remarks>
    /// <param name="newPath">Path of the new file.</param>
    /// <param name="newType">Optional, file type. If set to <see langword="null"/>, detects the type from the extension.</param>
    public void Update(string newPath, FileType? newType = null)
    {
        base.Update(newPath);
        Type = newType ?? Type;
        RefreshMeta();
    }

    /// <summary>
    /// Gets all CUE indices of this file.
    /// </summary>
    public IEnumerable<CueIndex> GetAllIndices() => _sheet.Data.Where(x => ReferenceEquals(x.File, this)).Select(x => x.Index);

    public override string ToString()
    {
        return "File " + Position.ToString("D2") + " \"" + Path + "\" " + Type;
    }
}
