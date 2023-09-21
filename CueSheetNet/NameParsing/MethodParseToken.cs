namespace CueSheetNet.NameParsing;

internal record MethodParseToken : ParseToken
{
    public Func<CueSheet, string> Method { get; }
    public MethodParseToken(string name,
                            Func<CueSheet, string> method,
                            string description,
                            params string[] alternatives) : base(name, description, alternatives)
    {
        Method = method;
    }
    public override string GetValue(CueSheet sheet)
    {
        return Method.Invoke(sheet);
    }
}
