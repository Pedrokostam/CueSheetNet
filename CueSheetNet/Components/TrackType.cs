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
    public Modes Mode { get; }
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
            _ => new TrackType(s, -1, Modes.None, audio: false, cdspec: false, "Unknown")
        };
    }
    public static readonly TrackType AUDIO =
        new(identifier: "AUDIO",
            sectorSize: CdSectorSize,
            mode: Modes.NonCd,
            audio: true,
            cdspec: false,
            description: "Audio (sector size: 2352)");

    public static readonly TrackType CDG =
        new(identifier: "CDG",
            sectorSize: CdSectorSize + 96,
            mode: Modes.CDG,
            audio: false,
            cdspec: true,
            description: "Karaoke CD+G (sector size: 2448)");


    public static readonly TrackType MODE1_2048 =
        new(identifier: "MODE1/2048",
            sectorSize: 2048,
            mode: Modes.Mode1,
            audio: false,
            cdspec: true,
            description: "CD-ROM Mode 1 data (cooked) (sector size: 2048)",
            "MODE1_RAW", "ISO/2048"
            );

    public static readonly TrackType MODE1_2352 =
        new(identifier: "MODE1/2352",
            sectorSize: CdSectorSize,
            mode: Modes.Mode1,
            audio: false,
            cdspec: true,
            description: "CD-ROM Mode 1 data (raw) (sector size: 2352)");


    public static readonly TrackType MODE2_2048 =
        new(identifier: "MODE2/2048",
            sectorSize: 2048,
            mode: Modes.Mode2,
            audio: false,
            cdspec: true,
            description: "CD-ROM Mode 2 XA form-1 data (sector size: 2048)");

    public static readonly TrackType MODE2_2324 =
        new(identifier: "MODE2/2324",
            sectorSize: 2324,
            mode: Modes.Mode2,
            audio: false,
            cdspec: true,
            description: "CD-ROM Mode 2 XA form-2 data (sector size: 2324)");

    public static readonly TrackType MODE2_2336 =
        new(identifier: "MODE2/2336",
            sectorSize: 2336,
            mode: Modes.Mode2,
            audio: false,
            cdspec: true,
            description: "CD-ROM Mode 2 data (sector size: 2336)");

    public static readonly TrackType MODE2_2352 =
        new(identifier: "MODE2/2352",
            sectorSize: CdSectorSize,
            mode: Modes.Mode2,
            audio: false,
            cdspec: true,
            description: "CD-ROM Mode 2 data (raw) (sector size: 2352)",
            alternates: "MODE2_RAW");

    public static readonly TrackType CDI_2336 =
        new(identifier: "CDI/2336",
            sectorSize: 2336,
            mode: Modes.CDI,
            audio: false,
            cdspec: true,
            description: "CDI Mode 2 data");

    public static readonly TrackType CDI_2352 =
        new(identifier: "CDI/2352",
            sectorSize: CdSectorSize,
            mode: Modes.CDI,
            audio: false,
            cdspec: true,
            description: "CDI Mode 2 data");
    public override string ToString() => Identifier;
}

