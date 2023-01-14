using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CueSheetNet.Syntax;
using CueSheetNet.TextParser;

namespace CueSheetNet;

public sealed record CueWriterSettings
{
    public static readonly Encoding DefaultEncoding = new UTF8Encoding(false);
    public enum RedundantFieldBehaviors
    {
        KeepAsIs,
        RemoveRedundant,
        AlwaysWrite,
    }

    /// <summary>
    /// If true, Every suitable field will be enclosed in quotes, even if it does not contain whitespace
    /// </summary>
    public bool ForceQuoting { get; set; } = false;

    public InnerQuotation InnerQuotationReplacement { get; set; } = InnerQuotation.CurvedDoubleTopQuotation;

    /// <summary>
    /// If true, Byte order mark will not be included in the text file, even if encoding specifies it.
    /// </summary>
    public bool SkipBOM { get; set; } = false;

    public Encoding? Encoding { get; set; }

    public string Newline { get; set; } = Environment.NewLine;

    public int IndentationDepth { get; set; } = 2;

    private char indentationCharacter = ' ';

    public RedundantFieldBehaviors RedundantFieldsBehavior { get; set; } = RedundantFieldBehaviors.KeepAsIs;

    public char IndentationCharacter
    {
        get => indentationCharacter;
        set
        {
            if (!char.IsWhiteSpace(value))
                throw new ArgumentException($"Indentation character must be whitespace (is: '{value}')");
            indentationCharacter = value;
        }
    }
}

public class CueWriter
{
    readonly StringBuilder Builder = new();
    public CueWriterSettings Settings { get; set; } = new CueWriterSettings();

    public CueWriter()
    {
    }
    public CueWriter(CueWriterSettings settings)
    {
        Settings = settings;
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
    private bool AppendStringify(string header, object? value, int depth, bool quoteAllowed)
    {
        if (value == null) return false;
        AppendIndentation(depth);
        Builder.AppendLine(Stringify(header, value,quoteAllowed));
        return true;
    }
    [return: NotNullIfNotNull("str")]
    private string? Replace(string? str) => Settings.InnerQuotationReplacement.ReplaceQuotes(str);
    private bool AppendRemark(Remark rem, int depth) => AppendStringify("REM " + rem.Field, Replace(rem.Value), depth,true);
    private bool AppendIndex(CueIndexImpl cim) => AppendStringify("INDEX " + cim.Number.ToString("D2"), cim.Time.ToString(), 2,false);
    [return: NotNullIfNotNull("s")]
    private static string? Enquote(string? s)
    {
        if (s == null) return null;
        return "\"" + s + "\"";
    }
    public string? Stringify(string Header, object? value,bool quoteAllowed)
    {
        if (value == null) return null;
        if (quoteAllowed && HasWhitespace(value.ToString()))
            return Header + " " + Enquote(value.ToString());
        else return Header + " " + value;
    }

    private void AppendTrackRems(CueTrack track) => AppendRems(track.RawRems, 2);
    private void AppendRems(IEnumerable<Remark> rems, int depth=0)
    {
        foreach (var item in rems)
            AppendRemark(item, depth);
    }
    private void AppendTrackComments(CueTrack track) => AppendComments(track.RawComments, 2);
    private void AppendComments(IEnumerable<string> comms, int depth=0)
    {
        foreach (var item in comms)
            AppendStringify("REM COMMENT", Replace(item), depth,true);
    }
    private void AppendIndentation(int level)
    {
        if (level > 0)
            Builder.Append(' ', level * Settings.IndentationDepth);
    }
    private void AppendOptionalField(CueTrack track, FieldSetFlags key)
    {
        string keyName = key.ToString();

        (string? trackValue, string? sheetValue) = key switch
        {
            FieldSetFlags.Title => (track.Title, null),
            FieldSetFlags.Performer => (track.Performer, track.ParentSheet.Performer),
            FieldSetFlags.Composer => (track.Composer, track.ParentSheet.Composer),
            _ => throw new NotImplementedException(),
        };
        bool isSame = trackValue == sheetValue;
        bool isSet = track.CommonFieldsSet.HasFlag(FieldSetFlags.Title);
        CueWriterSettings.RedundantFieldBehaviors behavior = Settings.RedundantFieldsBehavior;

        bool write = behavior switch
        {
            CueWriterSettings.RedundantFieldBehaviors.KeepAsIs => isSet,
            CueWriterSettings.RedundantFieldBehaviors.RemoveRedundant => isSet && !isSame,
            CueWriterSettings.RedundantFieldBehaviors.AlwaysWrite => (key == FieldSetFlags.Title && isSet) || key != FieldSetFlags.Title,
            _ => throw new NotImplementedException(),
        };

        if (write)
            AppendStringify(keyName.ToUpperInvariant(), Replace(trackValue), 2,true);
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
        AppendStringify("REM DISCID", sheet.DiscID, 0,true);
        AppendStringify("CDTEXTFILE", sheet.CdTextFile?.Name, 0, true);
        AppendStringify("CATALOG", sheet.Catalog, 0, true);
        AppendStringify("PERFORMER", Replace(sheet.Performer), 0, true);
        AppendStringify("REM COMPOSER", Replace(sheet.Composer), 0, true);
        AppendStringify("TITLE", Replace(sheet.Title), 0, true);
        CueTrack? track = null;
        CueFile? file = null;
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

                AppendOptionalField(track, FieldSetFlags.Title);
                AppendOptionalField(track, FieldSetFlags.Performer);
                AppendISRC(track);
                AppendOptionalField(track, FieldSetFlags.Composer);
                AppendFlags(track);
                AppendTrackRems(track);
                AppendTrackComments(track);
                AppendPregap(track);
            }
            AppendIndex(ind);
        }
        if (Settings.Newline != Environment.NewLine)
        {
            Builder.Replace(Environment.NewLine, Settings.Newline);
        }
    }

    private void AppendFileHeader(CueFile file)
    {
        Builder.Append("FILE ");
        AppendFilepath(file);
        Builder.Append(' ');
        Builder.AppendLine(file.Type);
    }

    private void AppendFilepath(CueFile file)
    {
        string filename = file.FileInfo.Name;
        string path = HasWhitespace(filename) ? Enquote(filename) : filename;
        Builder.Append(path);
    }

    private void AppendTrackHeader(CueTrack track)
    {
        AppendIndentation(1);
        Builder.Append("TRACK ");
        Builder.Append(track.Number.ToString("D2"));
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

    public void SaveCueSheet(CueSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet.FileInfo);
        string textData = WriteToString(sheet);
        Encoding encoding = Settings.Encoding ?? sheet.SourceEncoding ?? CueWriterSettings.DefaultEncoding;
        File.WriteAllText(sheet.FileInfo.FullName, textData, encoding);
        sheet.SourceEncoding = encoding;
    }
}
