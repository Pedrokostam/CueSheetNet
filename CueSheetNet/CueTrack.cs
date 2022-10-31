namespace CueSheetNet;

public class CueTrack : CueItemBase,IEquatable<CueTrack>, IRemCommentable
{
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
        get
        {
            if (_Title != null)
                return _Title;
            return Path.ChangeExtension(ParentFile.File.Name, null);
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _Title = null;
            }
            else
            {
                _Title = value;
            }
        }
    }
    private string? _Performer;
    public string? Performer
    {
        get => _Performer ?? ParentSheet.Performer;
        set => _Performer = value;
    }
    private string? _Composer;
    public string? Composer
    {
        get => _Composer ?? ParentSheet.Composer;
        set => _Composer = value;
    }
    public CueTrackFlags Flags { get; set; } = CueTrackFlags.None;
    public string? ISRC { get; set; }
    public string? Comment { get; set; }
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
    public readonly List<RemEntry> RawRems = new();
    public void ClearRems() => RawRems.Clear();

    public void AddRem(string type, string value) => AddRem(new RemEntry(type, value));
    public void AddRem(RemEntry entry) => RawRems.Add(entry);

    public void RemoveRem(int index)
    {
        if (index >= 0 || index < RawRems.Count)
            RawRems.RemoveAt(index);
    }

    public void RemoveRem(string field, string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase) => RemoveRem(new RemEntry(field, value), comparisonType);
    public void RemoveRem(RemEntry entry, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        int ind = RawRems.FindIndex(x => x.Equals(entry, comparisonType));
        if (ind >= 0)
            RawRems.RemoveAt(ind);
    }
    #endregion
    #region Comments
    public readonly List<string> RawComments = new();
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
    public bool Equals(CueTrack? other)
    {
        throw new NotImplementedException();
        if (ReferenceEquals(this, other)) return true;
        if(other == null) return false;


    }
}

