using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CueSheetNet.Logging;
[DebuggerDisplay("{Level}: {Message} - {Timestamp}")]
public partial class LogEntry
{
    public DateTime Timestamp { get; init; }

    public LogLevel Level { get; init; }

    public string Message { get; }

    public string MessageTemplate { get; }

    public ReadOnlyCollection<Argument> Elements { get; }

    string FormattingTemplate { get; }

    List<string> Identifiers { get; }


    public LogEntry(LogLevel level, string messageTemplate, object?[] args)
    {
        Level = level;
        Timestamp = DateTime.Now;
        MessageTemplate = messageTemplate;
        Identifiers = new List<string>();
        if (args.Length == 0 && !messageTemplate.Contains('{', StringComparison.Ordinal))
        {
            //No objects, no curly brackets - no identifiers -- no need to check
            Message = messageTemplate;
            FormattingTemplate = string.Empty;
            Elements = new(Array.Empty<Argument>());
            return;
        }

        FormattingTemplate = Tokenize();
        if (args.Length < Identifiers.Count)
        {
            throw new ArgumentException("Not enough arguments for the specified message template", nameof(args));
        }
        object?[] objects = new object?[Identifiers.Count];
        List<Argument> temp = new(Identifiers.Count);
        for (int i = 0; i < Identifiers.Count; i++)
        {
            objects[i] = args[i];
            if (Identifiers[i].Contains('.', StringComparison.OrdinalIgnoreCase))
            {
                string[] namePropMeth = Identifiers[i].Split('.');
                string name = namePropMeth[0];
                string propMeth = namePropMeth[1];
                propMeth = ParenthesisRegex().Replace(propMeth, "");
                temp.Add(new(name, args[i], propMeth));
            }
            else
            {
                temp.Add(new(Identifiers[i], args[i]));
            }
        }
        Elements = temp.AsReadOnly();
        Message = string.Format(CultureInfo.InvariantCulture, FormattingTemplate, Elements.Select(x => x.Get()).ToArray());
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
            int ind = Identifiers.FindIndex(x => string.Equals(x, identifier, StringComparison.OrdinalIgnoreCase));
            if (ind == -1)
            {
                ind = Identifiers.Count;
                Identifiers.Add(identifier);
            }
            strb.Append(ind);
            return end;
        }

        return i;
    }
#if NET7_0_OR_GREATER
    [GeneratedRegex(@"\(.*\)", RegexOptions.Compiled, 500)]
    private static partial Regex ParenthesisRegex();
#else
    private static readonly Regex ParenthesisRegexImpl = new(@"\(.*\)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));
    private static Regex ParenthesisRegex() => ParenthesisRegexImpl;
#endif
}
