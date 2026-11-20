namespace CueSheetNet.Tests;

internal static class TempDirectory
{
    public static string Create(params string[] parts)
    {
        List<string> pp = ["TestItems", "TempData", .. parts];
        string[] p = [.. pp];
        string output = Path.Combine(p);
        if (Directory.Exists(output))
        {
            Directory.Delete(output, true);
        }
        Directory.CreateDirectory(output);
        return output;
    }
}
