#pragma warning disable MA0008 // Add StructLayoutAttribute

namespace CueSheetNet.Components;

internal class InnerCueFile
{
    internal int Id { get; }
    public FileInfo File { get; set; }

    internal InnerCueFile(string filePath, int sequenceNumber)
    {
        Id = sequenceNumber;
        File = new FileInfo(filePath);
    }

}

#pragma warning restore MA0008 // Add StructLayoutAttribute