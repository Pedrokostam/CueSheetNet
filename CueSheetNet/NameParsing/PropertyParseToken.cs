using System.Reflection;

namespace CueSheetNet.NameParsing;

internal record PropertyParseToken : ParseToken
{
    private const BindingFlags _propertyBindingFlags = BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public;
    public PropertyInfo Property { get; }
    public PropertyParseToken(string name,
                              string description,
                              params string[] alternatives) : base(name, description, alternatives)
    {
        Property = typeof(CueSheet).GetProperty(Name, _propertyBindingFlags) ?? throw new NotImplementedException();
    }
    public override string GetValue(CueSheet sheet)
    {
        object? val = Property.GetValue(sheet);
        return val?.ToString() ?? string.Empty;
    }
}
