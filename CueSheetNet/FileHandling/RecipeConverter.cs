﻿using CueSheetNet.Logging;
using System.Text;
namespace CueSheetNet.FileHandling;

public class RecipeConverter : IAudioConverter
{
    public string RecipeOutputFolder { get; set; }
    public string OutputName { get; set; }
    public bool Append { get; set; }
    public Encoding OutputEncoding { get; set; }
    public string Separator { get; set; }

    readonly List<(string input, string output)> Elements;
    public RecipeConverter(string outputFolder, string fileName, bool append, Encoding encoding, string separator)
    {
        ArgumentNullException.ThrowIfNull(outputFolder, nameof(outputFolder));
        ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
        RecipeOutputFolder = outputFolder;
        OutputName = fileName;
        Elements = new();
        Append = append;
        OutputEncoding = encoding ?? new UTF8Encoding(false);
        Separator = separator;
    }
   
    public RecipeConverter(string recipePath) : this(Path.GetDirectoryName(recipePath)!, Path.GetFileName(recipePath)!, false, Encoding.UTF8, ";")
    {

    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="outputFolder">Folder in which the text file with list of converted files will placed</param>
    /// <param name="fileName">Filename of the text file with list of converted files</param>
    public RecipeConverter(string outputFolder, string fileName) : this(outputFolder, fileName, false, new UTF8Encoding(false), ";")
    {

    }
    public void Convert(string input, string output)
    {
        Elements.Add((input, output));
        Logger.LogInformation("Added record for conversion of {Input} to {Output}", input, output);
    }
    public void PostConvert()
    {
        string outputPath = Path.Combine(RecipeOutputFolder, OutputName);
        using StreamWriter stream = new(outputPath, Append, OutputEncoding);
        foreach (var (input, output) in Elements)
        {
            stream.Write(input);
            stream.Write(Separator);
            stream.WriteLine(output);
        }
        Logger.LogInformation("Saved all records for converting ({Number})", Elements.Count);
    }
    public string PreConvert(string format)
    {
        Directory.CreateDirectory(RecipeOutputFolder);
        Elements.Clear();
        return format;
    }

}