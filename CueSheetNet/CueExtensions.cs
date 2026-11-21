using CueSheetNet.Components;
using Microsoft.Extensions.Logging;
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
    static ILogger LoggerioFile => Loggingq.WritingLogger;
    // FileOps
    extension(CueSheet cueSheet)
    {
        public void Unlink(bool withAssociatedFiles, bool removeEmptyDirs)
        {
            cueSheet.RefreshFileInfos();
            if (cueSheet.SourceFile is null || !cueSheet.SourceFile.Exists)
            {
                throw new InvalidOperationException("Cannot unlink cue sheet whose source file does not exist");
            }
            List<string> filesToDelete = new(cueSheet.Files.Select(x => x.AbsolutePath).OfType<string>());
            if (withAssociatedFiles)
            {
                foreach (var item in cueSheet.AdditionalFiles)
                {
                    if (item.AbsolutePath is not null)
                        filesToDelete.Add(item.AbsolutePath);
                }
                //filesToDelete.AddRange(cueSheet.AdditionalFiles.Select(x => x.AbsolutePath).OfType<string>());
            }
            filesToDelete.Add(cueSheet.SourceFile.FullName);
            var dirs = filesToDelete.Select(x => Directory.GetParent(x)).OfType<DirectoryInfo>();
            foreach (var item in filesToDelete)
            {
                var finfo = new FileInfo(item);
                finfo.Delete();
                //if (File.Exists(item))
                //{
                //    File.Delete(item);
                //}
            }
            if (removeEmptyDirs)
            {
                foreach (var item in dirs)
                {

                    if (item.Exists && !item.EnumerateFileSystemInfos().Any())
                    {
                        LoggerioFile.RemovedFile(item.FullName);
                        item.Delete(recursive: false);
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="newCueSheetPath">The file path where the cue sheet file should be copied to.</param>
        /// <param name="withAssociatedFiles"></param>
        /// <param name="makePathsRelative"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public CueSheet Copy(string newCueSheetPath, bool withAssociatedFiles, bool makePathsRelative)
        {
            List<string> partialFilesToRemove = [];
            try
            {
                cueSheet.RefreshFileInfos();
                if (cueSheet.SourceFile is null || !cueSheet.SourceFile.Exists)
                    throw new InvalidOperationException("Cannot copy cue sheet whose source file does not exist. Use SaveAs()");
                var newRoot = Path.GetDirectoryName(newCueSheetPath) ?? throw new DirectoryNotFoundException($"Specified target path cannot be the root of drive ({newCueSheetPath}).");
                if (!Directory.Exists(newRoot))
                    Directory.CreateDirectory(newRoot);
                var clone = cueSheet.Clone();
                List<(string Source, string Target)> filesToMove = [];
                if (makePathsRelative)
                {
                    clone.ChangeFilePathsType(keepPathQualified: !makePathsRelative);
                }
                IEnumerable<FilePath> cueFiles = clone.Files.Cast<FilePath>();
                if (withAssociatedFiles)
                {
                    cueFiles = cueFiles.Concat(clone.AdditionalFiles);
                }
                foreach (var item in cueFiles)
                {
                    item.Info?.Refresh();
                    if (!item.Exists)
                    {
                        continue;
                    }
                    if (!item.IsQualified)
                    {
                        filesToMove.Add((item.AbsolutePath!, Path.Combine(newRoot, item.Path)));
                    }
                }
                foreach (var (Source, Target) in filesToMove)
                {
                    var targetParent = Directory.GetParent(Target) ?? throw new DirectoryNotFoundException($"Specified target path cannot be the root of drive ({Target}).");
                    if (!targetParent.Exists)
                    {
                        targetParent.Create();
                    }
                    File.Copy(Source, Target);
                    LoggerioFile.CopiedFile(Source, Target);
                    partialFilesToRemove.Add(Target);
                }
                clone.SaveAs(newCueSheetPath);
                partialFilesToRemove.Clear();
                clone.RefreshFileInfos();
                return clone;
            }
            finally
            {
                foreach (var item in partialFilesToRemove)
                {
                    LoggerioFile.RemovedFileRollback(item);
                    File.Delete(item);
                }
            }
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
            var reader = CueReader.CreateFromFile(path);
            return reader.Parse();
        }

        public static CueSheet ParseFromString(string content)
        {
            var reader = CueReader.CreateFromString(content);
            return reader.Parse();
        }

        public void Save()
        {
            ArgumentNullException.ThrowIfNull(cueSheet.SourceFile, "Cannot save cue sheet without a source file. Use SaveAs() to save to a new file.");
            CueWriter.Create().Save(cueSheet);
        }

        public void SaveAs(string path)
        {
            CueWriter.Create().SaveAs(cueSheet, path);
        }

        public string WriteToString()
        {
            return CueWriter.Create().WriteToString(cueSheet);
        }

        public char[] WriteToCharArray()
        {
            return CueWriter.Create().WriteToCharArray(cueSheet);
        }

        public void WriteToCharSpan(Span<char> destination)
        {
            CueWriter.Create().WriteToCharSpan(cueSheet, destination);
        }

        public CueSheet Clone()
        {
            var newCue = new CueSheet(cueSheet.SourceFile)
            {
                Catalog = cueSheet.Catalog,
                CdTextFile = null, // added after cloning, since reference is needed
                Composer = cueSheet.Composer,
                Performer = cueSheet.Performer,
                Songwriter = cueSheet.Songwriter,
                SourceEncoding = cueSheet.SourceEncoding,
                Title = cueSheet.Title,
                Upc = cueSheet.Upc,
            };
            newCue.ChangeFilePathsType(cueSheet.UseQualifiedPaths);
            newCue.CdTextFile = cueSheet.CdTextFile?.Clone(newCue);
            CueTrack? lastDonorTrack = null;
            CueDataFile? lastDonorFile = null;
            CueTrack lastAddedTrack = null!;
            CueDataFile lastAddedFile = null!;
            foreach (var rem in cueSheet.Remarks)
            {
                newCue.Remarks.Add(rem);
            }
            foreach (var link in cueSheet.Data)
            {
                if (!ReferenceEquals(link.File, lastDonorFile))
                {
                    lastAddedFile = new CueDataFile(link.File.Path, newCue, link.File.Type);
                    lastDonorFile = link.File;
                }
                if (!ReferenceEquals(link.Track, lastDonorTrack))
                {
                    lastAddedTrack = new CueTrack(link.Track, newCue);
                    lastDonorTrack = link.Track;
                }
                var ind = new CueIndex(link.Index.Position, link.Index.Time, newCue) { IsSplit = link.Index.IsSplit };
                newCue.AddIndex(ind, lastAddedTrack, lastAddedFile);
            }
            newCue.Invalidate();
            //newCue.NormalizeGaps();
            newCue.RefreshFileInfos();
            _ = newCue.AdditionalFiles;
            return newCue;
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
        public string? DiscId
        {
            get => cueSheet.Remarks.GetField("DISCID");
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    cueSheet.Remarks.AddUnique("DISCID", value);
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
                    cueSheet.Remarks.AddUnique("DATE", value.Value.ToString(CultureInfo.InvariantCulture));
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
                    cueSheet.Remarks.AddUnique("REPLAYGAIN_ALBUM_GAIN", value.Value.ToString(CultureInfo.InvariantCulture));
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
                    cueSheet.Remarks.AddUnique("REPLAYGAIN_ALBUM_PEAK", value.Value.ToString(CultureInfo.InvariantCulture));
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
                    track.Remarks.AddUnique("DATE", value.Value.ToString(CultureInfo.InvariantCulture));
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
                    track.Remarks.AddUnique("REPLAYGAIN_TRACK_GAIN", value.Value.ToString(CultureInfo.InvariantCulture));
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
                    track.Remarks.AddUnique("REPLAYGAIN_TRACK_PEAK", value.Value.ToString(CultureInfo.InvariantCulture));
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
                    track.Remarks.AddUnique("REPLAYGAIN_ALBUM_GAIN", value.Value.ToString(CultureInfo.InvariantCulture));
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
                    track.Remarks.AddUnique("REPLAYGAIN_ALBUM_PEAK", value.Value.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
