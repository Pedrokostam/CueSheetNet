using CueSheetNet.Extensions;
using CueSheetNet.FormatReaders;
using CueSheetNet.Syntax;
using CueSheetNet.Writing;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Security;
using System.Net.WebSockets;
namespace CueSheetNet.Components;

public class CueTrack : IRemarkable
{
    public Remarks Remarks { get; } = [];

    internal readonly CueSheet _sheet;
    readonly ResettableSlice<CueIndex, Link> _indices;
    private string? _title;
    private string? _performer;
    private string? _songwriter;
    internal int Gap { get; set => field = value < 0 ? 0 : value; }
    internal FieldsSet ExplicitFields
    {
        get
        {
            FieldsSet fields = FieldsSet.None;
            if (_title is not null)
                fields |= FieldsSet.Title;
            if (_performer is not null)
                fields |= FieldsSet.Performer;
            if (_songwriter is not null)
                fields |= FieldsSet.Songwriter;
            return fields;
        }
    }
    internal int AbsolutePosition => _sheet.Tracks.IndexOfRef(this);
    /// <summary>
    /// Track's position, calculated dynamically by its index in <see cref="CueSheet.Tracks"/> (+1) and optionally shifted by <see cref="Gap"/>.
    /// </summary>
    public int Position => AbsolutePosition + Gap + 1;

    // Track metadata (Performer and Songwriter can be inherited from the parent sheet)
    public string? Performer { get => _performer ?? _sheet.Performer; set => _performer = value; }
    public string? Songwriter { get => _songwriter ?? _sheet.Songwriter; set => _songwriter = value; }


    public string? Isrc { get; set; }

    // Title is not inherited from the sheet and must be present on the track if needed
    [AllowNull]
    public string Title { get => _title ?? GetDefaultTitle(); set => _title = value?.NonEmpty(); }


    public TrackType Type { get; internal set; }

    // Arbitrary flags (DCP, 4CH, PRE, SCMS, etc.)
    public TrackFlags Flags { get; internal set; }

    internal int ConsecutivePosition => _sheet._tracks.IndexOf(this);
    public CueDataFile File => _sheet.Data.First(x => ReferenceEquals(x.Track, this) && !x.Index.IsSplit)!.File;

    public IReadOnlyList<CueIndex> Indices => _indices;

    internal CueTrack(TrackType type, CueSheet sheet)
    {
        Type = type;
        _sheet = sheet;
        _indices = new(_sheet.Data, _sheet._indices, x => x.Index, GetIndicesOfTrack);
    }

    private bool GetIndicesOfTrack(Link x)
    {
        return ReferenceEquals(x.Track, this) && ReferenceEquals(x.File, this.File);
    }

    /// <summary>
    /// Deep copy constructor
    /// </summary>
    /// <param name="track"></param>
    /// <param name="sheet"></param>
    internal CueTrack(CueTrack track, CueSheet sheet) : this(track.Type, sheet)
    {
        this.Songwriter = track._songwriter;
        this.Pregap = track.Pregap;
        this.Postgap = track.Postgap;
        this.Flags = track.Flags;
        this.Title = track._title;
        this.Isrc = track.Isrc;
        this.Gap = track.Gap;
        this.Performer = track._performer;
        foreach (var item in track.Remarks)
        {
            this.Remarks.Add(item);
        }
    }
    internal CueTrack(string type, CueSheet sheet) : this(TrackType.FromString(type), sheet) { }
    internal static CueTrack FromInsertionParams(TrackInsertParameters? insertParameters, CueSheet sheet)
    {
        if (insertParameters is not null)
        {
            ArgumentNullException.ThrowIfNull(insertParameters.Value.Type, nameof(TrackInsertParameters.Type));

        }
        return new(insertParameters?.Type ?? TrackType.AUDIO, sheet)
        {
            Title = insertParameters?.Title,
            Performer = insertParameters?.Performer,
            Songwriter = insertParameters?.Songwriter,
            Isrc = insertParameters?.Isrc,
            Flags = insertParameters?.Flags ?? default,
            Gap = 0,
            Postgap = insertParameters?.Postgap,
            Pregap = insertParameters?.Pregap,
        };
    }

    /// <summary>
    /// Time value of INDEX 1 of the track.
    /// </summary>
    public CueTime Start => Indices[0].StartsContent ? Indices[0].Time : Indices[1].Time;

    public TimeSpan? Duration => CueDuration?.ToTimeSpan();
    public CueTime? CueDuration
    {
        get
        {
            Debug.Assert(File.IsAudio());
            var end = this.NextTrackInFile() switch
            {
                CueTrack next => next.Indices.Count > 0 ? next.Indices[0].Time : null,
                _ => this.File.Meta?.CueDuration,
            };
            return end - Start;
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
        if (inTrackIndex + diff >= _sheet.Tracks.Count || inTrackIndex + diff < 0)
        {
            return null;
        }
        var chain = _sheet.Tracks[inTrackIndex + diff];
        if (file)
        {
            if (!ReferenceEquals(chain.File, this.File))
            {
                return null;
            }
        }
        return chain;
    }

    public bool IsEacStyle => _sheet.Data.First(x => ReferenceEquals(x.Track, this)).Index.IsSplit;

    /// <summary>
    /// EAC-style tracks with the zeroth index in a previous file DO NOT count.
    /// </summary>
    public bool HasZerothIndex => _indices[0].Position == 0;

    public CueTime? Pregap { get; set; }
    public CueTime? Postgap { get; set; }

    /// <summary>
    /// Changes the value of the track's position.
    /// <para>
    /// Does not change its absolute position in the sheet!
    /// </para>
    /// <para>
    /// This is intended to introduce gaps in track numbering only.
    /// </para>
    /// </summary>
    /// <param name="newPosition"></param>
    public void SetPosition(int newPosition)
    {
        if (newPosition < AbsolutePosition + 1)
        {
            throw new InvalidOperationException("Cannot set track's position below its natural index");
        }
        Gap = newPosition - AbsolutePosition - 1;
        _sheet.NormalizeGaps(0, AbsolutePosition + 1);
    }

    private string GetDefaultTitle() => $"{Performer} {_sheet.Title} {Position:D2}".Trim();
    public override string ToString()
    {
        if (AbsolutePosition < 0)
        {
            return $"TRACK (detached) {Type} - {Title}";
        }
        return $"TRACK {Position:d2} {Type} - {Title}";
    }
}
