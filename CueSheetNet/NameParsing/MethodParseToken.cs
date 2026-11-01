using CueSheetNet.Components;

namespace CueSheetNet.NameParsing;

internal record MethodParseToken : ParseToken
{
    public Func<Sheet, string> Method { get; }
    public MethodParseToken(string name,
                            Func<Sheet, string> method,
                            string description,
                            params string[] alternatives) : base(name, description, alternatives)
    {
        Method = method;
    }
    public override string GetValue(Sheet sheet)
    {
        return Method.Invoke(sheet);
    }
}
