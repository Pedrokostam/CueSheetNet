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

    public InnerQuotation InnerQuotationReplacement { get; set; }

    public Encoding? Encoding { get; set; }

    public string NewLine { get; set; }

    private int _indentationDepth;
    public int IndentationDepth
    {
        get => _indentationDepth;
        set => _indentationDepth = Math.Max(value, 0);
    }
    public RedundantFieldBehaviors RedundantFieldsBehavior { get; set; }


    private char _indentationCharacter;
    public char IndentationCharacter
    {
        get => _indentationCharacter;
        set
        {
            if (!char.IsWhiteSpace(value))
                throw new ArgumentException($"Indentation character must be whitespace (is: '{value}' - 0x{(int)value:X})",nameof(value));
            _indentationCharacter = value;
        }
    }
    public CueWriterSettings()
    {
        _indentationDepth = 2;
        _indentationCharacter = ' ';
        NewLine = Environment.NewLine;
        Encoding = DefaultEncoding;
        InnerQuotationReplacement = InnerQuotation.CurvedDoubleTopQuotation;
        RedundantFieldsBehavior = RedundantFieldBehaviors.KeepAsIs;
    }
}
