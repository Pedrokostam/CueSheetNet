using CueSheetNet.Logging;
using CueSheetNet.Reading;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.TextParser;
internal class CueEncodingTester
{
    /// <summary>
    /// Compares bytes in a case-insensitive way. Case is changed by changing the sixth bit. Works for standard ASCII letters. 
    /// </summary>
    class ByteInvariantComparer : EqualityComparer<byte>
    {
        private static int ChangeCase(byte b)
        {
            //XOR with 32, since this is the bit that differentiates upper and lowercase. At least for letters...
            return b ^ 0b00100000;
        }
        public override bool Equals(byte x, byte y)
        {
            //XOR with 32, since this is the bit that differentiates upper and lowercase. At least for letters...
            return x == y || x == ChangeCase(y);
        }
        /// <summary>
        /// Combines hashcode of the input byte and the input byte with its sixth bit negated (so both uppercase and lowercase)
        /// </summary>
        public override int GetHashCode([DisallowNull] byte obj)
        {
            return HashCode.Combine(obj,ChangeCase(obj));
        }
    }

    public CueSource Source { get; }
    public Stream Stream { get; }
    private static readonly Encoding EncodingUTF32BE = Encoding.GetEncoding("utf-32BE");
    /// <summary>
    /// Common encodings which are identified by a preamble
    /// </summary>
    static readonly Encoding[] PreambledEncodings = new Encoding[] {
        Encoding.Unicode,
        Encoding.UTF8,
        Encoding.BigEndianUnicode,
        Encoding.UTF32,
        EncodingUTF32BE
    };

    /// <summary>
    /// Checks if the <paramref name="input"/> is larger or equal to <paramref name="minInclusive"/> and smaller or equal than <paramref name="maxInclusive"/>
    /// </summary>
    /// <param name="input">Value to check</param>
    /// <param name="minInclusive">Inclusive minimum</param>
    /// <param name="maxInclusive">Inclusive maximum</param>
    /// <returns></returns>
    private static bool CheckRange(byte input, byte minInclusive, byte maxInclusive)
    {
        return input >= minInclusive && input <= maxInclusive;
    }
    private static void AddUntilNewLine(Stream fs, List<byte> bytes)
    {
        int last = fs.ReadByte();
        do
        {
            // get either of newline signs; does not matter if we leave something - it'll be ignored later
            if (last == '\n' || last == '\r')
                break;
            bytes.Add((byte)last);
            last = fs.ReadByte();
        } while (last >= 0);
    }
    public static bool CompareBytes(ReadOnlySpan<byte> input, Encoding encoding)
    {
        ReadOnlySpan<byte> preamble = encoding.Preamble;
        if (input.Length < preamble.Length)
            return false;
        for (int i = 0; i < preamble.Length; i++)
        {
            if (input[i] != preamble[i])
                return false;
        }
        return true;
    }
    /// <summary>
    /// Checks bytes of file for various encodings, starting with preambled one (e.g. UTF16BE).
    /// Also supports encodings without BOM, both fixed size and variable size
    /// </summary>
    /// <param name="fs"></param>
    /// <returns>Detected encoding. If method could exactly tell the encoding the Default encoding for the system is returned</returns>
    public Encoding DetectCueEncoding()
    {
        Logger.LogDebug("Encoding detection started. Source: {Source}", Source);
        if (DetectEncodingFromBOM(Stream) is Encoding encodingBom)
            return encodingBom;
        if (DetectFixedWidthEncoding_Naive(Stream) is Encoding encodingFixed)
            return encodingFixed;
        // at this point the file is most likely ASCII, UTF8 or some local codepage
        // all of those should have cue keywords (standard English) written with 1-byte characters
        // now we find all the lines which begin with FILE, TITLE, PERFORMER, REM COMMENT, as those are most likely to contain diacritics
        List<byte> b = GetPotentialDiacritizedLines(Stream);
        return DetectUtf8Heuristically(b);
    }
    private Encoding DetectUtf8Heuristically(List<byte> bajtos)
    {
        // The list will not be modified so it is safe to access it as span
        Span<byte> s = CollectionsMarshal.AsSpan(bajtos);
        Logger.LogDebug("Heuristic encoding detection started. Source: {Source}", Source);
        int length = s.Length - 4;
        bool utf8 = false;
        for (int i = 0; i < length; i++)
        {
            // One byte
            // U+0000..U+007F      00..7F
            if (s[i] <= 0x7F)
            {
                continue;
                // not setting utf8 to true, because ASCII also meets this criterion, and if you can work in ASCII, why not?
            }
            // Two bytes
            // U+0080..U+07FF       C2..DF     80..BF
            // Range                C2..DF     80..BF
            // skipping C0, C1 - non-minimal
            else if (CheckRange(s[i], 0xC2, 0xDF)
              && CheckRange(s[i + 1], 0x80, 0xBF))
            {
                utf8 = true;
                i += 1; // skip next character
            }
            // Three bytes
            // U+0800..U+0FFF       E0         A0..BF      80..BF
            // U+1000..U+CFFF       E1..EC     80..BF      80..BF
            // U+D000..U+D7FF       ED         80..9F      80..BF
            // U+E000..U+FFFF       EE..EF     80..BF      80..BF
            // Range                E0..EF     80..BF      80..BF
            // skipping U+D800..U+DBFF - not valid UTF8
            // slightly simplified, as with first byte equal to E0 second has to be greater than A0, not 80...
            // but the third byte is consistent
            else if (CheckRange(s[i], 0xE0, 0xEF)
              && CheckRange(s[i + 1], 0x80, 0xBF)
              && CheckRange(s[i + 2], 0x80, 0xBF))
            {
                utf8 = true;
                i += 2;// skip next 2 characters
            }
            // Four bytes
            // U+10000..U+3FFFF     F0         90..BF      80..BF     80..BF
            // U+40000..U+FFFFF     F1..F3     80..BF      80..BF     80..BF
            // U+100000..U+10FFFF   F4         80..8F      80..BF     80..BF
            // Range                F0..F4     80..BF      80..BF     80..BF
            // slightly simplified, as with first byte equal to F0 second has to be greater than 90, not 80...
            // but the third and forth bytes are consistent
            else if (CheckRange(s[i], 0xF0, 0xF4)
              && CheckRange(s[i + 1], 0x80, 0xBF)
              && CheckRange(s[i + 2], 0x80, 0xBF)
              && CheckRange(s[i + 3], 0x80, 0xBF))
            {
                utf8 = true;
                i += 3; //skip next 3 characters
            }
            else
            {
                Logger.LogInformation("Non-UTF8 bytes detected. Last 4 bytes: 0x{Byte1:X2}, 0x{Byte2:X2}, 0x{Byte3:X2}, 0x{Byte4:X2}", s[i], s[i + 1], s[i + 2], s[i + 3]);
                // most propably something from 0x7F up - some regional codepage
                utf8 = false;
                break;
            }
        }
        if (utf8)
        {
            return new UTF8Encoding(false);
        }
        else
        {
            // Quite difficult if not impossible to guess which regional encoding is being used.
            // We're assuming it codepage 1252, as it is quite common for english text
            Encoding enc = Encoding.GetEncoding(1252);
            return enc;
        }
    }
    /// <summary>
    /// Scans stream looking for certain cue keywords. When found, takes everything after the keyword until newline. Repeats until end of stream.
    /// <para/>Keywords:
    /// <list>
    /// <item>REM COMMENT</item>
    /// <item>PERFORMER</item>
    /// <item>TITLE</item>
    /// <item>FILE</item>
    /// </list>
    /// Those are the only fields that can contain non-ASCII characters.
    /// </summary>
    /// <param name="fs"></param>
    /// <returns></returns>
    private List<byte> GetPotentialDiacritizedLines(Stream fs)
    {
        Logger.LogDebug("Keyword line selection started. Source: {Source}", Source);
        ByteInvariantComparer byteCaseComparer = new();
        fs.Seek(0, SeekOrigin.Begin);
        List<byte> bytes = new(512);
        // Get byte forms of common keywords, without the first character and with trailing space
        ReadOnlySpan<byte> RemCommentUppercase = "EM COMMENT "u8;
        ReadOnlySpan<byte> PerformerUppercase = "ERFORMER "u8;
        ReadOnlySpan<byte> TitleUppercase = "ITLE "u8;
        ReadOnlySpan<byte> FileUppercase = "ILE "u8;
        // The keywords are REM COMMENT, PERFORMER, TITLE, FILE. Those have higher chances of having values with non-English characters.
        // Additionaly, they all have different first character, making it easy to switch
        Span<byte> remCommentBuff = stackalloc byte[RemCommentUppercase.Length];
        Span<byte> performerBuff = stackalloc byte[PerformerUppercase.Length];
        Span<byte> titleBuff = stackalloc byte[TitleUppercase.Length];
        Span<byte> fileBuff = stackalloc byte[FileUppercase.Length];
        int intReading;
        while ((intReading = fs.ReadByte())>=0)// -1 means end of stream
        {
            byte readByte = (byte)intReading;
            if (byteCaseComparer.Equals(readByte, (byte)'R'))// last letter R or r =>? Rem Comment
            {
                fs.Read(remCommentBuff);
                if (remCommentBuff.SequenceEqual(RemCommentUppercase, byteCaseComparer))
                    AddUntilNewLine(fs, bytes);
            }
            else if (byteCaseComparer.Equals(readByte, (byte)'P'))// last letter P or p =>? Performer
            {
                fs.Read(performerBuff);
                if (performerBuff.SequenceEqual(PerformerUppercase, byteCaseComparer))
                    AddUntilNewLine(fs, bytes);
            }
            else if (byteCaseComparer.Equals(readByte, (byte)'F'))// last letter F or f =>? file
            {
                fs.Read(fileBuff);
                if (fileBuff.SequenceEqual(FileUppercase, byteCaseComparer))
                    AddUntilNewLine(fs, bytes);
            }
            else if (byteCaseComparer.Equals(readByte, (byte)'T'))// last letter T or t =>? title
            {
                fs.Read(titleBuff);
                if (titleBuff.SequenceEqual(TitleUppercase, byteCaseComparer))
                    AddUntilNewLine(fs, bytes);
            }
        }
        Logger.LogDebug("Found {Keyword count} bytes of keyword lines. Source: {Source}", bytes.Count, Source);
        return bytes;
    }
    public Encoding? DetectEncodingFromBOM(Stream fs)
    {
        Logger.LogDebug("Preamble encoding detection started. Source: {Source}", Source);
        Span<byte> bomArea = stackalloc byte[4];
        fs.Seek(0, SeekOrigin.Begin);
        fs.Read(bomArea);
        //test for encoding with BOM
        foreach (var encoding in PreambledEncodings)
        {
            if (CompareBytes(bomArea, encoding))
            {
                Logger.LogInformation("Encoding {Encoding.EncodingName} detected from preamble. Source: {Source}", encoding, Source);
                return encoding;
            }
        }
        return null;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fs"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException">When the data is too short, or has 4 nulls at the start</exception>
    public Encoding? DetectFixedWidthEncoding_Naive(Stream fs)
    {
        //16 bytes which will be enough to store 4 characters of largest encoding - UTF32
        Span<byte> bomArea = stackalloc byte[16];
        fs.Seek(0, SeekOrigin.Begin);
        long length = fs.Read(bomArea);
        if (length < 16)
            throw new InvalidDataException($"Input data has fewer than 16 bytes {Source}");
        /**/
        // There is a small possibility, that the text is a constant-width multi-byte encoding, but the BOM is missing
        // First letter of a cuesheet should be a standard ASCII letter (one byte of data and whatever padding)
        Encoding? naiveApproach = bomArea switch
        {
            [0, 0, 0, > 1/**/, 0, 0, 0, > 1/**/, 0, 0, 0, > 1/**/, 0, 0, 0, > 1/**/] => EncodingUTF32BE, // 3 nulls, followed by a non-zero bytes
            [> 1, 0, 0, 0/**/, > 1, 0, 0, 0/**/, > 1, 0, 0, 0/**/, > 1, 0, 0, 0/**/] => Encoding.UTF32,// non-zero bytes followed by 3 nulls
            [> 1, 0/**/, > 1, 0/**/, > 1, 0/**/, > 1, 0/**/, ..] => Encoding.Unicode, // non-zero bytes, null, ignore everything past 12 byte
            [0, > 1/**/, 0, > 1/**/, 0, > 1/**/, 0, > 1/**/, ..] => Encoding.BigEndianUnicode,// null, non-zero byte, ignore everything past 12 byte
            [0, 0, 0, 0, ..] => throw new InvalidDataException($"Four consecutive null bytes at the beginning of {Source}"),
            _ => null,
        };
        return naiveApproach;
    }
    [Obsolete("Method gives too much leeway for mixed encoding (UTF16 followed by UTF32, etc.). Also is complicated for its own good.")]
    public Encoding? DetectFixedWidthEncoding(Stream fs)
    {
        Logger.LogDebug("UTF16/32 encoding detection started. Source: {Source}", Source);
        // first 512 byte should contain many English keyword of cuesheet
        int length = 512;
        var sample = new byte[length];
        fs.Seek(0, SeekOrigin.Begin);
        fs.Read(sample);
        Span<int> counters = new int[]
        {
            0,// UTF 16 LE
            0,// UTF 16 BE
            0,// UTF 32 LE
            0,// UTF 32 BE
        };
        for (int i = 0; i < length; i += 4)
        {
            //if middle 2 bytes are zero, it points to one of utf32
            if (sample[i + 1] == 0 && sample[i + 2] == 0)
            {
                //zero at beginning, value at end - big endian
                if (sample[i] == 0 && sample[i + 3] > 0)
                    counters[3]++;
                //zero at end, value at beginning - big endian
                else if (sample[i] > 0 && sample[i + 3] == 0)
                    counters[2]++;
            }
            // non-zero middle, could be utf 16
            else
            {
                //zero at beginning, value at end - big endian
                if (sample[i] == 0 && sample[i + 1] > 0 && sample[i + 2] == 0 && sample[i + 3] > 0)
                    counters[1]++;
                //zero at end, value at beginning - big endian
                else if (sample[i] > 0 && sample[i + 1] == 0 && sample[i + 2] > 0 && sample[i + 3] == 0)
                    counters[0]++;
            }
        }
        int maxIndex = -1;
        int maximum = -1;
        for (int i = 0; i < 4; i++)
        {
            if (counters[i] > maximum)
            {
                maximum = counters[i];
                maxIndex = i;
            }
        }
        Logger.LogDebug("UTF16/32 encoding detection results: 16LE - {16LE Count}, 16BE - {16BE Count}, 32LE - {32LE Count}, 32BE - {32BE Count}. Source: {Source}", counters[0], counters[1], counters[2], counters[3], Source); ;
        //if no encoding had more than 33 % hit rate - return null
        if (maximum < length / 3)
        {
            return null;
        }
        return maxIndex switch
        {
            0 => Encoding.Unicode,
            1 => Encoding.BigEndianUnicode,
            2 => Encoding.UTF32,
            3 => Encoding.GetEncoding("utf-32BE"),
            _ => null
        };

    }
    static CueEncodingTester()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    }
    public CueEncodingTester(Stream stream, CueSource source)
    {
        Stream = stream;
        Source = source;

    }
}
