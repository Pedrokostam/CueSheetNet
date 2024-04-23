using System.Runtime.InteropServices;
using CueSheetNet.Syntax;

namespace CueSheetNet;

public partial class CueReader2
{

    [StructLayout(LayoutKind.Auto)]
    private record struct KeywordedLine(Keywords Keyword, Line Line)
    {
        public readonly string Text => Line.Text;
        public readonly int Number => Line.Number;

        public static implicit operator (Keywords Keyword, Line Line)(KeywordedLine value)
        {
            return (value.Keyword, value.Line);
        }

        public static implicit operator Line(KeywordedLine value)
        {
            return value.Line;
        }

        public static implicit operator KeywordedLine((Keywords Keyword, Line Line) value)
        {
            return new KeywordedLine(value.Keyword, value.Line);
        }
    }
}