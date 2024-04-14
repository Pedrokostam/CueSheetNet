namespace CueSheetNet.FormatReaders;

internal sealed class WaveFormatReader : IAudioFileFormatReader
{
    private static readonly byte[] RIFF = "RIFF"u8.ToArray(); //  0x52 0x49 0x46 0x46
    private static readonly byte[] WAVE = "WAVE"u8.ToArray(); //  0x57 0x41 0x56 0x45
    public string FormatName { get; } = "Wave";
    public string[] Extensions { get; } = [".WAV", ".WAVE"];

    public bool ExtensionMatches(string fileName)
    {
        string ext = Path.GetExtension(fileName);
        return Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    public bool FileSignatureMatches(Stream stream)
    {
        /*
        range_B     range_b     endian	type	expected
        [00..04]	[000..032]  big		str		"RIFF"
        [04..08]	[032..064]  little  int		filesize-8
        [08..12]	[064..096]  big 	str		"WAVE"
         */
        stream.Seek(0, SeekOrigin.Begin);
        Span<byte> four = stackalloc byte[4];
        _ = stream.Read(four);
        if (!four.SequenceEqual(RIFF))
            return false;
        stream.Seek(4, SeekOrigin.Current);
        _ = stream.Read(four);
        return four.SequenceEqual(WAVE);
    }

    /*
       range_B  range_b     endian	type	expected
       [00..04]	[000..032]  big		str		"RIFF"
       [04..08]	[032..064]  little  int		filesize-8
       [08..12]	[064..096]  big 	str		"WAVE"
       [12..16]	[096..128]  big 	str		"fmt "
       [16..20]	[128..160]  little	int32	format chunk size from this point (PCM -> 16)
       [20..22]	[160..176]  little	int16	format (PCM -> 1)
       [22..24]	[176..192]  little	int16	num_channels
       [24..28]	[192..224]  little	int32	sample_rate
       [34..36]	[272..288]  little	int16	bits_per_sample
       [36..40]	[288..320]  big		str		"data"
       [40..44]	[320..336]  little	int		data_chunk_size
    */
    public bool ReadMetadata(Stream stream, out FileMetadata metadata)
    {
        metadata = default;
        if (stream.Length < 44 || !FileSignatureMatches(stream))
            return false;

        stream.Seek(4, SeekOrigin.Begin); // Positioned at filesize
        using BinaryReader binaryReader = new(stream, Encoding.Default, leaveOpen: true);
        uint fileSize = binaryReader.ReadUInt32() + 8;

        if (fileSize != stream.Length)
            throw new InvalidDataFormatException(
                $"Specified file length in WAVE file ({fileSize}) does not equal actual length ({stream.Length})"
            );

        binaryReader.BaseStream.Seek(16, SeekOrigin.Begin);
        uint chunkSize = binaryReader.ReadUInt32();
        ushort format = binaryReader.ReadUInt16();

        if (format != 1)
            throw new InvalidDataFormatException(
                $"Only PCM WAVE files with are supported (format {format})"
            );
        if (chunkSize != 16)
            throw new InvalidDataFormatException(
                $"Only PCM WAVE files with format chunk size of 16 are supported (chunk size: {chunkSize})"
            );

        ushort numChannels = binaryReader.ReadUInt16();
        uint sampleRate = binaryReader.ReadUInt32();
        uint byteRate = binaryReader.ReadUInt32();
        ushort blockAlign = binaryReader.ReadUInt16(); // bytes per sample (all channels)
        ushort bitsPerSample = binaryReader.ReadUInt16();
        binaryReader.BaseStream.Seek(4, SeekOrigin.Current);
        uint dataChunkSize = binaryReader.ReadUInt32();

        uint calculatedBlockAlign = (uint)numChannels * bitsPerSample / 8;
        var numberSamples = 8D * dataChunkSize / (numChannels * bitsPerSample);
        double durationSec = numberSamples / sampleRate;
        uint calculatedByteRate = sampleRate * calculatedBlockAlign;

        if (blockAlign != calculatedBlockAlign)
            throw new InvalidDataFormatException(
                $"Written BlockAlign rate does not match calculated one ({blockAlign} vs {calculatedBlockAlign})"
            );
        if (byteRate != calculatedByteRate)
            throw new InvalidDataFormatException(
                $"Written ByteRate does not match calculated one ({byteRate} vs {calculatedByteRate})"
            );

        metadata = new()
        {
            Duration = TimeSpan.FromSeconds(durationSec),
            SampleRate = (int)sampleRate,
            Channels = numChannels,
            BitDepth = bitsPerSample,
            FormatName = FormatName,
        };
        return true;
    }

    public bool ReadMetadata(string path, out FileMetadata metadata)
    {
        using FileStream stream = File.OpenRead(path);
        return ReadMetadata(stream, out metadata);
    }
}
