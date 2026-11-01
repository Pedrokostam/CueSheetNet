using CueSheetNet.Components;
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
        Property = typeof(Sheet).GetProperty(Name, _propertyBindingFlags) ?? throw new NotSupportedException($"Property {Name} is not supported for parsing.");
    }
    public override string GetValue(Sheet sheet)
    {
        object? val = Property.GetValue(sheet);
        return val?.ToString() ?? string.Empty;
    }
}
