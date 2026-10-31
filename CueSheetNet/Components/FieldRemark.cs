using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CueSheetNet.Components;

public record FieldRemark : Remark
{
    public string Field { get; init; }
    public string Value { get; init; }
    internal FieldRemark(string field, string value)
    {
        Field = field.ToUpperInvariant();
        Value = value;
    }
    internal bool TryGet<T>([NotNullWhen(returnValue: true)] out T? result) where T : IParsable<T>
    {
        return T.TryParse(Value, CultureInfo.InvariantCulture, out result);
    }
    internal T? Get<T>() where T : class, IParsable<T>
    {
        return T.TryParse(Value, CultureInfo.InvariantCulture, out var result) ? result : null;
    }
}

#pragma warning restore MA0008 // Add StructLayoutAttribute