using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

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
        var p = subfolder.Prepend("TestItems").Prepend(Directory.GetCurrentDirectory());
        string dir = Path.Join(p.ToArray());
        return Directory.EnumerateFiles(dir, pattern);
    }
    /// <summary>
    /// $pwd/TestItems/...
    /// </summary>
    /// <param name="parts"></param>
    /// <returns></returns>
    public static string GetFile(params string[] parts)
    {
        var p = parts.Prepend("TestItems").Prepend(Directory.GetCurrentDirectory());
        return Path.Join(p.ToArray());
    }
}
