using System.Collections.ObjectModel;
using CueSheetNet.Collections;
using CueSheetNet.Internal;
using CueSheetNet.Syntax;

namespace CueSheetNet;

public class CueTrack(CueDataFile parentFile, TrackType type)
    : CueItemBase(parentFile.ParentSheet),
        IEquatable<CueTrack>,
        IRemCommentable
{
    private CueDataFile _parentFile = parentFile;

    public FieldsSet CommonFieldsSet { get; private set; }

    /// <summary>
    /// Absolute index for the whole CueSheet.
    /// </summary>
    public int Index { get; internal set; }
    public int Offset { get; internal set; }
    public TrackType Type { get; internal set; } = type;
    public CueTime PostGap { get; set; }
    public CueTime PreGap { get; set; }


    /// <summary>
    /// File in which Index 01 (or 00 if there is not 01) of Track appeared
    /// </summary>
    public CueDataFile ParentFile
    {
        get
        {
            CheckOrphaned();
            return _parentFile;
        }
        set { _parentFile = value; }
    }

    /// <summary>
    /// Number of track.
    /// </summary>
    public int Number => Index + Offset;

    private string? _Title;
    public string Title
    {
        get => _Title ?? Path.ChangeExtension(ParentFile.SourceFile.Name, extension: null);
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _Title = null;
                CommonFieldsSet &= ~FieldsSet.Title;
            }
            else
            {
                _Title = value;
                CommonFieldsSet |= FieldsSet.Title;
            }
        }
    }

    private string? _Performer;
    public string? Performer
    {
        get => _Performer ?? ParentSheet.Performer;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _Performer = null;
                CommonFieldsSet &= ~FieldsSet.Performer;
            }
            else
            {
                _Performer = value;
                CommonFieldsSet |= FieldsSet.Performer;
            }
        }
    }

    private string? _Composer;
    public string? Composer
    {
        get => _Composer ?? ParentSheet.Composer;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _Composer = null;
                CommonFieldsSet &= ~FieldsSet.Composer;
            }
            else
            {
                _Composer = value;
                CommonFieldsSet |= FieldsSet.Composer;
            }
        }
    }
    public TrackFlags Flags { get; set; } = TrackFlags.None;

    public string? ISRC { get; set; }

    public bool HasZerothIndex { get; internal set; }

    public CueIndex[] Indexes => ParentSheet.GetIndexesOfTrack(Index);

    public CueIndex AudioStartIndex
    {
        get
        {
            (int Start, int End) = ParentSheet.GetIndexesOfTrack_Range(Index);
            // non-dangling because audio cannot start on previous file
            if (End - Start == 1) // Only one index for track - only 00 or only 01 - just take it
            {
                return ParentSheet.GetCueIndexAt(Start);
            }

            if (ParentSheet.IndexesImpl[Start].Number == 0)
            {
                // first index is 00, so audio starts at 01 - next
                return ParentSheet.GetCueIndexAt(Start + 1);
            }

            // first index is 01, this is audio start
            return ParentSheet.GetCueIndexAt(Start);
        }
    }
    public CueTime? Duration
    {
        get
        {
            var (_, indexOfNextTrack) = ParentSheet.GetIndexesOfTrack_Range(Index);
            CueIndexImpl? nextTrackImplIndex = ParentSheet.GetIndexImplOrDefault(indexOfNextTrack);
            // it's the last track of cuesheet
            bool isLastTrackInCue = nextTrackImplIndex is null;
            // it's the last track of its file
            bool isLastTrackInFile = nextTrackImplIndex?.File != ParentFile;
            if (isLastTrackInCue || isLastTrackInFile)
            {
                // Only way to get track's duration is to subtract its start from the file duration
                CueTime? fileDur = ParentFile.Meta?.CueDuration;
                if (fileDur.HasValue)
                {
                    return fileDur.Value - AudioStartIndex.Time;
                }

                // File meta is unknown / no file
                return null;
            }

            return nextTrackImplIndex!.Time - AudioStartIndex.Time;
        }
    }

    public RemarkCollection Remarks { get; } = [];
    public CommentCollection Comments { get; } = [];

    public override string ToString()
    {
        return "Track " + Number.ToString("D2") + ": " + Title;
    }


    //#region Rem
    //private readonly List<CueRemark> RawRems = [];
    //public ReadOnlyCollection<CueRemark> Remarks => RawRems.AsReadOnly();

    //public void ClearRemarks() => RawRems.Clear();

    //public void Remarks.Add(string type, string value) => Remarks.Add(new CueRemark(type, value));

    //public void Remarks.Add(CueRemark entry) => RawRems.Add(entry);

    //public void Remarks.Add(IEnumerable<CueRemark> entries)
    //{
    //    foreach (CueRemark remark in entries)
    //    {
    //        Remarks.Add(remark);
    //    }
    //}

    internal CueTrack ClonePartial(CueDataFile newOwner)
    {
        CueTrack newTrack =
            new(newOwner, Type)
            {
                CommonFieldsSet = CommonFieldsSet,
                Composer = Composer,
                Flags = Flags,
                Title = Title,
                PostGap = PostGap,
                PreGap = PreGap,
                Performer = Performer,
                ISRC = ISRC,
                HasZerothIndex = HasZerothIndex,
                Index = Index,
                Offset = Offset,
                Orphaned = Orphaned,
            };
        newTrack.Remarks.Add(Remarks);
        newTrack.Comments.Add(Comments);
        return newTrack;
    }

    public bool Equals(CueTrack? other) => Equals(other, StringComparison.InvariantCulture);

    public bool Equals(CueTrack? other, StringComparison stringComparison)
    {
        if (ReferenceEquals(this, other))
            return true;
        if (other == null)
            return false;
        if (Comments.Count != other.Comments.Count)
            return false;
        if (Remarks.Count != other.Remarks.Count)
            return false;
        if (
            PostGap != other.PostGap
            || PreGap != other.PreGap
            || !string.Equals(Performer, other.Performer, stringComparison)
            || !string.Equals(ISRC, other.ISRC, stringComparison)
            || !string.Equals(Composer, other.Composer, stringComparison)
            || !string.Equals(Title, other.Title, stringComparison)
            || Flags != other.Flags)
        {
            return false;
        }

        bool commentsEqual = Comments.SequenceEqual(other.Comments,StringHelper.GetComparer(stringComparison));
        bool remarksEqual = Remarks.SequenceEqual(other.Remarks,StringHelper.GetComparer(stringComparison));
        if(!commentsEqual || !remarksEqual)
        { return false; }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CueTrack, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Title.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Performer?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            ISRC?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            Composer?.GetHashCode(StringComparison.OrdinalIgnoreCase),
            PostGap.GetHashCode(),
            Comments.Count.GetHashCode(),
            Remarks.Count.GetHashCode()
        );
    }
}
