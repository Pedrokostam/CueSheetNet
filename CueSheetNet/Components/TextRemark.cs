namespace CueSheetNet.Components;

public sealed record TextRemark : Remark
{
    public string Text { get; init; }
    public override string Line => Text;

    internal TextRemark(string text) => Text = text;
}
