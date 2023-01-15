using CueSheetNet.Syntax;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace CueSheetNet;

public class CueTrack : CueItemBase, IEquatable<CueTrack>, IRemarkableCommentable
{
    public FieldSetFlags CommonFieldsSet { get; private set; }
    /// <summary>
    /// Absolute index for the whole CueSheet
    /// </summary>
    public int Index { get; internal set; }
    public int Offset { get; internal set; }
    public CueTime PostGap { get; set; }
    public CueTime PreGap { get; set; }
    private CueFile _ParentFile;
    public CueFile ParentFile
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
        get => _Title ?? Path.ChangeExtension(ParentFile.FileInfo.Name, null);
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _Title = null;
                CommonFieldsSet &= ~FieldSetFlags.Title;
            }
            else
            {
                _Title = value;
                CommonFieldsSet |= FieldSetFlags.Title;
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
                CommonFieldsSet &= ~FieldSetFlags.Performer;
            }
            else
            {
                _Performer = value;
                CommonFieldsSet |= FieldSetFlags.Performer;
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
                CommonFieldsSet &= ~FieldSetFlags.Composer;
            }
            else
            {
                _Composer = value;
                CommonFieldsSet |= FieldSetFlags.Composer;
            }
        }
    }
    public TrackFlags Flags { get; set; } = TrackFlags.None;
    public string? ISRC { get; set; }
    public bool HasZerothIndex { get; internal set; }
    public CueIndex[] Indexes => ParentSheet.GetIndexesOfTrack(Index);


    public CueTrack(CueFile parentFile) : base(parentFile.ParentSheet)
    {
        _ParentFile = parentFile;
    }
    public override string ToString()
    {
        return "Track " + Number.ToString("D2") + ": " + Title;
    }
    #region Rem
    public readonly List<Remark> RawRems = new();
    public ReadOnlyCollection<Remark> Remarks => RawRems.AsReadOnly();
    public void ClearRemarks() => RawRems.Clear();

    public void AddRemark(string type, string value) => AddRemark(new Remark(type, value));
    public void AddRemark(Remark entry) => RawRems.Add(entry);
    public void AddRemark(IEnumerable<Remark> entries)
    {
        foreach (Remark remark in entries)
        {
            AddRemark(remark);
        }
    }

    public void RemoveRemark(int index)
    {
        if (index >= 0 || index < RawRems.Count)
            RawRems.RemoveAt(index);
    }

    internal CueTrack ClonePartial(CueFile newOwner)
    {
        CueTrack newTrack = new(newOwner)
        {
            CommonFieldsSet = CommonFieldsSet,
            Composer = Composer,
            Flags = Flags,
            Title = Title,
            PostGap = PostGap,
            PreGap = PreGap,
            Performer=Performer,
            ISRC=ISRC,
            HasZerothIndex=HasZerothIndex,
            Index=Index,
            Offset=Offset,
            Orphaned=Orphaned,
        };
        newTrack.AddRemark(RawRems.Select(x=>x with { }));
        newTrack.AddComment(RawComments);
        return newTrack;
    }
    public void RemoveRemark(string field, string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase) => RemoveRemark(new Remark(field, value), comparisonType);
    public void RemoveRemark(Remark entry, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        int ind = RawRems.FindIndex(x => x.Equals(entry, comparisonType));
        if (ind >= 0)
            RawRems.RemoveAt(ind);
    }
    #endregion
    #region Comments
    public readonly List<string> RawComments = new();
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
    public bool Equals(CueTrack? other) => Equals(other, StringComparison.CurrentCulture);
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
        return Equals(obj as CueTrack);
    }
}

