using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.TextParser
{
    internal static class QuoteFinder
    {
        static private readonly char[] OpeningQuotes = new char[]
      {
        '"',
        '\'',
        '“',
        '‘',
        '„',
        '«',
        '‹',
      };
        private static readonly int OpeningsCount = OpeningQuotes.Length;
        private static bool CompareOpening(char o)
        {
            for (int i = 0; i < OpeningsCount; i++)
            {
                if (o == OpeningQuotes[i]) return true;
            }
            return false;
        }
        private static char GetMatchingQuotationMark(char q)
        {
            return q switch
            {
                '"' => '"',
                '\'' => '\'',
                '“' or '”' => '”',
                '„' => '“',
                '«' => '»',
                '‘' => '’',
                '‹' => '›',
                _ => q
            };
        }
        public static bool CheckClosedQuotesPresence(string str)
        {
            int length = str.Length;
            for (int j = 0; j < length; j++)
            {
                if (CompareOpening(str[j]))
                {
                    char sought = GetMatchingQuotationMark(str[j]);
                    for (int k = length - 1; k > j; k--)
                    {
                        if (str[k] == sought)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }
            return false;
        }
        public static (int Start, int Length) GetInQuoteRange(string str, int startFrom=0)
        {
            int length = str.Length;
            if (length < startFrom + 1) return (startFrom, length - startFrom );
            for (int rangeStart = 0; rangeStart < length; rangeStart++)
            {
                if (CompareOpening(str[rangeStart]))
                {
                    char sought = GetMatchingQuotationMark(str[rangeStart]);
                    for (int rangeEnd = length - 1; rangeEnd > rangeStart; rangeEnd--)
                    {
                        if (str[rangeEnd] == sought)
                        {
                            return (rangeStart+1,rangeEnd-rangeStart-1);
                        }
                    }
                    return (startFrom, length);
                }
            }
            return (startFrom, length - startFrom);
        }
        public static (int Start, int Length) GetInQuoteRangeEndsWith(string str, int startFrom = 0)
        {
            int length = str.Length;
            if (length < startFrom + 1) return (startFrom, length-startFrom);
            for (int rangeStart = 0; rangeStart < length; rangeStart++)
            {
                if (CompareOpening(str[rangeStart]))
                {
                    char sought = GetMatchingQuotationMark(str[rangeStart]);
                    if (str[^1] != sought)
                        return (startFrom, length);
                    else
                        return (rangeStart + 1, length - rangeStart - 1);
                }
            }
            return (startFrom, length - startFrom);
        }
    }
}
