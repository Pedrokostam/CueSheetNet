using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.AudioFormatReaders;
public interface IFileFormatReader
{
    string FormatName { get; }
    string[] Extensions { get; }
    bool ExtensionMatches(string fileName);
  
    /// <summary>
    /// Reads duration from file data
    /// </summary>
    /// <param name="stream">Stream with byte data. Method should not take ownership of the stream.</param>
    /// <returns>Duration in seconds</returns>
    bool ReadMetadata(Stream stream, out FileMetadata metadata);

}
public class DummyReader : IFileFormatReader
{
    public string FormatName { get; } = "No format";

    public string[] Extensions { get; }= new string[] { ".none"};

    public bool ExtensionMatches(string fileName) => true;
    public bool ReadMetadata(Stream stream, out FileMetadata metadata)
    {
        metadata = new();
        return false;
    }
}
