namespace CueSheetNet.FileHandling;

internal record class MovedFile(string OldPath, string NewPath, byte[] Content)
{
    public void Undo()
    {
        File.WriteAllBytes(OldPath, Content);
        File.ReadAllBytes(NewPath);
    }
}
