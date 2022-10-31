using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileIO;
internal static class CueEncodingTester
{
    class ByteInvariantComparer : EqualityComparer<byte>
    {
        public override bool Equals(byte x, byte y)
        {
            return x == y || x == (y ^ 0b00100000);//XOR with 32, since this is the bit that differentiates upper and lowercase. At least for letters...
        }

        public override int GetHashCode([DisallowNull] byte obj)
        {
            throw new NotImplementedException();
        }
    }
    static readonly Encoding[] PreambledEncodings = new Encoding[] {
        Encoding.Unicode,
        Encoding.UTF8,
        Encoding.BigEndianUnicode,
        Encoding.UTF32,
        Encoding.GetEncoding("utf-32BE"),
    };
    /// <summary>
    /// "REM COMMENT " without first letter
    /// </summary>
    static readonly byte[] RemCommentUppercase = Encoding.UTF8.GetBytes("EM COMMENT ");
    /// <summary>
    /// "PERFORMER " without first letter
    /// </summary>
    static readonly byte[] PerformerUppercase = Encoding.UTF8.GetBytes("ERFORMER ");
    /// <summary>
    /// "TITLE " without first letter
    /// </summary>
    static readonly byte[] TitleUppercase = Encoding.UTF8.GetBytes("ITLE ");
    /// <summary>
    /// "FILE " without first letter
    /// </summary>
    static readonly byte[] FileUppercase = Encoding.UTF8.GetBytes("ILE ");
    private static bool CheckRange(this byte b, byte min, byte max)
    {
        return b >= min && b < max;
    }
    private static void AddUntilNewLine(Stream fs, List<byte> bytes)
    {
        int last = fs.ReadByte();
        do
        {
            if (last == '\n' || last == '\r')
                break;
            bytes.Add((byte)last);
            last = fs.ReadByte();
        } while (last > 0);
    }
    public static bool CompareBytes(ReadOnlySpan<byte> input, Encoding encoding)
    {
        var preamble = encoding.Preamble;
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
    public static Encoding DetectCueEncoding(Stream fs)
    {
        if (DetectEncodingFromBOM(fs) is Encoding encodingBom)
            return encodingBom;
        if (DetectFixedWidthEncoding(fs) is Encoding encodingFixed)
            return encodingFixed;
        // at this point the file is most likely ASCII, UTF8 or some local codepage
        // all of those should have cue keywords (standard English) written with 1-byte characters
        // now we find all the lines which begin with FILE, TITLE, PERFORMER, REM COMMENT, as those are most likely to contain diacritics
        List<byte> b = GetPotentialDiacritizedLines(fs);
        return DetectUtf8Heuristically(b);
    }
    private static Encoding DetectUtf8Heuristically(List<byte> b)
    {
        bool utf8 = false;
        for (int i = 0; i < b.Count - 4; i++)
        {
            // One byte
            // U+0000..U+007F     00..7F
            if (b[i] <= 0x7F)
            {
                continue;
                // not setting utf8 to true, because ASCII also meets this criteria, and if you can work in ASCII, why not?
            }
            // Two bytes
            // U+0080..U+07FF     C2..DF     80..BF
            // skipping C0, C1 - non-minimal
            else if (b[i].CheckRange(0xC2, 0xE0)
               && b[i + 1].CheckRange(0x80, 0xC0))
            {
                utf8 = true;
                i += 1; // skip next character
            }
            // Three bytes
            // U+0800..U+0FFF     E0         A0..BF      80..BF
            // U+1000..U+CFFF     E1..EC     80..BF      80..BF
            // U+D000..U+D7FF     ED         80..9F      80..BF
            // U+E000..U+FFFF     EE..EF     80..BF      80..BF
            // skipping U+D800..U+DBFF - not valid UTF8
            // slightly simplified, as with first byte equal to E0 second has to be greater than A0, not 80...
            // but the third byte is consistent
            else if (b[i].CheckRange(0xE0, 0xF0)
                && b[i + 1].CheckRange(0x80, 0xC0)
                && b[i + 2].CheckRange(0x80, 0xC0))
            {
                utf8 = true;
                i += 2;// skip next 2 characters
            }
            // Four bytes
            // U+10000..U+3FFFF   F0         90..BF      80..BF     80..BF
            // U+40000..U+FFFFF   F1..F3     80..BF      80..BF     80..BF
            // U+100000..U+10FFFF F4         80..8F      80..BF     80..BF
            // slightly simplified, as with first byte equal to F0 second has to be greater than 90, not 80...
            // but the third and forth bytes are consistent
            else if (b[i].CheckRange(0xF0, 0xF5)
                && b[i + 1].CheckRange(0x80, 0xC0)
                && b[i + 2].CheckRange(0x80, 0xC0)
                && b[i + 3].CheckRange(0x80, 0xC0))
            {
                utf8 = true;
                i += 3; //skip next 3 characters
            }
            else
            {
                // most propably something from 0x7F up - some regional codepage
                utf8 = false;
                break;
            }
        }
        if (utf8)
            return new UTF8Encoding(false);
        else
            return Encoding.Default;
    }
    private static List<byte> GetPotentialDiacritizedLines(Stream fs)
    {
        ByteInvariantComparer bytey = new();
        fs.Seek(0, SeekOrigin.Begin);
        List<byte> bytes = new(512);
        Span<byte> remComment = stackalloc byte[RemCommentUppercase.Length];
        Span<byte> performer = stackalloc byte[PerformerUppercase.Length];
        Span<byte> title = stackalloc byte[TitleUppercase.Length];
        Span<byte> file = stackalloc byte[FileUppercase.Length];
        while (true)
        {
            int bb = fs.ReadByte();
            if (bb < 0) break;
            byte reading = (byte)bb;
            if (bytey.Equals(reading, (byte)'R'))//last letter R or r =>? Performer
            {
                fs.Read(remComment);
                if (remComment.SequenceEqual(RemCommentUppercase,bytey))
                    AddUntilNewLine(fs,bytes);
            }
            else if (bytey.Equals(reading, (byte)'P'))//last letter T or T =>? Rem Comment
            {
                fs.Read(performer);
                if (remComment.SequenceEqual(PerformerUppercase, bytey))
                    AddUntilNewLine(fs, bytes);
            }
            else if (bytey.Equals(reading, (byte)'F'))//last letter E or e =>? title or file
            {
                fs.Read(file);
                if (remComment.SequenceEqual(FileUppercase, bytey))
                    AddUntilNewLine(fs, bytes);
            }
            else if (bytey.Equals(reading,(byte)'T'))//last letter E or e =>? title or file
            {
                fs.Read(title);
                if (remComment.SequenceEqual(TitleUppercase, bytey))
                    AddUntilNewLine(fs, bytes);
            }
        }
        return bytes;
    }

    public static Encoding? DetectEncodingFromBOM(Stream fs)
    {
        Span<byte> bomArea = stackalloc byte[4];
        fs.Seek(0, SeekOrigin.Begin);
        fs.Read(bomArea);
        //test for encoding with BOM
        foreach (var encoding in PreambledEncodings)
        {
            if (CompareBytes(bomArea, encoding))
                return encoding;
        }
        return null;
    }
    public static Encoding? DetectFixedWidthEncoding(Stream fs)
    {
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
        // if no encoding had more than 33% hit rate - return null
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
}
