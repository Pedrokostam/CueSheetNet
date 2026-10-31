using System.Globalization;

namespace CueSheetNet.Syntax
{
    internal static class CommonRemFields
    {

        private enum ValueType
        {
            Integer,
            OneWord,
            Float,
            String,
        }
        static readonly HashSet<string> Commons = new(StringComparer.Ordinal) {
        "DATE",
        "GENRE",
        "COMPOSER",
        "COMMENT",
        "DISCID",
        "UPC",
        "REPLAYGAIN_TRACK_GAIN",
        "REPLAYGAIN_TRACK_PEAK",
        "REPLAYGAIN_ALBUM_GAIN",
        "REPLAYGAIN_ALBUM_PEAK",
        };

        public static bool IsFieldRemark(string line, out string firstWord, out string? rest)
        {
            firstWord = CueReader.GetKeyword(line);
            rest = line.AsSpan()[firstWord.Length..].Trim().ToString();
            return IsFieldRemark(firstWord, rest);
        }
        public static bool IsFieldRemark(string firstWord, string? rest)
        {
            if (firstWord.Equals("COMMENT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if (rest is null || string.IsNullOrWhiteSpace(rest))
            {
                return true;
            }
            if (!Commons.Contains(firstWord))
            {
                // If the field is not known, but it is followed by only one word
                // we can consider it a remark
                // because how often would have something like: "REM Great Album!" ?
                return rest.AsSpan().Trim().Count(' ') switch
                {
                    0 => true,
                    _ => false
                };
            }
            // is a known common remark
            return true;
        }

    }
}
