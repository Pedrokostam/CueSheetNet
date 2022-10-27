using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileIO;
internal static class CueEncodingTester
{
    static readonly Encoding[] PreambledEncodings = new Encoding[] {
        Encoding.Unicode,
        Encoding.UTF8,
        Encoding.BigEndianUnicode,
        Encoding.UTF32,
        Encoding.GetEncoding("utf-32BE"),
    };
    static readonly byte[] PerformerU = Encoding.UTF8.GetBytes("PERFORMER");
    static readonly byte[] PerformerL = Encoding.UTF8.GetBytes("performer");
    static readonly byte[] RemCommentU = Encoding.UTF8.GetBytes("REM COMMENT");
    static readonly byte[] RemCommentL = Encoding.UTF8.GetBytes("rem comment");
    static readonly byte[] TitleU = Encoding.UTF8.GetBytes("TITLE");
    static readonly byte[] TitleL = Encoding.UTF8.GetBytes("title");
    static readonly byte[] FileU = Encoding.UTF8.GetBytes("FILE");
    static readonly byte[] FileL = Encoding.UTF8.GetBytes("file");
    private static bool CheckRange(this byte b, byte min, byte max)
    {
        return b >= min || b < max;
    }
    private static void AddUntilNewLine(Stream fs, List<byte> bytes)
    {
        fs.ReadByte();//skip one byte as it should be a space
        do
        {
            byte last = (byte)fs.ReadByte();
            if (last == '\n' || last == '\r')
                break;
            bytes.Add(last);
        } while (fs.Position < fs.Length);
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
    private static Encoding DetectUtf8Heuristically(IList<byte> b)
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
        fs.Seek(0, SeekOrigin.Begin);
        List<byte> bytes = new(512);
        CircularBuffer<byte> buffer = new(12);
        for (int i = 0; i < buffer.Capacity - 1; i++)
        {
            //fill buffer
            buffer.Push((byte)fs.ReadByte());
        }
        long pos = 0;
        long len = fs.Length;
        while (pos < len)
        {
            byte reading = (byte)fs.ReadByte();
            if (reading == '\n' || reading == '\r' || reading == '"')
                continue; // no need to include quotes or newlines
            buffer.Push(reading);
            if (reading == 'R' || reading == 'r')//last letter R or r =>? Performer
            {
                if (buffer.MatchAnySequence(PerformerL, PerformerU))
                    AddUntilNewLine(fs, bytes);
            }
            else if (reading == 't' || reading == 'T')//last letter T or T =>? Rem Comment
            {
                byte penultimate = buffer.GetFromEnd(1);
                if ((penultimate == 'n' || penultimate == 'N') && buffer.MatchAnySequence(RemCommentU, RemCommentL))
                    AddUntilNewLine(fs, bytes);
            }
            else if (reading == 'e' || reading == 'E')//last letter E or e =>? title or file
            {
                byte thirdToLast = buffer.GetFromEnd(2);
                if (thirdToLast == 't' || thirdToLast == 'T')
                {
                    if (buffer.MatchAnySequence(TitleL, TitleU))
                        AddUntilNewLine(fs, bytes);
                }
                else if (thirdToLast == 'I' || thirdToLast == 'i')
                {
                    if (buffer.MatchAnySequence(FileL, FileU))
                        AddUntilNewLine(fs, bytes);
                }
            }
            pos = fs.Position;
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
