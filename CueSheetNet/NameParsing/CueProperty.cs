namespace CueSheetNet.NameParsing;

public readonly record struct CueProperty(string Name, string[] AlternativeNames, string Description)
{
    internal CueProperty(ParseToken pt) : this(pt.Name, pt.Alternatives, pt.Description)
    {

    }
}
