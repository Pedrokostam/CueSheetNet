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
    [GeneratedRegex(@"UTF-(?<BIT>\d+)")]
    public static partial Regex UtfBitFinder();

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
    public static IEnumerable<object[]> EncodingFiles =>
        Utils.GetFiles("*.cue", "EncodingDetection").Select(f => new object[] { Path.GetFileName(f) });

    [TestMethod]
    [DynamicData(nameof(EncodingFiles))]
    public void TestEncodingDetection(string fileName)
    {
        string filepath = Utils.GetFile("EncodingDetection", fileName);
        Encoding target = ParseEncodingFromName(filepath);
        var cue = CueReader.CreateFromFile(filepath).Parse();
        Encoding cueEnc = cue.SourceEncoding!;
        bool match = target.GetPreamble().SequenceEqual(cueEnc.GetPreamble())
            && target.EncodingName == cueEnc.EncodingName;
        var a = cueEnc.GetPreamble();
        Assert.IsTrue(match, $"Target: {target.EncodingName}, actual: {cueEnc.EncodingName}, actual preamble: {string.Join(", ", a)}-{Path.GetFileNameWithoutExtension(filepath)}");
    }

    [TestMethod]
    public void TestParsingMinimum()
    {
        string minimalPath = Utils.GetFile("Parsing", "MinimalFoobarCue.cue");
        var cue = CueReader.CreateFromFile(minimalPath).Parse();
        Assert.AreEqual("A",cue.Files[0].Name);
        Assert.AreEqual("MinimalFoobarCue 01",cue.Tracks[0].Title);
        Assert.AreEqual(null,cue.Tracks[0].Duration );
        Assert.AreEqual(new CueTime(99), cue.Tracks[0].Start );
    }
}
