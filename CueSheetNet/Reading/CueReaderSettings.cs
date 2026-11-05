namespace CueSheetNet;

/// <summary>
/// 
/// </summary>
/// <param name="KeepQualified"></param>
/// <param name="Encoding"></param>
/// <param name="Quotation"> Character used to enclose text when it contains whitespace. Defaults to double quotes.</param>
public readonly record struct CueReaderSettings(bool KeepQualified, Encoding? Encoding, char Quotation, bool IgnoreIncorrectPlacement)
{
    public const char DefaultQuotation = '"';
    public CueReaderSettings() : this(KeepQualified: false, Encoding: null, Quotation: DefaultQuotation, false)
    {

    }
}
