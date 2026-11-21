
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.Reading;

public readonly record struct CueSource
{
    public SourceType Type { get; internal init; }
    public object? Data { get; internal init; }
    internal bool Initialized { get; }
    public Type? Datatype => Data?.GetType();
    internal CueSource(object? data, SourceType type)
    {
        Type = type;
        Data = data;
        Initialized = true;
    }
    public override string ToString()
    {
        if (!Initialized)
        {
            return "Unitialized";
        }
        return Type switch
        {
            SourceType.None => "No source",
            SourceType.File => "File \"" + Data + "\"",
            SourceType.Stream => $"Data of length {((Stream)Data!).Length}",
            SourceType.Bytes => $"Byte colletion of length {((IList)Data!).Count}",
            SourceType.String => $"String content of length {((string)Data!).Length}",
            _ => Type.ToString(),
        };
    }
}
