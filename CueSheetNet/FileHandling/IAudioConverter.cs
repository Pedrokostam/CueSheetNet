using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.FileHandling;

public interface IAudioConverter
{
    string PreConvert(string format);
    void Convert(string input, string output);
    void PostConvert();
}
public interface IAudioConverter2
{
    void PreConvert();
    IList<FileInfo> Convert(DirectoryInfo targetDir, params IReadOnlyList<CueDataFile> audioFiles);
    void PostConvert();
    void Cancel();
}
public sealed class Ignoramus
{
    [AllowNull]
    internal string Directory { get; }
    private Ignoramus(string? dir)
    {
        Directory = dir;
    }
    public static readonly Ignoramus Ignore = new(null);
    public static readonly Ignoramus BasePath = new(".");
    public static Ignoramus CustomPath(string directory) => new(directory);
}
public sealed class IgnoramusPlus
{
    [AllowNull]
    internal string Directory { get; }
    private IgnoramusPlus(string? dir)
    {
        Directory = dir;
    }
    public static readonly IgnoramusPlus Throw = new("");
    public static readonly IgnoramusPlus Ignore = new(null);
    public static readonly IgnoramusPlus BasePath = new(".");
    public static IgnoramusPlus CustomPath(string directory) => new(directory);
}

public interface IFileManagement
{
    void Prepare();
    void ProcessDataFiles(DirectoryInfo targetDir, params IReadOnlyList<CueDataFile> audioFiles);
    void ProcessExtraFiles(DirectoryInfo targetDir, params IReadOnlyList<CueExtraFile> audioFiles);
    void Finish();
    void Cancel();
}

public record Copier : IFileManagement
{
    public IList<string> ProcessedFiles { get; } = [];
    public Copier(Ignoramus dataFileFallbackBehavior, IgnoramusPlus extraFileFallbackBehavior)
    {
        DataFileFallbackBehavior = dataFileFallbackBehavior;
        ExtraFileFallbackBehavior = extraFileFallbackBehavior;
    }
    public Copier():this(Ignoramus.BasePath,IgnoramusPlus.BasePath)
    {
            
    }
    [DisallowNull]
    public Ignoramus DataFileFallbackBehavior { get; set; }
    [DisallowNull]
    public IgnoramusPlus ExtraFileFallbackBehavior { get; set; }

    public void Cancel()
    {
        foreach (var item in ProcessedFiles)
        {
            if (File.Exists(item))
            {
                File.Delete(item);
            }
        }
    }

    public void Finish()
    {
    }

    public void Prepare()
    {
        ProcessedFiles.Clear();
    }

    public void ProcessDataFiles(DirectoryInfo targetDir, params IReadOnlyList<CueDataFile> audioFiles)
    {
        throw new NotImplementedException();
    }

    public void ProcessExtraFiles(DirectoryInfo targetDir, params IReadOnlyList<CueExtraFile> audioFiles)
    {
        throw new NotImplementedException();
    }
}

public record FileManager<T>(IFileManagement Manager) where T : IFileManagement
{

    public void Process(CueSheet sheet)
    {

    }
}

public record NoopConverter : IAudioConverter2
{
    public void Cancel()
    {
        throw new NotImplementedException();
    }

    public IList<FileInfo> Convert(DirectoryInfo targetDir, params IReadOnlyList<CueDataFile> audioFiles)
    {
        if (audioFiles.any)
    }

    public void PostConvert()
    {
        throw new NotImplementedException();
    }

    public void PreConvert()
    {
        throw new NotImplementedException();
    }
}