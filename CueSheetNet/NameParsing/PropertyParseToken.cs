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
        Property = typeof(CueSheet).GetProperty(Name, _propertyBindingFlags) ?? throw new NotSupportedException($"Property {Name} is not supported for parsing.");
    }
    public override string? GetValue(CueSheet sheet)
    {
        object? val = Property.GetValue(sheet);
        return val?.ToString();
    }
}

internal record ExtensionPropertyParseToken : ParseToken
{
    private const BindingFlags _methodBindingFlags = BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public;
    public MethodInfo Property { get; }
    public ExtensionPropertyParseToken(string name,
                              string description,
                              params string[] alternatives) : base(name, description, alternatives)
    {
        Property = typeof(CueExtensions).GetMethod("get_"+Name, _methodBindingFlags, [typeof(CueSheet)]) ?? throw new NotSupportedException($"Static extension property {Name} is not supported for parsing.");
    }
    public override string? GetValue(CueSheet sheet)
    {
        object? val = Property.Invoke(null, [sheet]);
        return val?.ToString();
    }
}

