using CueSheetNet.FileHandling;
using CueSheetNet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CueSheetNet.NameParsing;
public static partial class CueTreeFormatter
{
    [GeneratedRegex(@"%(?<property>[\w\s]+)%")]
    private static partial Regex PropertyParser();
    [GeneratedRegex(@"^[\\/]+$")]
    private static partial Regex SeparatorChecker();

    [GeneratedRegex(@"[\\/]+")]
    private static partial Regex SeparatorNormalizer();
    /// <summary>
    /// Parse the format for output filepath. E.g. %Artist%/%DATE%/%Album% can result in
    /// ./Artist/2001/Album.
    /// All invalid characters are replaced with '_'.
    /// Property names do not need to match case.
    /// </summary>
    /// <param name="sheet"></param>
    /// <returns>Path stem made accoridng to the treeformat. All invalid path chars removed.</returns>
    /// <param name="treeFormat">Pattern which may contain reference to properies of CueSheet, case insensitive names. To get the original filename, specify %old%
    /// <para/>If parameter is null, the original filename will be used
    /// <para/>If parameter is null and the sheet has not filename yet, the following pattern will be used: "{Performer} - {Title}"
    /// </param>
    public static string ParseFormatPattern(CueSheet sheet, string? treeFormat)
    {
        if (string.IsNullOrWhiteSpace(treeFormat))
            return Path.GetFileNameWithoutExtension(sheet.SourceFile?.Name) ?? sheet.DefaultFilename;
        Regex formatter = PropertyParser();
        MatchCollection matches = formatter.Matches(treeFormat);
        foreach (Match match in matches.Cast<Match>())
        {
            string groupVal = match.Groups["property"].Value.Replace(" ", "").ToLower(); // No properties contain space in the name, so we remove them
            string val = match.Value;
            if (TagDict.TryGetValue(groupVal, out ParseToken? tag))
            {
                string newVal = tag.GetValue(sheet);
                treeFormat = treeFormat.Replace(val, newVal);
            }
            else
            {
                Logger.LogWarning("No matching property found for {Property} when parsing tree format {Format}.", val, treeFormat);
            }
        }
        //replace all invalid path chars with underscore
        string normalized = PathStringNormalization.RemoveInvalidPathCharacters(treeFormat);
        normalized = SeparatorNormalizer().Replace(normalized, Path.DirectorySeparatorChar.ToString());
        if (String.IsNullOrWhiteSpace(normalized) || normalized == Path.DirectorySeparatorChar.ToString())
        {
            string old = Path.GetFileNameWithoutExtension(sheet.SourceFile?.Name) ?? sheet.DefaultFilename;
            Logger.LogWarning("Name parsing for pattern {Pattern} resulted in empty string - replacing with old name {OldName}", treeFormat, old);
            normalized = old;
        }
        return normalized;
    }
    private static readonly ParseToken[] Tags;
    private static readonly Dictionary<string, ParseToken> TagDict;
    public static CueProperty[] AvailableProperties => Tags.Select(x => new CueProperty(x)).ToArray();
    static CueTreeFormatter()
    {
        Tags = new ParseToken[]
        {
            new PropertyParseToken("Title","Title of the album","Album","AlbumTitle","AlbumName"),
            new PropertyParseToken("Performer","Name of the performer (artist/band)","Artist", "AlbumArtist"),
            new PropertyParseToken("Date","Release data of the album","Year"),
            new PropertyParseToken("Composer","Name of the composer"),
            new PropertyParseToken("Catalog","Catalog number of the CD"),
            new PropertyParseToken("DiscID","ID of the CD"),
            new MethodParseToken ("Filename",(s)=>Path.GetFileNameWithoutExtension(s.SourceFile!.Name),"Name of the Cue file, sans extension","Current","Old","Name")
        };
        TagDict = new Dictionary<string, ParseToken>(StringComparer.Ordinal);
        foreach (var ptoken in Tags)
        {
            TagDict.Add(ptoken.Name.ToLower(), ptoken);
            foreach (string alternative in ptoken.Alternatives)
            {
                TagDict.Add(alternative.ToLower(), ptoken);
            }


        }
    }
  
}
