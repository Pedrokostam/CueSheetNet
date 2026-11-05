using CueSheetNet.Components;
using CueSheetNet.Logging;
using CueSheetNet.NameParsing;
using CueSheetNet.Syntax;
using CueSheetNet.TextParser;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

namespace CueSheetNet;

public sealed class CueWriter
{
    private static readonly CultureInfo InvCul = CultureInfo.InvariantCulture;
    /// <summary>
    /// Based on personal collection, where most of the sheets were well under 2000 characters long, with the longest being ~7500
    /// </summary>
    readonly StringBuilder Builder;
    public CueWriterSettings Settings { get; set; }

    public CueWriter()
        : this(settings: null) { }

    public CueWriter(CueWriterSettings? settings)
    {
        Settings = settings ?? new();
        Builder = new(2000);
        Logger.LogDebug("CueWriter created");
    }

    private bool HasWhitespace(string? val)
    {
        if (val == null)
            return false;
        if (Settings.ForceQuoting)
            return true;
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
        if (value == null)
            return false;
        AppendIndentation(depth);
        Builder.AppendLine(Stringify(header, value, quoteAllowed));
        return true;
    }

    /// <inheritdoc cref="InnerQuotation.ReplaceQuotes(string?)"/>
    /// <remarks>Also performs other string replacements, if specified in settings</remarks>
    [return: NotNullIfNotNull(nameof(str))]
    private string? Replace(string? str) => Settings.InnerQuotationReplacement.ReplaceQuotes(str);

    private bool AppendRemark(Remark rem, int depth)
    {
        switch (rem)
        {
            case FieldRemark field:
                return AppendStringify("REM " + field.Field, Replace(field.Value), depth, quoteAllowed: true);
            case TextRemark text:
                AppendIndentation(depth);
                Builder.AppendLine(text.Text);
                return true;
            default:
                throw new NotSupportedException($"Remark type {rem.GetType().Name} is not supported");
        }

    }

    private bool AppendIndex(CueIndex cim) =>
        AppendStringify("INDEX " + cim.Position.Pad(2), cim.Time.ToString(), 2, quoteAllowed: false);

    [return: NotNullIfNotNull(nameof(s))]
    private static string? Enquote(string? s)
    {
        if (s == null)
            return null;
        return "\"" + s + "\"";
    }

    public string? Stringify<T>(string Header, T? value, bool quoteAllowed)
    {
        if (value == null)
            return null;

        if (quoteAllowed && HasWhitespace(value.ToString()))
            return Header + " " + Enquote(value.ToString());

        return Header + " " + value;
    }


    private void AppendRems(IEnumerable<Remark> rems, int depth = 0)
    {
        foreach (var item in rems)
            AppendRemark(item, depth);
    }


    private void AppendComments(IEnumerable<string> comms, int depth = 0)
    {
        foreach (var item in comms)
            AppendStringify("REM COMMENT", Replace(item), depth, quoteAllowed: true);
    }

    private void AppendIndentation(int level)
    {
        if (level > 0)
            Builder.Append(' ', level * Settings.IndentationDepth);
    }

    private void AppendOptional(CueTrack track, FieldInfo fieldInfo)
    {
        var s = fieldInfo.GetValue(track) as string;
        if (!string.IsNullOrWhiteSpace(s))
        {
            var name = fieldInfo.Name.TrimStart('_').ToUpperInvariant();
            AppendStringify(name, Replace(s), 2, quoteAllowed: true);
        }
    }

    private void AppendOptionalFields(CueTrack track)
    {
        var fieldsSet = track.ExplicitFields;
        if (fieldsSet.HasFlag(FieldsSet.Title))
        {
            AppendStringify("TITLE", Replace(track.Title), 2, quoteAllowed: true);
        }
        if (fieldsSet.HasFlag(FieldsSet.Performer))
        {
            AppendStringify("PERFORMER", Replace(track.Performer), 2, quoteAllowed: true);
        }
        if (fieldsSet.HasFlag(FieldsSet.Songwriter))
        {
            AppendStringify("SONGWRITER", Replace(track.Songwriter), 2, quoteAllowed: true);
        }
    }

    public char[] WriteToCharArray(CueSheet sheet)
    {
        FillStringBuilder(sheet);
        char[] tablicaCzarów = new char[Builder.Length];
        Builder.CopyTo(0, tablicaCzarów, 0, Builder.Length);
        return tablicaCzarów;
    }

    public void WriteToCharSpan(CueSheet sheet, Span<char> destination)
    {
        FillStringBuilder(sheet);
        Builder.CopyTo(0, destination, Builder.Length);
    }

    public string WriteToString(CueSheet sheet)
    {
        FillStringBuilder(sheet);
        return Builder.ToString();
    }

    private void FillStringBuilder(CueSheet sheet)
    {
        Logger.LogDebug("Began sheet serialization");
        var st = Stopwatch.StartNew();
        Builder.Clear();
        AppendStringify("TITLE", Replace(sheet.Title), 0, quoteAllowed: true);
        AppendStringify("PERFORMER", Replace(sheet.Performer), 0, quoteAllowed: true);
        AppendStringify("CATALOG", sheet.Catalog, 0, quoteAllowed: true);
        AppendStringify("CDTEXTFILE", sheet.CdTextFile?.Path, 0, quoteAllowed: true);
        AppendRems(sheet.Remarks, 0);
        CueTrack? lastIteratedTrack = null;
        CueDataFile? lastIteratedFile = null;

        foreach (var link in sheet.Data)
        {
            if (!ReferenceEquals(link.File, lastIteratedFile))
            {
                lastIteratedFile = link.File;
                AppendFileHeader(link.File);
            }
            if (!ReferenceEquals(link.Track, lastIteratedTrack))
            {
                lastIteratedTrack = link.Track;
                AppendTrackHeader(link.Track);
                AppendPregap(link.Track);
                AppendOptionalFields(link.Track);
                AppendFlags(link.Track);
                AppendRems(link.Track.Remarks, depth: 2);
            }
            AppendIndex(link.Index);
            if (ReferenceEquals(link.Index, lastIteratedTrack.Indices[^1]))
            {
                // last index in track, so append postgap if any
                AppendPostgap(link.Track);
            }

        }
        if (!string.Equals(Settings.NewLine, Environment.NewLine, StringComparison.Ordinal))
        {
            Builder.Replace(Environment.NewLine, Settings.NewLine);
        }
        st.Stop();
        Logger.LogInformation("CueSheet {} serialized in {}ms", sheet.Title, st.ElapsedMilliseconds);
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
        string filename = file.Path;
        string path = HasWhitespace(filename) ? Enquote(filename) : filename;
        Builder.Append(path);
    }

    private void AppendTrackHeader(CueTrack track)
    {
        AppendIndentation(1);
        Builder.Append("TRACK ");
        Builder.Append(track.Position.ToString("D2", InvCul));
        Builder.AppendLine(" AUDIO");
    }

    private void AppendPostgap(CueTrack? track)
    {
        if (track != null && track.PostGap > CueTime.Zero)
            AppendStringify("POSTGAP", track.PostGap, 2, quoteAllowed: false);
    }

    private void AppendISRC(CueTrack track) =>
        AppendStringify("ISRC", track.Isrc, 2, quoteAllowed: true);

    private void AppendFlags(CueTrack track)
    {
        if (track.Flags != TrackFlags.None)
            AppendStringify("FLAGS", track.Flags.ToCueCompatible(), 2, quoteAllowed: true);
    }

    private void AppendPregap(CueTrack track)
    {
        if (track.PreGap > CueTime.Zero)
            AppendStringify("PREGAP", track.PreGap, 2, quoteAllowed: false);
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
        Encoding encodingBaza =
            Settings.Encoding ?? sheet?.SourceEncoding ?? CueWriterSettings.DefaultEncoding;
        if (encodingBaza.EncoderFallback != EncoderFallback.ExceptionFallback)
        {
            // If the encoding is a readonly instance, create a clone of it and use it instead
            encodingBaza = (Encoding)encodingBaza.Clone();
            encodingBaza.EncoderFallback = EncoderFallback.ExceptionFallback;
        }
        if (
            encodingBaza.GetPreamble().Length == 0
            && (encodingBaza is UTF32Encoding || encodingBaza is UnicodeEncoding)
        )
        {
            Logger.LogWarning(
                "Using non-standard encoding multi-byte encoding without byte order mark: {Encoding.BodyName}",
                encodingBaza
            );
        }
        return encodingBaza;
    }

    public void SaveCueSheet(CueSheet sheet)
    {
        ExceptionHelper.ThrowIfNull(sheet.SourceFile);
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
            Logger.LogWarning(
                "Specified encoding {Encoding} cannot be used to encode contents if {CueSheet} (Offending character - {Byte}) . Falling back to {DefaultEncoding}",
                encoding,
                sheet,
                offendingString,
                CueWriterSettings.DefaultEncoding
            );
            encoding = CueWriterSettings.DefaultEncoding;
            File.WriteAllText(
                sheet.SourceFile.FullName,
                textData,
                CueWriterSettings.DefaultEncoding
            );
        }
        sheet.SourceEncoding = encoding;
    }

    public void SaveCueSheet(CueSheet sheet, string newDestination)
    {
        sheet.SourceFile = new FileInfo(newDestination);
        SaveCueSheet(sheet);
    }

    public void SaveCueSheet(CueSheet sheet, string destination, string? pattern)
    {
        string patternParsed = CueTreeFormatter.ParseFormatPattern(sheet, pattern);
        string destinationWithPattern = Path.Combine(destination, patternParsed);
        SaveCueSheet(sheet, destinationWithPattern);
    }
}
