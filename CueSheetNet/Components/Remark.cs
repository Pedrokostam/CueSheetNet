using CueSheetNet.Syntax;

namespace CueSheetNet.Components;

public abstract record Remark
{
    public static Remark Create(string line)
    {
        if (line.StartsWith("REM", StringComparison.OrdinalIgnoreCase))
        {
            line = line.AsSpan()[3..].Trim().ToString();
        }
        return CommonRemFields.IsFieldRemark(line, out string firstWord, out string? rest) switch
        {
            true => new FieldRemark(firstWord, rest!),
            false => new TextRemark(line),
        };
    }
    public static Remark Create(string field, string value)
    {
        return CommonRemFields.IsFieldRemark(field, value) switch
        {
            true => new FieldRemark(field, value!),
            false => throw new ArgumentException("The field and value do not match any recognized field remark"),
        };
    }

}
