using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CueSheetNet.Logging;

public record struct Argument(string Identifier, object Object)
{
    public Type ObjectType => Object.GetType();
}
public class LogEntry
{
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string MessageTemplate { get; }
    public IReadOnlyCollection<Argument> Elements { get; }

    string FormattingTemplate { get; }
    public string Message { get; }
    List<string> Identifiers { get; }
    internal LogEntry(LogLevel level, string messageTemplate) : this(level, messageTemplate, Array.Empty<object>()) { }
    internal LogEntry(string messageTemplate) : this(messageTemplate, Array.Empty<object>()) { }
    internal LogEntry(LogLevel level, string messageTemplate, object[] objects) : this(messageTemplate, objects)
    {
        Level = level;
    }
    internal LogEntry(string messageTemplate, object[] args)
    {
        Timestamp = DateTime.Now;
        MessageTemplate = messageTemplate;
        Identifiers = new List<string>();
        FormattingTemplate = Tokenize();
        if (args.Length < Identifiers.Count)
        {
            throw new Exception();
        }
        object[] objects = new object[Identifiers.Count];
        List<Argument> temp = new(Identifiers.Count);
        for (int i = 0; i < Identifiers.Count; i++)
        {
            objects[i] = args[i];
            temp.Add(new(Identifiers[i], args[i]));
        }
        Elements = temp.AsReadOnly();
        Message = string.Format(FormattingTemplate, objects);
    }
    private string Tokenize()
    {
        int i = 0;
        int max = MessageTemplate.Length;
        int maxIdenLength = max - 2;
        StringBuilder strb = new(max);
        while (i < max)
        {
            char cc = MessageTemplate[i];
            strb.Append(cc);
            if (cc == '{')
            {
                if (i < maxIdenLength && MessageTemplate[i + 1] != '{')
                {
                    i = AddIdentifier(i, strb);
                }
                else
                {
                    strb.Append('{');
                    i += 2;
                }
            }
            else
            {
                i++;
            }
        }
        return strb.ToString();
    }

    private int AddIdentifier(int i, StringBuilder strb)
    {
        i++;
        int end = -1;
        for (int j = i; j < MessageTemplate.Length; j++)
        {
            if (MessageTemplate[j] == ':' || MessageTemplate[j] == '}')
            {
                end = j;
                break;
            }
        }
        string identifier;
        if (end > i)
        {

            identifier = MessageTemplate[i..end];
            int ind = Identifiers.FindIndex(x => x == identifier);
            if (ind == -1)
            {
                ind = Identifiers.Count;
                Identifiers.Add(identifier);
            }
            strb.Append(ind);
            return end;
        }
        else
        {
            return i;
        }
    }

}
