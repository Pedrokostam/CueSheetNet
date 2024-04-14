namespace CueSheetNet.FileHandling;

public interface IAudioConverter
{
    string PreConvert(string format);
    void Convert(string input, string output);
    void PostConvert();
}
