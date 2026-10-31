using CueSheetNet.Extensions;
using CueSheetNet.FormatReaders;
using CueSheetNet.Syntax;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Security;
using System.Net.WebSockets;
namespace CueSheetNet.Components;

public record CueTrack : IRemarkable
{
    public Remarks Remarks { get; }

    private readonly Sheet _sheet;
    private readonly ResettableSlice<CueIndex, Link> _indices;
    private string? _title;
    private string? _performer;
    private string? _songwriter;
    internal int Gap { get => field; set => field = value < 0 ? 0 : value; }

    internal int AbsolutePosition => _sheet.Tracks.IndexOfRef(this);
    public int Position => AbsolutePosition + Gap + 1;

    // Track metadata (Performer and Songwriter can be inherited from the parent sheet)
    public string? Performer { get => _performer ?? _sheet.Performer; set => _performer = value; }
    public string? Songwriter { get => _songwriter ?? _sheet.Songwriter; set => _songwriter = value; }

    public string? Composer
    {
        get => Remarks.GetField("COMPOSER") ?? _sheet.Composer;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Remarks.AddUnique("COMPOSER", value);
            }
        }
    }
    public string? Upc
    {
        get => Remarks.GetField("UPC") ?? _sheet.Upc;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Remarks.AddUnique("UPC", value);
            }
        }
    }
    public string? Genre
    {
        get => Remarks.GetField("GENRE") ?? _sheet.Genre;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Remarks.AddUnique("GENRE", value);
            }
        }
    }
    public int? Date
    {
        get
        {
            if (Remarks.GetField("DATE") is string d && int.TryParse(d, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }
            return _sheet.Date;
        }

        set
        {
            if (value is null)
                Remarks.RemoveField("DATE");
            else
                Remarks.AddUnique("DATE", value.ToString());
        }
    }
    public double? ReplayGainTrackGain
    {
        get
        {
            if (Remarks.GetField("REPLAYGAIN_TRACK_GAIN") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return null;
        }

        set
        {
            if (value is null)
                Remarks.RemoveField("REPLAYGAIN_TRACK_GAIN");
            else
                Remarks.AddUnique("REPLAYGAIN_TRACK_GAIN", value.ToString());
        }
    }
    public double? ReplayGainTrackPeak
    {
        get
        {
            if (Remarks.GetField("REPLAYGAIN_TRACK_PEAK") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return null;
        }

        set
        {
            if (value is null)
                Remarks.RemoveField("REPLAYGAIN_TRACK_PEAK");
            else
                Remarks.AddUnique("REPLAYGAIN_TRACK_PEAK", value.ToString());
        }
    }
    public double? ReplayGainAlbumGain
    {
        get
        {
            if (Remarks.GetField("REPLAYGAIN_ALBUM_GAIN") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
           
            return _sheet.ReplayGainAlbumGain;
        }

        set
        {
            if (value is null)
                Remarks.RemoveField("REPLAYGAIN_ALBUM_GAIN");
            else
                Remarks.AddUnique("REPLAYGAIN_ALBUM_GAIN", value.ToString());
        }
    }
    public double? ReplayGainAlbumPeak
    {
        get
        {
            if (Remarks.GetField("REPLAYGAIN_ALBUM_PEAK") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            
            return _sheet.ReplayGainAlbumPeak;
        }

        set
        {
            if (value is null)
                Remarks.RemoveField("REPLAYGAIN_ALBUM_PEAK");
            else
                Remarks.AddUnique("REPLAYGAIN_ALBUM_PEAK", value.ToString());
        }
    }
    public string? Isrc { get; set; }

    // Title is not inherited from the sheet and must be present on the track if needed
    [AllowNull]
    public string Title { get => _title ?? GetDefaultTitle(); set => _title = value?.NonEmpty(); }

    // Pregap/Postgap values (optional)
    public CueTime? Pregap { get; set; }
    public CueTime? Postgap { get; set; }

    public TrackType Type { get; internal set; }

    // Arbitrary flags (DCP, 4CH, PRE, SCMS, etc.)
    public TrackFlags Flags { get; internal set; }

    internal int ConsecutivePosition => _sheet._tracks.IndexOf(this);
    public CueDataFile File => _sheet.Data.First(x => ReferenceEquals(x.Track, this) && !x.Index.IsSplit)!.File;

    public IReadOnlyList<CueIndex> Indices => _indices;

    internal CueTrack(TrackType type, Sheet sheet)
    {
        Type = type;
        _sheet = sheet;
        _indices = new(_sheet.Data, _sheet._indices, x => x.Index, x => ReferenceEquals(x.Track, this) && ReferenceEquals(x.File, this.File));
    }
    internal CueTrack(string type, Sheet sheet) : this(TrackType.FromString(type), sheet) { }
    internal static CueTrack FromInsertionParams(TrackInsertParameters? insertParameters, Sheet sheet)
    {
        return new(insertParameters?.Type ?? TrackType.AUDIO, sheet)
        {
            Title = insertParameters?.Title,
            Performer = insertParameters?.Performer,
            Songwriter = insertParameters?.Songwriter,
            Isrc = insertParameters?.Isrc,
            Flags = insertParameters?.Flags ?? default,
            Gap = insertParameters?.Gap ?? 0,
        };
    }

    public CueTime Start => Indices[0].StartsContent ? Indices[0].Time : Indices[1].Time;

    public CueTime? Duration
    {
        get
        {
            Debug.Assert(File.IsAudio());
            var conInd = this.ConsecutivePosition;
            var lastIndexIndex = Indices[^1].AbsolutePosition;

            if (_sheet.Data.Count > lastIndexIndex + 1)
            {
                // there are indices (tracks) after this
                var nextLink = _sheet.Data[lastIndexIndex + 1];
                if (ReferenceEquals(nextLink.File, this.File))
                {
                    // Next index is in the same file, ergo next track
                    // may be eac or not, does not matter
                    return nextLink.Index.Time;
                }
                // different file, so need to refer to file
            }

            if (File.Meta is FileMetadata meta)
            {
                return meta.CueDuration - Start;
            }
            return null;
        }
    }

    public CueTrack? PreviousTrackInFile() => GetChain(false, true);

    public CueTrack? NextTrackInFile() => GetChain(true, true);

    public CueTrack? NextTrack() => GetChain(true, false);
    public CueTrack? PreviousTrack() => GetChain(false, false);

    private CueTrack? GetChain(bool forward, bool file)
    {
        var inTrackIndex = _sheet.Tracks.IndexOfRef(this);
        var diff = forward ? 1 : -1;
        if(inTrackIndex + diff >= _sheet.Tracks.Count || inTrackIndex<0)
        {
            return null;
        }
        var chain = _sheet.Tracks[inTrackIndex+diff];
        if (file)
        {
            if (!ReferenceEquals(chain.File, this.File))
            {
                return null;
            }
        }
        return chain;
    }

    public bool IsEacStyle => _indices[0].IsSplit;

    public bool HasZerothIndex => _indices[0].Position == 0;

    public CueTime PreGap { get; internal set; }
    public CueTime PostGap { get; internal set; }

    private string GetDefaultTitle() => $"{Performer} {_sheet.Title} {Position:D2}";
}

#pragma warning restore MA0008 // Add StructLayoutAttribute