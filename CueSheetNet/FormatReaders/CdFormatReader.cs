using CueSheetNet.Logging;

namespace CueSheetNet.FileReaders;

internal class CdFormatReader : IBinaryStreamFormatReader
{
    public string FormatName { get; } = "BINARY";
    public string[] Extensions { get; } = [".BIN", ".MM2", ".ISO", ".MOT", ".IMG"];

    private static readonly byte[] Header = [0x0, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00]; // 0
    public bool ExtensionMatches(string fileName)
    {
        string ext = Path.GetExtension(fileName);
        return Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }
    public bool FileSignatureMatches(Stream stream, int sectorSize, TrackType.Modes mode)
    {
        if (sectorSize != TrackType.CdSectorSize)
        {
            // Can't check SYNC header, since it's not included in Modes without full sector size
            return true;
        }
        stream.Seek(0, SeekOrigin.Begin);
        Span<byte> twelve = stackalloc byte[12];
        int read =stream.Read(twelve);
        if (read == twelve.Length && !twelve.SequenceEqual(Header))
            return false;
        stream.Seek(4, SeekOrigin.Current);
        int modeByte = stream.ReadByte();
        bool byteGood = mode switch
        {
            TrackType.Modes.Mode0 => modeByte == 0,
            TrackType.Modes.Mode1 => modeByte == 1,
            TrackType.Modes.Mode2 => modeByte == 2,
            _ => false
        };
        return byteGood;
    }


    public bool ReadMetadata(Stream stream, IEnumerable<TrackType> trackTypes, out FileMetadata metadata)
    {
        if (trackTypes.FirstOrDefault() is not TrackType trackType)
            throw new ArgumentException("No track type specified", nameof(trackTypes));
        var t = trackTypes.Select(x => x.SectorSize).Distinct().Count();
        if (t > 1)
            throw new InvalidDataFormatException("Differing sector sizes specified");

        int size = trackType.SectorSize;
        if (!FileSignatureMatches(stream, size, trackType.Mode))
        {
            Logger.LogWarning("Data stream has mismatched Header");
            metadata = default;
            return false;
        }
        long numberOfSectors = Math.DivRem(stream.Length, size, out long Remainder);
        if (Remainder > 0)
            throw new InvalidDataFormatException("Length of data is not a multiple of specified sector size");
        // each sector corresponds to 1 cue frame, so 75 of them is 1 second
        TimeSpan duration = TimeSpan.FromSeconds(numberOfSectors / 75.0);
        bool hasAudio = trackTypes.Where(x => x.ContainsAudioData).Any();
        metadata = new FileMetadata(
            duration,
            true,
            hasAudio ? 44100 : -1,
            hasAudio ? 2 : -1,
            hasAudio ? 16 : -1,
            $"Binary data ({trackType.Identifier})"
            );
        return true;
    }
    public bool ReadMetadata(string path, IEnumerable<TrackType> trackTypes, out FileMetadata metadata)
    {
        using Stream stream = File.OpenRead(path);
        return ReadMetadata(stream, trackTypes, out metadata);
    }
}
