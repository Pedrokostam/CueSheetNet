namespace CueSheetNet;

public record TrackType
{
    public enum Modes
    {
        None,
        CDDA,
        CDG,
        Mode0,
        Mode1,
        Mode2,
        CDI,
        NonCd,
    }
    public const int CdSectorSize = 2352;
    public string Identifier { get; }
    public string[] AlternateIdentifiers { get; }
    public bool ContainsAudioData { get; }
    public bool CdSpecification { get; }
    public string Description { get; }
    public int SectorSize { get; }
    public Modes Mode{ get; }
    private TrackType(string identifier,
                      int sectorSize,
                      Modes mode,
                      bool audio,
                      bool cdspec,
                      string description,
                      params string[] alternates)
    {
        Identifier = identifier;
        ContainsAudioData = audio;
        CdSpecification = cdspec;
        Mode = mode;
        Description = description;
        AlternateIdentifiers = alternates;
        SectorSize = sectorSize;
    }
    internal static TrackType FromString(string s)
    {
        return s switch
        {
            "AUDIO" => TrackType.AUDIO,
            "CDG" => TrackType.CDG,
            "MODE1_RAW" => TrackType.MODE1_2352,
            "MODE1/2048" => TrackType.MODE1_2048,
            "MODE1/2352" => TrackType.MODE1_2352,
            "MODE2_RAW" => TrackType.MODE2_2352,
            "MODE2/2048" => TrackType.MODE2_2048,
            "MODE2/2324" => TrackType.MODE2_2324,
            "MODE2/2336" => TrackType.MODE2_2336,
            "MODE2/2352" => TrackType.MODE2_2352,
            "CDI/2336" => TrackType.CDI_2336,
            "CDI/2352" => TrackType.CDI_2352,
            _ => new TrackType(s, -1,Modes.None, false,false, "Unknown")
        };
    }
    public static readonly TrackType AUDIO = new TrackType("AUDIO",
                                                           CdSectorSize,
                                                           Modes.NonCd,
                                                           true,false,
                                                           "Audio (sector size: 2352)");

    public static readonly TrackType CDG = new TrackType("CDG", CdSectorSize + 96,Modes.CDG,
                                                         false,true,
                                                         "Karaoke CD+G (sector size: 2448)");

    //public static readonly TrackType MODE1_RAW = new TrackType("MODE1_RAW", false, "CD-ROM Mode 1 data (raw) (sector size: 2352), used by cdrdao");

    public static readonly TrackType MODE1_2048 = new TrackType("MODE1/2048", 2048,Modes.Mode1,
                                                                false, true,
                                                                "CD-ROM Mode 1 data (cooked) (sector size: 2048)",
                                                                "MODE1_RAW", "ISO/2048");

    public static readonly TrackType MODE1_2352 = new TrackType("MODE1/2352", CdSectorSize, Modes.Mode1,
                                                                false, true,
                                                                "CD-ROM Mode 1 data (raw) (sector size: 2352)");

    //public static readonly TrackType MODE2_RAW = new TrackType("MODE2_RAW", false, "CD-ROM Mode 2 data (raw) (sector size: 2352), used by cdrdao");

    public static readonly TrackType MODE2_2048 = new TrackType("MODE2/2048", 2048, Modes.Mode2,
                                                                false, true,
                                                                "CD-ROM Mode 2 XA form-1 data (sector size: 2048)");

    public static readonly TrackType MODE2_2324 = new TrackType("MODE2/2324", 2324, Modes.Mode2,
                                                                false, true,
                                                                "CD-ROM Mode 2 XA form-2 data (sector size: 2324)");

    public static readonly TrackType MODE2_2336 = new TrackType("MODE2/2336", 2336, Modes.Mode2,
                                                                false, true,
                                                                "CD-ROM Mode 2 data (sector size: 2336)");

    public static readonly TrackType MODE2_2352 = new TrackType("MODE2/2352", CdSectorSize, Modes.Mode2,
                                                                false, true,
                                                                "CD-ROM Mode 2 data (raw) (sector size: 2352)",
                                                                "MODE2_RAW");

    public static readonly TrackType CDI_2336 = new TrackType("CDI/2336", 2336, Modes.CDI,
                                                              false, true,
                                                              "CDI Mode 2 data");

    public static readonly TrackType CDI_2352 = new TrackType("CDI/2352", CdSectorSize, Modes.CDI,
                                                              false, true,
                                                              "CDI Mode 2 data");
    public override string ToString() => Identifier;
}

