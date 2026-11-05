using CueSheetNet.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;

public static class CueExtensions
{
    // FileOps
    extension(CueSheet cueSheet)
    {
        public void Unlink(bool withAssociatedFiles, bool removeEmptyDirs)
        {
            cueSheet.RefreshAdditionalFiles();
            if (cueSheet.SourceFile is null || !cueSheet.SourceFile.Exists)
            {
                throw new InvalidOperationException("Cannot unlink cue sheet whose source file does not exist");
            }
            List<string> filesToDelete = new(cueSheet.Files.Select(x => x.AbsolutePath).OfType<string>());
            if (withAssociatedFiles)
            {
                filesToDelete.AddRange(cueSheet.AdditionalFiles.Select(x => x.AbsolutePath).OfType<string>());
            }
            filesToDelete.Add(cueSheet.SourceFile.FullName);
            var dirs = filesToDelete.Select(x => Directory.GetParent(x)).OfType<DirectoryInfo>();
            foreach (var item in filesToDelete)
            {
                if (File.Exists(item))
                {
                    File.Delete(item);
                }
            }
            if (removeEmptyDirs)
            {
                foreach (var item in dirs)
                {

                    if (item.Exists && !item.EnumerateFileSystemInfos().Any())
                    {
                        item.Delete(recursive: false);
                    }
                }
            }
        }
        public CueSheet Copy(string newCueSheetPath, bool withAssociatedFiles, bool makePathsRelative)
        {
            cueSheet.RefreshAdditionalFiles();
            if (cueSheet.SourceFile is null || !cueSheet.SourceFile.Exists)
            {
                throw new InvalidOperationException("Cannot copy cue sheet whose source file does not exist. Use SaveAs()");
            }
            var newRoot = Path.GetDirectoryName(newCueSheetPath) ?? throw new DirectoryNotFoundException($"Specified target path cannot be the root of drive ({newCueSheetPath}).");
            if (!Directory.Exists(newRoot))
            {
                Directory.CreateDirectory(newRoot);
            }
            var clone = cueSheet.Clone();
            List<(string Source, string Target)> filesToMove = [];
            if (makePathsRelative)
            {
                clone.ChangeFilePathsType(keepPathQualified: true);
            }
            IEnumerable<FilePath> cueFiles = clone.Files.Cast<FilePath>();
            if (withAssociatedFiles)
            {
                cueFiles = cueFiles.Concat(clone.AdditionalFiles);
            }
            foreach (var item in cueFiles)
            {
                if (!item.IsQualified)
                {
                    filesToMove.Add((item.AbsolutePath!, Path.Combine(newRoot, item.Path)));
                }
            }
            foreach (var item in filesToMove)
            {
                var targetParent = Directory.GetParent(item.Target) ?? throw new DirectoryNotFoundException($"Specified target path cannot be the root of drive ({item.Target}).");
                if (!targetParent.Exists)
                {
                    targetParent.Create();
                }
                File.Copy(item.Source, item.Target);
            }
            clone.SaveAs(newCueSheetPath);
            return clone;

        }

        public CueSheet Move(string newCueSheetPath, bool withAssociatedFiles, bool makePathsRelative)
        {
            var moved = cueSheet.Copy(newCueSheetPath, withAssociatedFiles, makePathsRelative);
            cueSheet.Unlink(withAssociatedFiles, removeEmptyDirs: false);
            return moved;
        }
    }

    // Parsing / Writing
    extension(CueSheet cueSheet)
    {
        public static CueSheet Parse(string path)
        {
            var reader = CueReader2.CreateFromFile(path);
            return reader.Parse();
        }

        public void Save()
        {
            ArgumentNullException.ThrowIfNull(cueSheet.SourceFile, "Cannot save cue sheet without a source file. Use SaveAs() to save to a new file.");
            var writer = new CueWriter();
            writer.SaveCueSheet(cueSheet);
        }

        public void SaveAs(string path)
        {
            path = Path.ChangeExtension(path, "cue");
            var writer = new CueWriter();
            writer.SaveCueSheet(cueSheet, path);
        }

        public string WriteToString()
        {
            var writer = new CueWriter();
            return writer.WriteToString(cueSheet);
        }

        public char[] WriteToCharArray()
        {
            var writer = new CueWriter();
            return writer.WriteToCharArray(cueSheet);
        }

        public void WriteToCharSpan(Span<char> destination)
        {
            var writer = new CueWriter();
            writer.WriteToCharSpan(cueSheet, destination);
        }

        public CueSheet Clone()
        {
            var s = cueSheet.WriteToString();
            var reader = CueReader2.CreateFromString(s);
            return reader.Parse();
        }
    }

    // Common Remarks - Sheet
    extension(CueSheet cueSheet)
    {
        public string? Composer
        {
            get => cueSheet.Remarks.GetField("COMPOSER");
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    cueSheet.Remarks.AddUnique("COMPOSER", value);
                }
            }
        }
        public string? Upc
        {
            get => cueSheet.Remarks.GetField("UPC");
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    cueSheet.Remarks.AddUnique("UPC", value);
                }
            }
        }
        public string? Genre
        {
            get => cueSheet.Remarks.GetField("GENRE");
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    cueSheet.Remarks.AddUnique("GENRE", value);
                }
            }
        }
        public int? Date
        {
            get
            {
                if (cueSheet.Remarks.GetField("DATE") is string d && int.TryParse(d, CultureInfo.InvariantCulture, out int result))
                {
                    return result;
                }
                return null;
            }

            set
            {
                if (value is null)
                    cueSheet.Remarks.RemoveField("DATE");
                else
                    cueSheet.Remarks.AddUnique("DATE", value.ToString());
            }
        }
        public double? ReplayGainAlbumGain
        {
            get
            {
                if (cueSheet.Remarks.GetField("REPLAYGAIN_ALBUM_GAIN") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
                return null;
            }

            set
            {
                if (value is null)
                    cueSheet.Remarks.RemoveField("REPLAYGAIN_ALBUM_GAIN");
                else
                    cueSheet.Remarks.AddUnique("REPLAYGAIN_ALBUM_GAIN", value.ToString());
            }
        }
        public double? ReplayGainAlbumPeak
        {
            get
            {
                if (cueSheet.Remarks.GetField("REPLAYGAIN_ALBUM_PEAK") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
                return null;
            }

            set
            {
                if (value is null)
                    cueSheet.Remarks.RemoveField("REPLAYGAIN_ALBUM_PEAK");
                else
                    cueSheet.Remarks.AddUnique("REPLAYGAIN_ALBUM_PEAK", value.ToString());
            }
        }
    }

    // Common Remarks - Track
    extension(CueTrack track)
    {
        public string? Composer
        {
            get => track.Remarks.GetField("COMPOSER") ?? track._sheet.Composer;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    track.Remarks.AddUnique("COMPOSER", value);
                }
            }
        }
        public string? Upc
        {
            get => track.Remarks.GetField("UPC") ?? track._sheet.Upc;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    track.Remarks.AddUnique("UPC", value);
                }
            }
        }
        public string? Genre
        {
            get => track.Remarks.GetField("GENRE") ?? track._sheet.Genre;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    track.Remarks.AddUnique("GENRE", value);
                }
            }
        }
        public int? Date
        {
            get
            {
                if (track.Remarks.GetField("DATE") is string d && int.TryParse(d, CultureInfo.InvariantCulture, out int result))
                {
                    return result;
                }
                return track._sheet.Date;
            }

            set
            {
                if (value is null)
                    track.Remarks.RemoveField("DATE");
                else
                    track.Remarks.AddUnique("DATE", value.ToString());
            }
        }
        public double? ReplayGainTrackGain
        {
            get
            {
                if (track.Remarks.GetField("REPLAYGAIN_TRACK_GAIN") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
                return null;
            }

            set
            {
                if (value is null)
                    track.Remarks.RemoveField("REPLAYGAIN_TRACK_GAIN");
                else
                    track.Remarks.AddUnique("REPLAYGAIN_TRACK_GAIN", value.ToString());
            }
        }
        public double? ReplayGainTrackPeak
        {
            get
            {
                if (track.Remarks.GetField("REPLAYGAIN_TRACK_PEAK") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
                return null;
            }

            set
            {
                if (value is null)
                    track.Remarks.RemoveField("REPLAYGAIN_TRACK_PEAK");
                else
                    track.Remarks.AddUnique("REPLAYGAIN_TRACK_PEAK", value.ToString());
            }
        }
        public double? ReplayGainAlbumGain
        {
            get
            {
                if (track.Remarks.GetField("REPLAYGAIN_ALBUM_GAIN") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }

                return track._sheet.ReplayGainAlbumGain;
            }

            set
            {
                if (value is null)
                    track.Remarks.RemoveField("REPLAYGAIN_ALBUM_GAIN");
                else
                    track.Remarks.AddUnique("REPLAYGAIN_ALBUM_GAIN", value.ToString());
            }
        }
        public double? ReplayGainAlbumPeak
        {
            get
            {
                if (track.Remarks.GetField("REPLAYGAIN_ALBUM_PEAK") is string d && double.TryParse(d, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }

                return track._sheet.ReplayGainAlbumPeak;
            }

            set
            {
                if (value is null)
                    track.Remarks.RemoveField("REPLAYGAIN_ALBUM_PEAK");
                else
                    track.Remarks.AddUnique("REPLAYGAIN_ALBUM_PEAK", value.ToString());
            }
        }
    }
}
