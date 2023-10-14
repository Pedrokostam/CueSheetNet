using CueSheetNet.TextParser;
using System.Text;

namespace CueSheetNet;

public sealed record CueWriterSettings
{
    public static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false,
                                                                       throwOnInvalidBytes: true);
    public enum RedundantFieldBehaviors
    {
        KeepAsIs,
        RemoveRedundant,
        AlwaysWrite,
    }

    /// <summary>
    /// If true, every suitable field will be enclosed in quotes, even if it does not contain whitespace
    /// </summary>
    public bool ForceQuoting { get; set; } = true;

    public InnerQuotation InnerQuotationReplacement { get; set; } = InnerQuotation.CurvedDoubleTopQuotation;

    public Encoding? Encoding { get; set; } = DefaultEncoding;

    public string Newline { get; set; } = Environment.NewLine;

    private int _indentationDepth = 2;
    public int IndentationDepth
    {
        get => _indentationDepth;
        set => _indentationDepth = Math.Max(value, 0);
    }
    public RedundantFieldBehaviors RedundantFieldsBehavior { get; set; } = RedundantFieldBehaviors.KeepAsIs;


    private char _indentationCharacter = ' ';
    public char IndentationCharacter
    {
        get => _indentationCharacter;
        set
        {
            if (!char.IsWhiteSpace(value))
                throw new ArgumentException($"Indentation character must be whitespace (is: '{value}' - 0x{(int)value:X})");
            _indentationCharacter = value;
        }
    }
}
