namespace CueSheetNet.FileReaders;
internal sealed class FlacFormatReader : IAudioBinaryStreamFormatReader
{
    public string FormatName { get; } = "Flac";
    public string[] Extensions { get; } = new string[] { ".flac" };
    private static readonly byte[] fLaC = "fLaC"u8.ToArray();// 0x66 0x4c 0x61 0x43
    public bool ExtensionMatches(string fileName)
    {
        string ext = Path.GetExtension(fileName);
        return Extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0060:The value returned by Stream.Read/Stream.ReadAsync is not used", Justification = "Length of stream is ensured in calling method")]
    public bool FileSignatureMatches(Stream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        Span<byte> four = stackalloc byte[4];
        stream.Read(four);
        return four.SequenceEqual(fLaC);
    }
    public bool ReadMetadata(Stream stream, out FileMetadata metadata)
    {
        metadata = default;
        if(stream.Length < 22)
        {
            return false;
        }
        if (!FileSignatureMatches(stream))
            return false;
        /*
               start_B     range_b    len  endian	type	expected
              [0     ]	[000..032]   12  big 	str		"fLaC"
              [4     ]	[032..040]   18  big 	int		header type == 0
              [5     ]	[040..064]   24  big 	int		format chunk length
              [8     ]	[064..080]   16  big 	int		Min block size
              [10    ]	[080..096]   16  big 	int		Max block size
              [12    ]	[096..120]   26  big 	int		Min frame size
              [15    ]	[120..144]   26  big 	int		Max frame size
            | [18    ]	[144..164]   20  big 	int		sample rate
            | [20.5  ]	[164..167]    3  big 	int		number of channels
            | [20.875]	[167..172]    5  big 	int		bits per sample
            | [21.5  ]	[172..208]   36  big 	str		total samples
              [26    ]	[208..336]   10  big 	int		md5
        */
        stream.Seek(4, SeekOrigin.Begin);
        if (stream.ReadByte() != 0)
            throw new InvalidDataFormatException("Invalid FLAC metadata block header");
        Span<byte> bytes = stackalloc byte[8];
        stream.Seek(18, SeekOrigin.Begin);
        if (stream.Read(bytes) != 8)
            throw new InvalidDataFormatException("FLAC file cut short");
        // SHIFT OPERATORS ARE DONE AFTER ADDING!!!
        // ________ ____0000 00001111 11112222
        int samples = (bytes[0] << 12)//12 insted of 16, because it spans only 2.5 bytes (20 bits)
                      + (bytes[1] << 4)
                      + (bytes[2] >> 4);
        // ________ ________ ____3333 44444444
        int numChannels = (bytes[2] & 0b00001110);
        int bitsPerSample = ((bytes[2] & 0b00000001) << 4) + (bytes[3] >> 4) + 1;
        int totalSample_24 = ((bytes[3] & 0x0F) << 16) + bytes[4];
        // ________ 55555555 66666666 77777777
        int totalSample_0 = (bytes[5] << 16)
                           + (bytes[6] << 8)
                           + bytes[7];
        // ________ ____3333 44444444 55555555 66666666 77777777
        var totalSamples = (ulong)totalSample_0 + ((ulong)totalSample_24 << 24);
        metadata = new()
        {
            Duration = TimeSpan.FromSeconds((double)totalSamples / samples),
            SampleRate = samples,
            Channels = numChannels,
            BitDepth = bitsPerSample,
            FormatName = FormatName
        };
        return true;
    }

    public bool ReadMetadata(string path, out FileMetadata metadata)
    {
        using FileStream stream = File.OpenRead(path);
        return ReadMetadata(stream, out metadata);
    }
}
