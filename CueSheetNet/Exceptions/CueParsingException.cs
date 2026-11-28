using CueSheetNet.Reading;

namespace CueSheetNet.Exceptions;
public class CueParsingException : FormatException
{
    public CueParsingException(string message, int lineNumber, string line, CueSource source, Exception? innerException = null) : base(message, innerException)
    {
        LineNumber = lineNumber;
        Line = line;
        DataSource = source;
    }

    public override string ToString()
    {
        if (InnerException is not null)
        {
            return $"{Message} at {LineNumber} in {Source} ({InnerException})";
        }
        return $"{Message} at {LineNumber} in {Source}";
    }

    public int LineNumber { get; init; }
    public string Line { get; init; }
    public CueSource DataSource { get; init; }
}