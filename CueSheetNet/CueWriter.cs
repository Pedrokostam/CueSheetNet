using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CueSheetNet.Syntax;

namespace CueSheetNet
{
    public class CueWriter
    {
        readonly StringBuilder Builder = new StringBuilder();
        private char indentationCharacter = ' ';

        public Encoding Encoding { get; set; } = new UTF8Encoding(false);
        public int IndentationDepth { get; set; } = 2;
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
        public string Newline { get; set; } = Environment.NewLine;
        /// <summary>
        /// If true, Every suitable field will be enclosed in quotes, even if it does not contain whitespace
        /// </summary>
        public bool ForceQuoting { get; set; } = false;
        public InnerQuotation InnerQuotationReplacement { get; set; } = InnerQuotation.CurvedDoubleTopQuotation;
        /// <summary>
        /// If true, Byte order mark will not be included in the text file, even if encoding specifies it.
        /// </summary>
        public bool SkipBOM { get; set; } = false;

        public CueWriter()
        {
        }
        private bool HasWhitespace(string? val)
        {
            if (val == null) return false;
            if (ForceQuoting) return true;
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
        private bool AppendStringify(string header, object? value, int depth)
        {
            if (value == null) return false;
            AppendIndentation(depth);
            Builder.AppendLine(Stringify(header, value));
            return true;
        }
        [return: NotNullIfNotNull("str")]
        private string? Replace(string? str) => InnerQuotationReplacement.ReplaceQuotes(str);
        private bool AppendStringify(RemEntry rem, int depth) => AppendStringify("REM " + rem.Field, Replace(rem.Value), depth);
        private bool AppendStringify(CueIndexImpl cim, int depth) => AppendStringify("INDEX " + cim.Number.ToString("D2"), cim.Time.ToString(), depth);
        [return: NotNullIfNotNull("s")]
        private static string? Enquote(string? s)
        {
            if (s == null) return null;
            return "\""+s+"\"";
        }
        public string? Stringify(string Header, object? value)
        {
            if (value == null) return null;
            if (HasWhitespace(value.ToString()))
                return Header + " "+ Enquote(value.ToString());
            else return Header + " " + value;
        }

        private void AppendRems(IEnumerable<RemEntry> rems, int depth)
        {
            foreach (var item in rems)
                AppendStringify(item, depth);
        }
        private void AppendComments(IEnumerable<string> comms, int depth)
        {
            foreach (var item in comms)
                AppendStringify("REM COMMENT", Replace(item), depth);
        }
        private void AppendIndentation(int level)
        {
            if (level > 0)
                Builder.Append(' ', level * IndentationDepth);
        }
        public string WriteToString(CueSheet sheet)
        {
            Builder.Clear();
            AppendRems(sheet.Remarks, 0);
            AppendComments(sheet.Comments, 0);
            AppendStringify("REM DATE", sheet.Date, 0);
            AppendStringify("REM DISCID", sheet.DiscID, 0);
            AppendStringify("CDTEXTFILE", sheet.CdTextFile?.Name, 0);
            AppendStringify("CATALOG", sheet.Catalog, 0);
            AppendStringify("PERFORMER", Replace(sheet.Performer), 0);
            AppendStringify("REM COMPOSER", Replace(sheet.Composer), 0);
            AppendStringify("TITLE", Replace(sheet.Title), 0);
            CueTrack? track = null;
            CueFile? file = null;
            foreach (CueIndexImpl ind in sheet.IndexesImpl)
            {
                if (file != ind.File)
                {
                    file = ind.File;
                    string filename = file.File.Name;
                    string path = HasWhitespace(filename) ?  Enquote(filename) : filename;
                    Builder.Append("FILE ");
                    Builder.Append(path);
                    Builder.Append(' ');
                    Builder.AppendLine(file.Type);
                }
                if (track != ind.Track)
                {
                    if (track != null && track.PostGap > 0)
                        AppendStringify("POSTGAP", track.PostGap, 2);
                    track = ind.Track;
                    AppendIndentation(1);
                    Builder.Append("TRACK ");
                    Builder.Append(track.Number.ToString("D2"));
                    Builder.AppendLine(" AUDIO");

                    AppendStringify("TITLE", Replace(track.Title), 2);
                    AppendStringify("PERFORMER", Replace(track.Performer), 2);
                    AppendStringify("ISRC", track.ISRC, 2);
                    AppendStringify("REM COMPOSER", Replace(track.Composer), 2);
                    if (track.Flags != TrackFlags.None)
                        AppendStringify("FLAGS", track.Flags.ToCueCompatible(), 2);
                    AppendRems(track.RawRems, 2);
                    AppendComments(track.RawComments, 2);
                    if (track.PreGap > 0)
                        AppendStringify("PREGAP", track.PreGap, 2);
                }
                AppendStringify(ind, 2);
            }
            return Builder.ToString();
        }
        public void SaveCueSheet(CueSheet sheet)
        {
            //sheet.FileInfo
        }
    }
}
