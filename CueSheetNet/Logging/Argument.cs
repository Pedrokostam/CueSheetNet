using System.Reflection;

namespace CueSheetNet.Logging;

/// <summary>
/// Wrapper around a named object.
/// Allows specifying the name of a property or parameterless method to be used instead of the input object.
/// </summary>
/// <param name="Identifier">Label used to call the argument</param>
/// <param name="Object">Actual data</param>
/// <param name="Accessor">Optional property or parameterless method to be used instead of value of object</param>
public record struct Argument(string Identifier, object? Object, string? Accessor = null)
{
    public Type? ObjectType => Object?.GetType();
    public object? Get()
    {
        if (Accessor is null) return Object;
        PropertyInfo? prop = ObjectType?.GetProperty(Accessor);
        if (prop is not null)
            return prop.GetValue(Object) ?? Object;
        MethodInfo? meth = ObjectType?.GetMethod(Accessor);
        if (meth is not null)
            return meth.Invoke(Object, Array.Empty<Object?>()) ?? Object;
        return Object;
    }
    //public override string ToString()
    //{
    //    string def = Object.ToString() ?? "N/A";
    //    if (Accessor is null) return def;
    //    PropertyInfo? prop = ObjectType.GetProperty(Accessor);
    //    if (prop is not null)
    //        return prop.GetValue(Object)?.ToString() ?? def;
    //    MethodInfo? meth = ObjectType.GetMethod(Accessor);
    //    if (meth is not null)
    //        return meth.Invoke(Object, Array.Empty<Object>())?.ToString() ?? def;
    //    return def;
    //}
}
