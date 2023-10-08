﻿using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata.Ecma335;
using CueSheetNet.Logging;

namespace CueSheetNet.FileReaders;

public sealed class FfprobeFormatReader : IAudioFileFormatReader
{
    public static bool? FfprobeDetected { get; private set; }
    private static string fFProbePath = "ffprobe";
    public static string FFProbePath
    {
        get => fFProbePath;
        set
        {
            fFProbePath = value;
            FfprobeDetected = null;
        }
    }
    private static readonly string fFormatName = "Dependent";
    private static readonly string[] extensions = new string[] { "*" };
    public string FormatName => fFormatName;
    public string[] Extensions => extensions;
    public bool ExtensionMatches(string fileName) => true;
    public bool ReadMetadata(string path, out FileMetadata metadata)
    {
        if (FfprobeDetected == false)
        {
            metadata = default;
            return false;
        }
        try
        {
            Process proc = new();
            ProcessStartInfo startInfo = new(FFProbePath);
            startInfo.ArgumentList.Add(path);
            startInfo.ArgumentList.Add(@"-hide_banner");
            startInfo.ArgumentList.Add(@"-v");
            startInfo.ArgumentList.Add(@"error");
            startInfo.ArgumentList.Add(@"-select_streams");
            startInfo.ArgumentList.Add(@"a:0");
            startInfo.ArgumentList.Add(@"-of");
            startInfo.ArgumentList.Add(@"default=noprint_wrappers=1");
            startInfo.ArgumentList.Add(@"-show_entries");
            startInfo.ArgumentList.Add(@"stream=duration,bit_rate,channels,sample_rate,bits_per_raw_sample,bits_per_sample:format=size,format_name");
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            proc.StartInfo = startInfo;
            proc.Start();
            proc.WaitForExit();
            ParseFfprobeOutput(proc.StandardOutput, out metadata);
            FfprobeDetected = true;
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            FfprobeDetected = false;
            Logger.LogWarning($"Could not find ffprobe using path: '{FFProbePath}' - file metadata not read. Specify path to ffprobe using {nameof(FfprobeFormatReader)}.{nameof(FfprobeFormatReader.FFProbePath)}");
        }
        catch (Exception e)
        {
            Logger.LogWarning("Error {Error.Message} - file metada not read", e);
        }
        metadata = default;
        return false;
    }
    private static readonly char[] Separators = new char[] { '\r', '\n' };

    private static T GetValue<T>(Dictionary<string, string> dict, string key, T default_val) where T : IParsable<T>
    {
        if (dict.TryGetValue(key, out var value))
        {
            if (!T.TryParse(value, CultureInfo.InvariantCulture, out T? result))
            {
                result = default_val;
            }
            return result;

        }
        else
        {
            return default_val;
        }
    }
    public void ParseFfprobeOutput(StreamReader sreader, out FileMetadata data)
    {
        string content = sreader.ReadToEnd();
        Dictionary<string, string> ini = new Dictionary<string, string>();
        var lines = content.Split(Separators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (string line in lines)
        {
            var parts = line.Split('=');
            ini.Add(parts[0], parts[1]);
        }
        int bit_depth = GetValue(ini, "bit_per_sample", -1);
        if (bit_depth < 0)
        {
            bit_depth = GetValue(ini, "bits_per_raw_sample", -1);
        }
        data = new FileMetadata(
            TimeSpan.FromSeconds(GetValue(ini, "duration", -1.0)),
            false,
            GetValue(ini, "sample_rate", -1),
            GetValue(ini, "channels", -1),
            bit_depth,
            ini.GetValueOrDefault("format_name", FormatName));
    }
}

