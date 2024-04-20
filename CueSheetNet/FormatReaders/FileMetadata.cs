namespace CueSheetNet.FormatReaders;

/// <summary>
/// Represents
/// </summary>
/// <param name="Duration">Time duration of the file.</param>
/// <param name="Binary">Whether the file contains binary data, instead of audio data.</param>
/// <param name="SampleRate">How many samples per seconds the audio has.</param>
/// <param name="Channels">How many channels the audio has.</param>
/// <param name="BitDepth">How many bit a sample has.</param>
/// <param name="FormatName">The name of the format.</param>
public readonly record struct FileMetadata(
    TimeSpan Duration,
    bool Binary,
    int SampleRate,
    int Channels,
    int BitDepth,
    string FormatName
)
{
    public CueTime CueDuration => CueTime.FromTimeSpan(Duration);
}
