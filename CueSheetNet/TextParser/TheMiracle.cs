using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CueSheetNet.TextParser
{
    public class TheMiracle
    {
        static private readonly Lazy<Regex> EmergencyFile = new(()=>new(@"(?<PATH>.*\..+) (?<TYPE>\w*)", RegexOptions.Compiled));
        public char Quotation { get; set; } = '"';

        /// <summary>
        /// Get the filename and file type. If quotation marks are present, simple iterating algorithm is used. Otherwise Regex is used
        /// </summary>
        /// <param name="s">String to get values from</param>
        /// <returns><see cref="ValueTuple"/> of filename and type</returns>
        public (string Path, string Type) GetFile(string s)
        {
            ReadOnlySpan<char> spanny = s.AsSpan().Trim();
            string path, type;
            if (spanny[0] == Quotation)
            {
                int end = -1;
                for (int i = spanny.Length - 1; i >= 1; i--)
                {
                    if (spanny[i] == Quotation)
                    {
                        end = i;
                        break;
                    }
                }
                if (end > 0)
                {
                    path = spanny[1..end].ToString();
                    type = GetSuffix(s[end..]);
                    return (path, type);
                }
            }
            string emer = spanny.ToString();
            Match m = EmergencyFile.Value.Match(emer);
            if (m.Success)
            {
                path = m.Groups["PATH"].Value;
                type = m.Groups["TYPE"].Value;
                return (path, type);
            }
            return (emer, "");
        }
        /// <summary>
        /// Gets the value in quotation marks, or trimmed area if no quotations are present
        /// </summary>
        /// <param name="s">String to get value from</param>
        /// <param name="start">Index from which the search will be started</param>
        /// <param name="end">How many characters are skipped at the end</param>
        /// <returns></returns>
        public string GetValue(string s, int start, int end = 0)
        {
            ReadOnlySpan<char> spanny = s.AsSpan(start, s.Length - start - end).Trim();

            if (spanny[^1] == Quotation && spanny[0] == Quotation)
                return spanny[1..^1].ToString();
            else
                return spanny.ToString();
        }
        /// <summary>
        /// Get the first full word from the specified start, stops at whitespace
        /// </summary>
        /// <param name="s">String to get value from</param>
        /// <param name="startIndex">INdex from which the word will be sought</param>
        /// <param name="maxSearchLength">Maximum search length. If exceeded, empty string is returned</param>
        /// <returns>String of characters from <paramref name="startIndex"/> to first whitespace, or empty string if no whitespace has been detected in the first <paramref name="maxSearchLength"/> characters</returns>
        public string GetKeyword(string s, int startIndex = 0, int maxSearchLength = 10)
        {
            maxSearchLength = Math.Clamp(startIndex + maxSearchLength, 0, s.Length);
            ReadOnlySpan<char> spanish = s.AsSpan(startIndex, maxSearchLength).TrimStart();
            for (int i = 0; i < spanish.Length; i++)
            {
                if (char.IsWhiteSpace(spanish[i]))
                    return spanish[..i].ToString();
            }
            return string.Empty;
        }
        /// <summary>
        /// GEts the last word of the string (string from the last whitespace till the end)
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public string GetSuffix(ReadOnlySpan<char> s)
        {
            s = s.TrimEnd();
            for (int i = s.Length - 1; i >= s.Length / 2; i--)
            {
                if (char.IsWhiteSpace(s[i]))
                    return s[i..].ToString();
            }
            return string.Empty;
        }

    }
}
