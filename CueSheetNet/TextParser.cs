using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace CueSheetNet
{
    internal static class TextParser
    {
        //NQ =  no quotes
        const RegexOptions opt = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        public static Regex QuoteEnd = new(@"("".*""|'.*')", opt);
        public static Regex Keyword = new(@"\s*(?<KEY>\S+)\s+(?<KEYSUB>\S+)", opt);//REM COMMENT

        public static Regex Rem = new(@"\s*REM\s+(?<FIELD>\S+)\s+[""|'](?<VALUE>.+)[""|']", opt);//REM COMMENT 'przerwa xD'
        public static Regex RemNQ = new(@"\s*REM\s+(?<FIELD>\S+)\s+?(?<VALUE>.+)", opt);//REM COMMENT przerwaxD

        public static Regex Performer = new(@"\s*PERFORMER\s+[""|'](?<PERFORMER>.+)[""|']", opt);//PERFORMER 'jimbo john'
        public static Regex PerformerNQ = new(@"\s*PERFORMER\s+(?<PERFORMER>.+)", opt);//PERFORMER jimbo john

        public static Regex Title = new(@"\s*TITLE\s+[""|'](?<TITLE>.+)[""|']", opt);//TITLE 'title'
        public static Regex TitleNQ = new(@"\s*TITLE\s+(?<TITLE>.+)", opt);//TITLE title

        public static Regex File = new(@"\s*FILE\s+[""|'](?<FILE>.+)[""|']\s+(?<TYPE>.*)", opt);//FILE "bnla vba.flac"
        public static Regex FileNQ = new(@"\s*FILE\s+(?<FILE>.+)\s+(?<TYPE>.*)", opt);//FILE vba.flac

        public static Regex CdTextFile = new(@"\s*CDTEXTFILE\s+[""|'](?<CDTEXTFILE>.+)[""|']", opt);//CDTEXTFILE 'x d.txt'
        public static Regex CdTextFileNQ = new(@"\s*CDTEXTFILE\s+(?<CDTEXTFILE>.+)", opt);//CDTEXTFILE d.txt

        public static Regex Track = new(@"\s*TRACK\s+(?<NUMBER>\d+)\s+AUDIO", opt);//TRACK 01 AUDIO
        public static Regex Flags = new(@"\s*FLAGS\s+(?<FLAGS>.+)", opt);//FLAGS 4CH DCP
        public static Regex Index = new(@"\s*INDEX\s+(?<NUMBER>\d+)\s+(?<MINUTES>\d+):(?<SECONDS>\d+):(?<FRAME>\d+)", opt);//INDEX 01 00:05:47
        public static Regex Gap = new(@"\s*(?<GAPTYPE>PRE|POST)GAP\s+(?<MINUTES>\d+):(?<SECONDS>\d+):(?<FRAME>\d+)", opt);//POSTGAP 00:05:47
        public static Regex ISRC = new(@"\s*ISRC\s+(?<ISRC>.+)", opt);//FLAGS 4CH DCP
        public static Regex Catalog = new(@"\s*CATALOG\s+(?<CATALOG>.+)", opt);//FLAGS 4CH DCP


        public static bool CheckQuotes(string line) => QuoteEnd.IsMatch(line);
        public static CueKeyWords GetKeyword(string line)
        {
            Match mtc = Keyword.Match(line);
            if (mtc.Success && Enum.TryParse(mtc.Groups["KEY"].Value.ToUpperInvariant(), out CueKeyWords keyWord))
                return keyWord;
            else
                return CueKeyWords.INVALID;
        }
        public static bool TryGetPerformer(string line, [NotNullWhen(true)] out string? performer)
        {
            Match match = CheckQuotes(line) ? Title.Match(line) : TitleNQ.Match(line);
            if (match.Success)
            {
                performer = match.Groups["PERFORMER"].Value;
            }
            else
            {
                performer = null;
            }
            return match.Success;
        }
        public static bool TryGetTitle(string line, [NotNullWhen(true)] out string? title)
        {
            Match match = CheckQuotes(line) ? Title.Match(line) : TitleNQ.Match(line);
            if (match.Success)
            {
                title = match.Groups["TITLE"].Value;
            }
            else
            {
                title = null;
            }
            return match.Success;
        }
        public static bool TryGetCdTextFile(string line, [NotNullWhen(true)] out string? cdTextFile)
        {
            Match match = CheckQuotes(line) ? CdTextFile.Match(line) : CdTextFileNQ.Match(line);
            if (match.Success)
            {
                cdTextFile = match.Groups["CDTEXTFILE"].Value;
            }
            else
            {
                cdTextFile = null;
            }
            return match.Success;
        }
        public static bool TryGetFilepath(string line, [NotNullWhen(true)] out string? path, [NotNullWhen(true)] out string? type)
        {
            Match match = CheckQuotes(line) ? File.Match(line) : FileNQ.Match(line);
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
