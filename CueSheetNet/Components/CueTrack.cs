using CueSheetNet.Internal;
using CueSheetNet.Syntax;
using System.Collections.ObjectModel;

namespace CueSheetNet;
public class CueTrack : CueItemBase, IEquatable<CueTrack>, IRemCommentable
{
    public FieldsSet CommonFieldsSet { get; private set; }
    /// <summary>
    /// Absolute index for the whole CueSheet
    /// </summary>
    public int Index { get; internal set; }
    public int Offset { get; internal set; }
    public TrackType Type { get; internal set; }
    public CueTime PostGap { get; set; }
    public CueTime PreGap { get; set; }
    private CueDataFile _ParentFile;
    /// <summary>
    /// File in which Index 01 (or 00 if there is not 01) of Track appeared
    /// </summary>
    public CueDataFile ParentFile
    {
        get
        {
            CheckOrphaned();
            return _ParentFile;
        }
        set
        {
            _ParentFile = value;
        }
    }
    /// <summary>
    /// Number of track (does not have to be con
    /// </summary>
    public int Number => Index + Offset;
    private string? _Title;
    public string Title
    {
        get => _Title ?? Path.ChangeExtension(ParentFile.SourceFile.Name, null);
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

    public CueTrack(CueDataFile parentFile, TrackType type) : base(parentFile.ParentSheet)
    {
        _ParentFile = parentFile;
        Type = type;
    }
    public override string ToString()
    {
        return "Track " + Number.ToString("D2") + ": " + Title;
    }
    #region Rem
    private readonly List<CueRemark> RawRems = [];
    public ReadOnlyCollection<CueRemark> Remarks => RawRems.AsReadOnly();
    public void ClearRemarks() => RawRems.Clear();

    public void AddRemark(string type, string value) => AddRemark(new CueRemark(type, value));
    public void AddRemark(CueRemark entry) => RawRems.Add(entry);
    public void AddRemark(IEnumerable<CueRemark> entries)
    {
        foreach (CueRemark remark in entries)
        {
            AddRemark(remark);
        }
    }

    public void RemoveRemark(int index)
    {
        if (index >= 0 || index < RawRems.Count)
            RawRems.RemoveAt(index);
    }

    internal CueTrack ClonePartial(CueDataFile newOwner)
    {
        CueTrack newTrack = new(newOwner, Type)
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
        newTrack.AddRemark(RawRems.Select(x => x with { }));
        newTrack.AddComment(RawComments);
        return newTrack;
    }
    public void RemoveRemark(string field, string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase) => RemoveRemark(new CueRemark(field, value), comparisonType);
    public void RemoveRemark(CueRemark entry, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        int ind = RawRems.FindIndex(x => x.Equals(entry, comparisonType));
        if (ind >= 0)
            RawRems.RemoveAt(ind);
    }
    #endregion
    #region Comments
    private readonly List<string> RawComments = [];
    public ReadOnlyCollection<string> Comments => RawComments.AsReadOnly();
    public void AddComment(IEnumerable<string> comments)
    {
        foreach (string comment in comments)
        {
            AddComment(comment);
        }
    }
    public void AddComment(string comment) => RawComments.Add(comment);
    public void RemoveComment(string comment, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        int ind = RawComments.FindIndex(x => x.Equals(comment, comparisonType));
        if (ind >= 0)
            RawComments.RemoveAt(ind);
    }
    public void RemoveComment(int index)
    {
        if (index >= 0 && index < RawComments.Count)
            RawComments.RemoveAt(index);
    }
    public void ClearComments() => RawComments.Clear();

    #endregion
    public bool Equals(CueTrack? other) => Equals(other, StringComparison.InvariantCulture);
    public bool Equals(CueTrack? other, StringComparison stringComparison)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other == null) return false;
        if (RawComments.Count != other.RawComments.Count) return false;
        if (RawRems.Count != other.RawRems.Count) return false;
        if (
               PostGap != other.PostGap
            || PreGap != other.PreGap
            || !string.Equals(Performer, other.Performer, stringComparison)
            || !string.Equals(ISRC, other.ISRC, stringComparison)
            || !string.Equals(Composer, other.Composer, stringComparison)
            || !string.Equals(Title, other.Title, stringComparison) ||
            Flags != other.Flags
           )
            return false;
        for (int i = 0; i < RawComments.Count; i++)
        {
            if (!string.Equals(RawComments[i], other.RawComments[i], stringComparison))
                return false;
        }
        for (int i = 0; i < RawRems.Count; i++)
        {
            if (!RawRems[i].Equals(other.RawRems[i], stringComparison))
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CueTrack, StringComparison.InvariantCulture);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Title.GetHashCode(StringComparison.OrdinalIgnoreCase),
                                Performer?.GetHashCode(StringComparison.OrdinalIgnoreCase),
                                ISRC?.GetHashCode(StringComparison.OrdinalIgnoreCase),
                                Composer?.GetHashCode(StringComparison.OrdinalIgnoreCase),
                                PostGap.GetHashCode(),
                                RawComments.Count.GetHashCode(),
                                RawRems.Count.GetHashCode());
    }
}

