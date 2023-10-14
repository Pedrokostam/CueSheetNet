
namespace CueSheetNet.Reading;

public readonly record struct CueSource
{
    public SourceType Type { get; private init; }
    public object? Data { get; private init; }
    public Type? Datatype => Data?.GetType();
    public CueSource(object? data)
    {
        Type = data switch
        {
            null => SourceType.None,
            string => SourceType.File,
            IEnumerable<byte> => SourceType.Bytes,
            Stream => SourceType.Stream,
            _ => throw new ArgumentException($"Incorrect type of source data: {data.GetType()}")
        };
        Data = Type switch
        {
            SourceType.Bytes => ((IEnumerable<byte>)data!).ToArray(),
            _ => data,
        };
    }
    internal static CueSource FromStringContent(string content)
    {
        return new CueSource() { Type = SourceType.String, Data = content };
    }
    public override string ToString()
    {
        return Type switch
        {
            SourceType.None => "No source",
            SourceType.File => "File \"" + Data + "\"",
            SourceType.Stream => $"Stream of length {((Stream)Data!).Length}",
            SourceType.Bytes => $"Byte array of length {((Stream)Data!).Length}",
            SourceType.String => $"String content of length {((string)Data!).Length}",
            _ => Type.ToString(),
        };
    }
}
