using CueSheetNet.Extensions;
using CueSheetNet.FormatReaders;
using CueSheetNet.Syntax;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Security;
using System.Net.WebSockets;
#pragma warning disable MA0008 // Add StructLayoutAttribute

namespace CueSheetNet.Components;

public record CueTrack : IRemarkable
{
    private readonly Remcom RemarksComments = new();
    private readonly Sheet _sheet;
    private readonly ResettableSlice<CueIndex, Link> _indices;
    private string? _title;
    private string? _performer;
    private string? _songwriter;

    internal int? DesiredPosition
    {
        get;
        set
        {
            if (value > 0 || value is null)
            {
                field = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(DesiredPosition), "Track position must be greater than zero");
            }
        }
    }
    /// <summary>
    /// It is not zero-based, it starts at 1
    /// </summary>
    internal int AbsolutePosition => _sheet.Tracks.IndexOfRef(this) + 1;
    public int Position => DesiredPosition ?? AbsolutePosition;

    // Track metadata (Performer and Songwriter can be inherited from the parent sheet)
    public string? Performer { get => _performer ?? _sheet.Performer; set => _performer = value; }
    public string? Songwriter { get => _songwriter ?? _sheet.Songwriter; set => _songwriter = value; }

    public string? Composer
    {
        get => RemarksComments.GetRemarkValue("COMPOSER");
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                RemarksComments.AddRemark("COMPOSER", value);
            }
        }
    }
    public string? Upc
    {
        get => RemarksComments.GetRemarkValue("UPC");
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                RemarksComments.AddRemark("UPC", value);
            }
        }
    }
    public string? Genre
    {
        get => RemarksComments.GetRemarkValue("GENRE");
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                RemarksComments.AddRemark("GENRE", value);
            }
        }
    }
    public int? Date
    {
        get
        {
            if (RemarksComments.GetRemarkValue("DATE") is string d && int.TryParse(d, CultureInfo.InvariantCulture, out int date))
            {
                return date;
            }
            return null;
        }

        set
        {
            if (value is null)
                RemarksComments.RemoveRemark("DATE");
            else
                RemarksComments.AddRemark("DATE", value.ToString());
        }
    }
    public double? ReplayGainTrackGain
    {
        get
        {
            if (RemarksComments.GetRemarkValue("REPLAYGAIN_TRACK_GAIN") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var date))
            {
                return date;
            }
            return null;
        }

        set
        {
            if (value is null)
                RemarksComments.RemoveRemark("REPLAYGAIN_TRACK_GAIN");
            else
                RemarksComments.AddRemark("REPLAYGAIN_TRACK_GAIN", value.ToString());
        }
    }
    public double? ReplayGainTrackPeak
    {
        get
        {
            if (RemarksComments.GetRemarkValue("REPLAYGAIN_TRACK_PEAK") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var date))
            {
                return date;
            }
            return null;
        }

        set
        {
            if (value is null)
                RemarksComments.RemoveRemark("REPLAYGAIN_TRACK_PEAK");
            else
                RemarksComments.AddRemark("REPLAYGAIN_TRACK_PEAK", value.ToString());
        }
    }
    public double? ReplayGainAlbumGain
    {
        get
        {
            if (RemarksComments.GetRemarkValue("REPLAYGAIN_ALBUM_GAIN") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var date))
            {
                return date;
            }
            return null;
        }

        set
        {
            if (value is null)
                RemarksComments.RemoveRemark("REPLAYGAIN_ALBUM_GAIN");
            else
                RemarksComments.AddRemark("REPLAYGAIN_ALBUM_GAIN", value.ToString());
        }
    }
    public double? ReplayGainAlbumPeak
    {
        get
        {
            if (RemarksComments.GetRemarkValue("REPLAYGAIN_ALBUM_GAIN") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var date))
            {
                return date;
            }
            return null;
        }

        set
        {
            if (value is null)
                RemarksComments.RemoveRemark("REPLAYGAIN_ALBUM_GAIN");
            else
                RemarksComments.AddRemark("REPLAYGAIN_ALBUM_GAIN", value.ToString());
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
        };
    }

    public CueTime Start => Indices[0].StartsContent ? Indices[0].Time : Indices[1].Time;

    public CueTime? Duration
    {
        get
        {
            Debug.Assert(File.IsAudio());
            var conInd = this.ConsecutivePosition;
            var lastIndexIndex = Indices[-1].AbsolutePosition;

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

    public CueTrack? PreviousTrackInFile()
    {
        var prevIndex = Position - 1;
        if (prevIndex < 0)
        {
            return null;
        }
        var prev = _sheet.Data[prevIndex];
        if (ReferenceEquals(prev.File, this.File))
        {
            return prev.Track;
        }
        return null;
    }

    public CueTrack? NextTrackInFile()
    {
        var nextIndex = Position + 1;
        if (nextIndex >= _sheet.Data.Count)
        {
            return null;
        }
        var next = _sheet.Data[nextIndex];
        if (ReferenceEquals(next.File, this.File))
        {
            return next.Track;
        }
        return null;
    }

    public bool IsEacStyle => _indices[0].IsSplit;

    public bool HasZerothIndex => _indices[0].Position == 0;

    private string GetDefaultTitle() => $"{Performer} {_sheet.Title} {Position:D2}";

    #region IRemarkable
    public IReadOnlyList<string> Comments => ((IRemarkable)RemarksComments).Comments;

    public IReadOnlyList<CueRemark> Remarks => ((IRemarkable)RemarksComments).Remarks;

    public CueTime? PreGap { get; internal set; }
    public CueTime? PostGap { get; internal set; }

    public void AddComment(string comment)
    {
        ((IRemarkable)RemarksComments).AddComment(comment);
    }

    public void AddRemark(string type, string value)
    {
        ((IRemarkable)RemarksComments).AddRemark(type, value);
    }

    public void ClearComments()
    {
        ((IRemarkable)RemarksComments).ClearComments();
    }

    public void ClearRemarks()
    {
        ((IRemarkable)RemarksComments).ClearRemarks();
    }

    public void RemoveComment(string comment)
    {
        ((IRemarkable)RemarksComments).RemoveComment(comment);
    }

    public void RemoveRemark(CueRemark remark)
    {
        ((IRemarkable)RemarksComments).RemoveRemark(remark);
    }

    public void RemoveRemark(string type, string value)
    {
        ((IRemarkable)RemarksComments).RemoveRemark(type, value);
    }
    #endregion // IRemarkable
}

#pragma warning restore MA0008 // Add StructLayoutAttribute