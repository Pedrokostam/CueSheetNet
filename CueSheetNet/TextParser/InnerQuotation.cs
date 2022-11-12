using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet.TextParser;

public record struct InnerQuotation
{
    public const char DefaultOpening = '“';
    public const char DefaultClosing = '”';
    /// <summary>
    /// Quotation marks consisting of simple symmetrical apostrophes. 'Example'.
    /// </summary>
    public static InnerQuotation Apostrophes => new ('\'', '\'');
    /// <summary>
    /// Quotation marks consisting of two asymmentrical double quotes, both at the top. Commonly used in English. “Example”.
    /// </summary>
    public static InnerQuotation CurvedDoubleTopQuotation => new (DefaultOpening, DefaultClosing);
    /// <summary>
    /// Quotation marks consisting of two asymmentrical double quotes, both comma like, opening one at the bottom , closing one at the top .„Example”.
    /// </summary>
    public static InnerQuotation CommaLikeBottomTopQuotation => new('„', '”');
    /// <summary>
    /// Quotation marks consisting of two asymmentrical double angle marks. Also known as guillemets. «Example».
    /// </summary>
    public static InnerQuotation DoubleAngleQuotation => new ('«', '»');
    /// <summary>
    /// Quotation marks consisting of two asymmentrical double angle marks. «Example»
    /// </summary>
    public static InnerQuotation Guillemets => DoubleAngleQuotation;

    private char openingQuote;
    private char closingQuote;
    private bool symmetrical;

    public char OpeningQuote
    {
        get => openingQuote;
        init
        {
            if (value == '"')
                throw new ArgumentException("Cannot set double quotes as replacement");
            openingQuote = value;
            symmetrical = closingQuote == openingQuote;
        }
    }
    public char ClosingQuote
    {
        get => closingQuote;
        init
        {
            if (value == '"')
                throw new ArgumentException("Cannot set double quotes as replacement");
            closingQuote = value;
            symmetrical = closingQuote == openingQuote;
        }
    }
    /// <summary>
    /// Initialize a new instance of InnerQuotation with default opening and closing
    /// </summary>
    public InnerQuotation()
    {
        symmetrical = false;
        openingQuote = DefaultOpening;
        closingQuote = DefaultClosing;
    }
    /// <summary>
    /// Initialize a new instance of InnerQuotation with specified opening and closing
    /// </summary>
    public InnerQuotation(char openingQuote, char closingQuote) : this()
    {
        OpeningQuote = openingQuote;
        ClosingQuote = closingQuote;
    }
    /// <summary>
    /// Initialize a new instance of InnerQuotation with specified char as both the opening and closing
    /// </summary>
    public InnerQuotation(char symmetricalQuote) : this(symmetricalQuote, symmetricalQuote) { }

    [return: NotNullIfNotNull("input")]
    public string? ReplaceQuotes(string? input)
    {
        if (input == null) return input;
        if (symmetrical)
            return input.Replace('"', OpeningQuote);
        List<int> ints = new();
        int j = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '"')
            {
                ints.Add(i);
            }
        }
        if (ints.Count == 0)
            return input; ;
        Span<char> inter = stackalloc char[input.Length];
        input.AsSpan().CopyTo(inter);
        for (int i = 0; i < ints.Count / 2; i++)
        {
            //take left and right extrema
            int openingIndex = ints[i];//opening index (from left)
            inter[openingIndex] = OpeningQuote;
            int closingIndex = ints[^(i + 1)];//closing index (from right) - +1 because ^0 is outside collection
            inter[closingIndex] = ClosingQuote;
        }
        if (ints.Count % 2 == 1)//if there was an odd number of quotations, the middle one will be an opening
        {
            inter[j / 2 + 1] = OpeningQuote;
        }
        return inter.ToString();
    }
}
