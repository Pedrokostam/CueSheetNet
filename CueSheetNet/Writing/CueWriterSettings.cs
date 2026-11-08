using CueSheetNet.TextParser;
using System.ComponentModel;

namespace CueSheetNet;

public record CueWriterSettings
{
    public string NewlineString()
    {
        return NewLine switch
        {
            NewLineType.LF => "\n",
            NewLineType.CRLF => "\r\n",
            _ => throw new InvalidEnumArgumentException(nameof(NewLine), (int)NewLine, typeof(NewLineType))
        };
    }
    public enum NewLineType { LF,  CRLF}
    public static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false,
                                                                       throwOnInvalidBytes: true);
    /// <summary>
    /// If true, every suitable field will be enclosed in quotes, even if it does not contain whitespace
    /// </summary>
    public bool ForceQuoting { get; init; }

    public InnerQuotation InnerQuotationReplacement
    {
        get; 
        init
        {
            if (!value.IsValid)
                throw new ArgumentException("Cannot use null char for quotation", nameof(value));
            field = value;
        }
    }

    public Encoding? Encoding { get; init; }

    public NewLineType NewLine { get; init; }
    public int IndentationDepth
    {
        get;
        init => field = Math.Max(value, 0);
    }
    public bool KeepRedundantFields { get; init; }

    public char IndentationCharacter
    {
        get;
        init
        {
            if (!char.IsWhiteSpace(value))
                throw new ArgumentException($"Indentation character must be whitespace (is: '{value}' - 0x{(int)value:X})", nameof(value));
            field = value;
        }
    }
    public CueWriterSettings()
    {
        ForceQuoting = false;
        IndentationDepth = 2;
        IndentationCharacter = ' ';
        NewLine = OperatingSystem.IsWindows() ? NewLineType.CRLF : NewLineType.LF;
        Encoding = null;
        InnerQuotationReplacement = InnerQuotation.CurvedDoubleTopQuotation;
        KeepRedundantFields = false;
    }
}
