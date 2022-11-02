namespace CueSheetNet.Syntax;

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
        List<string> strs = new();
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
        return string.Join(' ', strs);
    }
}
