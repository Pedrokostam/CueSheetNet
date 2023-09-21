namespace CueSheetNet.NameParsing;

internal abstract record ParseToken
{
    public string Name { get; }
    public string Description { get; }
    public string[] Alternatives { get; }
    public abstract string GetValue(CueSheet sheet);
    public ParseToken(string name,
                      string description,
                      params string[] alternatives)
    {
        Name = name;
        Description = description;
        Alternatives = alternatives;
    }
}
