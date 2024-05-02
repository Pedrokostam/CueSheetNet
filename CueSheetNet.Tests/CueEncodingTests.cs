using System.Text;
using System.Text.RegularExpressions;

namespace CueSheetNet.Tests;
[TestClass]
public partial class CueEncodingTests
{
    [TestInitialize]
    public void Init()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    readonly CueReader2 _reader = new();
#if NET7_0_OR_GREATER
    [GeneratedRegex(@"UTF-(?<BIT>\d+)")]
    public static partial Regex UtfBitFinder();
#else
    private static readonly Regex UtfBitFinderImpl = new(@"UTF-(?<BIT>\d+)");
    public static Regex UtfBitFinder() => UtfBitFinderImpl;
#endif
    private static bool IsBigEndian(string input) => input.Contains(" BE");
    private static bool HasBom(string input) => input.Contains(" BOM");
    private static Encoding ParseEncodingFromName(string name)
    {
        name = Path.GetFileNameWithoutExtension(name);
        var utf = UtfBitFinder().Match(name);
        if (utf.Success)
        {
            string bitString = utf.Groups["BIT"].Value;
            Encoding en = bitString switch
            {
                "8" => new UTF8Encoding(HasBom(name), false),
                "16" => new UnicodeEncoding(IsBigEndian(name), HasBom(name), false),
                "32" => new UTF32Encoding(IsBigEndian(name), HasBom(name), false),
                _ => throw new NotImplementedException(bitString),
            };
            return en;

        }
        else
        {
            return Encoding.GetEncoding(int.Parse(name));
        }
    }
    private void TestParsing(string filepath)
    {
        Encoding target = ParseEncodingFromName(filepath);
        var cue = _reader.Read(filepath);
        Encoding cueEnc = cue.SourceEncoding!;
        bool match = target.GetPreamble().SequenceEqual(cueEnc.GetPreamble())
            && target.EncodingName == cueEnc.EncodingName;
        var a = cueEnc.GetPreamble();
        Assert.IsTrue(match, $"Target: {target.EncodingName}, actual: {cueEnc.EncodingName}, actual preamble: {string.Join(", ", a)}-{Path.GetFileNameWithoutExtension(filepath)}");
    }

    [TestMethod]
    public void TestEncodingDetection()
    {
        var files = Utils.GetFiles("*.cue", "EncodingDetection");
        foreach (var file in files)
        {
            TestParsing(file);
        }
    }
    [TestMethod]
    public void TestParsingMinimum()
    {
        string minimalPath = Utils.GetFile("Parsing", "MinimalFoobarCue.cue");
        var cue = _reader.Read(minimalPath);
        Assert.AreEqual(cue.Files[0].SourceFile.Name, "A");
        Assert.AreEqual(cue.Tracks[0].Title, "A");
        Assert.AreEqual(cue.Tracks[0].Indices[0].Time, new CueTime(99));
    }
}
