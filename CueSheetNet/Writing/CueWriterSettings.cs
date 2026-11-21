using CueSheetNet.TextParser;
using System.ComponentModel;

namespace CueSheetNet;
public record CueWriterSettings
{
    public string NewlineString()
    {
        return NewLine switch
        {
            NewLineType.PlatformDefault => Environment.NewLine,
            NewLineType.LF => "\n",
            NewLineType.CRLF => "\r\n",
            _ => throw new InvalidEnumArgumentException(nameof(NewLine), (int)NewLine, typeof(NewLineType))
        };
    }
    /// <summary>
    /// The fallback encoding. Most compatible (UTF-8 without preamble).
    /// </summary>
    public static readonly Encoding DefaultEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false,
                                                                       throwOnInvalidBytes: true);
    /// <summary>
    /// If <see langword="true"/>, every suitable field will be enclosed in quotes, even if it does not contain whitespace
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

    /// <summary>
    /// Encoding used to save the text representaion of the sheet to a file.
    /// <para>
    /// If set to <see langword="null"/> it will first attempt to reuse the encoding used during parsing, then fall back to <see cref="CueWriterSettings.DefaultEncoding"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Ignored when writing to string.
    /// </remarks>
    public Encoding? Encoding { get; init; }

    public NewLineType NewLine { get; init; }
    public Indentation Indentation { get; init; }

    public RedundantFieldBehavior RedundantFields { get; init; }

    public CueWriterSettings()
    {
        ForceQuoting = true;
        Indentation = Indentation.Standard;
        NewLine = NewLineType.PlatformDefault;
        Encoding = null;
        InnerQuotationReplacement = InnerQuotation.CurvedDoubleTopQuotation;
        RedundantFields = RedundantFieldBehavior.IfExplicitlySet;
    }
}
