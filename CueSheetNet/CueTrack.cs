namespace CueSheetNet;

public class CueTrack : CueItemBase
{
    /// <summary>
    /// Absolute index for the whole CueSheet
    /// </summary>
    public int Index { get; internal set; }
    public int Offset { get; internal set; }
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
    public CueIndex[] Indices => ParentSheet.GetIndicesOfTrack(Index);
    public CueTrack(CueFile parentFile) : base(parentFile.ParentSheet)
    {
        _ParentFile = parentFile;
    }
    public override string ToString()
    {
        return "Track " + Number.ToString("D2") + ": " + Title;
    }
}

