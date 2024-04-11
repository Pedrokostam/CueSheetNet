namespace CueSheetNet.Syntax;
public static class Parser
{
    public static TrackFlags Parse(string flagstring)
    {
        TrackFlags flag = TrackFlags.None;
        var parts = flagstring
            .Replace("\"", "", StringComparison.Ordinal)
            .Replace("'", "",StringComparison.Ordinal)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            char trim = part[0];
            flag |= trim switch
            {
                '4' or 'f' or 'F' => TrackFlags.FourChannel,
                'p' or 'P' or 'e' or 'E' => TrackFlags.PreEmphasis,
                'd' or 'D' or 'C' or 'c' => TrackFlags.DigitalCopyPermitted,
                's' or 'S' => TrackFlags.SerialCopyManagementSystem,
                _ => TrackFlags.None,
            };
        }
        return flag;
    }
    public static string ToCueCompatible(this TrackFlags fl)
    {
        if (fl == TrackFlags.None) return string.Empty;
        List<string> strs = [];
        if (fl.HasFlag(TrackFlags.DigitalCopyPermitted))
        {
            strs.Add("DCP");
        }
        if (fl.HasFlag(TrackFlags.FourChannel))
        {
            strs.Add("4CH");
        }
        if (fl.HasFlag(TrackFlags.PreEmphasis))
        {
            strs.Add("PRE");
        }
        if (fl.HasFlag(TrackFlags.SerialCopyManagementSystem))
        {
            strs.Add("SCMS");
        }
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER // string.Join(char,IEnumerable<string>) added in NET Core 2.1 and NETStandard2.1
        return string.Join(' ', strs);
#else
        return string.Join(" ", strs);
#endif
    }
}
