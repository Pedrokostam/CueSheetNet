using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Writing;

[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class IncludedIfNotNullAttribute : Attribute
{
    public string FieldName { get; }
    public string Name { get; }

    public IncludedIfNotNullAttribute(string fieldName, string? name = null)
    {
        FieldName = fieldName;
        Name = name is not null ? name : fieldName.TrimStart('_').ToUpperInvariant();

    }
}
