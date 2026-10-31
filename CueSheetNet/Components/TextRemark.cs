namespace CueSheetNet.Components;

public sealed record TextRemark : Remark
{
    public string Text { get; init; }
    internal TextRemark(string text) => Text = text;
}
