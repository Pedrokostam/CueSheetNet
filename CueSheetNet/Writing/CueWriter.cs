using CueSheetNet.Internal;
using CueSheetNet.Logging;
using CueSheetNet.NameParsing;
using CueSheetNet.Syntax;
using CueSheetNet.TextParser;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace CueSheetNet;

public sealed class CueWriter
{
    /// <summary>
    /// Based on personal collection, where most of the sheets were well under 2000 characters long, with the longest being ~7500
    /// </summary>
    readonly StringBuilder Builder;
    public CueWriterSettings Settings { get; set; }

    public CueWriter() : this(null)
    {
    }
    public CueWriter(CueWriterSettings? settings)
    {
        Settings = settings ?? new();
        Builder = new(2000);
        Logger.LogDebug("CueWriter created");
    }
    private bool HasWhitespace(string? val)
    {
        if (val == null) return false;
        if (Settings.ForceQuoting) return true;
        foreach (var item in val)
        {
            if (char.IsWhiteSpace(item))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Converts to string with optional enquoting and appends to stringbuilder. If value is null nothing is appended
    /// </summary>
    private bool AppendStringify<T>(string header, T? value, int depth, bool quoteAllowed)
    {
        if (value == null) return false;
        AppendIndentation(depth);
        Builder.AppendLine(Stringify(header, value, quoteAllowed));
        return true;
    }

    /// <inheritdoc cref="InnerQuotation.ReplaceQuotes(string?)"/>
    /// <remarks>Also performs other string replacements, if specified in settings</remarks>
    [return: NotNullIfNotNull(nameof(str))]
    private string? Replace(string? str) => Settings.InnerQuotationReplacement.ReplaceQuotes(str);
    private bool AppendRemark(CueRemark rem, int depth) => AppendStringify("REM " + rem.Field, Replace(rem.Value), depth, true);
    private bool AppendIndex(CueIndexImpl cim) => AppendStringify("INDEX " + cim.Number.ToString("D2"), cim.Time.ToString(), 2, false);
    [return: NotNullIfNotNull(nameof(s))]
    private static string? Enquote(string? s)
    {
        if (s == null) return null;
        return "\"" + s + "\"";
    }
    public string? Stringify<T>(string Header, T? value, bool quoteAllowed)
    {
        if (value == null) return null;
        if (quoteAllowed && HasWhitespace(value.ToString()))
            return Header + " " + Enquote(value.ToString());
        else return Header + " " + value;
    }

    private void AppendTrackRems(CueTrack track) => AppendRems(track.Remarks, 2);
    private void AppendRems(IEnumerable<CueRemark> rems, int depth = 0)
    {
        foreach (var item in rems)
            AppendRemark(item, depth);
    }
    private void AppendTrackComments(CueTrack track) => AppendComments(track.Comments, 2);
    private void AppendComments(IEnumerable<string> comms, int depth = 0)
    {
        foreach (var item in comms)
            AppendStringify("REM COMMENT", Replace(item), depth, true);
    }
    private void AppendIndentation(int level)
    {
        if (level > 0)
            Builder.Append(' ', level * Settings.IndentationDepth);
    }
    private void AppendOptionalField(CueTrack track, FieldsSet key)
    {
        string keyName = key.ToString();
        bool isSet = track.CommonFieldsSet.HasFlag(key);
        // If it is not set we can only write with AlwaysWrite
        if (!isSet && Settings.RedundantFieldsBehavior != CueWriterSettings.RedundantFieldBehaviors.AlwaysWrite)
        {
            return;
        }
        (string? trackValue, string? sheetValue) = key switch
        {
            FieldsSet.Title => (track.Title, null),
            FieldsSet.Performer => (track.Performer, track.ParentSheet.Performer),
            FieldsSet.Composer => (track.Composer, track.ParentSheet.Composer),
            _ => throw new NotImplementedException($"{key} is not implemented"),
        };
        bool write = Settings.RedundantFieldsBehavior switch
        {
            // if it was set, write it down
            CueWriterSettings.RedundantFieldBehaviors.KeepAsIs => isSet,
            // if both values are the same (no matter, if track is not set) don't write it
            CueWriterSettings.RedundantFieldBehaviors.RemoveRedundant => !string.Equals(trackValue, sheetValue, StringComparison.OrdinalIgnoreCase),
            // does not matter, if its not set, take sheet value instead
            CueWriterSettings.RedundantFieldBehaviors.AlwaysWrite => true,
            _ => throw new NotSupportedException(),
        };
        if (write) //if both track and sheet value are null, next method will skip it
            AppendStringify(keyName.ToUpperInvariant(), Replace(trackValue), 2, true);
    }

    internal char[] WriteToCharArray(CueSheet sheet)
    {
        FillStringBuilder(sheet);
        char[] tablicaCzarów = new char[Builder.Length];
        Builder.CopyTo(0, tablicaCzarów, Builder.Length);
        return tablicaCzarów;
    }

    public string WriteToString(CueSheet sheet)
    {
        FillStringBuilder(sheet);
        return Builder.ToString();
    }

    private void FillStringBuilder(CueSheet sheet)
    {
        Builder.Clear();
        AppendRems(sheet.Remarks, 0);
        AppendComments(sheet.Comments, 0);
        AppendStringify("REM DATE", sheet.Date, 0, true);
        AppendStringify("REM DISCID", sheet.DiscID, 0, true);
        AppendStringify("CDTEXTFILE", sheet.CdTextFile?.Name, 0, true);
        AppendStringify("CATALOG", sheet.Catalog, 0, true);
        AppendStringify("PERFORMER", Replace(sheet.Performer), 0, true);
        AppendStringify("REM COMPOSER", Replace(sheet.Composer), 0, true);
        AppendStringify("TITLE", Replace(sheet.Title), 0, true);
        CueTrack? track = null;
        CueDataFile? file = null;
        foreach (CueIndexImpl ind in sheet.IndexesImpl)
        {
            if (file != ind.File)
            {
                file = ind.File;
                AppendFileHeader(file);
            }
            if (track != ind.Track)
            {
                AppendPostgap(track);

                track = ind.Track;

                AppendTrackHeader(track);

                AppendOptionalField(track, FieldsSet.Title);
                AppendOptionalField(track, FieldsSet.Performer);
                AppendISRC(track);
                AppendOptionalField(track, FieldsSet.Composer);
                AppendFlags(track);
                AppendTrackRems(track);
                AppendTrackComments(track);
                AppendPregap(track);
            }
            AppendIndex(ind);
            var s = Builder.ToString();
        }
        if (Settings.NewLine != Environment.NewLine)
        {
            Builder.Replace(Environment.NewLine, Settings.NewLine);
        }
    }

    private void AppendFileHeader(CueDataFile file)
    {
        Builder.Append("FILE ");
        AppendFilepath(file);
        Builder.Append(' ');
        Builder.AppendLine(file.Type.ToString());
    }


    private void AppendFilepath(CueDataFile file)
    {
        string filename = file.GetRelativePath();
        string path = HasWhitespace(filename) ? Enquote(filename) : filename;
        Builder.Append(path);
    }

    private void AppendTrackHeader(CueTrack track)
    {
        AppendIndentation(1);
        Builder.Append("TRACK ");
        Builder.Append(track.Number.ToString("D2", CultureInfo.InvariantCulture));
        Builder.AppendLine(" AUDIO");
    }

    private void AppendPostgap(CueTrack? track)
    {
        if (track != null && track.PostGap > CueTime.Zero)
            AppendStringify("POSTGAP", track.PostGap, 2, false);
    }

    private void AppendISRC(CueTrack track) => AppendStringify("ISRC", track.ISRC, 2, true);
    private void AppendFlags(CueTrack track)
    {
        if (track.Flags != TrackFlags.None)
            AppendStringify("FLAGS", track.Flags.ToCueCompatible(), 2, true);
    }

    private void AppendPregap(CueTrack track)
    {
        if (track.PreGap > CueTime.Zero)
            AppendStringify("PREGAP", track.PreGap, 2, false);
    }
    /// <summary>
    /// Returns an <see cref="Encoding"/> object. Three sources will be tried in this order:
    /// <list type="number">
    /// <item><see cref="Encoding"/> from <see cref="Settings"/></item>
    /// <item>Source <see cref="Encoding"/> of <paramref name="sheet"/> </item>
    /// <item><see cref="CueWriterSettings.DefaultEncoding"/> </item>
    /// </list>
    /// First non-null object will be used.
    /// <para/>The <see cref="Encoding"/> will be modified to include <see cref="EncoderFallback.ExceptionFallback"/>.
    /// <para/>If <see cref="Encoding"/><see cref="Encoding"/> cannot write the contents of the sheet, it will be replaced with <see cref="CueWriterSettings.DefaultEncoding"/>
    /// </summary>
    /// <param name="sheet">Sheet which may containt source <see cref="Encoding"/></param>
    /// <returns></returns>
    private Encoding GetProperEncoding(CueSheet? sheet)
    {
        Encoding encodingBaza = Settings.Encoding ?? sheet?.SourceEncoding ?? CueWriterSettings.DefaultEncoding;
        if (encodingBaza.EncoderFallback != EncoderFallback.ExceptionFallback)
        {
            // If the encoding is a readonly instance, create a clone of it and use it instead
            encodingBaza = (Encoding)encodingBaza.Clone();
            encodingBaza.EncoderFallback = EncoderFallback.ExceptionFallback;
        }
        if (encodingBaza.Preamble.Length == 0 && (encodingBaza is UTF32Encoding || encodingBaza is UnicodeEncoding))
        {
            Logger.LogWarning("Using non-standard encoding multi-byte encoding without byte order mark: {Encoding.BodyName}", encodingBaza);
        }
        return encodingBaza;
    }
    public void SaveCueSheet(CueSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet.SourceFile);
        string textData = WriteToString(sheet);
        sheet.SourceFile.Directory!.Create();
        Encoding encoding = GetProperEncoding(sheet);
        try
        {
            File.WriteAllText(sheet.SourceFile.FullName, textData, encoding);
            sheet.SourceEncoding = encoding;
        }
        catch (EncoderFallbackException x)
        {
            int offendingChar = x.CharUnknown;
            string offendingString = $"0x{offendingChar:X8} - {offendingChar}";
            Logger.LogWarning("Specified encoding {Encoding} cannot be used to encode contents if {CueSheet} (Offending character - {Byte}) . Falling back to {DefaultEncoding}", encoding, sheet, offendingString, CueWriterSettings.DefaultEncoding);
            encoding = CueWriterSettings.DefaultEncoding;
            File.WriteAllText(sheet.SourceFile.FullName, textData, CueWriterSettings.DefaultEncoding);
        }
        sheet.SourceEncoding = encoding;
    }
    public void SaveCueSheet(CueSheet sheet, string newDestination)
    {
        sheet.SetCuePath(newDestination);
        SaveCueSheet(sheet);
    }
    public void SaveCueSheet(CueSheet sheet, string destination, string? pattern)
    {
        string patternParsed = CueTreeFormatter.ParseFormatPattern(sheet, pattern);
        string destinationWithPattern = Path.Combine(destination, patternParsed);
        SaveCueSheet(sheet, destinationWithPattern);
    }
}
