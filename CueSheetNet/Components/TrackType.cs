namespace CueSheetNet;

public record TrackType
{
    public string Identifier { get; }
    public bool Audio { get; }
    public string Description { get; }
    private TrackType(string identifier, bool audio, string description)
    {
        Identifier = identifier;
        Audio = audio;
        Description = description;
    }
    internal static TrackType FromString(string s)
    {
        return s switch
        {
            "AUDIO" => TrackType.AUDIO,
            "CDG" => TrackType.CDG,
            "MODE1_RAW" => TrackType.MODE1_RAW,
            "MODE1/2048" => TrackType.MODE1_2048,
            "MODE1/2352" => TrackType.MODE1_2352,
            "MODE2_RAW" => TrackType.MODE2_RAW,
            "MODE2/2048" => TrackType.MODE2_2048,
            "MODE2/2324" => TrackType.MODE2_2324,
            "MODE2/2336" => TrackType.MODE2_2336,
            "MODE2/2352" => TrackType.MODE2_2352,
            "CDI/2336" => TrackType.CDI_2336,
            "CDI/2352" => TrackType.CDI_2352,
            _ => new TrackType(s, false, "Unknown")
        };
    }
    public static readonly TrackType AUDIO = new TrackType("AUDIO", true, "Audio (sector size: 2352)");
    public static readonly TrackType CDG = new TrackType("CDG", false, "Karaoke CD+G (sector size: 2448)");
    public static readonly TrackType MODE1_RAW = new TrackType("MODE1_RAW", false, "CD-ROM Mode 1 data (raw) (sector size: 2352), used by cdrdao");
    public static readonly TrackType MODE1_2048 = new TrackType("MODE1/2048", false, "CD-ROM Mode 1 data (cooked) (sector size: 2048)");
    public static readonly TrackType MODE1_2352 = new TrackType("MODE1/2352", false, "CD-ROM Mode 1 data (raw) (sector size: 2352)");
    public static readonly TrackType MODE2_RAW = new TrackType("MODE2_RAW", false, "CD-ROM Mode 2 data (raw) (sector size: 2352), used by cdrdao");
    public static readonly TrackType MODE2_2048 = new TrackType("MODE2/2048", false, "CD-ROM Mode 2 XA form-1 data (sector size: 2048)");
    public static readonly TrackType MODE2_2324 = new TrackType("MODE2/2324", false, "CD-ROM Mode 2 XA form-2 data (sector size: 2324)");
    public static readonly TrackType MODE2_2336 = new TrackType("MODE2/2336", false, "CD-ROM Mode 2 data (sector size: 2336)");
    public static readonly TrackType MODE2_2352 = new TrackType("MODE2/2352", false, "CD-ROM Mode 2 data (raw) (sector size: 2352)");
    public static readonly TrackType CDI_2336 = new TrackType("CDI/2336", false, "CDI Mode 2 data");
    public static readonly TrackType CDI_2352 = new TrackType("CDI/2352", false, "CDI Mode 2 data");
    public override string ToString() => Identifier;
}

