using CueSheetNet.FileHandling;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace CueSheetNet.NameParsing;

public static partial class CueTreeFormatter
{
    static ILogger Loggerio => Loggingq.WritingLogger;
    [GeneratedRegex(@"%(?<property>[\w\s]+)%", RegexOptions.Compiled, 500)]
    private static partial Regex PropertyParser();

    [GeneratedRegex(@"[\\/]+", RegexOptions.Compiled, 500)]
    private static partial Regex SeparatorNormalizer();


    /// <summary>
    /// Parse the format for output filepath. E.g. %Artist%/%DATE%/%Album% can result in
    /// ./Artist/2001/Album.
    /// All invalid characters in the path are replaced with '_'.
    /// If the sheet has no value for the token it will be replaced <paramref name="missingPropertyReplacement"/> (which defaults to empty string).
    /// If the resulting path ends in a path separator (system-dependent) that last separator will be removed.
    /// Spaces will be trimmed from the result.
    /// Property names do not need to match case.
    /// </summary>
    /// <param name="sheet"></param>
    /// <returns>Path stem made accoridng to the treeformat. All invalid path chars removed.</returns>
    /// <param name="treeFormat">Pattern which may contain reference to properies of CueSheet, case insensitive names. To get the original filename, specify %old%
    /// <para/>If parameter is null, the original filename will be used
    /// <para/>If parameter is null and the sheet's source does not have a name yet, the following pattern will be used: "{Performer} - {Title}"
    /// </param>
    public static string ParseFormatPattern(CueSheet sheet, string? treeFormat, string? missingPropertyReplacement = null)
    {
        if (string.IsNullOrWhiteSpace(treeFormat))
        {
            return Path.GetFileNameWithoutExtension(sheet.SourceFile?.Name)
                ?? sheet.GetDefaultTitle();
        }
        List<char> result = new(treeFormat.Length);
        var treeSpan = treeFormat.AsSpan();
        int percentStart = -1;
        int percentEnd = -1;
        for (int i = 0; i < treeFormat.Length; i++)
        {
            if (treeSpan[i] != '%')
            {
                if (PathStringNormalization.IsInvalidPathCharacter(treeSpan[i]))
                {

                    result.Add('_');
                }
                else
                {
                    result.Add(treeSpan[i]);
                }
                continue;
            }
            percentStart = i;
            i++;
            for (; i < treeFormat.Length; i++)
            {
                if (treeSpan[i] == '%')
                {
                    percentEnd = i;
                    break;
                }
            }
            if (percentEnd > percentStart + 1)
            {
                int afterPercent = percentStart + 1;
                var value = treeSpan[afterPercent..percentEnd].ToString();
                if (TagDict.TryGetValue(value, out ParseToken? token))
                {
                    var newVal = token.GetValue(sheet) ?? missingPropertyReplacement ?? string.Empty;
                    result.AddRange(newVal);
                }
                else
                {
                    int afterEndPercent = percentEnd + 1;
                    result.AddRange(treeSpan[percentStart..afterEndPercent]);
                    Loggerio.NoMatchingParseProperty(value, treeFormat);
                }
                percentEnd = -1;
                percentStart = -1;
            }
        }
        if (percentStart >= 0)
        {
            result.AddRange(treeSpan[percentStart..]);
        }
        var resultSpan = CollectionsMarshal.AsSpan(result);
        for (int i = 0; i < resultSpan.Length; i++)
        {
            if (PathStringNormalization.IsInvalidPathCharacter(resultSpan[i]))
            {
                resultSpan[i] = '_';
            }
        }
        var q = resultSpan.TrimEnd([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar,' ']).Trim();
        return q.ToString();

    }

    private static readonly ParseToken[] Tags;
    private static readonly Dictionary<string, ParseToken> TagDict;
    public static CueProperty[] AvailableProperties =>
        Tags.Select(x => new CueProperty(x)).ToArray();

    static CueTreeFormatter()
    {
        Tags =
        [
            new PropertyParseToken(
                "Title",
                "Title of the album",
                "Album",
                "AlbumTitle",
                "AlbumName"
            ),
            new PropertyParseToken(
                "Performer",
                "Name of the performer (artist/band)",
                "Artist",
                "AlbumArtist"
            ),
            new PropertyParseToken("Songwriter", "Name of the composer"),
            new PropertyParseToken("Catalog", "Catalog number of the CD"),
            new ExtensionPropertyParseToken("DiscID", "ID of the CD"),
            new ExtensionPropertyParseToken("Date", "Release data of the album", "Year"),
            new ExtensionPropertyParseToken("Composer", "Name of the composer"),
            new ExtensionPropertyParseToken("Upc", "Universal Product Code (or EAN) of the release"),
            new ExtensionPropertyParseToken("Genre", "Genre of the album"),
            new MethodParseToken(
                "Filename",
                (s) => Path.GetFileNameWithoutExtension(s.SourceFile!.Name),
                "Name of the Cue file, sans extension",
                "Current",
                "Old",
                "Name"
            ),
        ];
        TagDict = new Dictionary<string, ParseToken>(StringComparer.OrdinalIgnoreCase);
        foreach (var ptoken in Tags)
        {
            TagDict.Add(ptoken.Name.ToLowerInvariant(), ptoken);
            foreach (string alternative in ptoken.Alternatives)
            {
                TagDict.Add(alternative.ToLowerInvariant(), ptoken);
            }
        }
    }
}
