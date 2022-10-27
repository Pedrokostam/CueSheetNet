namespace CueSheetNet;

public static class CueExtensions
{
    public static CueTrackFlags Parse(string flagstring)
    {
        CueTrackFlags flag = CueTrackFlags.None;
        var parts = flagstring.Replace("\"", "").Replace("'", "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {

            char trim = part[0];
            flag |= trim switch
            {
                '4' or 'f' or 'F' => CueTrackFlags.FourChannel,
                'p' or 'P' or 'e' or 'E' => CueTrackFlags.PreEmphasis,
                'd' or 'D' or 'C' or 'c' => CueTrackFlags.DigitalCopyPermitted,
                's' or 'S' => CueTrackFlags.SerialCopyManagementSystem,
                _ => CueTrackFlags.None,
            };
        }
        return flag;
    }
    public static string ToCueCompatible(this CueTrackFlags fl)
    {
        if (fl == CueTrackFlags.None) return string.Empty;
        List<string> strs = new();
        if ((fl.HasFlag(CueTrackFlags.DigitalCopyPermitted)))
        {
            strs.Add("DCP");
        }
        if ((fl.HasFlag(CueTrackFlags.FourChannel)))
        {
            strs.Add("4CH");
        }
        if ((fl.HasFlag(CueTrackFlags.PreEmphasis)))
        {
            strs.Add("PRE");
        }
        if ((fl.HasFlag(CueTrackFlags.SerialCopyManagementSystem)))
        {
            strs.Add("SCMS");
        }
        return "FLAGS " + string.Join(' ', strs);
    }
}
