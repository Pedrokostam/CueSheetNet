namespace CueSheetNet.Tests;

public readonly record struct Sample(string Folder, string File)
{
    public string Path => Utils.GetFile(Folder, File);

    public CueSheet Load() => CueReader.CreateFromFile(Path).Parse();

    public string ReadText() => System.IO.File.ReadAllText(Path);

    public override string ToString() => $"{Folder}/{File}";
}

/// <summary>Every shared test sheet. The only place that spells folder and file names.</summary>
internal static class Samples
{
    public static readonly Sample Base = new("Equality", "Base.cue");

    public static readonly Sample JethroTullAqualung = new("Sheets/Complete", "Jethro Tull - Aqualung.cue");
    public static readonly Sample JudasPriestScreamingForVengeance = new("Sheets/Complete", "Judas Priest - Screaming For Vengeance.cue");
    public static readonly Sample SpandauBalletTrue = new("CopyingTests", "Spandau Ballet - True.cue");

    public static readonly Sample MultiFile = new("Sheets/Complete", "MultiFile.cue");
    public static readonly Sample NonConsecutive = new("Sheets/Complete", "NonConsecutive.cue");
    public static readonly Sample MinimalFoobarCue = new("Sheets/Partial", "MinimalFoobarCue.cue");
    public static readonly Sample ThreeTracks = new("Sheets/Partial", "ThreeTracks.cue");

    public static readonly Sample Cp1252 = new("EncodingDetection", "1252.cue");

    public static Sample EqualityVariant(string name) => new("Equality", name + ".cue");

    public static IEnumerable<Sample> RoundTripCorpus => Enumerate("Sheets/Complete", SearchOption.TopDirectoryOnly)
        .Concat(Enumerate("Equality", SearchOption.TopDirectoryOnly))
        .Append(SpandauBalletTrue);

    // Sheets that survive clone, save and reload unchanged (the old SavingTests folder).
    public static IEnumerable<Sample> SaveCorpus => [JethroTullAqualung, SpandauBalletTrue,JudasPriestScreamingForVengeance, MultiFile,Base, ThreeTracks, NonConsecutive];

    public static IEnumerable<Sample> Encodings => Enumerate("EncodingDetection", SearchOption.TopDirectoryOnly);

    // Materialized so MSTest names each data row by Sample.ToString().
    public static IEnumerable<object[]> RoundTripRows => RoundTripCorpus.Select(s => new object[] { s });

    public static IEnumerable<object[]> SaveRows => SaveCorpus.Select(s => new object[] { s });

    public static IEnumerable<object[]> EncodingRows => Encodings.Select(s => new object[] { s });

    private static IEnumerable<Sample> Enumerate(string folder, SearchOption option)
    {
        string root = Utils.GetFile();
        return Directory.EnumerateFiles(Utils.GetFile(folder), "*.cue", option)
            .Order(StringComparer.Ordinal)
            .Select(f => new Sample(Path.GetRelativePath(root, Path.GetDirectoryName(f)!).Replace('\\', '/'), Path.GetFileName(f)));
    }
}
