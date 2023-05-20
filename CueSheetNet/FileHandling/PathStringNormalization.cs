using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace CueSheetNet.FileHandling;

public static class PathStringNormalization
{
    /// <summary>
    /// Replace compound characters with their components. Don't be too aggresive, e.g. long S will remain as is, not be converted to standard S, ligatures and superscripts remain.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [return: NotNullIfNotNull(nameof(input))]
    public static string? NormalizeString(string? input)
    {
        if (input == null) return null;
        //Replace compound characters with their components. Don't be to aggresive, e.g. long S will remain as is, not be converted to standard S, ligatures and superscripts remain.
        string norm = input.Normalize(NormalizationForm.FormD);
        StringBuilder sbd = new();
        foreach (char c in norm)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sbd.Append(c);
        }
        return sbd.ToString().Normalize(NormalizationForm.FormC);
    }
    [return: NotNullIfNotNull(nameof(input))]
    public static string? RemoveInvalidPathCharacters(string? input, string replacement = "_")
    {
        if (input == null) return null;
        // Faster than regex, but uses more memory
        return string.Join(replacement, input.Split(Path.GetInvalidPathChars()));
    }
    [return: NotNullIfNotNull(nameof(input))]
    public static string? RemoveInvalidNameCharacters(string? input, string replacement = "_")
    {
        if (input == null) return null;
        // Faster than regex, but uses more memory
        return string.Join(replacement, input.Split(Path.GetInvalidFileNameChars()));
    }

}
