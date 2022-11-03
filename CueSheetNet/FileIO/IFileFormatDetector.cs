using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.FileIO;
public interface IFileFormatDetector
{
    string FormatName { get; }
    string[] Extensions { get; }
    bool ExtensionMatches(string fileName);
    /// <summary>
    /// Checks if the first bytes match the signature of the format. Before checking sets stream position to Zero, does not reset position after reading.
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    bool FileSignatureMatches(Stream stream);
    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="stream"></param>
    /// <returns>Duration in seconds</returns>
    double ReadDuration(Stream stream);

}
public class DummyReader : IFileFormatDetector
{
    public string FormatName { get; } = "No file";

    public string[] Extensions { get; }= new string[] { ".none"};

    public bool ExtensionMatches(string fileName) => true;
    public bool FileSignatureMatches(Stream stream) => true;
    public double ReadDuration(Stream stream) => -1;
}
