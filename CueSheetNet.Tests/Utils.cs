namespace CueSheetNet.Tests;
internal static class Utils
{
    /// <summary>
    /// $pwd/TestItems/...
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="subfolder"></param>
    /// <returns></returns>
    public static IEnumerable<string> GetFiles(string pattern, params string[] subfolder)
    {
        var p = subfolder.Prepend("TestItems", Directory.GetCurrentDirectory());
        string dir = Path.Combine(p);
        return Directory.EnumerateFiles(dir, pattern);
    }
    /// <summary>
    /// $pwd/TestItems/...
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string GetFile(params string[] parts)
    {
        var p = parts.Prepend("TestItems", Directory.GetCurrentDirectory());
        return Path.Combine(p);
    }
}
