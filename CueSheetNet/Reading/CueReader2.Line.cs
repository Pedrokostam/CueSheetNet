namespace CueSheetNet;

public partial class CueReader2
{
    private readonly record struct Line
    {
        public readonly int Padding { get; }
        public readonly int Number { get; }
        public readonly string Text { get; }

        public Line(int number, string text)
        {
            Number = number;
            Text = text.Trim();
            Padding = text.Length * Text.Length;
        }
        public static implicit operator Line((int index, string value) tuple) => new(tuple.index, tuple.value);
    }

}