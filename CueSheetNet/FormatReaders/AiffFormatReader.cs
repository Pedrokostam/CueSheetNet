using CueSheetNet.FormatReaders;
using CueSheetNet.Logging;
using static System.Buffers.Binary.BinaryPrimitives;
namespace CueSheetNet.FormatReaders;
public sealed class AiffFormatReader : IAudioFileFormatReader
{
    private static readonly string[] extensions = [".AIFF", ".AIF", ".AIFC."];
    private static readonly string formatName = "Aiff";
    private static readonly byte[] FORM = "FORM"u8.ToArray();
    private static readonly byte[] COMM = "COMM"u8.ToArray();
    private static readonly byte[] AIFF = "AIFF"u8.ToArray();
    private static readonly byte[] AIFC = "AIFC"u8.ToArray();
    public string[] Extensions => extensions;
    public string FormatName => formatName;

    public bool ExtensionMatches(string fileName)
    {
        string ext = Path.GetExtension(fileName);
        return extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }

    public bool FileSignatureMatches(Stream stream)
    {
        /*
        range_B     range_b     endian	type	expected
        [00..04]	[000..032]  big		str		"FORM"
        [04..08]	[032..064]  big     int32	filesize-8
        [08..12]	[064..096]  big 	str		"AIFF" or "AIFC"
         */
        stream.Seek(0, SeekOrigin.Begin);
        Span<byte> four = stackalloc byte[4];
        _ = stream.Read(four);
        if (!four.SequenceEqual(FORM))
            return false;
        stream.Seek(4, SeekOrigin.Current); // skip bytes with filesize
        _ = stream.Read(four);
        return four.SequenceEqual(AIFF) || four.SequenceEqual(AIFC);
    }

    //[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "MA0060:The value returned by Stream.Read/Stream.ReadAsync is not used", Justification = "Length of stream is ensured at the start of method")]
    public bool ReadMetadata(Stream stream, out FileMetadata metadata)
    {
        /*
         * 
         *  range_B range_b     endian	type	expected
           [00..04] [000..032]  big		str		"FORM"
           [04..08] [032..064]  big     int32	filesize-8
           [08..12] [064..096]  big 	str		"AIFF" or "AIFC"
           [12..16] [096..128]  big 	str		"COMM" - Required Common chunk starts here
           [16..20] [128..160]  big	    int32	format chunk size from this point (18 for common chunk)
           [20..22] [160..176]  big   	int16	num_channels
           [22..26] [176..208]  big   	uint32	num_sampleFrames => how many mono/stereo samples are there
           [26..28] [208..224]  big   	int16	bits_per_sample
           [28..38] [224..304]  big   	ext		sampleRate
        */
        metadata = default;
        if(stream.Length < 38)
        {
            return false;
        }
        if (!FileSignatureMatches(stream))
            return false;
        stream.Seek(4, SeekOrigin.Begin); // Positioned at filesize
        Span<byte> four = stackalloc byte[4];
        Span<byte> two = stackalloc byte[2];
        Span<byte> ten = stackalloc byte[10];
        _ = stream.Read(four);

        Int32 size = ReadInt32BigEndian(four);
        if (size + 8 != stream.Length)
        {
            Logger.LogWarning("Mismatched declared file length vs actual");
        }
        _ = stream.Seek(20, SeekOrigin.Begin); // at num_channels
        _ = stream.Read(two);
        Int16 numChannels = ReadInt16BigEndian(two);
        _ = stream.Read(four);
        UInt32 samples = ReadUInt32BigEndian(four);
        _ = stream.Read(two);
        Int16 bitDepth = ReadInt16BigEndian(two);
        _ = stream.Read(ten);
        int sampleRate = (int)ReadAppleExtended80(ten);
        double seconds = samples / (double)sampleRate;
        metadata = new FileMetadata()
        {
            Duration = TimeSpan.FromSeconds(seconds),
            SampleRate = sampleRate,
            Channels = numChannels,
            BitDepth = bitDepth,
            FormatName = FormatName,
        };
        return true;
    }
    private static decimal ReadAppleExtended80(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 10) throw new ArgumentException("Expected 10 bytes for Apple Extended",nameof(bytes));

        Span<byte> workingBytes = stackalloc byte[bytes.Length];
        bytes.CopyTo(workingBytes);


        int signVal = workingBytes[0] & 0b10000000;
        int sign = signVal > 0 ? -1 : 1;
        workingBytes[0] &= 0b01111111;

        int integerBit = workingBytes[4] & 0b10000000;
        // zero the integer bit, so it does not matter when readfing uint64
        workingBytes[2] &= 0b01111111;

        int exponent = ReadUInt16BigEndian(workingBytes[..2]);
        UInt64 fractionRaw = ReadUInt64BigEndian(bytes);
        // to get the fraction, divide the read number by 2^63 (which is 1 higher than the maximum value fractionRaw could be)
        decimal fraction = fractionRaw / 9223372036854775808M;

        decimal x = (1 + fraction) * (decimal)Math.Pow(2, exponent - 16383);
        // sign * 2^(expo - 16383) * 1.fraction if not special (expo = 32767)
        // sign * 2^(expo - 16383) * 0.fraction if special
        return x;
    }
    public bool ReadMetadata(string path, out FileMetadata metadata)
    {
        using Stream stream = File.OpenRead(path);
        return ReadMetadata(stream, out metadata);
    }
}
