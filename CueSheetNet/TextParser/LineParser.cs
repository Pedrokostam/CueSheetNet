using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CueSheetNet.TextParser
{
    internal static class LineParser
    {
        //NQ =  no quotes
        const RegexOptions opt = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        public static Regex Keyword = new(@"\s*(?<KEY>\S+)\s+(?<KEYSUB>\S+)", opt);//REM COMMENT

        public static Regex Rem = new(@"\s*REM\s+(?<FIELD>\S+)\s+[""|'](?<VALUE>.+)[""|']", opt);//REM COMMENT 'przerwa xD'
        public static Regex RemNQ = new(@"\s*REM\s+(?<FIELD>\S+)\s+?(?<VALUE>.+)", opt);//REM COMMENT przerwaxD

        public static Regex File = new(@"\s*FILE\s+[""|'](?<FILE>.+)[""|']\s+(?<TYPE>.*)", opt);//FILE "bnla vba.flac"
        public static Regex FileNQ = new(@"\s*FILE\s+(?<FILE>.+)\s+(?<TYPE>.*)", opt);//FILE vba.flac

        public static Regex Track = new(@"\s*TRACK\s+(?<NUMBER>\d+)\s+AUDIO", opt);//TRACK 01 AUDIO
        public static Regex Flags = new(@"\s*FLAGS\s+(?<FLAGS>.+)", opt);//FLAGS 4CH DCP
        public static Regex Index = new(@"\s*INDEX\s+(?<NUMBER>\d+)\s+(?<MINUTES>\d+):(?<SECONDS>\d+):(?<FRAME>\d+)", opt);//INDEX 01 00:05:47
        public static Regex Gap = new(@"\s*(?<GAPTYPE>PRE|POST)GAP\s+(?<MINUTES>\d+):(?<SECONDS>\d+):(?<FRAME>\d+)", opt);//POSTGAP 00:05:47
        public static Regex ISRC = new(@"\s*ISRC\s+(?<ISRC>.+)", opt);//FLAGS 4CH DCP
        public static Regex Catalog = new(@"\s*CATALOG\s+(?<CATALOG>.+)", opt);//Catalog


        public static CueKeyWords GetKeyword(string line)
        {
            Match mtc = Keyword.Match(line);
            if (mtc.Success && Enum.TryParse(mtc.Groups["KEY"].Value.ToUpperInvariant(), out CueKeyWords keyWord))
                return keyWord;
            else
                return CueKeyWords.INVALID;
        }
        public static bool TryGetOneValue(string line, string key, [NotNullWhen(true)] out string? value)
        {
            (int start, int length) = QuoteFinder.GetInQuoteRangeEndsWith(line, key.Length);
            value = line.Substring(start, length);
            if (value.Length > 0)
            {
                return true;
            }
            value = null;
            return false;
        }
        //public static bool TryGetPerformer(string line, [NotNullWhen(true)] out string? performer)
        //{
        //    (int start, int length) = QuoteFinder.GetInQuoteRange(line, 8);// start from 8, since performer has length of 8
        //    performer = line.Substring(start, length);
        //    if (performer.Length > 0)
        //    {
        //        return true;
        //    }
        //    performer = null;
        //    return false;
        //}
        //public static bool TryGetCatalog(string line, [NotNullWhen(true)] out string? catalog)
        //{
        //    (int start, int length) = QuoteFinder.GetInQuoteRange(line, 7);// start from 7, since catalog has length of 7
        //    catalog = line.Substring(start, length);
        //    if (catalog.Length > 0)
        //    {
        //        return true;
        //    }
        //    catalog = null;
        //    return false;
        //}
        //public static bool TryGetTitle(string line, [NotNullWhen(true)] out string? title)
        //{
        //    (int start, int length) = QuoteFinder.GetInQuoteRange(line, 5);// start from 5, since title has length of 5
        //    title = line.Substring(start, length);
        //    if (title.Length > 0)
        //    {
        //        return true;
        //    }
        //    title = null;
        //    return false;
        //}
        //public static bool TryGetCdTextFile(string line, [NotNullWhen(true)] out string? cdTextFile)
        //{

        //    (int start, int length) = QuoteFinder.GetInQuoteRange(line, 10);// start from 10, since cdtextfile has length of 10
        //    cdTextFile = line.Substring(start, length);
        //    if (cdTextFile.Length > 0)
        //    {
        //        return true;
        //    }
        //    cdTextFile = null;
        //    return false;
        //}
        public static bool TryGetFilepath(string line, [NotNullWhen(true)] out string? path, [NotNullWhen(true)] out string? type)
        {
            Match match = QuoteFinder.CheckClosedQuotesPresence(line) ? File.Match(line) : FileNQ.Match(line);
            if (match.Success)
            {
                path = match.Groups["FILE"].Value;
                type = match.Groups["TYPE"].Value;
            }
            else
            {
                path = null;
                type = null;
            }
            return match.Success;
        }
        public static bool TryGetTrack(string line, out int trackNumber)
        {
            Match match = Track.Match(line);
            if (match.Success)
            {
                return int.TryParse(match.Groups["NUMBER"].Value, out trackNumber);
            }
            else
            {
                trackNumber = -1;
            }
            return match.Success;
        }
        public static bool TryGetFlags(string line, out CueTrackFlags flags)
        {
            Match match = Flags.Match(line);
            if (!match.Success)
            {
                flags = default;
                return false;
            }
            flags = CueTrackFlags.None;
            var parts = match.Groups["FLAGS"].Value.Replace("\"", "").Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                char trim = part[0];
                flags |= trim switch
                {
                    '4' or 'f' or 'F' => CueTrackFlags.FourChannel,
                    'p' or 'P' or 'e' or 'E' => CueTrackFlags.PreEmphasis,
                    'd' or 'D' or 'C' or 'c' => CueTrackFlags.DigitalCopyPermitted,
                    's' or 'S' => CueTrackFlags.SerialCopyManagementSystem,
                    _ => CueTrackFlags.None,
                };
            }
            return true;
        }

    }
}
