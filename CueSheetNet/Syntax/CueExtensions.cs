namespace CueSheetNet.Syntax;
#pragma warning disable MA0048 // File name must match type name
public static class Parser
{
    public static TrackFlags Parse(string flagstring)
    {
        TrackFlags flag = TrackFlags.None;
        var parts = flagstring.Replace("\"", "").Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
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
#if NETCOREAPP2_0_OR_GREATER
        return string.Join(' ', strs);
#else
        return string.Join(" ", strs);
#endif
    }
}
#pragma warning restore MA0048 // File name must match type name
